using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.GridOperation.Utils
{
    public static class GridMergeService
    {
        static double maxSpacing = 3500;
        public static void LineMerge(List<Line> lines, List<Polyline> columns)
        {
            //var lineGroup = LineGridGroup(lines);
        }



        private static void MergeLine(Dictionary<Vector3d, List<Line>> lineGroups, List<Polyline> columns)
        {
            List<List<Line>> resLines = new List<List<Line>>();
            foreach (var group in lineGroups)
            {
                List<Line> lines = new List<Line>();
                var orderGrids = OrderGridLines(group.Key, group.Value, out List<Line> firLine, out List<Line> lastLine, out Matrix3d matrix);
                //orderGrids.Remove(lastLine);
                //var firMergeGrids = orderGrids.Where(x => Math.Abs(firLine.StartPoint.Y - x.StartPoint.Y) < maxSpacing).ToList();
                //orderGrids.Remove(firLine);
                while (orderGrids.Count > 0)
                {
                    //var firGrid = orderGrids
                }
            }
        }

        private static void MergeGrid(List<Line> mergeGrids, List<Polyline> columns, bool isBoundary = false)
        {
            //var resGrid = new List<Line>();
            //if (isBoundary)
            //{
            //    var boundaryLine = mergeGrids.First();
            //    mergeGrids.Remove(boundaryLine);
            //    resGrid.Add(boundaryLine);
            //    foreach (var grid in mergeGrids)
            //    {
            //        resGrid.AddRange(CutGrid(boundaryLine, grid));
            //    }
            //}
            //var columnGrids = mergeGrids.ToDictionary(x => x, y => CalCrossingColumnNum(y, columns));
            //while (mergeGrids.Count > 0)
            //{
            //    if (columnGrids.Count > 0)
            //    {
            //        var firGrid = columnGrids.OrderBy(x => x.Value).First().Key;
            //        var rangeGrid =
            //    }

            //    var rangeGrid = mergeGrids.Where(x => Math.Abs(firGrid.StartPoint.Y - x.StartPoint.Y) < maxSpacing)
            //        .OrderBy(x => CalCrossingColumnNum(x, columns))
            //        .ThenBy(x => x.Length)
            //        .ThenBy(x => Math.Abs(firGrid.StartPoint.Y - x.StartPoint.Y))
            //        .FirstOrDefault();
            //    if (rangeGrid == null)
            //    {
            //        resGrid.Add(firGrid);
            //    }
            //    CutGrid(firGrid, rangeGrid);
            //}
        }

        private static void MergeFirst(List<Line> orderGrids, Line firGrid)
        {
            var resGrid = new List<Line>();
            var boundaryLine = orderGrids.First();
            orderGrids.Remove(boundaryLine);
            resGrid.Add(boundaryLine);
            foreach (var grid in orderGrids)
            {
                resGrid.AddRange(CutGrid(boundaryLine, grid));
            }
        }

        private static List<Line> CutGrid(Line line, Line otherLine)
        {
            var lineMinX = line.StartPoint.X < line.EndPoint.X ? line.StartPoint.X : line.EndPoint.X;
            var lineMaxX = line.StartPoint.X > line.EndPoint.X ? line.StartPoint.X : line.EndPoint.X;
            var otherMinX = otherLine.StartPoint.X < otherLine.EndPoint.X ? otherLine.StartPoint.X : otherLine.EndPoint.X;
            var otherMaxX = otherLine.StartPoint.X > otherLine.EndPoint.X ? otherLine.StartPoint.X : otherLine.EndPoint.X;
            var otherY = otherLine.StartPoint.Y;
            var ptCoordinate = new List<double>();
            if (!(lineMinX < otherMinX && otherMinX < lineMaxX))
            {
                ptCoordinate.Add(otherMinX);
            }
            if (!(lineMinX < otherMaxX && otherMaxX < lineMaxX))
            {
                ptCoordinate.Add(otherMaxX);
            }
            ptCoordinate.Add(lineMinX);
            ptCoordinate.Add(lineMaxX);
            ptCoordinate = ptCoordinate.OrderBy(x => x).ToList();
            List<Line> resLines = new List<Line>();
            for (int i = 1; i < ptCoordinate.Count; i++)
            {

                if (Math.Abs(ptCoordinate[i - 1] - lineMinX) < 1 && Math.Abs(ptCoordinate[i] - lineMaxX) < 1)
                {
                    var pt1 = new Point3d(ptCoordinate[i - 1], otherY, 0);
                    var pt2 = new Point3d(ptCoordinate[i], otherY, 0);
                    resLines.Add(new Line(pt1, pt2));
                }
            }

            return resLines;
        }

        private static List<Line> OrderGridLines(Vector3d dir, List<Line> grids, out List<Line> firLine, out List<Line> lastLine, out Matrix3d matrix3d)
        {
            var matrix = GetMatrix(dir);
            var orderGrids = grids.ToDictionary(x => x, y =>
            {
                var cloneLine = y.Clone() as Line;
                cloneLine.TransformBy(matrix);
                return cloneLine;
            })
                .OrderBy(x => x.Value.StartPoint.Y)
                .Select(x => x.Value)
                .ToList();

            matrix3d = matrix;
            firLine = GetBindaryLine(orderGrids);
            orderGrids.Reverse();
            lastLine = GetBindaryLine(orderGrids);
            return orderGrids;
        }

        private static List<Line> GetBindaryLine(List<Line> orderGrids)
        {
            var allLines = orderGrids.Where(x => Math.Abs(x.StartPoint.Y - orderGrids.First().StartPoint.Y) < maxSpacing).ToList();
            var bindaryLine = new List<Line>();
            while (allLines.Count > 0)
            {
                var line = allLines.First();
                allLines.Remove(line);
                if (bindaryLine.Where(x =>
                {
                    var minX = x.StartPoint.X < x.EndPoint.X ? x.StartPoint.X : x.EndPoint.X;
                    var maxX = x.StartPoint.X > x.EndPoint.X ? x.StartPoint.X : x.EndPoint.X;
                    var lineMinX = line.StartPoint.X < line.EndPoint.X ? line.StartPoint.X : line.EndPoint.X;
                    var lineMaxX = line.StartPoint.X > line.EndPoint.X ? line.StartPoint.X : line.EndPoint.X;
                    return lineMaxX < minX || maxX < lineMinX;
                }).Count() <= 0)
                {
                    bindaryLine.Add(line);
                }
                else
                {
                    break;
                }
            }

            return bindaryLine;
        }

        /// <summary>
        /// 计算轴网穿过的柱个数
        /// </summary>
        /// <param name="line"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        private static int CalCrossingColumnNum(Line line, List<Polyline> columns)
        {
            return columns.Where(x => line.IsIntersects(line)).Count();
        }

        private static Matrix3d GetMatrix(Vector3d vector)
        {
            var xDir = vector.GetNormal();
            var zDir = Vector3d.ZAxis;
            var yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(new double[]{
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0});
            return matrix;
        }
    }
}
