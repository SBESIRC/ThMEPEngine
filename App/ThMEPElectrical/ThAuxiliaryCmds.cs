using ThMEPElectrical.Command;
using Autodesk.AutoCAD.Runtime;

namespace ThMEPElectrical
{
    public class ThAuxiliaryCmds
    {
        [CommandMethod("TIANHUACAD", "THLCDY", CommandFlags.Modal)]
        public void ThCreateStorey()
        {
            using (var cmd = new ThInsertStoreyCommand())
            {
                cmd.Execute();
            }
        }
    }
}
