using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS
{
    public partial class ThSystemDiagramCmds
    {
        [CommandMethod("TIANHUACAD", "-THWTKSB", CommandFlags.Modal)]
        public void ThTestBlockConfig()
        {
            using (var cmd = new BlockNameConfig.Cmd(null))
            {
                cmd.Execute();
            }
        }
    }
}
