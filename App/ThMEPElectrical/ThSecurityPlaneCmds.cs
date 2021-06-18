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

        [CommandMethod("TIANHUACAD", "ThVMLSystem", CommandFlags.Modal)]
        public void ThVideoMLSystem()
        {
            using (var cmd = new ThVideoMonitoringSystemWithLaneCommand())
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

        [CommandMethod("TIANHUACAD", "ThIAHSystem", CommandFlags.Modal)]
        public void ThIntrusionAlarmHositingSystem()
        {
            using (var cmd = new ThIntrusionAlarmSystemHositingCommand())
            {
                cmd.Execute();
            }
        }
    }
}
