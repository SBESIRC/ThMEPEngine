using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Model
{ 
    class PointCompute
    {
        public static Line PointInLine(Point3d pt, List<Line> lineList)
        {
            double Tolerance = 10.0;
            foreach (var line in lineList)
            {
                var isOnLine = line.PointOnLine(pt, false, Tolerance);
                //if (PointIsLineTerm(pt, line))
                //{
                //    continue;
                //}
                if (isOnLine)
                {
                    return line;
                }
            }
            
            return new Line();
        }

        public static bool PointIsLineTerm(Point3d pt1, Line line)
        {
            double Tolerance = 10.0;
            if (pt1.DistanceTo(line.StartPoint) < Tolerance || pt1.DistanceTo(line.EndPoint) < Tolerance)
            {
                return true;
            }
            return false;
        }

        public static List<Line> CreateNewLine(Point3d pt, Line line)
        {
            var pt1 = line.StartPoint;
            var pt2 = line.EndPoint;
            var lineList = new List<Line>();
            
            lineList.Add(new Line(pt1, pt));
            lineList.Add(new Line(pt, pt2));

            return lineList;
        }

        public static bool IsNullLine(Line line)
        {
            double Tolerance = 10.0;
            var pt1 = new Point3d(0, 0, 0);
            if(pt1.DistanceTo(line.StartPoint) < Tolerance && pt1.DistanceTo(line.EndPoint) < Tolerance)
            {
                return true;
            }
            return false;
        }

        public static bool IsSecondLoop(Point3dEx pt1, Point3dEx pt2, double angle)
        {
            double Tolerance = 0.004;//弧度制
            double ang = PointAngle.ComputeAngle(pt1._pt, pt2._pt);
            var flag = Math.Abs(angle - ang) < Tolerance || Math.Abs(Math.Abs(angle - ang) - 2 * Math.PI) < Tolerance;
            return flag;
        }
    }
}
