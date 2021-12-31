using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.GridOperation.Model;

namespace ThMEPEngineCore.GridOperation.Utils
{
    public static class GridLineMergeService
    {
        static double maxSpacing = 3900;
        /// <summary>
        /// 合并轴网线
        /// </summary>
        /// <param name="lineGroups"></param>
        /// <param name="columns"></param>
        public static List<LineGridModel> MergeLine(List<LineGridModel> lineGroups, List<Polyline> columns)
        {
            var resLines = new List<LineGridModel>();
            foreach (var group in lineGroups)
            {
                LineGridModel lineGrid = new LineGridModel()
                {
                    vecter = group.vecter,
                    xLines = new List<Line>(),
                    yLines = new List<Line>(),
                };
                
                lineGrid.xLines.AddRange(MergeLine(group.xLines, group.vecter, columns));
                var yDir = Vector3d.ZAxis.CrossProduct(group.vecter);
                lineGrid.yLines.AddRange(MergeLine(group.yLines, yDir, columns));
                resLines.Add(lineGrid);
            }

            return resLines;
        }

        /// <summary>
        /// 合并线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="vector"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public static List<Line> MergeLine(List<Line> lines, Vector3d vector, List<Polyline> columns)
        {
            var orderGrids = OrderGridLines(vector, lines, out List<Line> firLine, out List<Line> lastLine, out Matrix3d matrix);
            orderGrids = orderGrids.Except(firLine).ToList();
            orderGrids = orderGrids.Except(lastLine).ToList();
            var firMergeGrids = orderGrids.Where(x => firLine.Any(y => Math.Abs(y.StartPoint.Y - x.StartPoint.Y) < maxSpacing)).ToList();
            orderGrids = orderGrids.Except(firMergeGrids).ToList();
            var resGrids = MergeFirst(firMergeGrids, firLine);
            var lastMergeGrids = orderGrids.Where(x => lastLine.Any(y => Math.Abs(y.StartPoint.Y - x.StartPoint.Y) < maxSpacing)).ToList();
            orderGrids = orderGrids.Except(lastMergeGrids).ToList();
            resGrids.AddRange(MergeFirst(lastMergeGrids, lastLine));
            orderGrids.AddRange(resGrids);
            orderGrids = MergeGridByColumn(orderGrids, columns);
            orderGrids = MergeGridByLength(orderGrids);
            orderGrids.AddRange(firLine);
            orderGrids.AddRange(lastLine);
            TransToMatrix(matrix, orderGrids);

            return orderGrids;
        }

        /// <summary>
        /// 讲轴网转回世界坐标系
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="transGrids"></param>
        private static void TransToMatrix(Matrix3d matrix, List<Line> transGrids)
        {
            transGrids.ForEach(x => x.TransformBy(matrix));
        }

        /// <summary>
        /// 根据穿过柱数量合并轴网
        /// </summary>
        /// <param name="mergeGrids"></param>
        /// <param name="columns"></param>
        /// <param name="isBoundary"></param>
        /// <returns></returns>
        private static List<Line> MergeGridByColumn(List<Line> mergeGrids, List<Polyline> columns)
        {
            var resGrids = new List<Line>();
            var columnGrids = mergeGrids.ToDictionary(x => x, y => CalCrossingColumnNum(y, columns)).Where(x => x.Value > 0).ToDictionary(x => x.Key, y => y.Value);
            while (columnGrids.Count > 0)
            {
                var firGrid = columnGrids.OrderBy(x => x.Value).First().Key;
                resGrids.Add(firGrid);
                columnGrids.Remove(firGrid);
                mergeGrids.Remove(firGrid);
                var rangeGrid = mergeGrids.Where(x => Math.Abs(x.StartPoint.Y - firGrid.StartPoint.Y)/*x.Distance(firGrid)*/ < maxSpacing).ToList();
                foreach (var cGrid in rangeGrid)
                {
                    columnGrids.Remove(cGrid);
                    mergeGrids.Remove(cGrid);
                }
                foreach (var grid in rangeGrid)
                {
                    var cutGrids = CutGrid(firGrid, grid);
                    foreach (var cGrid in cutGrids)
                    {
                        columnGrids.Add(cGrid, CalCrossingColumnNum(cGrid, columns));
                        mergeGrids.Add(cGrid);
                    }
                }
            }
            resGrids.AddRange(mergeGrids);

            return resGrids;
        }

        /// <summary>
        /// 根据轴网长度合并线
        /// </summary>
        /// <param name="mergeGrids"></param>
        /// <returns></returns>
        private static List<Line> MergeGridByLength(List<Line> mergeGrids)
        {
            var checkGrids = new List<Line>(mergeGrids);
            var resGrids = new List<Line>();
            while (checkGrids.Count > 0)
            {
                if (checkGrids.Count > 0)
                {
                    var firGrid = checkGrids.OrderByDescending(x => x.Length).First();
                    resGrids.Add(firGrid);
                    checkGrids.Remove(firGrid);
                    var rangeGrid = checkGrids.Where(x => x.Distance(firGrid) < maxSpacing).ToList();
                    checkGrids = checkGrids.Except(rangeGrid).ToList();
                    foreach (var grid in rangeGrid)
                    {
                        checkGrids.AddRange(CutGrid(firGrid, grid));
                    }
                }
            }
            return resGrids;
        }

