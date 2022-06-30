using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.Command;

namespace ThMEPHVAC
{
    class ThSmokeProofSystemCmd
    {
        [CommandMethod("TIANHUACAD", "THssssss", CommandFlags.Modal)]
        public void THSmokeProofSystem()
        {
            using (var cmd = new SmokeProofSystemCmd())
            {
                cmd.Execute();
            }
        }
    }
}
