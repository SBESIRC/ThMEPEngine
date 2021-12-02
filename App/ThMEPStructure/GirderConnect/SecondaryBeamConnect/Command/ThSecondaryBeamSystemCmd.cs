using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Command
{
    public class ThSecondaryBeamSystemCmd
    {
        [CommandMethod("TIANHUACAD", "THCLBZ", CommandFlags.Modal)]
        public void THBorderPoint()
        {
            using (var cmd = new SecondaryBeamConnectCmd())
            {
                cmd.Execute();
            }
        }
    }
}
