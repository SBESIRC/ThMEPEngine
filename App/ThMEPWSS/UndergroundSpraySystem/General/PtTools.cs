using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.General
{
    public static class PtTools
    {
        public static bool AddNewPtDic(this SprayIn sprayIn, DBObjectCollection objs, Point3dEx pt, ref List<Line> lines)
        {
            if(objs.Count != 2) return false;//不是两根线，不必进行连接

            var pt1 = new Point3dEx( (objs[0] as Line).GetClosedPt(pt));//获取最近点1
            var pt2 = new Point3dEx((objs[1] as Line).GetClosedPt(pt));//获取最近点2

            //点集中不存在，就算了
            if (!sprayIn.PtDic.ContainsKey(pt1) || !sprayIn.PtDic.ContainsKey(pt2))
                return false;
            
            //点集中邻接点数大于1也算了
            if(sprayIn.PtDic[pt1].Count != 1 || sprayIn.PtDic[pt2].Count != 1)
                return false;
            
            lines.Add(new Line(pt1._pt, pt2._pt));
            return true;
        }

        public static bool AddNewPtDic(this FireHydrantSystemIn fireHydrantSysIn, DBObjectCollection objs, Point3d pt, ref List<Line> lines,
            bool isMiddleRiser)
        {
            double tolerance = 120;
            if (objs.Count <= 1) return false;//立管连接的管线数目小于2，直接pass
            if (objs.Count != 2)
            {
                //不是两根线，不必进行连接
                return false;
            }

            var l1 = objs[0] as Line;
            var l2 = objs[1] as Line;

            Point3dEx pt1, pt2;
            //找出和立管的连接点
            if (l1.StartPoint.DistanceTo(pt) < tolerance)
            {
                pt1 = new Point3dEx(l1.StartPoint);
            }
            else
            {
                pt1 = new Point3dEx(l1.EndPoint);
            }
            if (l2.StartPoint.DistanceTo(pt) < tolerance)
            {
                pt2 = new Point3dEx(l2.StartPoint);
            }
            else
            {
                pt2 = new Point3dEx(l2.EndPoint);
            }
            //点集中不存在，就算了
            if (!fireHydrantSysIn.PtDic.ContainsKey(pt1) || !fireHydrantSysIn.PtDic.ContainsKey(pt2))
            {
                return false;
            }
            //点集中邻接点数大于1也算了
            if (fireHydrantSysIn.PtDic[pt1].Count != 1 || fireHydrantSysIn.PtDic[pt2].Count != 1)
            {
                return false;
            }
            lines.Add(new Line(pt1._pt, pt2._pt));
            if(isMiddleRiser)
            {
                fireHydrantSysIn.TermPtDic.Add(pt1);
            }
            return true;
        }

        public static Point3d Cloned(this Point3d pt)
        {
            return new Point3d(pt.X, pt.Y, 0);
        }

        public static string GetFloor(this Point3d pt, Dictionary<string, Polyline> floorRect)
        {
            foreach(var f in floorRect.Keys)
            {
                if(floorRect[f].Contains(pt))
                {
                    return f;
                }
            }
            return "";
        }

        public static int GetFloorInt(this Point3d pt, Dictionary<string, Polyline> floorRect)
        {
            if(floorRect.Count == 0)
            {
                return 0;
            }
            var floorStr = pt.GetFloor(floorRect);
            try
            {
                return Convert.ToInt32(floorStr.Last().ToString());
            }
            catch (Exception ex)
            {
            }
            return 999;

        }

        public static bool IsOnLine(this Point3d pt, Line line, double tolerance = 10)
        {
            if (line is null)
            {
                return false;
            }
            if (line.GetClosestPointTo(pt, false).DistanceTo(pt) < tolerance)//点在线的内部
            {
                return true;
            }
            else//点在线的外部
            {
                if (line.StartPoint.DistanceTo(pt) < tolerance || line.StartPoint.DistanceTo(pt) < tolerance)
                {
                    return true;
                }
            }
            return false;
        }

        public static Line GetClosestLine(this Point3d pt, List<Line> lines, double tolerance = 10)
        {
            foreach(var line in lines)
            {
                if(pt.IsOnLine(line))
                {
                    return line;
                }
            }
            return new Line();
        }

        public static Point3d GetMidPt(Point3d pt1, Point3d pt2)
        {
            double x = (pt1.X + pt2.X) / 2;
            double y = (pt1.Y + pt2.Y) / 2;
            return new Point3d(x, y, 0);
        }

        public static Point3d OffsetXReverse(this Point3d pt, double x)
        {
            return new Point3d(pt.X - x, pt.Y, 0);
        }


        public static Point3d OffsetXReverseY(this Point3d pt, double x, double y)
        {
            return new Point3d(pt.X - x, pt.Y + y, 0);

        }
    }
}
