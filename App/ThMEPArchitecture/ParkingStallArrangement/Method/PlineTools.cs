using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class PlineTools
    {
        public static bool IsBoundOf(this Line line, Polyline pline)
        {
            var pts = pline.GetPoints().ToList();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                for (int j = 0; j < pts.Count; j++)
                {
                    var line2 = new Line(pts[i], pts[j]);
                    if (line.IsOverlap(line2))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool IsOverlap(this Line line1, Line line2)
        {
            var spt1 = line1.StartPoint.ToPoint2d();
            var ept1 = line1.EndPoint.ToPoint2d();
            var spt2 = line2.StartPoint.ToPoint2d();
            var ept2 = line2.EndPoint.ToPoint2d();

            var line2d1 = new Line2d(spt1, ept1);
            var line2d2 = new Line2d(spt2, ept2);
            if (line2d1.Overlap(line2d2) is null)
            {
                return false;
            }

            return true;
        }
    }
}

