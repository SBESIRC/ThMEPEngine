using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            List<Point2d> pts = new List<Point2d>();
            var polyUp = polyline.GetOffsetCurves(distance)[0] as Polyline;
            for (int i = 0; i < polyUp.NumberOfVertices; i++)
            {
                pts.Add(polyUp.GetPoint2dAt(i));
            }

            var polyDown = polyline.GetOffsetCurves(-distance)[0] as Polyline;
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
    }
}
