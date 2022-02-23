using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPArchitecture.PartitionLayout
{
    public partial class TestCommond
    {
        [CommandMethod("TIANHUACAD", "ThParkPartitionTest", CommandFlags.Modal)]
        public void ThParkPartitionTest()
        {
            PolylineToHatch();
            //TestExtractHatch();
            //Execute();
        }

        private void PolylineToHatch()
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
                   .Where(o => o is Polyline)
                   .ToList();
                foreach (var obj in objs)
                {
                    var ids = new ObjectIdCollection();
                    ids.Add(obj.Id);
                    Hatch hatch = new Hatch();
                    hatch.PatternScale = 1;
                    hatch.CreateHatch(HatchPatternType.PreDefined, "SOLID", true);
                    hatch.AppendLoop(HatchLoopTypes.Outermost, ids);
                    hatch.EvaluateHatch(true);
                }
            }
        }

        private void TestExtractHatch()
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
                   .Select(o => o.Clone() as Hatch)
                   .ToList();
                var edges=new List<Polyline>();
                foreach (Hatch obj in objs)
                {
                    var pl = (Polyline)obj.Boundaries()[0];
                    var plrec = pl.GeometricExtents;
                    var rec = obj.GeometricExtents;
                    if (plrec.GetCenter().DistanceTo(rec.GetCenter()) > 1)
                    {
                        pl.TransformBy(Matrix3d.Mirroring(new Line3d(new Point3d(0, 0, 0), new Point3d(0, 1, 0))));
                        plrec = pl.GeometricExtents;
                        var vec = new Vector3d(rec.MinPoint.X - plrec.MinPoint.X, rec.MinPoint.Y - plrec.MinPoint.Y, 0);
                        pl.TransformBy(Matrix3d.Displacement(vec));
                    }
                    edges.Add(pl);
                }
                edges.ForEach(e => e.ColorIndex = ((int)ColorIndex.Red));
                edges.AddToCurrentSpace();
            }
        }

        private void Execute()
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
                        else if (o is Line) walls.Add(GeoUtilities.CreatePolyFromLine((Line)o));
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
        }
    }
}
