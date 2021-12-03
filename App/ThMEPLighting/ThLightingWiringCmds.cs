using Autodesk.AutoCAD.Runtime;
using ThMEPLighting.Command;

namespace ThMEPLighting
{
    public class ThLightingWiringCmds
    {
        [CommandMethod("TIANHUACAD", "THZMLX", CommandFlags.Modal)]
        public void ThLightingRoute()
        {
            using (var cmd = new ThLightingRouteComand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THHZLX", CommandFlags.Modal)]
        public void ThAFASRoute()
        {
            using (var cmd = new ThAFASRouteCommand())
            {
                cmd.Execute();
            }
        }
    }
}
