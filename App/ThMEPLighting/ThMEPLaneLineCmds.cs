using System.Linq;
using ThMEPElectrical.Command;
using Autodesk.AutoCAD.Runtime;

namespace ThMEPLighting
{
    public class ThMEPLaneLineCmds
    {
        [CommandMethod("TIANHUACAD", "THTCDX", CommandFlags.Modal)]
        public void ThLaneLine()
        {
            using (var cmd = new ThLaneLineCommand())
            using (var acad = Linq2Acad.AcadDatabase.Active())
            {
                cmd.LaneLineLayers = ThMEPLightingService.Instance.LanelineLayers
                    .Where(o=>acad.Layers.Contains(o))
                    .Select(o => o)
                    .ToList();
                cmd.Execute();
            }
        }
    }
}
