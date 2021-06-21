using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThHydrantBranchLine
    {
        public Polyline BranchPolyline { set; get; }
        public ThHydrantBranchLine()
        {
            BranchPolyline = new Polyline();
        }
    }
}
