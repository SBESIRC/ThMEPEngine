using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.LaneLine;

namespace ThMEPElectrical.SecurityPlaneSystem.Utls
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

        /// <summary>
        /// 判断两个向量是否在容差范围内平行
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="otherDir"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static bool IsParallelWithTolerance(this Vector3d dir, Vector3d otherDir, double angle)
        {
            if (dir.DotProduct(otherDir) < 0)
            {
                otherDir = -otherDir;
            }

            double dirAngle = dir.GetAngleTo(otherDir);
            if (dirAngle > Math.PI)
            {
                dirAngle = Math.PI * 2 - dirAngle;
            }

            var checkAngele = dirAngle / Math.PI * 180;
            if (checkAngele<= angle)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 找到一个点在车道线上的投影点
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static Point3d GetProjectPtOnLane(List<Line> lane, Point3d pt)
        {
            return lane.Select(x => x.GetClosestPointTo(pt, false)).OrderBy(x => x.DistanceTo(pt)).First();
        }

        /// <summary>
        /// 根据某个方向排序点
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static List<Point3d> OrderPoints(List<Point3d> pts, Vector3d dir)
        {
            var xDir = dir;
            var yDir = Vector3d.ZAxis.CrossProduct(xDir);
            var zDir = Vector3d.ZAxis;
            Matrix3d matrix = new Matrix3d(new double[]{
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0});

            var orderPts = pts.Select(x => x.TransformBy(matrix.Inverse())).OrderBy(x => x.X).ToList();
            return orderPts.Select(x => x.TransformBy(matrix)).ToList();
        }

        /// <summary>
        /// 计算控制器布置点位
        /// </summary>
        /// <param name="structs"></param>
        /// <param name="dir"></param>
        /// <param name="doorPt"></param>
        /// <returns></returns>
        public static Dictionary<Line, Point3d> CalLayoutInfo(List<Polyline> structs, Vector3d dir, Point3d doorPt, Polyline door, double angle, double blockWidth, bool intersect = false)
        {
            Dictionary<Line, Point3d> resLayoutInfo = new Dictionary<Line, Point3d>();
            foreach (var str in structs)
            {
                var allLines = str.GetAllLinesInPolyline().ToList();
                var bufferDoor = door.Buffer(10)[0] as Polyline;
                foreach (var line in allLines)
                {
                    if (intersect)
                    {
                        if (!bufferDoor.Intersects(line))
                        {
                            continue;
                        }
                    }
                    var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                    if (!dir.IsParallelWithTolerance(lineDir, angle) && line.Length > blockWidth)
                    {
                        var pt = line.StartPoint.DistanceTo(doorPt) < line.EndPoint.DistanceTo(doorPt) ? line.StartPoint : line.EndPoint;
                        var checkDir = (pt - doorPt).GetNormal();
                        if (checkDir.DotProduct(lineDir) < 0)
                        {
                            lineDir = -lineDir;
                        }

                        var layoutPt = pt + lineDir * (blockWidth / 2);
                        resLayoutInfo.Add(line, layoutPt);
                    }
                }
            }

            return resLayoutInfo.OrderBy(x => x.Value.DistanceTo(doorPt)).ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// 扩张line成polyline
        /// </summary>
        /// <param name="line"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Polyline ExpandLine(Line line, double distance, double tol = 0)
        {
            Vector3d lineDir = line.Delta.GetNormal();
            Vector3d moveDir = Vector3d.ZAxis.CrossProduct(lineDir);
            Point3d p1 = line.StartPoint + lineDir * tol + moveDir * distance;
            Point3d p2 = line.EndPoint - lineDir * tol + moveDir * distance;
            Point3d p3 = line.EndPoint - lineDir * tol - moveDir * distance;
            Point3d p4 = line.StartPoint + lineDir * tol - moveDir * distance;

            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, p1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p3.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p4.ToPoint2D(), 0, 0, 0);
            return polyline;
        }
    }
}
