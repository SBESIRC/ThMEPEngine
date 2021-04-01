using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
