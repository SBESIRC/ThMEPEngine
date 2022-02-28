using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Service
{
    public static class GeometryUtils
    {
        /// <summary>
        /// 将长边外扩（或内缩）一定距离。tips：仅支持矩形
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="tol"></param>
        public static Polyline ExtendByLengthLine(this Polyline polyline, double tol)
        {
            var allLines = StructGeoService.GetAllLineByPolyline(polyline).OrderBy(x => x.Length).ToList();
            var firLine = allLines[0];
            var lastLine = allLines[1];
            var firExtendLine = ExtendLine(firLine, tol);
            var lastExtendLine = ExtendLine(lastLine, tol);

            Polyline resPoly = new Polyline() { Closed = true };
            resPoly.AddVertexAt(0, firExtendLine.StartPoint.ToPoint2D(), 0, 0, 0);
            resPoly.AddVertexAt(1, firExtendLine.EndPoint.ToPoint2D(), 0, 0, 0);
            resPoly.AddVertexAt(2, lastExtendLine.StartPoint.ToPoint2D(), 0, 0, 0);
            resPoly.AddVertexAt(3, lastExtendLine.EndPoint.ToPoint2D(), 0, 0, 0);
            return resPoly;
        }

        /// <summary>
        /// 两边延申线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static Line ExtendLine(Line line, double tol)
        {
            var dir = (line.EndPoint - line.StartPoint).GetNormal();
            var sPt = line.StartPoint - dir * tol;
            var ePt = line.EndPoint + dir * tol;
            return new Line(sPt, ePt);
        }

        /// <summary>
        /// 从集合中找到所有连接线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="line"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static List<Line> GetConenctLine(List<Line> lines, Line line, double tol = 1)
        {
            var resLines = new List<Line>();
            var allLines = new List<Line>(lines);
            var connectLines = lines.Where(x => x.StartPoint.DistanceTo(line.StartPoint) < tol ||
                x.StartPoint.DistanceTo(line.EndPoint) < tol ||
                x.EndPoint.DistanceTo(line.StartPoint) < tol ||
                x.EndPoint.DistanceTo(line.EndPoint) < tol).ToList();
            resLines.AddRange(connectLines);
            allLines = allLines.Except(connectLines).ToList();
            var resConnectLines = new List<Line>();
            foreach (var rLine in resLines)
            {
                resConnectLines.AddRange(GetConenctLine(allLines, rLine, tol));
            }
            resLines.AddRange(resConnectLines);
            return resLines;
        }

        /// <summary>
        /// 找到距离点最近的线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="startPt"></param>
        /// <returns></returns>
        public static KeyValuePair<Line, Point3d> GetClosetLine(List<Line> lines, Point3d startPt)
        {
            var lanePtInfo = lines.ToDictionary(x => x, y => y.GetClosestPointTo(startPt, false))
                .OrderBy(x => x.Value.DistanceTo(startPt))
                .First();

            return lanePtInfo;
        }

        /// <summary>
        /// 获取boundingbox
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static Polyline GetBoungdingBox(List<Point3d> points, Vector3d xDir)
        {
            Vector3d zDir = Vector3d.ZAxis;
            Vector3d yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(new double[] {
                xDir.X, xDir.Y, xDir.Z, 0,
                yDir.X, yDir.Y, yDir.Z, 0,
                zDir.X, zDir.Y, zDir.Z, 0,
                0.0, 0.0, 0.0, 1.0
            });
            points = points.Select(x => x.TransformBy(matrix.Inverse())).ToList();

            points = points.OrderBy(x => x.X).ToList();
            double minX = points.First().X;
            double maxX = points.Last().X;
            points = points.OrderBy(x => x.Y).ToList();
            double minY = points.First().Y;
            double maxY = points.Last().Y;

            Point2d pt1 = new Point2d(minX, minY);
            Point2d pt2 = new Point2d(minX, maxY);
            Point2d pt3 = new Point2d(maxX, maxY);
            Point2d pt4 = new Point2d(maxX, minY);
            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, pt1, 0, 0, 0);
            polyline.AddVertexAt(1, pt2, 0, 0, 0);
            polyline.AddVertexAt(2, pt3, 0, 0, 0);
            polyline.AddVertexAt(3, pt4, 0, 0, 0);
            polyline.TransformBy(matrix);

            return polyline;
        }

        /// <summary>
        /// 获取polyline所有点
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Point3d> GetAllPolylinePts(Polyline polyline)
        {
            List<Point3d> allPts = new List<Point3d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                allPts.Add(polyline.GetPoint3dAt(i));
            }

            return allPts;
        }
    }
}
