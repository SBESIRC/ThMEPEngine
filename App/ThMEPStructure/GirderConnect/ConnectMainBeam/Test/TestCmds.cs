using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.Overlay.Snap;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.ApplicationServices;
using NetTopologySuite.Triangulate;
using NetTopologySuite.LinearReferencing;
using AcHelper.Commands;
using ThMEPStructure.GirderConnect.ConnectMainBeam.Utils;
using ThMEPStructure.GirderConnect.ConnectMainBeam.ConnectProcess;
using NFox.Cad;
using ThMEPStructure.GirderConnect.Data;
using ThMEPStructure.GirderConnect.Command;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore;
using ThMEPStructure.GirderConnect.Data.Utils;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Test
{
    class TestCmds
    {
        [CommandMethod("TIANHUACAD", "THCH", CommandFlags.Modal)]
        public void THCH()
        {
            var points = Algorithms.GetConvexHull(GetObject.GetPoints());
            if (points.Count <= 1)
            {
                return;
            }
            for (int i = 0; i < points.Count; ++i)
            {
                int pre = i == 0 ? points.Count - 1 : i - 1;
                ShowInfo.DrawLine(points[pre], points[i]);
            }
        }

        [CommandMethod("TIANHUACAD", "CLSP", CommandFlags.Modal)]
        public void CLSP()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                MPolygon mPolygon = GetObject.GetMpolygon(acdb);
                List<Line> lines = CenterLine.CLSimplify(mPolygon, 20);
            }
        }

        [CommandMethod("TIANHUACAD", "THVD", CommandFlags.Modal)]
        public void THVD()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var points = GetObject.GetCenters(acdb);
                var voronoiDiagram = new VoronoiDiagramBuilder();
                voronoiDiagram.SetSites(points.ToNTSGeometry());
                //foreach (Polygon polygon in voronoiDiagram.GetDiagram(ThCADCoreNTSService.Instance.GeometryFactory).Geometries) //同等效力
                foreach (Polygon polygon in voronoiDiagram.GetSubdivision().GetVoronoiCellPolygons(ThCADCoreNTSService.Instance.GeometryFactory))
                {
                    HostApplicationServices.WorkingDatabase.AddToModelSpace(polygon.ToDbEntity());
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THDT", CommandFlags.Modal)] //此为copy
        public void ThDelaunayTriangulation()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var points = GetObject.GetCenters(acdb);
                foreach (Entity diagram in points.DelaunayTriangulation())
                {
                    diagram.ColorIndex = 1;
                    acdb.ModelSpace.Add(diagram);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THVDC", CommandFlags.Modal)]
        public void THVDC()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var points = GetObject.GetCenters(acdb);
                HashSet<Tuple<Point3d, Point3d>> tuples = StructureBuilder.VoronoiDiagramConnect(points);
                foreach (var tuple in tuples)
                {
                    ShowInfo.DrawLine(tuple.Item1, tuple.Item2, 130);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THDTC", CommandFlags.Modal)]
        public void THDTC()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var points = GetObject.GetCenters(acdb);
                HashSet<Tuple<Point3d, Point3d>> tuples = StructureBuilder.DelaunayTriangulationConnect(points);
                foreach (var tuple in tuples)
                {
                    ShowInfo.DrawLine(tuple.Item1, tuple.Item2, 130);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THCDT", CommandFlags.Modal)] //抄来测试用
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

        [CommandMethod("TIANHUACAD", "THCDTC", CommandFlags.Modal)]
        public void THCDTC()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var points = GetObject.GetCenters(acdb);
                var polylines = GetObject.GetMultiLineString(acdb);
                StructureBuilder.ConformingDelaunayTriangulationConnect(points, polylines);
            }
        }

        [CommandMethod("TIANHUACAD", "THCutBranchLoop", CommandFlags.Modal)]
        public void THCutBranchLoop()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var mPolygon = GetObject.GetMpolygon(acdb);
                var loopTime = Active.Editor.GetInteger("\n请输入剪枝次数");
                if (loopTime.Status != PromptStatus.OK)
                {
                    return;
                }
                CenterLine.CutBrancheLoop(mPolygon, loopTime.Value, 50);
            }
        }

        [CommandMethod("TIANHUACAD", "THWallPoint", CommandFlags.Modal)]
        public void THExtractWallPoints()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var mPolygon = GetObject.GetMpolygon(acdb);
                CenterLine.WallEdgePoint(mPolygon, 100);
            }
        }

        [CommandMethod("TIANHUACAD", "THOutPoints", CommandFlags.Modal)]
        public void THPointClassify()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                Polyline polyline = GetObject.GetPolyline(acdb);
                if (polyline == null)
                {
                    return;
                }
                //Dictionary<Point3d, int> PointClass = new Dictionary<Point3d, int>(); //test
                //PointsDealer.PointClassify(polyline, PointClass); //test basic isRight
                List<Point3d> outPoints = PointsDealer.OutPoints(polyline);
                ShowInfo.ShowPoints(outPoints, 'O', 130);
            }
        }

        [CommandMethod("TIANHUACAD", "THSplit", CommandFlags.Modal)]
        public void THSplit()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                Polyline polyline = GetObject.GetPolyline(acdb);
                if (polyline == null)
                {
                    return;
                }
                List<List<Tuple<Point3d, Point3d>>> polylines = new List<List<Tuple<Point3d, Point3d>>>();
                StructureDealer.SplitPolyline(LineDealer.Polyline2Tuples(polyline), polylines);
                foreach (var lines in polylines)
                {
                    polyline = LineDealer.Tuples2Polyline(lines);
                    polyline.ColorIndex = 210;
                    acdb.ModelSpace.Add(polyline);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THMerge", CommandFlags.Modal)]
        public void THMerge()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                Polyline polylineA = GetObject.GetPolyline(acdb);
                if (polylineA == null)
                {
                    return;
                }
                Polyline polylineB = GetObject.GetPolyline(acdb);
                if (polylineB == null)
                {
                    return;
                }
                var lines = StructureDealer.MergePolyline(LineDealer.Polyline2Tuples(polylineA), LineDealer.Polyline2Tuples(polylineB));
                var polyline = LineDealer.Tuples2Polyline(lines);
                polyline.ColorIndex = 210;
                acdb.ModelSpace.Add(polyline);
            }
        }

        [CommandMethod("TIANHUACAD", "THNearPoints", CommandFlags.Modal)]
        public void THNearPoints()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var points = GetObject.GetCenters(acdb);
                List<Polyline> polylines = GetObject.GetPolylines(acdb);
                if (polylines == null)
                {
                    return;
                }
                Dictionary<Polyline, Point3dCollection> poly2points = new Dictionary<Polyline, Point3dCollection>();
                foreach (var pl in polylines)
                {
                    poly2points.Add(pl, new Point3dCollection());
                }

                var nearPoints = PointsDealer.NearPoints(poly2points, points);
                ShowInfo.ShowPoints(nearPoints, 'X', 130, 500);
            }
        }

        [CommandMethod("TIANHUACAD", "THCloestLine", CommandFlags.Modal)]
        public void THCloestLine()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var points = GetObject.GetCenters(acdb);
                Polyline polyline = GetObject.GetPolyline(acdb);
                Line line = GetObject.GetClosetLineOfPolyline(polyline, points[0], polyline.GetClosestPointTo(points[0], false) - points[0]);
                ShowInfo.DrawLine(line.StartPoint, line.EndPoint);
            }
        }

        [CommandMethod("TIANHUACAD", "THClrearConnect", CommandFlags.Modal)]
        public void THClrearConnect()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var points = GetObject.GetCenters(acdb);
                List<Polyline> polylines = GetObject.GetPolylines(acdb);
                if (polylines == null)
                {
                    return;
                }
                Dictionary<Polyline, Point3dCollection> poly2points = new Dictionary<Polyline, Point3dCollection>();
                foreach (var pl in polylines)
                {
                    poly2points.Add(pl, new Point3dCollection());
                }
                PointsDealer.VoronoiDiagramNearPoints(points, poly2points);

                //有可能不需要哈希结构（占内存）此处可调整
                HashSet<Point3d> nearPoints = new HashSet<Point3d>();
                foreach (var pts in poly2points.Values)
                {
                    foreach (var pt in pts)
                    {
                        if (pt is Point3d ptt && !nearPoints.Contains(ptt))
                            nearPoints.Add(ptt);
                    }
                }
                HashSet<Tuple<Point3d, Point3d>> tuples = StructureBuilder.DelaunayTriangulationConnect(points);
                LineDealer.DeleteSameClassLine(nearPoints, tuples);
                foreach (var tuple in tuples)
                {
                    ShowInfo.DrawLine(tuple.Item1, tuple.Item2, 130);
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THBorderPoint", CommandFlags.Modal)]
        public void THBorderPoint()
        {
            using (var cmd = new ThBeamConnectorCommand())
            {
                cmd.SubExecute();
            }
        }

        [CommandMethod("TIANHUACAD", "THCntNear2Wall", CommandFlags.Modal)]
        public void THCntNear2Wall()
        {
            using (AcadDatabase acdb = AcadDatabase.Active()) 
            {
                //获取柱点
                var clumnPts = GetObject.GetCenters(acdb);
                Dictionary<Polyline, HashSet<Point3d>> outlineClumns = new Dictionary<Polyline, HashSet<Point3d>>();
                //获取某个墙外边框
                Polyline outline = GetObject.GetPolyline(acdb);
                if (outline == null)
                {
                    return;
                }
                if (!outlineClumns.ContainsKey(outline))
                {
                    outlineClumns.Add(outline, new HashSet<Point3d>());
                }
                //获取多边形
                HashSet<Polyline> walls = GetObject.GetPolylines(acdb).ToHashSet();
                if (walls == null)
                {
                    return;
                }
                GetObject.FindPointsInOutline(clumnPts, outlineClumns);

                //预处理
                Point3dCollection ptsInOutline = new Point3dCollection();
                foreach (var sets in outlineClumns.Values)
                {
                    foreach (Point3d pt in sets)
                    {
                        ptsInOutline.Add(pt);
                    }
                }
                Point3dCollection newClumnPts = PointsDealer.RemoveSimmilerPoint(clumnPts, ptsInOutline);

                Dictionary<Polyline, HashSet<Polyline>> outlineWalls = new Dictionary<Polyline, HashSet<Polyline>>();
                outlineWalls.Add(outline, walls);

                //计算
                DataProcess.MergeWall(outlineWalls);
                var dicTuples = Connect.Calculate(newClumnPts, outlineWalls, outlineClumns, acdb);
                
                // 输出
                MainBeamPostProcess.MPostProcess(dicTuples);
            }
            //{
            //    //DBObjectCollection objs = new DBObjectCollection();
            //    List<Line> lines = new List<Line>();
            //    lines.Select(o => o.ExtendLine(1));
            //    var spaces = lines.ToCollection().PolygonsEx();
            //}
            //{
            //    List<Polyline> polylines = new List<Polyline>();
            //    ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(polylines.ToCollection());
            //    Polyline polyline = polylines[0].Buffer(10)[0] as Polyline;
            //    var entitys= spatialIndex.SelectCrossingPolygon(polyline);
            //    //var entitys= spatialIndex.SelectWindowPolygon(polyline);
            //    //var entitys= spatialIndex.SelectFence(polyline);
            //}
        }
        [CommandMethod("TIANHUACAD", "THPCL", CommandFlags.Modal)]
        public void THPCL()
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
                    objs.Add(acadDatabase.Element<Curve>(obj));
                }

                //ThMEPEngineCoreLayerUtils.CreateAICenterLineLayer(acadDatabase.Database);
                objs.BuildArea()
                    .OfType<Entity>()
                    .ForEach(e =>
                    {
                        ThMEPPolygonService.CenterLine(e)
                        .ToCollection()
                        .LineMerge()
                        .OfType<Entity>()
                        .ForEach(o =>
                        {
                            acadDatabase.ModelSpace.Add(o);
                            //o.Layer = ThMEPEngineCoreLayerUtils.CENTERLINE;
                        });
                    });
            }
        }
    }
}
