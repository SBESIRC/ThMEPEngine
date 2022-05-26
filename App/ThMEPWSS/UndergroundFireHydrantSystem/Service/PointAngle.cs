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
            if (line.Length < 180 && line.Length > 400)//线长不合理直接返回false
            {
                return false;
            }
            var ang = line.Angle;
            if(ang > 0)
            {
                while(ang > 0)
                {
                    if(Math.Abs(ang - Math.PI / 4) < torlerance)
                    {
                        return true;
                    }
                    ang -= Math.PI / 2;
                }
            }
            else
            {
                while (ang < 0)
                {
                    if (Math.Abs(ang + Math.PI / 4) < torlerance)
                    {
                        return true;
                    }
                    ang += Math.PI / 2;
                }
            }
            
            return false;
        }
    }
}
