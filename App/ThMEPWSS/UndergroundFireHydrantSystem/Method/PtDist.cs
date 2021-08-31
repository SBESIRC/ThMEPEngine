using Autodesk.AutoCAD.Geometry;
using System;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Method
{
    public static class PtDist
    {
        public static double DistanceToEx(this Point3dEx pt1, Point3dEx pt2)
        {
            return Math.Sqrt(Math.Pow(Math.Abs(pt1._pt.X - pt2._pt.X), 2) +
                             Math.Pow(Math.Abs(pt1._pt.Y - pt2._pt.Y), 2));
        }
    }
}
