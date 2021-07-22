using ThMEPElectrical.Command;
using Autodesk.AutoCAD.Runtime;

namespace ThMEPElectrical
{
    public class ThProtectThunderCmd
    {

        [CommandMethod("TIANHUACAD", "THDCLCMD", CommandFlags.Modal)]
        public void THDCLCMD()
        {
            using (var cmd = new ThDclCommand())
            {
                cmd.Execute();
            }
        }
    }
}
