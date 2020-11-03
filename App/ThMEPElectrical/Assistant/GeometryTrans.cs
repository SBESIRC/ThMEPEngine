using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Geometry;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Assistant
{
    public static class GeometryTrans
    {
        public static List<Curve> Polylines2Curves(this List<Polyline> srcPolylines)
        {
            if (srcPolylines == null || srcPolylines.Count == 0)
                return null;
            var curves = new List<Curve>();

            foreach (var polyline in srcPolylines)
            {
                curves.Add(polyline);
            }

            return curves;
        }

        public static List<Point2d> Polyline2Point2d(this Polyline poly)
        {
            var ptLst = new List<Point2d>();
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                var pt = poly.GetPoint2dAt(i);
                ptLst.Add(pt);
            }

            var firstPt = ptLst.First();
            var lastPt = ptLst.Last();
            if (GeomUtils.Point2dIsEqualPoint2d(firstPt, lastPt))
                return ptLst;

            ptLst.Add(lastPt);
            return ptLst;
        }

        public static Point3dCollection Points2PointCollection(this List<Point2d> pts)
        {
            var ptCollection = new Point3dCollection();
            foreach (var pt in pts)
            {
                ptCollection.Add(pt.Point3D());
            }

            return ptCollection;
        }

        public static List<Point2d> RemoveNearSamePoints(this Polyline poly)
        {
            var ptLst = new List<Point2d>();
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                var pt = poly.GetPoint2dAt(i);
                if (!ptLst.Contains(pt))
                    ptLst.Add(pt);
            }

            var firstPt = ptLst.First();
            var lastPt = ptLst.Last();
            if (GeomUtils.Point2dIsEqualPoint2d(firstPt, lastPt))
                return ptLst;

            ptLst.Add(lastPt);
            return ptLst;
        }

        public static List<Curve> PlaceInputDatas2Curves(List<PlaceInputProfileData> inputProfileDatas)
        {
            var curves = new List<Curve>();
            inputProfileDatas.ForEach(e => curves.Add(e.MainBeamOuterProfile));
            return curves;
        }


        public static Polyline PolylineWithNoSamePoints(this Polyline poly)
        {
            return poly.RemoveNearSamePoints().Points2PointCollection().ToPolyline();
        }

        public static List<Point3d> Pt2stoPt3ds(this List<Point2d> pt2dS)
        {
            if (pt2dS == null || pt2dS.Count == 0)
                return null;

            var ptS = new List<Point3d>();
            pt2dS.ForEach(pt => ptS.Add(pt.Point3D()));

            return ptS;
        }

        public static Point2d Point2D(this Point3d pt)
        {
            return new Point2d(pt.X, pt.Y);
        }

        public static Point3d Point3D(this Point2d pt)
        {
            return new Point3d(pt.X, pt.Y, 0);
        }

        public static Line Line3dLine(this LineSegment3d line3d)
        {
            return new Line(line3d.StartPoint, line3d.EndPoint);
        }

        /// <summary>
        /// 矩阵转换
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="matrixs"></param>
        /// <returns></returns>
        public static Polyline TransByMatrix(Polyline poly, List<Matrix3d> matrixs)
        {
            if (matrixs == null || matrixs.Count == 0)
                return poly;

            var resPoly = poly;
            for (int i = 0; i < matrixs.Count; i++)
            {
                resPoly = resPoly.GetTransformedCopy(matrixs[i]) as Polyline;
            }

            return resPoly;
        }

        public static List<Point3d> TransByMatrix(List<Point3d> srcPts, List<Matrix3d> matrixs)
        {
            if (matrixs == null || matrixs.Count == 0)
                return srcPts;

            if (srcPts == null || srcPts.Count == 0)
                return null;

            var pts = new List<Point3d>();

            foreach (var pt in srcPts)
            {
                var transPt = TransByMatrix(pt, matrixs);
                pts.Add(transPt);
            }

            return pts;
        }

        public static Point3d TransByMatrix(Point3d srcPt, List<Matrix3d> matrixs)
        {
            if (matrixs == null || matrixs.Count == 0)
                return srcPt;

            var pt = srcPt;
            for (int i = 0; i < matrixs.Count; i++)
            {
                pt = pt.TransformBy(matrixs[i]);
            }

            return pt;
        }

        public static List<Point3d> ToPointList(this Point3dCollection ptCollection)
        {
            var ptLst = new List<Point3d>();

            foreach (Point3d pt in ptCollection)
            {
                ptLst.Add(pt);
            }

            return ptLst;
        }

        public static Polyline ToPolyline(this Point3dCollection ptCollection)
        {
            var poly = new Polyline();
            poly.Closed = true;
            poly.CreatePolyline(ptCollection);
            return poly;
        }

        public static Point3dCollection PointsTransform(this Point3dCollection pts, Matrix3d matrix)
        {
            var resPts = new Point3dCollection();
            for (int i = 0; i < pts.Count; i++)
            {
                resPts.Add(pts[i].TransformBy(matrix));
            }

            return resPts;
        }

        public static List<Curve> MatrixSystemCurves(Matrix3d matrix, double length)
        {
            var coordinateSystem = matrix.CoordinateSystem3d;
            var origin = coordinateSystem.Origin;
            var x = coordinateSystem.Xaxis;
            var y = coordinateSystem.Yaxis;
            var xEnd = origin + x * length;
            var yEnd = origin + y * length;
            var lineX = new Line(origin, xEnd);
            var lineY = new Line(origin, yEnd);

            var curves = new List<Curve>() { lineX, lineY };
            return curves;
        }

        public static Polyline GenerateSmallInValidPoly()
        {
            var pts = new Point3dCollection()
            {
                new Point3d(0, 0,0),
                new Point3d(100, 0, 0),
                new Point3d(100, 100, 0),
                new Point3d(0, 100, 0)
            };

            return pts.ToPolyline();
        }

        /// <summary>
        /// 根据点集，半径，绘制圆
        /// </summary>
        /// <param name="srcPts"></param>
        /// <param name="radius"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public static List<Circle> Points2Circles(List<Point3d> srcPts, double radius, Vector3d normal)
        {
            if (srcPts == null || srcPts.Count == 0)
                return null;

            var circles = new List<Circle>();
            foreach (var pt in srcPts)
            {
                var circle = new Circle(pt, normal, radius);
                circles.Add(circle);
            }

            return circles;
        }


        public static List<Curve> Circles2Curves(List<Circle> circles)
        {
            if (circles == null || circles.Count == 0)
                return null;

            var curves = new List<Curve>();

            foreach (var circle in circles)
            {
                curves.Add(circle);
            }

            return curves;
        }

        public static Polyline Points2Poly(List<Point3d> pts)
        {
            if (pts == null || pts.Count < 3)
                return null;

            var ptFirst = pts.First();
            var ptLast = pts.Last();

            if (GeomUtils.Point3dIsEqualPoint3d(ptFirst, ptLast))
                pts.Remove(ptLast);

            var poly = new Polyline()
            {
                Closed = true
            };

            for (int i = 0; i < pts.Count; i++)
            {
                poly.AddVertexAt(i, pts[i].ToPoint2D(), 0, 0, 0);
            }
            return poly;
        }
    }
}
