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
    }
}
