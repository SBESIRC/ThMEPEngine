using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class PointAngle
    {
        public static double ComputeAngle(Point3d pt1, Point3d pt2)
        {
            return Math.PI - Math.Atan2((pt2.X - pt1.X), (pt2.Y - pt1.Y));
        }

        public static double ComputeAngle(Line line)
        {
            var pt1 = line.StartPoint;
            var pt2 = line.EndPoint;

            return Math.Atan2((pt2.X - pt1.X), (pt2.Y - pt1.Y));
        }

        public static bool IsParallelLine(Line line1, Line line2)
        {
            double angleError = 0.035;
            return Math.Abs(ComputeAngle(line1) - ComputeAngle(line2)) < angleError ||
                   Math.Abs(Math.Abs(ComputeAngle(line1) - ComputeAngle(line2)) - Math.PI) < angleError ||
                   Math.Abs(Math.Abs(ComputeAngle(line1) - ComputeAngle(line2)) - 2 * Math.PI) < angleError;
        }

        public static bool IsParallelLine(Line line1, Line line2, double angleError)
        {
            return Math.Abs(line1.Angle - line2.Angle) < angleError ||
                   Math.Abs(line1.Angle - line2.Angle - Math.PI) < angleError ||
                   Math.Abs(line1.Angle - line2.Angle - 2 * Math.PI) < angleError;
        }

        public static bool IsParallelLine(double angle1, double angle2)
        {
            double angleError = 0.035;
            return Math.Abs(angle1 - angle2) < angleError ||
                   Math.Abs(Math.Abs(angle1 - angle2) - Math.PI) < angleError ||
                   Math.Abs(Math.Abs(angle1 - angle2) - 2 * Math.PI) < angleError;
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
