using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPElectrical.Broadcast.Service
{
    public class StructureService
    {
        readonly double lengthTol = 800;

        /// <summary>
        /// 获取停车线周边构建信息
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public List<Polyline> GetStruct(List<Line> lines, List<Polyline> polys, double tol)
        {
            var polyCollection = polys.ToCollection();
            var resPolys = lines.SelectMany(x =>
            {
                var linePoly = expandLine(x, tol);
                return linePoly.Intersection(polyCollection).Cast<Polyline>().ToList();
            }).ToList();

            return resPolys;
        }

        /// <summary>
        /// 沿着线将柱分隔成上下两部分
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public List<List<Polyline>> SeparateColumnsByLine(List<Polyline> polyline, Line line)
        {
            Vector3d xDir = CalLineDirection(line);
            Vector3d yDir = Vector3d.ZAxis.CrossProduct(xDir);
            Vector3d zDir = Vector3d.ZAxis;
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, line.StartPoint.X,
                    xDir.Y, yDir.Y, zDir.Y, line.StartPoint.Y,
                    xDir.Z, yDir.Z, zDir.Z, line.StartPoint.Z,
                    0.0, 0.0, 0.0, 1.0
                });

            List<Polyline> upPolyline = new List<Polyline>();
            List<Polyline> downPolyline = new List<Polyline>();
            foreach (var poly in polyline)
            {
                var transPt = GetStructCenter(poly).TransformBy(matrix.Inverse());
                if (transPt.Y < 0)
                {
                    downPolyline.Add(poly);
                }
                else
                {
                    upPolyline.Add(poly);
                }
            }

            return new List<List<Polyline>>() { upPolyline, downPolyline };
        }

        /// <summary>
        /// 让线指向x或者y轴正方向
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private Vector3d CalLineDirection(Line line)
        {
            Vector3d lineDir = (line.EndPoint - line.StartPoint).GetNormal();

            double xDotValue = Vector3d.XAxis.DotProduct(lineDir);
            double yDotValue = Vector3d.YAxis.DotProduct(lineDir);
            if (Math.Abs(xDotValue) > Math.Abs(yDotValue))
            {
                return xDotValue > 0 ? lineDir : -lineDir;
            }
            else
            {
                return yDotValue > 0 ? lineDir : -lineDir;
            }
        }

        /// <summary>
        /// 大概计算一下构建中点
        /// </summary>
        /// <param name="colums"></param>
        /// <returns></returns>
        private Point3d GetStructCenter(Polyline polyline)
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
        /// 扩张line成polyline
        /// </summary>
        /// <param name="line"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private Polyline expandLine(Line line, double distance)
        {
            Vector3d lineDir = line.Delta.GetNormal();
            Vector3d moveDir = Vector3d.ZAxis.CrossProduct(lineDir);
            Point3d p1 = line.StartPoint + lineDir * lengthTol + moveDir * distance;
            Point3d p2 = line.EndPoint - lineDir * lengthTol + moveDir * distance;
            Point3d p3 = line.EndPoint - lineDir * lengthTol - moveDir * distance;
            Point3d p4 = line.StartPoint + lineDir * lengthTol - moveDir * distance;

            Polyline polyline = new Polyline() { Closed = true };
            polyline.AddVertexAt(0, p1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p3.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, p4.ToPoint2D(), 0, 0, 0);
            return polyline;
        }
    }
}
