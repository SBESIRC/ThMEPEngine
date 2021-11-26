using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class PtTools
    {
        public static Point3d GetMiddlePt(Point3d pt1, Point3d pt2)
        {
            return new Point3d((pt1.X + pt2.X)/2, (pt1.Y + pt2.Y) / 2, 0);
        }

        public static Point3d GetMiddlePt(this Line line)
        {
            return GetMiddlePt(line.StartPoint, line.EndPoint);
        }
    }
}
