using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Command;

namespace ThMEPWSS
{
    public partial class ThSystemDiagramCmds
    {
        [CommandMethod("TIANHUACAD", "THDXXHSXTT_Test", CommandFlags.Modal)]
        public void ThTestFireHydrant()
        {
            using (var cmd = new ThFireHydrantCmd())
            {
                cmd.Execute();
            }
        }
    }
}
