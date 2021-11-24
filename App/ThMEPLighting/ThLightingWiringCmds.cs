using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.Command;

namespace ThMEPLighting
{
    public class ThLightingWiringCmds
    {
        [CommandMethod("TIANHUACAD", "THZMLX", CommandFlags.Modal)]
        public void THLX()
        {
            using (var cmd = new ThLigtingRouteComand())
            {
                cmd.Execute();
            }
        }
    }
}