        /// <summary>
        /// 首先保留边界线，然后根据边界线切割范围内的其他线
        /// </summary>
        /// <param name="orderGrids"></param>
        /// <param name="BindaryLine"></param>
        /// <returns></returns>
        private static List<Line> MergeFirst(List<Line> orderGrids, List<Line> BindaryLine)
        {
            var otherGrids = orderGrids.Except(BindaryLine).ToList();
            var resGrid = new List<Line>();
            while (otherGrids.Count > 0)
            {
                var firGrids = new List<Line>() { otherGrids.First() };
                otherGrids.Remove(otherGrids.First());
                foreach (var bLine in BindaryLine)
                {
                    var cutResLine = new List<Line>();
                    foreach (var firLine in firGrids)
                    {
                        cutResLine.AddRange(CutGrid(bLine, firLine));
                    }
                    firGrids = cutResLine;
                }
                resGrid.AddRange(firGrids);
            }

            return resGrid;
        }
        
        /// <summary>
        /// 剪切线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="otherLine"></param>
        /// <returns></returns>
        private static List<Line> CutGrid(Line line, Line otherLine)
        {
            var lineMinX = line.StartPoint.X < line.EndPoint.X ? line.StartPoint.X : line.EndPoint.X;
            var lineMaxX = line.StartPoint.X > line.EndPoint.X ? line.StartPoint.X : line.EndPoint.X;
            var otherMinX = otherLine.StartPoint.X < otherLine.EndPoint.X ? otherLine.StartPoint.X : otherLine.EndPoint.X;
            var otherMaxX = otherLine.StartPoint.X > otherLine.EndPoint.X ? otherLine.StartPoint.X : otherLine.EndPoint.X;
            var otherY = otherLine.StartPoint.Y;
            if (otherMaxX < lineMinX || otherMinX > lineMaxX)
            {
                return new List<Line>() { otherLine };
            }

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
                if (!(Math.Abs(ptCoordinate[i - 1] - lineMinX) < 1 && Math.Abs(ptCoordinate[i] - lineMaxX) < 1))
                {
                    var pt1 = new Point3d(ptCoordinate[i - 1], otherY, 0);
                    var pt2 = new Point3d(ptCoordinate[i], otherY, 0);
                    resLines.Add(new Line(pt1, pt2));
                }
            }

            return resLines.Where(x => x.Length < 10).ToList();
        }

        /// <summary>
        /// 排序轴网线
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="grids"></param>
        /// <param name="firLine"></param>
        /// <param name="lastLine"></param>
        /// <param name="matrix3d"></param>
        /// <returns></returns>
        private static List<Line> OrderGridLines(Vector3d dir, List<Line> grids, out List<Line> firLine, out List<Line> lastLine, out Matrix3d matrix3d)
        {
            var matrix = GetMatrix(dir);
            var orderGrids = grids.Select(y =>
            {
                var cloneLine = y.Clone() as Line;
                cloneLine.TransformBy(matrix.Inverse());
                return cloneLine;
            })
                .OrderBy(x => x.StartPoint.Y)
                .ToList();
            
            matrix3d = matrix;
            firLine = GetBindaryLine(orderGrids);
            orderGrids = orderGrids.Except(firLine).ToList();
            orderGrids.Reverse();
            lastLine = GetBindaryLine(orderGrids);
            return orderGrids;
        }

        /// <summary>
        /// 获取边界轴网
        /// </summary>
        /// <param name="orderGrids"></param>
        /// <returns></returns>
        private static List<Line> GetBindaryLine(List<Line> orderGrids)
        {
            var allLines = orderGrids.Where(x => Math.Abs(x.StartPoint.Y - orderGrids.First().StartPoint.Y) < maxSpacing).ToList();
            var bindaryLine = new List<Line>();
            var usedLine = new List<Line>();
            while (allLines.Count > 0)
            {
                var line = allLines.First();
                allLines.Remove(line);
                if (usedLine.Where(x =>
                {
                    var minX = x.StartPoint.X < x.EndPoint.X ? x.StartPoint.X : x.EndPoint.X;
                    var maxX = x.StartPoint.X > x.EndPoint.X ? x.StartPoint.X : x.EndPoint.X;
                    var lineMinX = line.StartPoint.X < line.EndPoint.X ? line.StartPoint.X : line.EndPoint.X;
                    var lineMaxX = line.StartPoint.X > line.EndPoint.X ? line.StartPoint.X : line.EndPoint.X;
                    return !(lineMaxX < minX || maxX < lineMinX);
                }).Count() <= 0)
                {
                    bindaryLine.Add(line);
                }
                usedLine.Add(line);
            }

            return bindaryLine;
        }

        /// <summary>
        /// 计算轴网穿过的柱个数
        /// </summary>
        /// <param name="line"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        private static int CalCrossingColumnNum(Curve curve, List<Polyline> columns)
        {
            return columns.Where(x => curve.IsIntersects(x)).Count();
        }

        /// <summary>
        /// 创建坐标系
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
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