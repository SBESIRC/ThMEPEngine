using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.Broadcast.Service
{
    public static class StructUtils
    {
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
