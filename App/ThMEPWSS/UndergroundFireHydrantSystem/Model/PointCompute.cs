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
            var termPointLine = new Line();
            foreach (var line in lineList)
            {
                var isOnLine = line.PointOnLine(pt, false, Tolerance);//判断点是否在线上
                if(isOnLine)//点在线上
                {
                    if(!PointIsLineTerm(pt, line))//点不是线的端点
                    {
                        return line;//直接返回
                    }
                    else//是端点
                    {
                        termPointLine = line;//先保存
                    }
                }
            }
            
            return new Line();
        }

        public static Line PointOnLine(Point3d pt, List<Line> lineList)
        {
            double Tolerance = 10.0;
            double disTorlerance = 100;
            foreach (var line in lineList)
            {
                var isOnLine = line.PointOnLine(pt, true, Tolerance);//判断点是否在延长线上
                if (isOnLine)//点在延长线上
                {
                    if(line.PointOnLine(pt, false, Tolerance))//点在线上
                    {
                        return line;//直接返回
                    }
                    else//点不在线上
                    {
                        if(pt.DistanceTo(line.StartPoint) < disTorlerance || pt.DistanceTo(line.EndPoint) < disTorlerance)
                        {
                            //距离小于100返回
                            return line;
                        }
                    }
                }
            }

            {
                ;
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
            double Tolerance = 0.035;//弧度制
            double ang = PointAngle.ComputeAngle(pt1._pt, pt2._pt);
            var flag = Math.Abs(angle - ang) < Tolerance || Math.Abs(Math.Abs(angle - ang) - 2 * Math.PI) < Tolerance;
            return flag;
        }
    }
}
