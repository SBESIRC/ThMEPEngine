using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Command;

namespace ThMEPElectrical
{
    public class ThAutoFireAlarmSystemCmd
    {
        [CommandMethod("TIANHUACAD", "ThAFAS", CommandFlags.Modal)]
        public void ThAFAS()
        {
            using (var cmd = new ThAutoFireAlarmSystemCommand())
            {
                cmd.Execute();
            }
        }
    }
}
