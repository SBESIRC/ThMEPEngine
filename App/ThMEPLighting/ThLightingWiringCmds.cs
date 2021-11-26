using Autodesk.AutoCAD.Runtime;
using ThMEPLighting.Command;

namespace ThMEPLighting
{
    public class ThLightingWiringCmds
    {
        [CommandMethod("TIANHUACAD", "THZMLX", CommandFlags.Modal)]
        public void THLigtingWiring()
        {
            using (var cmd = new ThLigtingRouteComand())
            {
                cmd.Execute();
            }
        }
    }
}
