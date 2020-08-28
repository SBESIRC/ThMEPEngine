using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.Utils;

namespace ThMEPWSS.Bussiness
{
    public class GridService
    {
        public readonly double tol = 800;

        /// <summary>
        /// 构建柱轴网
        /// </summary>
        /// <param name="points"></param>
        /// <param name="xLength"></param>
        /// <param name="yLength"></param>
        public List<KeyValuePair<Vector3d, List<Polyline>>> CreateGrid(Polyline polyline, List<Polyline> colums)
        {
            List<Point3d> points = GetColumCenter(colums);
            Matrix3d matrix = GeoUtils.GetGridMatrix(polyline, out Line longLine, out Line shortLine);

            List<KeyValuePair<Vector3d, List<Polyline>>> gridPolys = new List<KeyValuePair<Vector3d, List<Polyline>>>()
            {
                CreateGridLine(matrix, points, longLine, shortLine),
                CreateGridLine(RotateMatrix(matrix), points, longLine, shortLine),
            };
            return gridPolys;
        }

        /// <summary>
        /// 构建某方向上的轴网线
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="points"></param>
        /// <param name="longLine"></param>
        /// <param name="shortLine"></param>
        /// <returns></returns>
        public KeyValuePair<Vector3d, List<Polyline>> CreateGridLine(Matrix3d matrix, List<Point3d> points, Line longLine, Line shortLine)
        {
            points = points.Select(x => x.TransformBy(matrix.Inverse())).ToList();
            List<Point3d> linePts = new List<Point3d>() { longLine.StartPoint, longLine.EndPoint, shortLine.StartPoint, shortLine.EndPoint };
            List<Point3d> polyPts = GeoUtils.CalBoundingBox(linePts);
            polyPts = polyPts.Select(x => x.TransformBy(matrix.Inverse())).ToList();
            double minY = polyPts.First().Y;
            double maxY = polyPts.Last().Y;
            points.AddRange(polyPts);

            List<Polyline> gridPoly = new List<Polyline>();
            points = points.OrderByDescending(x => x.X).ToList();
            while (points.Count > 0)
            {
                Point3d sp = points.First();
                points.Remove(sp);
                List<Point3d> groupPts = new List<Point3d>() { sp };
                while (true)
                {
                    var matchPts = points.Where(x =>
                    {
                        if (Math.Abs(x.X - sp.X) < tol)
                        {
                            return true;
                        }
                        return false;
                    }).ToList();
                    if (matchPts.Count <= 0)
                    {
                        break;
                    }

                    sp = matchPts.OrderBy(x => x.Y).First();
                    groupPts.Add(sp);
                    points.Remove(sp);
                }

                if (groupPts.Count <= 1)
                {
                    continue;
                }

                groupPts.Add(new Point3d(sp.X, maxY, 0));
                groupPts.Add(new Point3d(sp.X, minY, 0));
                groupPts = groupPts.OrderBy(x => x.Y).ToList();
                Polyline poly = new Polyline();
                for (int i = 0; i < groupPts.Count; i++)
                {
                    poly.AddVertexAt(i, groupPts[i].ToPoint2D(), 0, 0, 0);
                }
                gridPoly.Add(poly);
            }

            gridPoly.ForEach(x => x.TransformBy(matrix));
            return new KeyValuePair<Vector3d, List<Polyline>>(matrix.CoordinateSystem3d.Xaxis, gridPoly);
        }

        /// <summary>
        /// 将矩阵旋转90度
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public Matrix3d RotateMatrix(Matrix3d matrix)
        {
            var xDir = matrix.CoordinateSystem3d.Xaxis;
            var yDir = matrix.CoordinateSystem3d.Yaxis;
            var zDir = matrix.CoordinateSystem3d.Zaxis;
            Matrix3d roMatrix = new Matrix3d(
                new double[]{
                    yDir.X, xDir.X, zDir.X, 0,
                    yDir.Y, xDir.Y, zDir.Y, 0,
                    yDir.Z, xDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0});
            return roMatrix;
        }

        /// <summary>
        /// 计算柱中点
        /// </summary>
        /// <param name="colums"></param>
        /// <returns></returns>
        public List<Point3d> GetColumCenter(List<Polyline> colums)
        {
            List<Point3d> resPoints = new List<Point3d>();
            foreach (var col in colums)
            {
                List<Point3d> points = new List<Point3d>();
                for (int i = 0; i < col.NumberOfVertices; i++)
                {
                    points.Add(col.GetPoint3dAt(i));
                }

                List<Point3d> ptsLst = GeoUtils.CalBoundingBox(points);
                resPoints.Add(new Point3d((ptsLst.First().X + ptsLst.Last().X) / 2, (ptsLst.First().Y + ptsLst.Last().Y) / 2, 0));
            }

            return resPoints;
        }
    }
}
