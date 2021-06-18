using ThMEPWSS.Command;
using Autodesk.AutoCAD.Runtime;

namespace ThMEPWSS
{
    public class ThHydrantCmds
    {
        [CommandMethod("TIANHUACAD", "ThHydrantProtectRadiusCheck", CommandFlags.Modal)]
        public void ThHydrantProtectRadiusCheck()
        {
            using (var cmd = new ThHydrantProtectionRadiusCmd())
            {
                cmd.Execute();
            }
        }
    }
}
