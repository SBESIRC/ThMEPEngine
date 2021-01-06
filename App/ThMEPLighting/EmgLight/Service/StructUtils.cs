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
        /// 扩张line成polyline
        /// </summary>
        /// <param name="line"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static Polyline ExpandLine(Line line, double up, double right,double down,double left)
        {
            Vector3d lineDir = line.Delta.GetNormal();
            Vector3d moveDir = Vector3d.ZAxis.CrossProduct(lineDir);
           
            //向前延伸
            Point3d p1 = line.StartPoint - lineDir * left + moveDir * up;
            Point3d p2 = line.EndPoint + lineDir* right + moveDir * up;
            Point3d p3 = line.EndPoint + lineDir * right - moveDir * down;
            Point3d p4 = line.StartPoint - lineDir* left - moveDir * down;

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
        /// 沿线方向单边buffer
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        //public static List<Polyline> createRecBuffer(List<Line> lines, double length)
        //{
        //    //var newLines = lines.Select(x => x.Normalize()).ToList();
        //    var newLines = lines;
        //    List<Polyline> linePolys = new List<Polyline>();

        //    foreach (var line in newLines)
        //    {

        //        var lineDir = (line.EndPoint - line.StartPoint).GetNormal();

        //        //不需要前后延伸
        //        //line.StartPoint = line.StartPoint - lineDir * length;
        //        //line.EndPoint = line.EndPoint + lineDir * length;

        //        //find single sided buffer direction
        //        var bufferLength = length;
        //        if (Math.Abs(lineDir.X) > Math.Abs(lineDir.Y))
        //        {
        //            if (lineDir.X < 0)
        //            {
        //                bufferLength = -bufferLength;
        //            }
        //        }
        //        else
        //        {
        //            if (lineDir.Y < 0)
        //            {
        //                bufferLength = -bufferLength;
        //            }
        //        }
        //        linePolys.AddRange(new DBObjectCollection() { line }.SingleSidedBuffer(bufferLength).Cast<Polyline>().ToList());
        //    }
        //    return linePolys;
        //}
        public static List<Polyline> createRecBuffer(List<Line> lines, double length)
        {
            //var newLines = lines.Select(x => x.Normalize()).ToList();
            var newLines = lines;
            List<Polyline> linePolys = new List<Polyline>();

            foreach (var line in newLines)
            {
                linePolys.Add (ExpandLine(line, length, 0, 0, 0));

            }
            return linePolys;
        }
    }
}
