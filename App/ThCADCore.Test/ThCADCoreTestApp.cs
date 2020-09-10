﻿using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using StraightSkeletonNet;
using Vector2d = StraightSkeletonNet.Primitives.Vector2d;

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

        [CommandMethod("TIANHUACAD", "ThMBB", CommandFlags.Modal)]
        public void ThMBB()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var pline = acadDatabase.Element<Polyline>(result.ObjectId);
                acadDatabase.ModelSpace.Add(pline.MinimumBoundingBox());
            }
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

        [CommandMethod("TIANHUACAD", "ThConvexHull", CommandFlags.Modal)]
        public void ThConvexHull()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var pline = acadDatabase.Element<Polyline>(result.ObjectId);
                acadDatabase.ModelSpace.Add(pline.ConvexHull());
            }
        }

        [CommandMethod("TIANHUACAD", "ThEnvelope", CommandFlags.Modal)]
        public void ThEnvelope()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var pline = acadDatabase.Element<Polyline>(result.ObjectId);
                acadDatabase.ModelSpace.Add(pline.GetOctagonalEnvelope());
            }
        }

        [CommandMethod("TIANHUACAD", "ThCBoundary", CommandFlags.Modal)]
        public void ThCompositeBoundary()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var objs = new DBObjectCollection();
                foreach(var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }
                foreach(var obj in objs.Boundaries())
                {
                    acadDatabase.ModelSpace.Add(obj as Entity);
                }
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

        [CommandMethod("TIANHUACAD", "ThMerge", CommandFlags.Modal)]
        public void ThMerge()
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
                foreach (var obj in objs.Merge())
                {
                    acadDatabase.ModelSpace.Add(obj as Entity);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThBuffer", CommandFlags.Modal)]
        public void ThBuffer()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetDistance("请输入距离");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var pline = acadDatabase.Element<Polyline>(result.ObjectId);
                foreach (Entity obj in pline.Buffer(-result2.Value))
                {
                    obj.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(obj);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThPolygonizer", CommandFlags.Modal)]
        public void ThPolygonizer()
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
                foreach (var obj in objs.Polygons())
                {
                    acadDatabase.ModelSpace.Add(obj as Entity);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThPreprocess", CommandFlags.Modal)]
        public void ThPreprocess()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var pline = acadDatabase.Element<Polyline>(result.ObjectId);
                foreach (Polyline segment in pline.Preprocess())
                {
                    segment.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(segment);
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
                foreach (Entity diagram in pline.VoronoiTriangulation(pline.Length/50.0))
                {
                    diagram.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(diagram);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThCenterline", CommandFlags.Modal)]
        public void ThCenterline()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var result2 = Active.Editor.GetDistance("请输入差值距离");
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var pline = acadDatabase.Element<Polyline>(result.ObjectId);
                var centerlines = ThCADCoreNTSCenterlineBuilder.Centerline(pline, result2.Value);
                foreach(Entity centerline in centerlines)
                {
                    centerline.ColorIndex = 2;
                    acadDatabase.ModelSpace.Add(centerline);
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

        [CommandMethod("TIANHUACAD", "ThTrim", CommandFlags.Modal)]
        public void ThTrim()
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
                foreach (Entity diagram in frame.Trim(curve))
                {
                    diagram.ColorIndex = 1;
                    acadDatabase.ModelSpace.Add(diagram);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThSimplify", CommandFlags.Modal)]
        public void ThSimplify()
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

                var options = new PromptKeywordOptions("\n请指定简化方式")
                {
                    AllowNone = true
                };
                options.Keywords.Add("DP", "DP", "DP(D)");
                options.Keywords.Add("VW", "VW", "VW(V)");
                options.Keywords.Add("TP", "TP", "TP(T)");
                options.Keywords.Default = "DP";
                var result3 = Active.Editor.GetKeywords(options);
                if (result3.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline pline = null;
                double distanceTolerance = result2.Value;
                var obj = acadDatabase.Element<Polyline>(result.ObjectId);
                if (result3.StringResult == "DP")
                {
                    pline = obj.DPSimplify(distanceTolerance);
                }
                else if (result3.StringResult == "VW")
                {
                    pline = obj.VWSimplify(distanceTolerance);
                }
                else if (result3.StringResult == "TP")
                {
                    pline = obj.TPSimplify(distanceTolerance);
                }
                pline.ColorIndex = 1;
                acadDatabase.ModelSpace.Add(pline);
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

                var objs = new DBObjectCollection();
                foreach (var obj in result.Value.GetObjectIds())
                {
                    objs.Add(acadDatabase.Element<Entity>(obj));
                }
       
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var frame = acadDatabase.Element<Polyline>(result2.ObjectId);
                foreach (Entity item in spatialIndex.SelectWindowPolygon(frame))
                {
                    item.Highlight();
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
                foreach(Entity neighbour in nearestNeighbours)
                {
                    neighbour.UpgradeOpen();
                    neighbour.ColorIndex = 1;
                    neighbour.DowngradeOpen();
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThLoops", CommandFlags.Modal)]
        public void ThLoops()
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
                    if(acadDatabase.Element<Entity>(obj) is Curve curve)
                    {
                        objs.Add(curve);
                    }
                    else if(acadDatabase.Element<Entity>(obj) is BlockReference br)
                    {
                        DBObjectCollection explodeObjs = new DBObjectCollection();
                        TianHua.AutoCAD.Utility.ExtensionTools.ThBlockReferenceExtensions.Burst(br, explodeObjs);
                        foreach(DBObject dbObj in explodeObjs)
                        {
                            if(dbObj is Curve curveEnt)
                            {
                                objs.Add(curveEnt);
                            }
                        }
                    }
                }
                foreach (var obj in objs.FindLoops())
                {
                    acadDatabase.ModelSpace.Add(obj as Entity);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "ThSkeleton", CommandFlags.Modal)]
        public void ThSkeletonStraight()
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
                    if (acadDatabase.Element<Entity>(obj) is Polyline poly)
                    {
                        var points = new List<Vector2d>();
                        for (int i = 0; i < poly.NumberOfVertices; i++)
                        {
                            var point2d = poly.GetPoint2dAt(i);
                            points.Add(new Vector2d(point2d.X, point2d.Y));
                        }

                        var polygonRes = SkeletonBuilder.Build(points);
                        if (polygonRes != null)
                        {
                            var lines = new List<Line>();
                            foreach (var edge in polygonRes.Edges)
                            {
                                var edgeS = edge.Edge.Begin;
                                var edgeE = edge.Edge.End;
                                var innerPoly = new Polyline();
                                for (int i = 0; i < edge.Polygon.Count; i++)
                                {
                                    var polygonPt = edge.Polygon[i];
                                    var centerPt = new Point3d(polygonPt.X, polygonPt.Y, 0);
                                    innerPoly.AddVertexAt(i, centerPt.ToPoint2D(), 0, 0, 0);
                                    var circle = new Circle(centerPt, new Vector3d(0, 0, 1), 0.5);
                                    acadDatabase.ModelSpace.Add(circle);
                                }

                                innerPoly.Closed = true;
                                acadDatabase.ModelSpace.Add(innerPoly);
                                var ptS = new Point3d(edgeS.X, edgeS.Y, 0);
                                var ptE = new Point3d(edgeE.X, edgeE.Y, 0);
                                var line = new Line(ptS, ptE);
                                lines.Add(line);
                            }
                            foreach (var drawLine in lines)
                            {
                                acadDatabase.ModelSpace.Add(drawLine);
                            }
                        }
                    }
                }

            }
        }
    }
}
