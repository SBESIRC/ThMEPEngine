using ThMEPElectrical.Command;
using Autodesk.AutoCAD.Runtime;

namespace ThMEPElectrical
{
    public class ThLaneLineCmds
    {
        [CommandMethod("TIANHUACAD", "THTCD", CommandFlags.Modal)]
        public void ThLaneLine()
        {
            using (var cmd = new ThLaneLineCommand())
            {
                cmd.Execute();
            }
        }
    }
}
