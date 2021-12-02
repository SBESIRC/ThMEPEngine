using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPArchitecture
{
    public partial class BuildingBoxTest
    {
        [CommandMethod("TIANHUACAD", "BuildingBox", CommandFlags.Modal)]
        public void ThParkPartitionTest()
        {
            var walls = new List<Polyline>();
            var iniLanes = new List<Line>();
            var obstacles = new List<Polyline>();
            Read(walls, iniLanes, obstacles);
            var boundary = GeoUtilities.JoinCurves(walls, iniLanes)[0];
            ;
        }
        public void Read(List<Polyline> walls, List<Line> iniLanes, List<Polyline> obstacles)
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
                    else if (o.Layer == "obstacles") obstacles.Add((Polyline)o);
                }
            }
        }

        public void f(List<Polyline> walls, List<Line> iniLanes, List<Polyline> obstacles, Polyline boundary)
        {
            return ;
        }

    }
}
