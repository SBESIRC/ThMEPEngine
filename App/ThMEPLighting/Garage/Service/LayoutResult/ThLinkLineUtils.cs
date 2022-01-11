using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public static class ThLinkLineUtils
    {
        public static List<List<Line>> GetCrosses(this List<Line> lines)
        {
            var centerSidesQuery = new ThLineRoadQueryService(lines);
            return centerSidesQuery.GetCross();
        }
        public static List<List<Line>> GetThreeWays(this List<Line> lines)
        {
            var centerSidesQuery = new ThLineRoadQueryService(lines);
            return centerSidesQuery.GetThreeWay();
        }
        public static bool IsLessThan45Degree(this Line first, Line second)
        {
            return ThGarageUtils.IsLessThan45Degree(
                first.StartPoint, first.EndPoint,
                second.StartPoint, second.EndPoint);
        }
        public static double GetLineOuterAngle(this Line first, Line second)
        {
            return ThGarageUtils.CalculateTwoLineOuterAngle(
                first.StartPoint, first.EndPoint, second.StartPoint, second.EndPoint);
        }
        public static Polyline CreatePolyline(this List<Point3d> pts, bool isClosed = true)
        {
            var newPts = new Point3dCollection();
            pts.ForEach(p => newPts.Add(p));
            return newPts.CreatePolyline(isClosed);
        }

        /// <summary>
        /// 创建平行四边形
        /// </summary>
        /// <param name="a">邻边a</param>
        /// <param name="b">邻边b</param>
        /// <returns></returns>
        public static Polyline CreateParallelogram(this Line a, Line b)
        {
            var pts = a.IntersectWithEx(b, Intersect.ExtendBoth);
            if (pts.Count == 0)
            {
                return new Polyline();
            }
            var first = pts[0];
            var second = first.GetNextLinkPt(a.StartPoint, a.EndPoint);
            var four = first.GetNextLinkPt(b.StartPoint, b.EndPoint);
            var vec1 = first.GetVectorTo(second);
            var vec2 = first.GetVectorTo(four);
            var third = first + vec1 + vec2;
            var points = new Point3dCollection() { first, second, third, four };
            return points.CreatePolyline();
        }

        public static bool IsCollinear(this Line first, Line second, double tolerance)
        {
            return ThGeometryTool.IsCollinearEx(
                first.StartPoint, first.EndPoint, second.StartPoint, second.EndPoint, tolerance);
        }

        public static List<Line> GroupSides(this Polyline partition, List<Line> sides)
        {
            return sides
                .Where(e => partition.IsContains(e.StartPoint) || partition.IsContains(e.EndPoint))
                .ToList();
        }

        public static Point3d GetMidPt(this Line line)
        {
            return line.StartPoint.GetMidPt(line.EndPoint);
        }

        public static List<Point3d> GetPoints(this Line line)
        {
            return new List<Point3d> { line.StartPoint, line.EndPoint };
        }
        public static Polyline ToPolyline(this List<Line> lines, Point3d startPt)
        {
            // lines是前后连续且互相相连的线，
            var path = lines.ToPolyline(ThGarageLightCommon.RepeatedPointDistance);
            bool isCloseStart = startPt.DistanceTo(path.StartPoint) <= ThGarageLightCommon.RepeatedPointDistance;
            bool isCloseEnd = startPt.DistanceTo(path.EndPoint) <= ThGarageLightCommon.RepeatedPointDistance;
            if (isCloseStart || isCloseEnd)
            {
                if (isCloseEnd)
                {
                    return path.Reverse();
                }
            }
            return path;
        }
        public static bool IsGeometryEqual(Point3d firstSp,Point3d firstEp,Point3d secondSp,Point3d secondEp,double tolerance= 1.0)
        {
            if(firstSp.IsEqualTo(secondSp,new Tolerance(tolerance, tolerance)) && 
                firstEp.IsEqualTo(secondEp, new Tolerance(tolerance, tolerance)))
            {
                return true;
            }
            if (firstSp.IsEqualTo(secondEp, new Tolerance(tolerance, tolerance)) &&
                firstEp.IsEqualTo(secondSp, new Tolerance(tolerance, tolerance)))
            {
                return true;
            }
            return false;
        }
    }
}
