using ThMEPElectrical.Command;
using Autodesk.AutoCAD.Runtime;

namespace ThMEPElectrical
{
    public class ThGroundGridCmds
    {
        [CommandMethod("TIANHUACAD", "THGGD", CommandFlags.Modal)]
        public void THGGD()
        {
            using (var cmd = new ThGroundGridCommand())
            {
                cmd.Execute();
            }
        }
    }
}
