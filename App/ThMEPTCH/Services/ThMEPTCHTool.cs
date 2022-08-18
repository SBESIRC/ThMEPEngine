using Autodesk.AutoCAD.Geometry;

namespace ThMEPTCH.Services
{
    internal class ThMEPTCHTool
    {
        public static double DistanceTo(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            var pt1 = new Point3d(x1, y1, z1);
            var pt2 = new Point3d(x2, y2, z2);
            return pt1.DistanceTo(pt2);
        }
    }
}
