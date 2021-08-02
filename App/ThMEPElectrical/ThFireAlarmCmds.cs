using ThMEPElectrical.Command;
using Autodesk.AutoCAD.Runtime;

namespace ThMEPElectrical
{
    public class ThFireAlarmCmds
    {
        [CommandMethod("TIANHUACAD", "THFireAlarmData", CommandFlags.Modal)]
        public void THFireAlarmData()
        {
            using (var cmd = new ThFireAlarmCommand())
            {
                cmd.Execute();
            }
        }
    }
}
