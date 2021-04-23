using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
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
    public class ThExtractSpaceRecognitionEngine : ThSpatialElementRecognitionEngine
    {
        private List<Entity> SpaceNames { get; set; }
        private List<Entity> SpaceBoundaries { get; set; }
        private Dictionary<Entity, List<Entity>> TextContainer { get; set; }
        private Dictionary<Entity, List<Entity>> AreaContainer { get; set; }
        private Dictionary<Entity, ThTempSpace> SpaceIndex { get; set; } //用几何对象快速查找ThIfcSpace对象

        public List<ThTempSpace> TempSpaces { get; set; } //临时使用，后续

        public string SpaceLayer { get; set; }
        public string NameLayer { get; set; }
        
        public ThExtractSpaceRecognitionEngine()
        {
            SpaceNames = new List<Entity>();
            TempSpaces = new List<ThTempSpace>();
            SpaceBoundaries = new List<Entity>();
            TextContainer = new Dictionary<Entity, List<Entity>>();
            AreaContainer = new Dictionary<Entity, List<Entity>>();
            SpaceIndex = new Dictionary<Entity, ThTempSpace>();
        }
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                //Load Data
                SpaceNames = RecognizeSpaceNameText(database, polygon);
                SpaceBoundaries = RecognizeSpaceBoundary(database, polygon);
                SpaceBoundaries = BuildAreas(SpaceBoundaries);                
                BuildTextContainers();
                BuildAreaContainers();
                CreateSpaceBoundaries();
                SpaceMatchText();
            }
        }
        private List<Entity> BuildAreas(List<Entity> spaces)
        {
            if(spaces.Count==0)
            {
                return new List<Entity>();
            }
            else
            {
                var objs = spaces.ToCollection().BuildArea();
                return objs.Cast<Entity>().ToList();
            }
        }

        private void BuildTextContainers()
        {
            this.TextContainer = new Dictionary<Entity, List<Entity>>();
            SpaceNames.ForEach(m =>
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
                    var containers = SelectTextIntersectPolygon(SpaceBoundaries, textBoundary);
                    //过滤包含文字中心点的轮廓
                    containers = containers.Where(n => n.IsContains(textCenterPt)).ToList();
                    this.TextContainer.Add(m, containers);
                }
            });
        }
        private void BuildAreaContainers()
        {
            SpaceBoundaries.ForEach(m =>
            {
                if(m is Polyline polyline)
                {
                    var containers = SelectPolylineContainers(SpaceBoundaries, polyline);
                    containers.Remove(polyline);
                    this.AreaContainer.Add(m, containers.Cast<Curve>().OrderBy(o => o.Area).Cast<Entity>().ToList());
                }
                else if(m is Circle circle)//新加的
                {
                    Polyline polyline1 = circle.Tessellate(50);
                    var containers = SelectPolylineContainers(SpaceBoundaries, polyline1);
                    containers.Remove(m);
                    this.AreaContainer.Add(m, containers.Cast<Curve>().OrderBy(o => o.Area).Cast<Entity>().ToList());
                }
                else
                {
                    this.AreaContainer.Add(m, new List<Entity>());
                }
            });
        }        
        private List<Entity> RecognizeSpaceNameText(Database database,Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var objs = new DBObjectCollection();
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is DBText dbText)
                    {
                        if (IsSpaceNameLayer(dbText.Layer))
                        {
                            objs.Add(dbText);
                        }
                    }
                    else if (ent is MText mtext)
                    {
                        if (IsSpaceNameLayer(mtext.Layer))
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
        private List<Entity> RecognizeSpaceBoundary(Database HostDb, Point3dCollection polygon)
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
                        if (IsSpaceLayer(polyline.Layer))
                        {
                            var newPolyline = polyline.Clone() as Polyline;
                            objs.Add(newPolyline.TessellatePolylineWithArc(50.0));
                        }
                    }
                }
                var spaces = new List<Entity>();
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
                        spaces.Add(filterObj as Curve);
                    }
                }
                else
                {
                    spaces = objs.Cast<Entity>().ToList();
                }
                return spaces;
            }
        }
        private bool IsSpaceLayer(string layerName)
        {
            return layerName.ToUpper() == SpaceLayer;
        }
        private bool IsSpaceNameLayer(string layerName)
        {
            return layerName.ToUpper() == NameLayer;
        }
        private void CreateSpaceBoundaries()
        {
            SpaceBoundaries.ForEach(o => TempSpaces.Add(new ThTempSpace { Outline = o}));
            TempSpaces.ForEach(o => SpaceIndex.Add(o.Outline, o));
        }
        private void SpaceMatchText()
        {
            DBObjectCollection dbObjs = new DBObjectCollection();           
            Dictionary<Entity, List<string>> dict = new Dictionary<Entity, List<string>>();
            SpaceNames.ForEach(o =>
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

                    var entAreaDic = new Dictionary<Entity, double>();
                    foreach(var ent in curves)
                    {
                        if (ent is Curve curve)
                        {
                            entAreaDic.Add(ent,curve.Area);
                        }
                        else if(ent is MPolygon mPolygon)
                        {
                            entAreaDic.Add(ent, mPolygon.Area);
                        }
                    }
                    var belonged = entAreaDic.OrderBy(k => k.Value).First().Key;
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
            TempSpaces.ForEach(o =>
            {
                if (dict.ContainsKey(o.Outline))
                {
                    o.Tags = dict[o.Outline];
                }
            });
        }
        /// <summary>
        /// Crossing selection
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<Entity> SelectTextIntersectPolygon(List<Entity> boundaries , Polyline textOBB)
        {
            return boundaries.Where(o =>
            {
                if (o is Polyline polyline)
                {
                    ThCADCoreNTSRelate relation = new ThCADCoreNTSRelate(polyline, textOBB);
                    return relation.IsOverlaps || relation.IsCovers;
                }
                else if (o is MPolygon mPolygon)
                {
                    ThCADCoreNTSRelate relation = new ThCADCoreNTSRelate(mPolygon.ToNTSPolygon(), textOBB.ToNTSPolygon());
                    return relation.IsOverlaps || relation.IsCovers;
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
        private List<Entity> SelectPolylineContainers(List<Entity> boundaries, Polyline son)
        {
            var bufferObjs = son.Buffer(-5.0);
            return boundaries.Where(o =>
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
    public class ThTempSpace:ThIfcSpace
    {
        public Entity Outline { get; set; }
    }
}
