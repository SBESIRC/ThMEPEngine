using System;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using System.Collections.Generic;
using ThMEPEngineCore.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.EmgLight.Common
{
    class GeomUtils
    {
        /// <summary>
        /// 扩张line成polyline, 以线方向为准
        /// P1-----------------P2
        /// |      left        |
        /// |down [s====>e] up |
        /// |      right       |
        /// P4-----------------P3
        /// </summary>
        /// <param name="line"></param>
        /// <param name="left"></param>
        /// <param name="up"></param>
        /// <param name="right"></param>
        /// <param name="down"></param>
        /// <returns></returns>
        public static Polyline ExpandLine(Line line, double left, double up, double right, double down)
        {
            Vector3d lineDir = line.Delta.GetNormal();
            Vector3d moveDir = Vector3d.ZAxis.CrossProduct(lineDir);

            //向前延伸
            Point3d p1 = line.StartPoint - lineDir * down + moveDir * left;
            Point3d p2 = line.EndPoint + lineDir * up + moveDir * left;
            Point3d p3 = line.EndPoint + lineDir * up - moveDir * right;
            Point3d p4 = line.StartPoint - lineDir * down - moveDir * right;

            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, p1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p3.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p4.ToPoint2D(), 0, 0, 0);

            return polyline;
        }

        /// <summary>
        /// 大概计算一下构建中点
        /// </summary>
        /// <param name="colums"></param>
        /// <returns></returns>
        public static Point3d GetStructCenter(Polyline polyline)
        {
            List<Point3d> points = new List<Point3d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                points.Add(polyline.GetPoint3dAt(i));
            }

            double maxX = points.Max(x => x.X);
            double minX = points.Min(x => x.X);
            double maxY = points.Max(x => x.Y);
            double minY = points.Min(x => x.Y);

            return new Point3d((maxX + minX) / 2, (maxY + minY) / 2, 0);
        }

        /// <summary>
        /// 已pt点为中心做一个方框
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="moveDir"></param>
        /// <param name="tolX"></param>
        /// <param name="tolY"></param>
        /// <returns></returns>
        public static Polyline CreateExtendPoly(Point3d pt, Vector3d moveDir, int tolX, int tolY)
        {
            moveDir = moveDir.GetNormal();
            var ExtendPolyStart = pt - moveDir * tolX;
            var ExtendPolyEnd = pt + moveDir * tolX;

            var ExtendLine = new Line(ExtendPolyStart, ExtendPolyEnd);
            var ExtendPoly = GeomUtils.ExpandLine(ExtendLine, tolY, 0, tolY, 0);

            DrawUtils.ShowGeometry(ExtendPoly, EmgLightCommon.LayerExtendPoly, 44);

            return ExtendPoly;
        }

        public static Matrix3d getLineMatrix(Point3d startPt, Point3d endPt)
        {
            var dir = (endPt - startPt).GetNormal();
            //getAngleTo根据右手定则旋转(一般逆时针)
            var rotationangle = Vector3d.XAxis.GetAngleTo(dir, Vector3d.ZAxis);
            var matrix = Matrix3d.Displacement(startPt.GetAsVector()) * Matrix3d.Rotation(rotationangle, Vector3d.ZAxis, new Point3d(0, 0, 0));
            return matrix;
        }

        public static Dictionary<Point3d, Point3d> orderLineListPts(List<Line> lineList, Matrix3d lineMatrix)
        {
            var tol = new Tolerance(10, 10);
            var pts = new List<Point3d>();
            var orderLinePts = new Dictionary<Point3d, Point3d>();

            foreach (var l in lineList)
            {
                var notInList = pts.Where(x => x.IsEqualTo(l.StartPoint, tol));
                if (notInList.Count() == 0)
                {
                    pts.Add(l.StartPoint);
                }
                notInList = pts.Where(x => x.IsEqualTo(l.EndPoint, tol));
                if (notInList.Count() == 0)
                {
                    pts.Add(l.EndPoint);
                }
            }

             orderLinePts = pts.ToDictionary(x => x, x => x.TransformBy(lineMatrix.Inverse())).OrderBy(x => x.Value.X).ToDictionary(x => x.Key, x => x.Value);

            return orderLinePts;
        }
    }
}
