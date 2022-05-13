using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Command;
using ThMEPWSS.UndergroundSpraySystem.Command;

namespace ThMEPWSS
{
    public partial class ThSystemDiagramCmds
    {
        [CommandMethod("TIANHUACAD", "-THDXXHSXTT", CommandFlags.Modal)]
        public void ThTestFireHydrant()
        {
            using (var cmd = new ThFireHydrantCmd(null))
            {
                cmd.Test();
            }
        }

        [CommandMethod("TIANHUACAD", "-THDXPLXTT", CommandFlags.Modal)]
        public void ThTestSpraySystem()
        {
            using (var cmd = new ThSpraySystemCmd(null))
            {
                cmd.Test();
            }
        }
    }
}
