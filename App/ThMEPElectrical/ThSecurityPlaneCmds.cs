using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Command;

namespace ThMEPElectrical
{
    public class ThSecurityPlaneCmds
    {
        [CommandMethod("TIANHUACAD", "ThVMSystem", CommandFlags.Modal)]
        public void ThVideoMSystem()
        {
            using (var cmd = new ThVideoMonitoringSystemCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "ThIASystem", CommandFlags.Modal)]
        public void ThIntrusionAlarmSystem()
        {
            using (var cmd = new ThIntrusionAlarmSystemCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "ThGTSystem", CommandFlags.Modal)]
        public void ThGuardToourSystem()
        {
            using (var cmd = new ThGuardToourSystemCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "ThACSystem", CommandFlags.Modal)]
        public void ThAccessControlSystem()
        {
            using (var cmd = new ThAccessControlSystemCommand())
            {
                cmd.Execute();
            }
        }
    }
}
