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
using ThMEPStructure.GirderConnect.Utils;
using ThMEPStructure.GirderConnect.ConnectProcess;

namespace ThMEPStructure.GirderConnect.Test
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
        public void ThVVD()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var points = GetObject.GetCenters(acdb);
                ConnectMainBeam.VoronoiDiagramConnect(points);
            }
        }

        [CommandMethod("TIANHUACAD", "THDTC", CommandFlags.Modal)]
        public void THDTC()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var points = GetObject.GetCenters(acdb);
                ConnectMainBeam.DelaunayTriangulationConnect(points);
            }
        }

        [CommandMethod("TIANHUACAD", "THCDTC", CommandFlags.Modal)]
        public void THCDTC()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var points = GetObject.GetCenters(acdb);
                var polylines = GetObject.GetPolylines(acdb);
                ConnectMainBeam.ConformingDelaunayTriangulationConnect(points, polylines);
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
                if(polyline == null)
                {
                    return;
                }
                //Dictionary<Point3d, int> PointClass = new Dictionary<Point3d, int>(); //test
                //PointsDealer.PointClassify(polyline, PointClass); //test basic isRight
                PointsDealer.OutPoints(polyline);
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
                ConnectMainBeam.SplitPolyline(LineDealer.Polyline2Tuples(polyline), polylines);
                foreach(var lines in polylines)
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
                var lines = ConnectMainBeam.MergePolyline(LineDealer.Polyline2Tuples(polylineA), LineDealer.Polyline2Tuples(polylineB));
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
                Polyline polyline = GetObject.GetPolyline(acdb);
                var neatPoints = PointsDealer.NearPoints(polyline, points);
            }
        }
    }
}
