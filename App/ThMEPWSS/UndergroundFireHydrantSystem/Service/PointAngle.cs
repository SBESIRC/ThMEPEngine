using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class PointAngle
    {
        public static double ComputeAngle(Point3d pt1, Point3d pt2)
        {
            return Math.Atan2((pt2.Y - pt1.Y), (pt2.X - pt1.X));
        }

        public static bool IsSplashLine(Line line)
        {
            var torlerance = 0.035;
            var lenTor = 20;
            var ang = line.Angle;
            if(Math.Abs(ang - Math.PI/4) < torlerance || Math.Abs(ang - Math.PI * 5 / 4) < torlerance)
            {
                if(Math.Abs(line.Length - 200) < lenTor)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
