using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThHydrantBranchLine
    {
        public Polyline BranchPolylineObb { set; get; }
        public Polyline BranchPolyline { set; get; }
        public ThHydrantBranchLine()
        {
            BranchPolyline = new Polyline();
        }

        public static ThHydrantBranchLine Create(Entity data)
        {
            var branchLine = new ThHydrantBranchLine();
            if (data is Polyline)
            {
                var polyline = data as Polyline;
                branchLine.BranchPolyline = polyline;

                var objcets = polyline.Buffer(50);
                var obb = objcets[0] as Polyline;
                branchLine.BranchPolylineObb = obb;
            }

            return branchLine;
        }
    }
}
