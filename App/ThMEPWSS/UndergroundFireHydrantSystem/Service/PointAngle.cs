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
    }
}
