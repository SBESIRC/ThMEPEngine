using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Command;

namespace ThMEPElectrical
{
    class ThFireAlarmSystemLayoutCmds
    {
        //Tian hua Fire alarm layout command
        [CommandMethod("TIANHUACAD", "THFALC", CommandFlags.Modal)]
        public void THFALC()
        {
            using (var cmd = new ThFireAlarmSystemLayoutCommand())
            {
                cmd.Execute();
            }
        }
    }
}
