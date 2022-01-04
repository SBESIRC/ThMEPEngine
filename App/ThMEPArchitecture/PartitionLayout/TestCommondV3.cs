using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPArchitecture.PartitionLayout
{
    public partial class TestCommondV3
    {
        [CommandMethod("TIANHUACAD", "ThPPPPPParkTest", CommandFlags.Modal)]
        public void ThPPPPPParkTest()
        {
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs = result.Value
                   .GetObjectIds()
                   .Select(o => adb.Element<Entity>(o))
                   .Where(o => o is Hatch)
                   .Select(o => o.Clone() as Entity)
                   .ToList();
                var pls = objs.Select(e => (Hatch)e);
                foreach (var e in pls)
                {
                    var k = e.Boundaries()[0];
                    k.Layer = "障碍物边缘";
                    k.AddToCurrentSpace();
                }
                return;
            }

            //using (AcadDatabase adb = AcadDatabase.Active())
            //{
            //    var result = Active.Editor.GetSelection();
            //    if (result.Status != PromptStatus.OK)
            //    {
            //        return;
            //    }
            //    var objs = result.Value
            //       .GetObjectIds()
            //       .Select(o => adb.Element<Entity>(o))
            //       .Where(o => o is Polyline)
            //       .Select(o => o.Clone() as Entity)
            //       .ToList();
            //    var pls = objs.Select(e => (Polyline)e);
            //    pls = pls.OrderByDescending(e => e.Area);
            //    var a = pls.First();
            //    var b = pls.Last();
            //    DBObjectCollection obs = new DBObjectCollection();
            //    obs.Add(a);
            //    ThCADCoreNTSSpatialIndex sindex = new ThCADCoreNTSSpatialIndex(a.ToNTSPolygon().ToDbCollection());
            //    var crossed = sindex.SelectCrossingPolygon(b);
            //    var c = sindex.SelectWindowPolygon(b);
            //    var m = sindex.SelectFence(b);
            //    var dd = sindex.Intersects(b, true);
            //    var t = sindex.Intersects(b);
            //    var k = a.IsPointIn(b.GetRecCentroid());
            //    ;
            //}
            Execute();
        }
        public void Execute()
        {
            var walls = new List<Polyline>();
            var iniLanes = new List<Line>();
            var obstacles = new List<Polyline>();
            var buildingBox = new List<Polyline>();
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                var result = Active.Editor.GetSelection();
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var objs = result.Value
                   .GetObjectIds()
                   .Select(o => adb.Element<Entity>(o))
                   .Where(o => o is Line || o is Polyline)
                   .Select(o => o.Clone() as Entity)
                   .ToList();
                foreach (var o in objs)
                {
                    if (o.Layer == "inilanes") iniLanes.Add((Line)o);
                    else if (o.Layer == "walls")
                    {
                        if (o is Polyline) walls.Add((Polyline)o);
                        else if (o is Line) walls.Add(GeoUtilities.PolyFromLine((Line)o));
                    }
                    else if (o.Layer == "obstacles")
                    {
                        if (o is Polyline) obstacles.Add((Polyline)o);
                    }
                    else if (o.Layer == "buildingBoxes")
                    {
                        if (o is Polyline) buildingBox.Add((Polyline)o);
                    }
                }
            }

            var boundary = GeoUtilities.JoinCurves(walls, iniLanes)[0];
            Extents3d ext = new Extents3d();
            obstacles.ForEach(o => ext.AddExtents(o.GeometricExtents));
            var Cutters = new DBObjectCollection();
            obstacles.ForEach(e => Cutters.Add(e));
            //Cutters.Add(boundary);
            var ObstaclesSpatialIndex = new ThCADCoreNTSSpatialIndex(Cutters);
            PartitionV3 partition = new PartitionV3(walls, iniLanes, obstacles, boundary, buildingBox);
            partition.ObstaclesSpatialIndex = ObstaclesSpatialIndex;
            partition.GenerateParkingSpaces();
            partition.Display();
        }
    }
}
