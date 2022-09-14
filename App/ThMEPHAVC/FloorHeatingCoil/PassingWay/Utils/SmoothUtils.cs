using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public static class SmoothUtils
    {
        /// <summary>
        /// 去除多段线的重合点和边上的点
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static List<Point3d> SmoothPoints(List<Point3d> points, double eps = 2)
        {
            for (int i = points.Count - 1; i > 0; --i)
                if (points[i].DistanceTo(points[i - 1]) < eps) 
                    points.RemoveAt(i);
            for(int i = points.Count - 2; i > 0; --i)
            {
                if (PassageWayUtils.PointOnSegment(points[i], points[i + 1], points[i - 1], eps)) 
                    points.RemoveAt(i);
            }
            return points;
        }
        /// <summary>
        /// 去除多边形（首尾点不同）的重合点和边上的点
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static List<Point3d> SmoothPolygon(List<Point3d> points)
        {
            points.Add(points.First());
            points = SmoothPoints(points);
            points.RemoveAt(points.Count - 1);
            var cur = points[0];
            var next = points[1];
            var pre = points.Last();
            if (PassageWayUtils.PointOnSegment(cur, pre, next))
                points.RemoveAt(0);
            return points;
        }
        public static void RoundXY(ref List<Point3d> points, bool is_round = true, bool is_closed = true)
        {
            if (is_round)
            {
                for (int i = 0; i < points.Count; ++i)
                    points[i] = new Point3d((int)points[i].X, (int)points[i].Y, 0);
            }
            for(int i = 0; i < points.Count; ++i)
            {
                if (!is_closed && i == points.Count - 1) continue;
                var next = (i + 1) % points.Count;
                var dx = points[next].X - points[i].X;
                var dy = points[next].Y - points[i].Y;
                if (dx != 0 && dy != 0 && Math.Min(Math.Abs(dx), Math.Abs(dy)) < 10) 
                {
                    if (Math.Abs(dx) < Math.Abs(dy))
                        points[next] = new Point3d(points[i].X, points[next].Y, 0);
                    else
                        points[next] = new Point3d(points[next].X, points[i].Y, 0);
                }
            }
        }

        public static Polyline SmoothPolygonByRoundXY(Polyline polygon)
        {
            var points = PassageWayUtils.GetPolyPoints(polygon);
            RoundXY(ref points);
            points.Add(points.First());
            points = SmoothPoints(points);
            return PassageWayUtils.BuildPolyline(points);
        }
    }
}
