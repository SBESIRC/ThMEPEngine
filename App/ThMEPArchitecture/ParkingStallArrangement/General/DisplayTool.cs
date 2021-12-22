using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPArchitecture.ParkingStallArrangement.General
{
    internal class DisplayTool
    {
        public static string GetPlineValues(Polyline pline)
        {
            var values = new List<double>();
            var pts = pline.GetPoints();
            foreach (var pt in pts)
            {
                values.Add(pt.X);
                values.Add(pt.Y);
            }
            string rst = "";
            foreach (var val in values)
            {
                rst += Convert.ToString(val);
                rst += ',';
            }
            return rst;
        }
    }
}
