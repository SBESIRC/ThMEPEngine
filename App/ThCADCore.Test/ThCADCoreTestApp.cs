﻿using System;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.Overlay.Snap;
using Autodesk.AutoCAD.Colors;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.ApplicationServices;
using NetTopologySuite.Triangulate;

namespace ThCADCore.Test
{
    public class ThCADCoreTestApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }


        [CommandMethod("TIANHUACAD", "ThMBC", CommandFlags.Modal)]
        public void ThMBC()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var pline = acadDatabase.Element<Polyline>(result.ObjectId);
                acadDatabase.ModelSpace.Add(pline.MinimumBoundingCircle());
            }
        }

        [CommandMethod("TIANHUACAD", "ThOBB", CommandFlags.Modal)]
        public void ThOBB()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }
                acadDatabase.ModelSpace.Add(objs.GetMinimumRectangle());
            }
        }

        [CommandMethod("TIANHUACAD", "ThOutline", CommandFlags.Modal)]
        public void ThOutline()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }
                foreach (var obj in objs.Outline())
                {
                    acadDatabase.ModelSpace.Add(obj as Entity);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThDifference", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void ThDifference()
        {
            using (var ov = new ThCADCoreNTSFixedPrecision())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetEntity("\n请选择框线");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Polyline>(obj));
                }

                var frame = result2.ObjectId;
                var frameObj = acadDatabase.Element<Polyline>(frame);
                foreach (Entity obj in frameObj.Difference(objs))
                {
                    obj.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(obj as Entity);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThPolygonizer", CommandFlags.Modal)]
        public void ThPolygonizer()
        {
            using (var ov = new ThCADCoreNTSFixedPrecision())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }
                foreach (var obj in objs.Polygons())
                {
                    acadDatabase.ModelSpace.Add(obj as Entity);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThUnaryUnionOp", CommandFlags.Modal)]
        public void ThUnion()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }
                var geometry = objs.ToNTSNodedLineStrings();

                foreach (Entity obj in geometry.ToDbCollection())
                {
                    obj.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(obj);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThPolygonIntersect", CommandFlags.Modal)]
        public void ThPolygonIntersect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var singleResult = Active.Editor.GetEntity("请选择对象");
                if (singleResult.Status != PromptStatus.OK)
                {
                    return;
                }

                var frame = singleResult.ObjectId;
                var frameObj = acadDatabase.Element<Polyline>(frame);
                
                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }
                foreach (Polygon obj in objs.Polygonize())
                {
                    // 洞
                    //obj.SymmetricDifference(frameObj.ToNTSPolygon());
                    //var ntsObj = obj.Difference(new DBObjectCollection() { frameObj }.UnionGeometries());
                    var ntsObj =  obj.Union(frameObj.ToNTSPolygon());
                    foreach (Entity entity in ntsObj.ToDbCollection())
                    {
                        entity.ColorIndex = 2;
                        acadDatabase.ModelSpace.Add(entity);
                    }
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThSnapDifference", CommandFlags.Modal)]
        public void ThThDifference()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                //var objs = new DBObjectCollection();
                //foreach (var obj in result.Value.GetObjectIds())
                //{
                //    objs.Add(acadDatabase.Element<Entity>(obj));
                //}
                //var geometrys = objs.ToNTSLineStrings();

                //var snapGeometry = SnapIfNeededOverlayOp.Overlay(geometrys.First(), geometrys.Last(), SpatialFunction.Difference);
                //foreach (Entity obj in snapGeometry.ToDbCollection())
                //{
                //    obj.ColorIndex = 1;
                //    acadDatabase.ModelSpace.Add(obj);
                //}

                var polys = new List<Polyline>();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    polys.Add(acadDatabase.Element<Polyline>(obj));
                }

                var snapGeometry = SnapIfNeededOverlayOp.Overlay(polys.First().ToNTSPolygon(), polys.Last().ToNTSPolygon(), SpatialFunction.Union);
                foreach (Entity obj in snapGeometry.ToDbCollection())
                {
                    obj.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(obj);
                }
            }
        }


        [CommandMethod("TIANHUACAD", "ThVoronoiDiagram", CommandFlags.Modal)]
        public void ThVoronoiDiagram()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var pline = acadDatabase.Element<Polyline>(result.ObjectId);
                foreach (Entity diagram in pline.VoronoiTriangulation(pline.Length / 50.0))
                {
                    diagram.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(diagram);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THDT", CommandFlags.Modal)]
        public void ThDelaunayTriangulation()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var points = new Point3dCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    points.Add(acadDatabase.Element<Entity>(obj).GeometricExtents.CenterPoint());
                }
                foreach (Entity diagram in points.DelaunayTriangulation())
                {
                    diagram.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(diagram);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THCDT", CommandFlags.Modal)]
        public void ThConformingDelaunayTriangulation()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result1 = Active.Editor.GetSelection();
                if (result1.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetEntity("请选择对象");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var points = new Point3dCollection();
                foreach (var obj in result1.Value.GetObjectIds())
                {
                    points.Add(acadDatabase.Element<Entity>(obj).GeometricExtents.CenterPoint());
                }
                var pline = acadDatabase.Element<Polyline>(result2.ObjectId);
                foreach (Entity diagram in points.ConformingDelaunayTriangulation(pline))
                {
                    diagram.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(diagram);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THPileGroup", CommandFlags.Modal)]
        public void THPileGroup()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result1 = Active.Editor.GetSelection();
                if (result1.Status != PromptStatus.OK)
                {
                    return;
                }

                var dbLst1 = new List<Curve>();
                foreach (var obj in result1.Value.GetObjectIds())
                {
                    dbLst1.Add(acadDatabase.Element<Curve>(obj));
                }

                var totalIds = new ObjectIdList();
                var ids = DrawProfile(dbLst1, "dbLst");
                totalIds.AddRange(ids);
                var groupId = GroupTools.CreateGroup(acadDatabase.Database, "d", totalIds);

                var dbLst2 = new List<Curve>();
                var result2 = Active.Editor.GetEntity("select");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                dbLst2.Add(acadDatabase.Element<Curve>(result2.ObjectId));

                var ids2 = DrawProfile(dbLst2, "dbLst2");
                var totalIds2 = new ObjectIdList();
                totalIds2.AddRange(ids2);

                var groupId2 = GroupTools.CreateGroup(acadDatabase.Database, "d2", totalIds2);

                var totalIds3 = new ObjectIdList();
                totalIds3.AddRange(totalIds2);
                totalIds3.AddRange(totalIds);

                var groupId3 = GroupTools.CreateGroup(acadDatabase.Database, "totalIds3", totalIds3);

            }
        }

        public static List<ObjectId> DrawProfile(List<Curve> curves, string LayerName, Color color = null)
        {
            var objectIds = new List<ObjectId>();
            if (curves == null || curves.Count == 0)
                return objectIds;

            using (var db = AcadDatabase.Active())
            {
                if (color == null)
                    CreateLayer(LayerName, Color.FromRgb(255, 0, 0));
                else
                    CreateLayer(LayerName, color);

                foreach (var curve in curves)
                {
                    var clone = curve.Clone() as Curve;
                    clone.Layer = LayerName;
                    objectIds.Add(db.ModelSpace.Add(clone));
                }
            }

            return objectIds;
        }

        public static ObjectId CreateLayer(string aimLayer, Color color)
        {
            LayerTableRecord layerRecord = null;
            using (var db = AcadDatabase.Active())
            {
                foreach (var layer in db.Layers)
                {
                    if (layer.Name.Equals(aimLayer))
                    {
                        layerRecord = db.Layers.Element(aimLayer);
                        break;
                    }
                }

                // 创建新的图层
                if (layerRecord == null)
                {
                    layerRecord = db.Layers.Create(aimLayer);
                    layerRecord.Color = color;
                    layerRecord.IsPlottable = false;
                }
                else
                {
                    if (!layerRecord.Color.Equals(color))
                    {
                        layerRecord.UpgradeOpen();
                        layerRecord.Color = color;
                        layerRecord.IsPlottable = false;
                        layerRecord.DowngradeOpen();
                    }
                }
            }
            return layerRecord.ObjectId;
        }

        [CommandMethod("TIANHUACAD", "ThTrim", CommandFlags.Modal)]
        public void ThTrim()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result1 = Active.Editor.GetEntity("\n请选择框线");
                if (result1.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetSelection();
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new List<DBObject>();
                var frame = acadDatabase.Element<Polyline>(result1.ObjectId);
                var clipper = new ThCADCoreNTSFastGeometryClipper(frame.ToNTSPolygon().EnvelopeInternal);
                foreach (var obj in result2.Value.GetObjectIds())
                {
                    var curve = acadDatabase.Element<Curve>(obj);
                    objs.AddRange(clipper.clip(curve.ToNTSGeometry(), true).ToDbObjects());
                }
                foreach (Entity obj in objs)
                {
                    obj.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(obj);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThPlTrim", CommandFlags.Modal)]
        public void ThPlTrim()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetEntity("请选择框线");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var curve = acadDatabase.Element<Polyline>(result.ObjectId);
                var frame = acadDatabase.Element<Polyline>(result2.ObjectId);

                foreach (Entity diagram in curve.ToNTSGeometry().Intersection(frame.ToNTSGeometry()).ToDbCollection())
                {
                    diagram.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(diagram);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThClosePoint", CommandFlags.Modal)]
        public void ThClosePoint()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetEntity("请选择框线");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var poly = acadDatabase.Element<Polyline>(result.ObjectId);
                var circle = acadDatabase.Element<Circle>(result2.ObjectId);
                var closestPt = poly.GetClosestPointTo(circle.Center, true);

                var verticalLine = new Line(closestPt, circle.Center);

                acadDatabase.ModelSpace.Add(verticalLine);
            }
        }

        [CommandMethod("TIANHUACAD", "ThPBuffer", CommandFlags.Modal)]
        public void ThBuffer()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetDistance("\n请输入距离");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                double distanceTolerance = result2.Value;
                var obj = acadDatabase.Element<Polyline>(result.ObjectId);
                foreach (Polyline pl in obj.BufferPL(20))
                {
                    pl.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(pl);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThSingleBuffer", CommandFlags.Modal)]
        public void ThSingleBuffer()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetDistance("\n请输入距离");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                double distanceTolerance = result2.Value;
                var obj = acadDatabase.Element<Entity>(result.ObjectId);
                var dbCol = new DBObjectCollection();
                dbCol.Add(obj);
                foreach (Polyline pl in dbCol.SingleSidedBuffer(distanceTolerance))
                {
                    pl.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(pl);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThOrientation", CommandFlags.Modal)]
        public void ThOrientation()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var pline = acadDatabase.Element<Polyline>(result.ObjectId);
                if (pline.IsCCW())
                {
                    Active.Editor.WriteLine("It's oriented counter-clockwise.");
                }
                else
                {
                    Active.Editor.WriteLine("It's oriented clockwise.");
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThSpatialIndex", CommandFlags.Modal)]
        public void ThSpatialIndex()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetEntity("请选择框线");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var options = new PromptKeywordOptions("\n请指定选择方式")
                {
                    AllowNone = true
                };
                options.Keywords.Add("Window", "Window", "Window(W)");
                options.Keywords.Add("Crossing", "Crossing", "Crossing(C)");
                options.Keywords.Add("Fence", "Fence", "Fence(F)");
                options.Keywords.Default = "Window";
                var result3 = Active.Editor.GetKeywords(options);
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }

                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var frame = acadDatabase.Element<Polyline>(result2.ObjectId);
                if (result3.StringResult == "Window")
                {
                    spatialIndex.SelectWindowPolygon(frame).Cast<Entity>().ForEachDbObject(o => o.Highlight());
                }
                else if (result3.StringResult == "Crossing")
                {
                    spatialIndex.SelectCrossingPolygon(frame).Cast<Entity>().ForEachDbObject(o => o.Highlight());
                }
                else if (result3.StringResult == "Fence")
                {
                    spatialIndex.SelectFence(frame).Cast<Entity>().ForEachDbObject(o => o.Highlight());
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThNeighbour", CommandFlags.Modal)]
        public void ThNeighbour()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetEntity("请选择查询对象");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var result3 = Active.Editor.GetInteger("请输入邻居个数");
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }

                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var host = acadDatabase.Element<Curve>(result2.ObjectId);
                var nearestNeighbours = spatialIndex.NearestNeighbours(host, result3.Value);
                foreach (Entity neighbour in nearestNeighbours)
                {
                    neighbour.UpgradeOpen();
                    neighbour.ColorIndex = 1;
                    neighbour.DowngradeOpen();
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThPolylineTessellate", CommandFlags.Modal)]
        public void ThPolylineTessellate()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }
                foreach (var item in objs)
                {
                    if (item is Polyline poly)
                    {
                        var polyline_Chord = poly.TessellatePolylineWithChord(100);
                        polyline_Chord.ColorIndex = 2;
                        acadDatabase.ModelSpace.Add(polyline_Chord);
                        var polyline_Arc = poly.TessellatePolylineWithArc(100);
                        polyline_Arc.ColorIndex = 1;
                        acadDatabase.ModelSpace.Add(polyline_Arc);
                    }
                    else if (item is Arc arc)
                    {
                        var arc_Chord = arc.TessellateArcWithChord(100);
                        arc_Chord.ColorIndex = 2;
                        acadDatabase.ModelSpace.Add(arc_Chord);
                        var arc_Arc = arc.TessellateArcWithArc(100);
                        arc_Arc.ColorIndex = 1;
                        acadDatabase.ModelSpace.Add(arc_Arc);
                    }
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThTestGetLine", CommandFlags.Modal)]
        public void ThTestGetLine()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();//获取所有的选中项

                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }
                foreach (var item in objs)
                {
                    if(item is Line line1)
                    {
                        line1.UpgradeOpen();
                        line1.ColorIndex = 4;
                        Point3d ptend = new Point3d(100, 0, 0);
                        ObjectId id2 = line1.ObjectId.Copy(Point3d.Origin, ptend);
                        id2.Rotate(ptend, Math.PI / 2);
                        Line line2 = acadDatabase.Element<Line>(id2);
                        //acadDatabase.ModelSpace.Add(line2);//在上面的ObjectId.Copy方法里已经执行了Add方法，所以不用重新Add，会报错
                    }
                    if (item is Polyline poly)
                    {
                        if (poly.IsCCW())
                        {
                            Active.Editor.WriteLine("It's oriented counter-clockwise.");
                        }
                        else
                        {
                            Active.Editor.WriteLine("It's oriented clockwise.");
                        }
                        //var polyline_Chord = poly.TessellatePolylineWithChord(100);
                        //polyline_Chord.ColorIndex = 2;
                        //acadDatabase.ModelSpace.Add(polyline_Chord);
                        //var polyline_Arc = poly.TessellatePolylineWithArc(100);
                        //polyline_Arc.ColorIndex = 1;
                        //acadDatabase.ModelSpace.Add(polyline_Arc);
                    }
                    else if (item is Arc arc)
                    {
                        //var arc_Chord = arc.TessellateArcWithChord(100);
                        //arc_Chord.ColorIndex = 2;
                        //acadDatabase.ModelSpace.Add(arc_Chord);
                        //var arc_Arc = arc.TessellateArcWithArc(100);
                        //arc_Arc.ColorIndex = 1;
                        //acadDatabase.ModelSpace.Add(arc_Arc);
                    }
                }

                //新建一个线并放到CAD中
                {
                    Point3d test = Point3d.Origin;
                    Point3d startPoint = new Point3d(0, 1000, 0);
                    Point3d endPoint = new Point3d(100, 1000, 0);
                    Line line = new Line(startPoint, endPoint);

                    Point3d startPoint1 = new Point3d(1000, 0, 0);
                    Point3d endPoint1 = new Point3d(100, 1000, 0);
                    Line line1 = new Line(startPoint1, endPoint1);

                    acadDatabase.ModelSpace.Add(line);
                    acadDatabase.ModelSpace.Add(line1);
                }


                PromptEntityOptions option = new PromptEntityOptions("\n请选择一个多段线");
                option.SetRejectMessage("你选择的类型不对");
                option.AddAllowedClass(typeof(Polyline), true);
                PromptEntityResult res = Active.Editor.GetEntity(option);
                if (res.Status != PromptStatus.OK)
                {
                    return;
                }
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("选择的对象" + res.ObjectId);
            }
        }

        [CommandMethod("TIANHUACAD", "ThTestFireCompartmentPolygons", CommandFlags.Modal)]
        public void ThTestPolygons()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                Active.Editor.WriteLine("\n请选择");
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs1 = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    Polyline polyline = acadDatabase.Element<Polyline>(obj);
                    objs1.Add(polyline);
                }
                ThFireCompartmentCleanService cleanService = new ThFireCompartmentCleanService();
                var cleanobjs= cleanService.Clean(objs1);
                bool test = false ;
                if (test)
                {
                    foreach (Entity item in cleanobjs)
                    {
                        acadDatabase.ModelSpace.Add(item);
                    }
                }
                else
                { 
                    var objs = new List<DBObject>();
                    foreach (Polygon polygon in cleanobjs.Polygonize())
                    {
                        objs.Add(polygon.ToDbEntity());
                    }

                    foreach (var obj in objs)
                    {
                        var entity = obj as Entity;
                        entity.ColorIndex = 2;
                        if (entity is Polyline poly && poly.Area < 5E+6)
                            continue;
                        if (entity is MPolygon mpoly && mpoly.Area < 5E+6)
                            continue;
                        acadDatabase.ModelSpace.Add(entity);
                    }
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THCH", CommandFlags.Modal)]
        public void DRAW_A_CONVEXHULL()
        {
            var points = Algorithms.GetConvexHull(GetPoints());

            if (points.Count <= 1)
            {
                return;
            }
            for (int i = 0; i < points.Count; ++i)
            {
                int pre = i == 0 ? points.Count - 1 : i - 1;
                draw_line(points[pre], points[i]);
            }
        }

        public static List<Point3d> GetPoints()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

            var points = new List<Point3d>();

            PromptSelectionResult psr = ed.GetSelection();
            SelectionSet ss = psr.Value;
            using (Transaction trans = doc.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ss.GetObjectIds())
                {
                    DBPoint ent = (DBPoint)trans.GetObject(id, OpenMode.ForRead);
                    if (true)
                    {
                        points.Add(ent.Position);
                    }
                }
                trans.Commit();
            }
            return points;
        }

        public void draw_line(Point3d point1, Point3d point2, int color = 1)
        {
            Line line = new Line(point1, point2);
            line.ColorIndex = color;
            line.AddToCurrentSpace();
        }

        //维诺图
        [CommandMethod("TIANHUACAD", "THVD", CommandFlags.Modal)]
        public void ThVD()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var points = new Point3dCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    points.Add(acadDatabase.Element<Entity>(obj).GeometricExtents.CenterPoint());
                }
                var voronoiDiagram = new VoronoiDiagramBuilder();
                voronoiDiagram.SetSites(points.ToNTSGeometry());
                //foreach (Polygon polygon in voronoiDiagram.GetDiagram(ThCADCoreNTSService.Instance.GeometryFactory).Geometries) //同等效力
                foreach (Polygon polygon in voronoiDiagram.GetSubdivision().GetVoronoiCellPolygons(ThCADCoreNTSService.Instance.GeometryFactory))
                {
                    HostApplicationServices.WorkingDatabase.AddToModelSpace(polygon.ToDbEntity());
                }
            }
        }
    }
}
