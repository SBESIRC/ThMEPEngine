using System;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.EmgLight.Service
{
    class StructUtils
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
    }
}
