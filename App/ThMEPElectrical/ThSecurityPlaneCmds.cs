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
