﻿using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractRoomRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        private List<Entity> RoomNames { get; set; }
        private List<Curve> RoomBoundaries { get; set; }
        private Dictionary<Entity, List<Curve>> TextContainer { get; set; }
        private Dictionary<Curve, List<Curve>> AreaContainer { get; set; }
        private Dictionary<Curve, ThIfcRoom> RoomIndex { get; set; } //用几何对象快速查找ThIfcRoom对象
        private ThCADCoreNTSSpatialIndex RoomSpatialIndex;

        public string RoomLayer { get; set; }
        public string NameLayer { get; set; }
        
        public ThExtractRoomRecognitionEngine()
        {
            Rooms = new List<ThIfcRoom>();
            RoomNames = new List<Entity>();
            RoomBoundaries = new List<Curve>();
            TextContainer = new Dictionary<Entity, List<Curve>>();
            AreaContainer = new Dictionary<Curve, List<Curve>>();
            RoomIndex = new Dictionary<Curve, ThIfcRoom>();
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                //Load Data
                RoomNames = RecognizeRoomNameText(database, polygon);
                RoomBoundaries = RecognizeRoomBoundary(database, polygon);
                BuildTextContainers();
                BuildAreaContainers();
                CreateRoomBoundaries();
                RooomMatchText();
            }
        }
        private void BuildRoomSpatialIndex()
        {
            DBObjectCollection dbObjs = new DBObjectCollection();
            RoomBoundaries.ForEach(o => dbObjs.Add(o));
            RoomSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
        }
        public void Print(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                RoomNames.ForEach(o => acadDatabase.ModelSpace.Add(o));
                RoomBoundaries.ForEach(o => acadDatabase.ModelSpace.Add(o));
                Rooms.ForEach(m=>
                {
                    Point3d cenpt = ThGeometryTool.GetMidPt(
                        m.Boundary.GeometricExtents.MinPoint,
                        m.Boundary.GeometricExtents.MaxPoint);
                    var totalIds = new ObjectIdList();
                    totalIds.Add(m.Boundary.ObjectId);
                    m.Tags.ForEach(n=>totalIds.Add(acadDatabase.ModelSpace.Add(CreateText(cenpt,n))));
                    var groupName = Guid.NewGuid().ToString();
                    GroupTools.CreateGroup(database, groupName, totalIds);
                });
            }  
        }
        private DBText CreateText(Point3d pt,string text,double height=200.0)
        {
            DBText dbText = new DBText();
            dbText.SetDatabaseDefaults(Active.Database);
            dbText.TextString = text;
            dbText.Position = pt;
            dbText.Height = height;
            return dbText;
        }
        private void BuildTextContainers()
        {
            this.TextContainer = new Dictionary<Entity, List<Curve>>();
            RoomNames.ForEach(m =>
            {
                var textBoundary = new Polyline();
                if (m is DBText dbText)
                {
                    textBoundary = ThGeometryTool.TextOBB(dbText);
                }
                else if(m is MText mText)
                {
                    textBoundary = ThGeometryTool.TextOBB(mText);
                }
                if(textBoundary.Area>0.0)
                {
                    Point3d textCenterPt = ThGeometryTool.GetMidPt(
                    textBoundary.GetPoint3dAt(0), textBoundary.GetPoint3dAt(2));
                    //获取包含此文字的轮廓
                    var containers = SelectTextIntersectPolygon(RoomBoundaries, textBoundary);
                    //过滤包含文字中心点的轮廓
                    containers = containers.Where(n => n is Polyline polyline && polyline.Contains(textCenterPt)).ToList();
                    this.TextContainer.Add(m, containers);
                }
            });
        }
        private void BuildAreaContainers()
        {
            this.AreaContainer = new Dictionary<Curve, List<Curve>>();
            RoomBoundaries.ForEach(m =>
            {
                if(m is Polyline polyline)
                {
                    var containers = SelectPolylineContainers(RoomBoundaries, polyline);
                    containers.Remove(m);
                    this.AreaContainer.Add(m, containers.OrderBy(o => o.Area).ToList());
                }
                else if(m is Circle circle)//新加的
                {
                    Polyline polyline1 = circle.Tessellate(50);
                    var containers = SelectPolylineContainers(RoomBoundaries, polyline1);
                    containers.Remove(m);
                    this.AreaContainer.Add(m, containers.OrderBy(o => o.Area).ToList());
                }
                else
                {
                    this.AreaContainer.Add(m, new List<Curve>());
                }
            });
        }        
        private List<Entity> RecognizeRoomNameText(Database database,Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var objs = new DBObjectCollection();
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is DBText dbText)
                    {
                        if (IsRoomNameLayer(dbText.Layer))
                        {
                            objs.Add(dbText);
                        }
                    }
                    else if (ent is MText mtext)
                    {
                        if (IsRoomNameLayer(mtext.Layer))
                        {
                            objs.Add(mtext);
                        }
                    }
                }
                var texts = new List<Entity>();
                if (polygon.Count > 0)
                {                   
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                    var pline = new Polyline()
                    {
                        Closed = true,
                    };
                    pline.CreatePolyline(polygon);
                    foreach (var filterObj in spatialIndex.SelectCrossingPolygon(pline))
                    {
                        texts.Add(filterObj as Entity);
                    }
                }
                else
                {
                    texts = objs.Cast<Entity>().ToList();
                }
                return texts;
            }
        }
        private List<Curve> RecognizeRoomBoundary(Database HostDb, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                //Boundary可能有闭合的Polyline,Circle,Ellipse...
                //目前仅支持闭合的Polyline
                var objs = new DBObjectCollection();
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is Polyline polyline)
                    {
                        if (IsRoomLayer(polyline.Layer))
                        {
                            var newPolyline = polyline.Clone() as Polyline;
                            objs.Add(newPolyline.TessellatePolylineWithArc(50.0));
                        }
                    }
                }
                var rooms = new List<Curve>();
                if (polygon.Count > 0)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                    var pline = new Polyline()
                    {
                        Closed = true,
                    };
                    pline.CreatePolyline(polygon);
                    foreach (var filterObj in spatialIndex.SelectCrossingPolygon(pline))
                    {
                        rooms.Add(filterObj as Curve);
                    }
                }
                else
                {
                    rooms = objs.Cast<Curve>().ToList();
                }
                return rooms;
            }
        }
        private bool IsRoomLayer(string layerName)
        {
            return layerName.ToUpper() == RoomLayer;
        }
        private bool IsRoomNameLayer(string layerName)
        {
            return layerName.ToUpper() == NameLayer;
        }
        private void CreateRoomBoundaries()
        {
            RoomBoundaries.ForEach(o => Rooms.Add(new ThIfcRoom { Boundary = o}));
            Rooms.ForEach(o => RoomIndex.Add(o.Boundary, o));
        }
        private void RooomMatchText()
        {
            DBObjectCollection dbObjs = new DBObjectCollection();           
            Dictionary<Curve, List<string>> dict = new Dictionary<Curve, List<string>>();
            RoomNames.ForEach(o =>
            {
                var curves = TextContainer[o];
                if (curves.Count > 0)
                {
                    string textString = "";
                    if(o is DBText dbText)
                    {
                        textString = dbText.TextString;
                    }
                    else if(o is MText mText)
                    {
                        textString = mText.Text;
                    }
                    var belonged = curves.Cast<Polyline>().OrderBy(k => k.Area).First();
                    if(!dict.ContainsKey(belonged))
                    {
                        dict.Add(belonged, new List<string> { textString });
                    }
                    else
                    {
                        if(dict[belonged].IndexOf(textString) <0)
                        {
                            dict[belonged].Add(textString);
                        }
                    }
                }
            });
            Rooms.ForEach(o =>
            {
                if (dict.ContainsKey(o.Boundary))
                {
                    o.Tags = dict[o.Boundary];
                }
            });
        }
        /// <summary>
        /// Crossing selection
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<Curve> SelectTextIntersectPolygon(List<Curve> curves , Polyline textOBB)
        {
            return curves.Where(o =>
            {
                if (o is Polyline polyline)
                {
                    using (var ov = new ThCADCoreNTSFixedPrecision())
                    {
                        ThCADCoreNTSRelate relation = new ThCADCoreNTSRelate(polyline, textOBB);
                        return relation.IsOverlaps || relation.IsCovers;
                    }
                }
                return false;
            }).ToList();
        }
        /// <summary>
        /// 获取完全包括area的容器
        /// </summary>
        /// <param name="curves"></param>
        /// <param name="area"></param>
        /// <returns></returns>
        private List<Curve> SelectPolylineContainers(List<Curve> curves, Polyline son)
        {
            var bufferObjs = son.Buffer(-5.0);
            return curves.Where(o =>
            {
                if (o is Polyline parent)
                {
                    return bufferObjs.Cast<Curve>().Where(m =>
                    {
                        if (m is Polyline polyline)
                        {
                            ThCADCoreNTSRelate relation = new ThCADCoreNTSRelate(parent, polyline);
                            return relation.IsCovers;
                        }
                        return false;
                    }).Any();
                }
                return false;
            }).ToList();
        }

        public override void RecognizeMS(Database database, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }

        public override void Recognize(List<ThRawIfcSpatialElementData> datas, Point3dCollection polygon)
        {
            throw new NotImplementedException();
        }
    }
}
