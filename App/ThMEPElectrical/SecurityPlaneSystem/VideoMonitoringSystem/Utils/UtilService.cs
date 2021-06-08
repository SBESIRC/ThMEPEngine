using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.LaneLine;

namespace ThMEPElectrical.VideoMonitoringSystem.Utls
{
    public static class UtilService
    {
        /// <summary>
        /// 获取polyline所有line
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Line> GetAllLinesInPolyline(this Polyline polyline)
        {
            List<Line> lines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                var line = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices));
                if (line.Length > 5)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        /// <summary>
        /// 计算矩形中心线
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static Point3d GetRectangleCenterPt(this Polyline rectangle)
        {
            var pt1 = rectangle.GetPoint3dAt(0);
            var pt2 = rectangle.GetPoint3dAt(2);
            var centerPt = new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);

            return centerPt;
        }

        /// <summary>
        /// 根据交点打断线
        /// </summary>
        /// <param name="handleLines"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Line> GetNodedLines(List<Line> handleLines, Polyline polyline)
        {
            var parkingLinesService = new ParkingLinesService();
            var parkingLines = parkingLinesService.CreateNodedParkingLines(polyline, handleLines, out List<List<Line>> otherPLines);
            parkingLines.AddRange(otherPLines);

            return parkingLines.SelectMany(x => x).ToList();
        }

        /// <summary>
        /// 判断两根线是否相等
        /// </summary>
        /// <param name="line"></param>
        /// <param name="otherLine"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static bool CheckLineIsEqual(this Line line, Line otherLine, Tolerance tol)
        {
            return (line.StartPoint.IsEqualTo(otherLine.StartPoint, tol) && line.EndPoint.IsEqualTo(otherLine.EndPoint, tol))
                || (line.EndPoint.IsEqualTo(otherLine.StartPoint, tol) && line.StartPoint.IsEqualTo(otherLine.EndPoint, tol));
        }

        /// <summary>
        /// 获取所有点
        /// </summary>
        /// <param name="lines"></param>
        public static List<Point3d> GetAllPoints(this List<Line> lines)
        {
            List<Point3d> allPts = new List<Point3d>();
            foreach (var line in lines)
            {
                if (!allPts.Any(x=>x.IsEqualTo(line.StartPoint, new Tolerance(1, 1))))
                {
                    allPts.Add(line.StartPoint);
                }

                if (!allPts.Any(x => x.IsEqualTo(line.EndPoint, new Tolerance(1, 1))))
                {
                    allPts.Add(line.EndPoint);
                }
            }

            return allPts;
        }

        /// <summary>
        /// 判断点是否在线上
        /// </summary>
        /// <param name="line"></param>
        /// <param name="pt"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static bool CheckPointIsOnLine(Line line, Point3d pt, double tol)
        {
            var closetPt = line.GetClosestPointTo(pt, false);
            return closetPt.DistanceTo(pt) < tol;
        }
    }
}
