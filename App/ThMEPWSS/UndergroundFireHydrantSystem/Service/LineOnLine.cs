using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.CADExtensionsNs;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class LineOnLine
    {
        public static Line LineIsOnLine(Line valve, List<Line> pipeLineList)
        {
            foreach(var pipe in pipeLineList)
            {
                var tolerance = 10;
                var f1 = pipe.PointOnLine(valve.StartPoint, false, tolerance);
                var f2 = pipe.PointOnLine(valve.EndPoint, false, tolerance);
                if (f1 && f2)
                {
                    return pipe;
                }
            }
            return new Line();
        }

        public static bool LineIsOnLineList(Line valve, List<Line> pipeLineList)//阀门线在管线上
        {
            foreach(var line in pipeLineList)
            {
                if(PointAngle.IsParallelLine(valve, line))//是平行线
                {
                    if(line.PointOnLine(valve.StartPoint, true, 10) || line.PointOnLine(valve.EndPoint, true, 10))
                        //透射点距离近
                    {
                        if(LineIsNearToLine(valve, line))//终点距离近
                        {
                            return true;
                        }

                    }
                }
            }
            return false;
        }

        public static bool LineIsNearToLine(Line line1, Line line2)
        {
            var tolerance = 10;
            //两条线分开
            if(line1.StartPoint.DistanceTo(line2.StartPoint) < tolerance || 
               line1.StartPoint.DistanceTo(line2.EndPoint) < tolerance ||
               line1.EndPoint.DistanceTo(line2.StartPoint) < tolerance ||
               line1.EndPoint.DistanceTo(line2.EndPoint) < tolerance)
            {
                return true;
            }
            if(PointIsInLine(line1.StartPoint, line2) || PointIsInLine(line1.EndPoint, line2))
            {
                return true;
            }
            return false;
        }

        public static bool PointIsInLine(Point3d pt, Line line)
        {
            double tolerance = 10;
            return line.PointOnLine(pt, false, tolerance);
            
        }

        public static void LineSplit(Line valve, Line pipe, ref List<Line> pipeLineList)
        {
            pipeLineList.Remove(pipe);
            pipeLineList.Add(valve);
            if (valve.StartPoint.DistanceTo(pipe.StartPoint) < valve.EndPoint.DistanceTo(pipe.StartPoint))
            {
                pipeLineList.Add(new Line(pipe.StartPoint, valve.StartPoint));
                pipeLineList.Add(new Line(pipe.EndPoint, valve.EndPoint));
            }
            else
            {
                pipeLineList.Add(new Line(pipe.StartPoint, valve.EndPoint));
                pipeLineList.Add(new Line(pipe.EndPoint, valve.StartPoint));
            }
        }
    }
}
