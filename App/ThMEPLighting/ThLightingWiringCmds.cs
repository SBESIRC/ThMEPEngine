using Autodesk.AutoCAD.Runtime;
using ThMEPLighting.Command;

namespace ThMEPLighting
{
    public class ThLightingWiringCmds
    {
        [CommandMethod("TIANHUACAD", "THZMLX", CommandFlags.Modal)]
        public void ThLightingRoute()
        {
            using (var cmd = new ThLigtingRouteComand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THHZLX", CommandFlags.Modal)]
        public void ThAFASRoute()
        {
            using (var cmd = new ThFireAlarmRouteCommand())
            {
                cmd.Execute();
            }
        }
    }
}
