using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class PlineTools
    {
        public static bool IsBoundOf(this Line line, Polyline pline)
        {
            var lines = pline.ToLines();
            try
            {
                foreach (var l in lines)
                {
                    if (line.IsOverlap(l)) return true;
                }
            }
            finally
            {
                lines.ForEach(l => l.Dispose());
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
            try
            {
                if (!line2d1.IsParallelTo(line2d2))
                {
                    return false;
                }
                if (line2d1.Overlap(line2d2) is null)
                {
                    return false;
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                line2d1.Dispose();
                line2d2.Dispose();
            }

            return true;
        }

        public static double GetMaxWidth(this Polyline pline)
        {
            var pts = pline.GetPoints();
            var ptsOrderedByX = pts.OrderBy(p => p.X);
            var ptsOrderedByY = pts.OrderBy(p => p.Y);
            var width = Math.Abs(ptsOrderedByX.First().X - ptsOrderedByX.Last().X);
            var height = Math.Abs(ptsOrderedByY.First().Y - ptsOrderedByY.Last().Y);
            return Math.Max(width, height);
        }
    }
}

