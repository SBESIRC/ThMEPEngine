using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPArchitecture.PartitionLayout;

namespace ThMEPArchitecture
{
    public partial class ParkingPartitionTest
    {
        [CommandMethod("TIANHUACAD", "ThParkPartitionTest", CommandFlags.Modal)]
        public void ThParkPartitionTest()
        {
            Execute();
        }

        public void Execute()
        {
            var walls = new List<Polyline>();
            var iniLanes = new List<Line>();
            var obstacles = new List<Polyline>();
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
                        else if (o is Line) walls.Add(PartitionLayout.GeoUtilities.CreatePolyFromLine((Line)o));
                    }
                    else if (o.Layer == "obstacles") obstacles.Add((Polyline)o);
                }
            }
            var Cutters = new DBObjectCollection();
            walls.ForEach(e => Cutters.Add(e));
            iniLanes.ForEach(e => Cutters.Add(e));
            obstacles.ForEach(e => Cutters.Add(e));
            var ObstaclesSpatialIndex = new ThCADCoreNTSSpatialIndex(Cutters);
            ParkingPartition partition = new ParkingPartition(walls, iniLanes, obstacles, new Polyline());
            partition.Boundary = GeoUtilities.JoinCurves(walls, iniLanes)[0];
            partition.Cutters = Cutters;
            partition.ObstaclesSpatialIndex = ObstaclesSpatialIndex;
            //partition.Display();
            partition.GenerateParkingSpaces();
        }
    }
}
