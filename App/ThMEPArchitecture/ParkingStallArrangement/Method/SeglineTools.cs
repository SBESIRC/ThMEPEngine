using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class SeglineTools
    {
        public static List<Line> SeglinePrecut(List<Line> SegLines, Polyline WallLine, double prop = 0.8)
        {
            for (int i = 0; i < SegLines.Count; ++i)
            {
                var line1 = SegLines[i];
                for (int j = i; j < SegLines.Count; ++j)
                {
                    if (i == j) continue;
                    var line2 = SegLines[j];
                    //找交点
                    var templ = line1.Intersect(line2, Intersect.OnBothOperands);
                    if (templ.Count != 0)
                    {
                        var pt = templ.First();//交点
                        if (!WallLine.Contains(pt))//交点在边界外，需要切割
                        {
                            //点不在区域内部，需要切割
                            //1.找到在边界上距离pt最近的点

                            //line1在边界上距离pt最近的点
                            var dis1 = GetNearestOnWall(line1, WallLine, pt, out Point3d wpt1);

                            //line2在边界上距离pt最近的点
                            var dis2 = GetNearestOnWall(line2, WallLine, pt, out Point3d wpt2);

                            ////选伸出边界较长的一根切割
                            //if(dis1 > dis2)
                            //{
                            //    CutLine(ref line1, wpt1, pt, prop);
                            //    SegLines[i] = line1;
                            //}
                            //else
                            //{
                            //    CutLine(ref line2, wpt2, pt, prop);
                            //    SegLines[j] = line2;
                            //}

                            //两根都切一下
                            CutLine(ref line1, wpt1, pt, prop);
                            SegLines[i] = line1;
                            CutLine(ref line2, wpt2, pt, prop);
                            SegLines[j] = line2;
                        }
                    }
                }
            }
            return SegLines;
        }

        private static double GetNearestOnWall(Line line, Polyline WallLine, Point3d IntPt, out Point3d wpt)
        {
            var templ = line.Intersect(WallLine, Intersect.OnBothOperands);
            if (templ.Count == 0) throw new ArgumentException("线不与边界相交");
            if (templ.Count == 1)
            {
                wpt = templ.First();
                return IntPt.DistanceTo(wpt);
            }

            else
            {
                if (line.GetDirection() == 1)
                {
                    templ = templ.OrderBy(pt => pt.Y).ToList();
                    if (IntPt.DistanceTo(templ.First()) < IntPt.DistanceTo(templ.Last()))
                    {
                        wpt = templ.First();
                    }
                    else
                    {
                        wpt = templ.Last();
                    }
                    return IntPt.DistanceTo(wpt);
                }
                else
                {
                    templ = templ.OrderBy(pt => pt.X).ToList();
                    if (IntPt.DistanceTo(templ.First()) < IntPt.DistanceTo(templ.Last()))
                    {
                        wpt = templ.First();
                    }
                    else
                    {
                        wpt = templ.Last();
                    }
                    return IntPt.DistanceTo(wpt);
                }
            }
        }


        private static void CutLine(ref Line line, Point3d wpt, Point3d pt, double prop)
        {
            // 把line切割,在边界外的线只保留交点和墙线之前部分的prop比例的线
            // wpt 在墙线上的点
            // pt 在外部的交点
            // prop 切断后留的比例
            var spt = line.StartPoint;
            var ept = line.EndPoint;
            double dis = pt.DistanceTo(wpt);
            Point3d tempPT;// 切割后的线的一个端点
            if (line.GetDirection() == 1)
            {
                if (wpt.Y < pt.Y)
                {
                    tempPT = new Point3d(wpt.X, wpt.Y + dis * prop, 0);
                }
                else
                {
                    tempPT = new Point3d(wpt.X, wpt.Y - dis * prop, 0);
                }
            }
            else
            {
                if (wpt.X < pt.X)
                {
                    tempPT = new Point3d(wpt.X + dis * prop, wpt.Y, 0);
                }
                else
                {
                    tempPT = new Point3d(wpt.X - dis * prop, wpt.Y, 0);
                }
            }
            if (spt.DistanceTo(wpt) < spt.DistanceTo(pt))
            {
                // 起始点在保留的一侧
                line.EndPoint = tempPT;
            }
            else
            {
                // 终点在保留的一侧
                line.StartPoint = tempPT;
            }
        }

    }
}
