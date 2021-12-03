using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPArchitecture.ParkingStallArrangement;

namespace ThMEPArchitecture
{

        public partial class ThParkingStallArrangement
    {
            [CommandMethod("TIANHUACAD", "-THDXQYFG", CommandFlags.Modal)]
            public void ThArrangeParkingStall()
            {
                using (var cmd = new ThParkingStallArrangementCmd())
                {
                    cmd.Execute();
                }
            }
        
    }
}
