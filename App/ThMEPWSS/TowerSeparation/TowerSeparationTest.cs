using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Command;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Data;
using ThMEPWSS.TowerSeparation.TowerExtract;
using ThMEPWSS.DrainageSystemDiagram;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;
using System.Linq;
using System.Collections.Generic;

namespace ThMEPWSS.SprinklerConnect.Cmd
{
    public partial class ThSprinklerConnectNoUICmd
    {
        [CommandMethod("TIANHUACAD", "THSeparateTowerTest", CommandFlags.Modal)]
        public void SeparateTower()
        {
            var cmd = new TowerSeparationTest();
            cmd.Execute();
        }

        [CommandMethod("TIANHUACAD", "THIntersectTest", CommandFlags.Modal)]
        public void IntersectExecute()
        {
            Polyline targetPoly = new Polyline();
            Polyline crossPoly = new Polyline();
            Polyline edgePoly = new Polyline();
            targetPoly.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
            targetPoly.AddVertexAt(1, new Point2d(15, 0), 0, 0, 0);
            targetPoly.AddVertexAt(2, new Point2d(15, 10), 0, 0, 0);
            targetPoly.AddVertexAt(3, new Point2d(0, 10), 0, 0, 0);
            targetPoly.AddVertexAt(4, new Point2d(0, 0), 0, 0, 0);
            crossPoly.AddVertexAt(0, new Point2d(10, 5), 0, 0, 0);
            crossPoly.AddVertexAt(1, new Point2d(30, 5), 0, 0, 0);
            crossPoly.AddVertexAt(2, new Point2d(30, 15), 0, 0, 0);
            crossPoly.AddVertexAt(3, new Point2d(10, 15), 0, 0, 0);
            crossPoly.Closed = true;
            edgePoly.AddVertexAt(0, new Point2d(15, 0), 0, 0, 0);
            edgePoly.AddVertexAt(1, new Point2d(25, 0), 0, 0, 0);
            edgePoly.AddVertexAt(2, new Point2d(25, 5), 0, 0, 0);
            edgePoly.AddVertexAt(3, new Point2d(15, 5), 0, 0, 0);
            edgePoly.Closed = true;
            DrawUtils.ShowGeometry(targetPoly, "targetPoly", 2);
            DrawUtils.ShowGeometry(crossPoly, "crossPoly", 3);
            DrawUtils.ShowGeometry(edgePoly, "edgePoly", 4);
            //Point3d pt = new Point3d(0, 0, 0);
            //var flag = ThCADCoreNTSPolygonExtension.ContainsOrOnBoundary(targetPoly, pt);
            //var flag2 = ThCADCoreNTSPolygonExtension.Contains(targetPoly, pt);
            //if (flag&&flag2)
            //{
            //    DrawUtils.ShowGeometry(pt, "testForContains", 1);
            //}
            //else if (flag && !flag2)
            //{
            //    DrawUtils.ShowGeometry(pt, "testForContains", 5);
            //}else if (!flag && flag2)
            //{
            //    DrawUtils.ShowGeometry(pt, "testForContains", 6);
            //}
            //else
            //{
            //    DrawUtils.ShowGeometry(pt, "testForContains", 7);
            //}


            //DBObjectCollection polySet = new DBObjectCollection { crossPoly, edgePoly };
            //DBObjectCollection targetSet = new DBObjectCollection { targetPoly };
            //DrawUtils.ShowGeometry(ThDbObjectCollectionExtension.Difference(targetSet, polySet).Cast<Entity>().ToList(), "testForDifference2", 7);

            //var intersectPt1 = ThGeometryTool.IntersectWithEx(targetPoly, edgePoly);
            //foreach(Point3d p in intersectPt1)
            //{
            //    DrawUtils.ShowGeometry(p, "testForIntersection1", 1);
            //}

            //var intersect1 = ThCADCoreNTSEntityExtension.Intersection(targetPoly, crossPoly).Cast<Polyline>().ToList();
            //DrawUtils.ShowGeometry(intersect1, "testForIntersection1", 1);
            //var intersect2 = ThCADCoreNTSEntityExtension.Intersection(targetPoly, polySet).Cast<Polyline>().ToList();
            //DrawUtils.ShowGeometry(intersect2, "testForIntersection2", 5);
            //var differce = ThCADCoreNTSEntityExtension.Difference(targetPoly, polySet).Cast<Entity>().ToList();
            //DrawUtils.ShowGeometry(differce, "testForDifference", 6);
            //var buffered = (differce[0] as Polyline).Buffer(10);
            //DrawUtils.ShowGeometry(buffered.Cast<Entity>().ToList(), "testForDifference", 7);
            ////Polyline line = new Polyline();
            //line.AddVertexAt(0, new Point2d(5, -10), 0, 0, 0);
            //line.AddVertexAt(1, new Point2d(5, 20), 0, 0, 0);
            //line.Closed = true;
            //Line line = new Line(new Point3d(5, -10, 0), new Point3d(5, 20, 0));
            //DrawUtils.ShowGeometry(line, "line", 5);
            //bool flag1 = targetPoly.Intersects(line);
            //DBObjectCollection polySet = new DBObjectCollection { targetPoly, crossPoly, edgePoly };
            //var polySpatialIndex = new ThCADCoreNTSSpatialIndex(polySet);
            //bool flag2 = polySpatialIndex.Intersects(line.Buffer(1));
            //var temp = line.Buffer(1);
            ////var temp = line.Buffer(1).Cast<Polyline>().ToList().FirstOrDefault();
            //var difference2 = ThCADCoreNTSEntityExtension.Difference(temp, new DBObjectCollection { targetPoly });
            //DrawUtils.ShowGeometry(difference2.Cast<Entity>().ToList(), "testForDifference2", 6);
            //var doubleInter = ThCADCoreNTSEntityExtension.Intersection(line, difference2);
            //DrawUtils.ShowGeometry(doubleInter.Cast<Entity>().ToList(), "testForDifference3", 7);

            Polyline testPoly = new Polyline();
            testPoly.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
            testPoly.AddVertexAt(1, new Point2d(20, 0), 0, 0, 0);
            testPoly.AddVertexAt(2, new Point2d(20, 5), 0, 0, 0);
            testPoly.AddVertexAt(3, new Point2d(5, 5), 0, 0, 0);
            testPoly.AddVertexAt(4, new Point2d(5, 15), 0, 0, 0);
            testPoly.AddVertexAt(5, new Point2d(0, 15), 0, 0, 0);
            testPoly.Closed = true;
            DrawUtils.ShowGeometry(testPoly, "testPoly", 5);
            var temp = testPoly.Buffer(-2).Cast<Entity>().ToList();
            DrawUtils.ShowGeometry(temp, "testPolyBuffer", 6);

        }
    }

    public class TowerSeparationTest : ThMEPBaseCommand
    {
        public TowerSeparationTest()
        {
        }
        public override void SubExecute()
        {
            TowerSeparateExecute();
        }

        public void TowerSeparateExecute()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var frame = ThSprinklerDataService.GetFrame();
                if (frame == null || frame.Area < 10)
                {
                    return;
                }


                var dataset = new ThSprinklerConnectDataFactory();
                var geos = dataset.Create(acadDatabase.Database, frame.Vertices()).Container;
                var dataQuery = new ThSprinklerDataQueryService(geos);
                dataQuery.ClassifyData();
                var TowerExtractor = new TowerExtractor();

                var shearWalls = TowerExtractor.Extractor(dataQuery.ShearWallList, frame);
                DrawUtils.ShowGeometry(shearWalls, "testForExtractor", 1);

                //foreach(Polyline l in shearWalls)
                //{
                //    l.ColorIndex = 5;
                //}
                //acadDatabase.ModelSpace.Add(shearWalls);



            }
        }


    }
}
