using System;
using System.Linq;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.ConnectPipe.Service
{
    public static class GeUtils
    {
        /// <summary>
        /// 上下buffer Polyline
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="distance"></param>
        public static Polyline BufferPoly(this Polyline polyline, double distance)
        {
            if (polyline.EndPoint.DistanceTo(polyline.StartPoint) < distance)
            {
                return polyline;
            }
            List<Point2d> pts = new List<Point2d>();
            var newPoly = polyline.ExtendPolyline(150);
            //newPoly.ReverseCurve();
            var polyUp = newPoly.GetOffsetCurves(distance)[0] as Polyline;
            for (int i = 0; i < polyUp.NumberOfVertices; i++)
            {
                pts.Add(polyUp.GetPoint2dAt(i));
            }

            var polyDown = newPoly.GetOffsetCurves(-distance)[0] as Polyline;
            for (int i = polyDown.NumberOfVertices - 1; i >= 0; i--)
            {
                pts.Add(polyDown.GetPoint2dAt(i));
            }

            Polyline resPoly = new Polyline() { Closed = true };
            for (int i = 0; i < pts.Count; i++)
            {
                resPoly.AddVertexAt(i, pts[i], 0, 0, 0);
            }

            return resPoly;
        }

        /// <summary>
        /// Poly只能有线段构成
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Polyline ExtendPolyline(this Polyline poly, double length = 5.0)
        {
            var sp = poly.GetPoint3dAt(0);
            var spNext = poly.GetPoint3dAt(1);

            Vector3d spVec = sp.GetVectorTo(spNext).GetNormal();
            var startExendPt = sp - spVec.MultiplyBy(length);

            var ep = poly.GetPoint3dAt(poly.NumberOfVertices - 1);
            var epPrev = poly.GetPoint3dAt(poly.NumberOfVertices - 2);

            Vector3d epVec = ep.GetVectorTo(epPrev).GetNormal();
            var endExendPt = ep - epVec.MultiplyBy(length);

            Polyline newPoly = new Polyline();
            newPoly.AddVertexAt(0, startExendPt.ToPoint2D(), 0, 0, 0);
            for (int i = 1; i < poly.NumberOfVertices - 1; i++)
            {
                newPoly.AddVertexAt(i, poly.GetPoint3dAt(i).ToPoint2D(), 0, 0, 0);
            }
            newPoly.AddVertexAt(poly.NumberOfVertices - 1, endExendPt.ToPoint2D(), 0, 0, 0);

            return newPoly;
        }

        /// <summary>
        /// 指定方向排序点
        /// </summary>
        /// <param name="points"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static List<Point3d> OrderPoints(List<Point3d> points, Vector3d dir)
        {
            var xDir = dir;
            var zDir = Vector3d.ZAxis;
            var yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0
            });

            return points.OrderBy(x => x.TransformBy(matrix).X).ToList();
        }

        /// <summary>
        /// 按起始点开始排序
        /// </summary>
        /// <param name="points"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static List<Point3d> OrderPoints(List<Point3d> points, Point3d sPt)
        {
            List<Point3d> resPts = new List<Point3d>();
            while (points.Count > 0)
            {
                var resPt = points.OrderBy(x => x.DistanceTo(sPt)).First();
                resPts.Add(resPt);
                points.Remove(resPt);
                sPt = resPt;
            }

            return resPts;
        }

        /// <summary>
        /// 计算一点在另一点上指定方向的投影距离
        /// </summary>
        /// <param name="sPt"></param>
        /// <param name="otherPt"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static double GetDistanceByDir(Point3d sPt, Point3d otherPt, Vector3d dir, out Point3d resPt)
        {
            Ray ray = new Ray();
            ray.BasePoint = sPt;
            ray.UnitDir = dir;
            resPt = ray.GetClosestPointTo(otherPt, true);
            return resPt.DistanceTo(otherPt);
        }

        /// <summary>
        /// 找到不重复点位
        /// </summary>
        /// <param name="fingdingPoly"></param>
        /// <returns></returns>
        public static List<Point3d> FindingPolyPoints(List<Polyline> fingdingPoly)
        {
            List<Point3d> sPts = new List<Point3d>();
            foreach (var poly in fingdingPoly)
            {
                if (sPts.Where(x => x.IsEqualTo(poly.StartPoint, new Tolerance(1, 1))).Count() <= 0)
                {
                    sPts.Add(poly.StartPoint);
                }

                if (sPts.Where(x => x.IsEqualTo(poly.EndPoint, new Tolerance(1, 1))).Count() <= 0)
                {
                    sPts.Add(poly.EndPoint);
                }
            }

            return sPts;
        }

        /// <summary>
        /// 计算两根平行线的距离
        /// </summary>
        /// <param name="firLine"></param>
        /// <param name="secLine"></param>
        /// <returns></returns>
        public static double CalParallelLineDistance(Line firLine, Line secLine)
        {
            var xDir = (firLine.EndPoint - firLine.StartPoint).GetNormal();
            var zDir = Vector3d.ZAxis;
            var yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0
            });

            return Math.Abs(firLine.StartPoint.TransformBy(matrix.Inverse()).Y - secLine.StartPoint.TransformBy(matrix.Inverse()).Y);
        }

        /// <summary>
        /// 找到polyline中的最长线
        /// </summary>
        /// <param name="connectPolys"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, Line> CalLongestLineByPoly(List<Polyline> connectPolys)
        {
            Dictionary<Polyline, Line> polyDic = new Dictionary<Polyline, Line>();
            foreach (var poly in connectPolys)
            {
                var handleLine = HandleConnectPolys(poly);
                List<Line> lines = new List<Line>();
                for (int i = 0; i < handleLine.NumberOfVertices - 1; i++)
                {
                    lines.Add(new Line(handleLine.GetPoint3dAt(i), handleLine.GetPoint3dAt(i + 1)));
                }
                var longestLine = lines.OrderByDescending(x => x.Length).First();
                polyDic.Add(poly, longestLine);
            }

            return polyDic;
        }

        /// <summary>
        /// 找到polyline中的最长线
        /// </summary>
        /// <param name="connectPolys"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, Line> CalIntersectPtPoly(List<Polyline> connectPolys, Point3d intersectPt)
        {
            Dictionary<Polyline, Line> polyDic = new Dictionary<Polyline, Line>();
            foreach (var poly in connectPolys)
            {
                var handleLine = HandleConnectPolys(poly);
                List<Line> lines = new List<Line>();
                for (int i = 0; i < handleLine.NumberOfVertices - 1; i++)
                {
                    lines.Add(new Line(handleLine.GetPoint3dAt(i), handleLine.GetPoint3dAt(i + 1)));
                }
                var longestLine = lines.OrderBy(x => x.GetClosestPointTo(intersectPt, false).DistanceTo(intersectPt)).First();
                polyDic.Add(poly, longestLine);
            }

            return polyDic;
        }

        /// <summary>
        /// 判断点是否在线上
        /// </summary>
        /// <param name="line"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static bool IsPointOnLine(Line line, Point3d pt)
        {
            return line.IsPointOnLine(pt, false, 1);
        }

        /// <summary>
        /// 去除连接线上多余点
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static Polyline HandleConnectPolys(Polyline polyline)
        {
            if (polyline.NumberOfVertices <= 2)
            {
                return polyline;
            }

            List<Point3d> allPts = new List<Point3d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                if (!allPts.Any(x => x.IsEqualTo(polyline.GetPoint3dAt(i), new Tolerance(1, 1))))
                {
                    allPts.Add(polyline.GetPoint3dAt(i));
                }
            }

            List<Point3d> pts = new List<Point3d>() { allPts.First() };
            for (int i = 1; i < allPts.Count - 1; i++)
            {
                Line line = new Line(allPts[i - 1], allPts[i + 1]);
                if (!IsPointOnLine(line, allPts[i]))
                {
                    pts.Add(allPts[i]);
                }
            }
            pts.Add(allPts.Last());

            Polyline resPoly = new Polyline();
            for (int i = 0; i < pts.Count; i++)
            {
                resPoly.AddVertexAt(i, pts[i].ToPoint2D(), 0, 0, 0);
            }

            return resPoly;
        }
    }
}
