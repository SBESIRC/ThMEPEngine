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
        [CommandMethod("TIANHUACAD", "THCRSD", CommandFlags.Modal)]
        public void ThCreateFireControlSystemDiagram()
        {
            using (var cmd = new ThFireControlSystemDiagramCmd())
            {
                cmd.Execute();
            }
        }
    }
}
