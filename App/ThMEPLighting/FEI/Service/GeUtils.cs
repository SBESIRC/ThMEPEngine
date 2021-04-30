using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.FEI.Service
{
    public static class GeUtils
    {
        /// <summary>
        /// 获取最近车道线
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="startPt"></param>
        /// <returns></returns>
        public static KeyValuePair<Line, Point3d> GetClosetLane(List<Line> lanes, Point3d startPt)
        {
            var lanePtInfo = lanes.ToDictionary(x => x, y => y.GetClosestPointTo(startPt, false))
                .OrderBy(x => x.Value.DistanceTo(startPt))
                .First();

            return lanePtInfo;
        }

        /// <summary>
        /// polyline转换成line
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Line> PLineToLine(Polyline polyline)
        {
            List<Line> resLines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                resLines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt(i + 1)));
            }

            if (polyline.Closed)
            {
                resLines.Add(new Line(polyline.GetPoint3dAt(polyline.NumberOfVertices - 1), polyline.GetPoint3dAt(0)));
            }

            return resLines;
        }

        /// <summary>
        /// 判断一条线在另一条线段上的直线上的投影是否有重叠部分
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="otherLins"></param>
        public static Line LineOverlap(List<Line> lines, List<Line> otherLins)
        {
            var xDir = (lines.First().EndPoint - lines.First().StartPoint).GetNormal();
            var zDir = Vector3d.ZAxis;
            var yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(new double[] {
                xDir.X, yDir.X, zDir.X, 0,
                xDir.Y, yDir.Y, zDir.Y, 0,
                xDir.Z, yDir.Z, zDir.Z, 0,
                0.0, 0.0, 0.0, 1.0,
            });

            List<Line> transLines = lines.Select(x => x.Clone() as Line).ToList();
            transLines.ForEach(x => x.TransformBy(matrix));
            List<Line> otherTransLines = otherLins.Select(x => x.Clone() as Line).ToList();
            otherTransLines.ForEach(x => x.TransformBy(matrix));
            var linePts = transLines.SelectMany(x => new List<Point3d>() { x.EndPoint, x.StartPoint }).ToList();
            var otherLinePts = otherTransLines.SelectMany(x => new List<Point3d>() { x.EndPoint, x.StartPoint }).ToList();

            var minLineX = linePts.OrderBy(x => x.X).First().X;
            var maxLineX = linePts.OrderBy(x => x.X).Last().X;
            var minOtherLineX = otherLinePts.OrderBy(x => x.X).First().X;
            var maxOtherLineX = otherLinePts.OrderBy(x => x.X).Last().X;
            double y = linePts.First().Y;

            if (maxOtherLineX < minLineX || maxLineX < minOtherLineX)
            {
                return null;
            }
            else if (maxLineX < maxOtherLineX)
            {
                return new Line(new Point3d(minOtherLineX, y, 0).TransformBy(matrix.Inverse()), new Point3d(maxLineX, y, 0).TransformBy(matrix.Inverse()));
            }
            else
            {
                return new Line(new Point3d(minLineX, y, 0).TransformBy(matrix.Inverse()), new Point3d(maxOtherLineX, y, 0).TransformBy(matrix.Inverse()));
            }
        }

        /// <summary>
        /// 找到一个点从特定方向到线上是否有交点
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="dir"></param>
        /// <param name="line"></param>
        public static bool GetIntersectPtByDir(Point3d pt, Vector3d dir, Line line, out Point3dCollection intersectPts)
        {
            intersectPts = new Point3dCollection();
            Ray ray = new Ray();
            ray.BasePoint = pt;
            ray.UnitDir = dir;
            ray.IntersectWith(line, Intersect.OnBothOperands, intersectPts, (IntPtr)0, (IntPtr)0);

            return intersectPts.Count > 0;
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

        /// <summary>
        /// 将线noded打断
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static List<Line> GetNodedLines(List<Line> lines)
        {
            lines = lines.Select(y =>
            {
                var dir = (y.EndPoint - y.StartPoint).GetNormal();
                return new Line(y.StartPoint - dir * 1, y.EndPoint + dir * 1);
            }).ToList();
            var objs = new DBObjectCollection();
            lines.ForEach(x => objs.Add(x));
            var nodeGeo = objs.ToNTSNodedLineStrings();
            var handleLines = new List<Line>();
            if (nodeGeo != null)
            {
                handleLines = nodeGeo.ToDbObjects()
                .SelectMany(x =>
                {
                    DBObjectCollection entitySet = new DBObjectCollection();
                    (x as Polyline).Explode(entitySet);
                    return entitySet.Cast<Line>().ToList();
                })
                .Where(x => x.Length > 3)
                .ToList();
            }

            return handleLines;
        }

        /// <summary>
        /// 判断一根线是否和一堆线中有线有重合部分
        /// </summary>
        /// <param name="line"></param>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static bool IsHasOverlapInList(Line line, List<Line> lines, double tol)
        {
            foreach (var checkLine in lines)
            {
                if (line.IsOverlapByTol(checkLine, tol))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断两根线是否有共线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="otherLine"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static bool IsOverlapByTol(this Line line, Line otherLine, double tol)
        {
            if (line.Length < tol || otherLine.Length < tol)
            {
                return false;
            }

            Vector3d dir = (line.EndPoint - line.StartPoint).GetNormal();
            Vector3d otherDir = (otherLine.EndPoint - otherLine.StartPoint).GetNormal();
            if (!dir.IsEqualTo(otherDir, new Tolerance(0.0001, 0.0001)))
            {
                return false;
            }

            Line newLine = new Line(line.StartPoint + dir * tol, line.EndPoint - dir * tol);
            Polyline poly = newLine.Buffer(tol);

            return poly.IsIntersects(otherLine);
        }
    }
}
