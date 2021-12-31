using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.ArcAlgorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.GridOperation.Model;

namespace ThMEPEngineCore.GridOperation.Utils
{
    public static class GridArcMergeService
    {
        static double maxSpacing = 3900;
        public static List<ArcGridModel> MergeArcGrid(List<ArcGridModel> arcGroup, List<Polyline> columns)
        {
            var resArcLines = new List<ArcGridModel>();
            foreach (var group in arcGroup)
            {
                ArcGridModel arcGrid = new ArcGridModel()
                {
                    centerPt = group.centerPt,
                    arcLines = new List<Arc>(),
                    lines = new List<Line>(),
                };
                arcGrid.arcLines.AddRange(MergeArc(group.arcLines, columns));
                arcGrid.lines.AddRange(MergeLine(group.lines, group.centerPt, columns));
                resArcLines.Add(arcGrid);
            }

            return resArcLines;
        }

        #region 直线线段合并
        /// <summary>
        /// 合并直线轴网
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="centerPt"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        private static List<Line> MergeLine(List<Line> lines, Point3d centerPt, List<Polyline> columns)
        {
            var orderGrids = OrderLineGrid(lines, centerPt, out List<Line> firLine, out List<Line> lastLine);
            orderGrids = orderGrids.Except(firLine).ToList();
            orderGrids = orderGrids.Except(lastLine).ToList();

            var firMergeGrids = orderGrids.Where(x => firLine.Any(y => y.Distance(x) < maxSpacing)).ToList();
            orderGrids = orderGrids.Except(firMergeGrids).ToList();
            var resGrids = MergeLineFirst(firMergeGrids, firLine, centerPt);
            var lastMergeGrids = orderGrids.Where(x => lastLine.Any(y => y.Distance(x) < maxSpacing)).ToList();
            orderGrids = orderGrids.Except(lastMergeGrids).ToList();
            resGrids.AddRange(MergeLineFirst(lastMergeGrids, lastLine, centerPt));

            orderGrids.AddRange(resGrids);
            orderGrids = MergeLineGridByColumn(orderGrids, columns, centerPt);
            orderGrids = MergeLineGridByLength(orderGrids, centerPt);
            orderGrids.AddRange(firLine);
            orderGrids.AddRange(lastLine);

            return orderGrids;
        }

        /// <summary>
        /// 根据穿过柱的数量合并弧形轴网线
        /// </summary>
        /// <param name="mergeGrids"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        private static List<Line> MergeLineGridByColumn(List<Line> mergeGrids, List<Polyline> columns, Point3d centerPt)
        {
            var resGrids = new List<Line>();
            var columnGrids = mergeGrids.ToDictionary(x => x, y => CalCrossingColumnNum(y, columns)).Where(x => x.Value > 0).ToDictionary(x => x.Key, y => y.Value);
            while (columnGrids.Count > 0)
            {
                var firGrid = columnGrids.OrderByDescending(x => x.Value).ThenByDescending(x => x.Key.Length).First().Key;
                resGrids.Add(firGrid);
                columnGrids.Remove(firGrid);
                mergeGrids.Remove(firGrid);
                var rangeGrid = mergeGrids.Where(x => x.Distance(firGrid) < maxSpacing).ToList();
                foreach (var rGrid in rangeGrid)
                {
                    columnGrids.Remove(rGrid);
                    mergeGrids.Remove(rGrid);
                }
                foreach (var grid in rangeGrid)
                {
                    var cutLines = firGrid.CutLineGrid(grid, centerPt);
                    foreach (var cLine in cutLines)
                    {
                        columnGrids.Add(cLine, CalCrossingColumnNum(cLine, columns));
                        mergeGrids.Add(cLine);
                    }
                }
            }
            resGrids.AddRange(mergeGrids);

            return resGrids;
        }

        /// <summary>
        /// 根据长度合并弧形轴网线
        /// </summary>
        /// <param name="mergeGrids"></param>
        /// <returns></returns>
        private static List<Line> MergeLineGridByLength(List<Line> mergeGrids, Point3d centerPt)
        {
            var checkGrids = new List<Line>(mergeGrids);
            var resGrids = new List<Line>();
            while (checkGrids.Count > 0)
            {
                if (checkGrids.Count > 0)
                {
                    var firGrid = checkGrids.OrderBy(x => x.Length).First();
                    resGrids.Add(firGrid);
                    checkGrids.Remove(firGrid);
                    var rangeGrid = checkGrids.Where(x => x.Distance(firGrid) < maxSpacing).ToList();
                    checkGrids = checkGrids.Except(rangeGrid).ToList();
                    foreach (var grid in rangeGrid)
                    {
                        checkGrids.AddRange(firGrid.CutLineGrid(grid, centerPt));
                    }
                }
            }

            return resGrids;
        }

        /// <summary>
        ///  首先保留边界线，然后根据边界线切割范围内的其他线
        /// </summary>
        /// <param name="orderGrids"></param>
        /// <param name="BindaryLine"></param>
        /// <param name="centerPt"></param>
        /// <returns></returns>
        private static List<Line> MergeLineFirst(List<Line> orderGrids, List<Line> BindaryLine, Point3d centerPt)
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
                        cutResLine.AddRange(bLine.CutLineGrid(firLine, centerPt));
                    }
                    firGrids = cutResLine;
                }
                resGrid.AddRange(firGrids);
            }

            return resGrid;
        }

        /// <summary>
        /// 剪切轴网
        /// </summary>
        /// <param name="line"></param>
        /// <param name="otherLine"></param>
        /// <param name="centerPt"></param>
        /// <returns></returns>
        private static List<Line> CutLineGrid(this Line line, Line otherLine, Point3d centerPt)
        {
            var startDis = line.StartPoint.DistanceTo(centerPt);
            var endDis = line.EndPoint.DistanceTo(centerPt);
            var otherSDis = otherLine.StartPoint.DistanceTo(centerPt);
            var otherEDis = otherLine.EndPoint.DistanceTo(centerPt);
            var minDis = startDis < endDis ? startDis : endDis;
            var maxDis = startDis > endDis ? startDis : endDis;
            var otherMinDis = otherSDis < otherEDis ? otherSDis : otherEDis;
            var otherMaxDis = otherSDis > otherEDis ? otherSDis : otherEDis;
            var dir = (line.StartPoint - centerPt).GetNormal();

            if (otherMaxDis < minDis || otherMinDis > maxDis)
            {
                return new List<Line>() { otherLine };
            }

            var ptCoordinate = new List<double>();
            if (!(minDis < otherMinDis && otherMinDis < maxDis))
            {
                ptCoordinate.Add(otherMinDis);
            }
            if (!(minDis < otherMaxDis && otherMaxDis < maxDis))
            {
                ptCoordinate.Add(otherMaxDis);
            }
            ptCoordinate.Add(otherMinDis);
            ptCoordinate.Add(otherMaxDis);
            ptCoordinate = ptCoordinate.OrderBy(x => x).ToList();
            List<Line> resLines = new List<Line>();
            for (int i = 1; i < ptCoordinate.Count; i++)
            {
                if (!(Math.Abs(ptCoordinate[i - 1] - minDis) < 1 && Math.Abs(ptCoordinate[i] - maxDis) < 1))
                {
                    var pt1 = centerPt + dir * ptCoordinate[i - 1];
                    var pt2 = centerPt + dir * ptCoordinate[i];
                    resLines.Add(new Line(pt1, pt2));
                }
            }

            return resLines.Where(x => x.Length > 10).ToList();
        }

        /// <summary>
        /// 排序直线轴网线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="centerPt"></param>
        /// <param name="firLine"></param>
        /// <param name="lastLine"></param>
        private static List<Line> OrderLineGrid(List<Line> lines, Point3d centerPt, out List<Line> firLine, out List<Line> lastLine)
        {
            firLine = new List<Line>();
            lastLine = new List<Line>();
            lines = lines.OrderBy(x =>
            {
                var sPt = x.StartPoint.DistanceTo(centerPt) < x.EndPoint.DistanceTo(centerPt) ? x.StartPoint : x.EndPoint;
                var ePt = x.StartPoint.DistanceTo(centerPt) > x.EndPoint.DistanceTo(centerPt) ? x.StartPoint : x.EndPoint;
                var dir = (ePt - sPt).GetNormal();
                return dir.GetAngleTo(Vector3d.XAxis);
            }).ToList();

            firLine = GetBindaryLine(lines, centerPt);
            lines.Reverse();
            lastLine = GetBindaryLine(lines, centerPt);
            return lines;
        }

        /// <summary>
        /// 获取直线轴网边界
        /// </summary>
        /// <param name="orderGrids"></param>
        /// <returns></returns>
        private static List<Line> GetBindaryLine(List<Line> orderGrids, Point3d centerPt)
        {
            var allLines = orderGrids.Where(x => x.Distance(orderGrids.First()) < maxSpacing).ToList();
            var bindaryLine = new List<Line>();
            var usedLine = new List<Line>();
            while (allLines.Count > 0)
            {
                var line = allLines.First();
                allLines.Remove(line);
                if (usedLine.Where(x => x.CalLineOverLap(line, centerPt)).Count() <= 0)
                {
                    bindaryLine.Add(line);
                }
                usedLine.Add(line);
            }

            return bindaryLine;
        }

        /// <summary>
        /// 计算是否是overlap
        /// </summary>
        /// <param name="line"></param>
        /// <param name="otherLine"></param>
        /// <param name="centerPt"></param>
        /// <returns></returns>
        private static bool CalLineOverLap(this Line line, Line otherLine, Point3d centerPt)
        {
            var startDis = line.StartPoint.DistanceTo(centerPt);
            var endDis = line.EndPoint.DistanceTo(centerPt);
            var otherSDis = otherLine.StartPoint.DistanceTo(centerPt);
            var otherEDis = otherLine.EndPoint.DistanceTo(centerPt);
            var minDis = startDis < endDis ? startDis : endDis;
            var maxDis = startDis > endDis ? startDis : endDis;
            var otherMinDis = otherSDis < otherEDis ? otherSDis : otherEDis;
            var otherMaxDis = otherSDis > otherEDis ? otherSDis : otherEDis;
            if (maxDis < otherMinDis || minDis > otherMaxDis)
            {
                return false;
            }

            return true;
        }
        #endregion

        #region 弧形线段合并
        /// <summary>
        /// 合并弧形轴网
        /// </summary>
        /// <param name="arcs"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        private static List<Arc> MergeArc(List<Arc> arcs, List<Polyline> columns)
        {
            arcs = OrderArcLines(arcs, out List<Arc> firArc, out List<Arc> lastArc);
            arcs = arcs.Except(firArc).ToList();
            arcs = arcs.Except(lastArc).ToList();

            var firMergeGrids = arcs.Where(x => firArc.Any(y => Math.Abs(x.Radius - y.Radius) < maxSpacing)).ToList();
            arcs = arcs.Except(firMergeGrids).ToList();
            var resGrids = MergeArcFirst(firMergeGrids, firArc);
            var lastMergeGrids = arcs.Where(x => lastArc.Any(y => Math.Abs(y.Radius - x.Radius) < maxSpacing)).ToList();
            arcs = arcs.Except(lastMergeGrids).ToList();
            resGrids.AddRange(MergeArcFirst(lastMergeGrids, lastArc));

            arcs.AddRange(resGrids);
            arcs = MergeArcGridByColumn(arcs, columns);
            arcs = MergeArcGridByLength(arcs);
            arcs.AddRange(firArc);
            arcs.AddRange(lastArc);

            return arcs;
        }

        /// <summary>
        /// 根据穿过柱的数量合并弧形轴网线
        /// </summary>
        /// <param name="mergeGrids"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        private static List<Arc> MergeArcGridByColumn(List<Arc> mergeGrids, List<Polyline> columns)
        {
            var resGrids = new List<Arc>();
            var columnGrids = mergeGrids.ToDictionary(x => x, y => CalCrossingColumnNum(y, columns)).Where(x => x.Value > 0).ToDictionary(x => x.Key, y => y.Value);
            while (columnGrids.Count > 0)
            {
                var firGrid = columnGrids.OrderByDescending(x => x.Value).ThenByDescending(x => x.Key.Length).First().Key;
                resGrids.Add(firGrid);
                columnGrids.Remove(firGrid);
                mergeGrids.Remove(firGrid);
                var rangeGrid = mergeGrids.Where(x => Math.Abs(x.Radius - firGrid.Radius) < maxSpacing).ToList();
                foreach (var rGrid in rangeGrid)
                {
                    columnGrids.Remove(rGrid);
                    mergeGrids.Remove(rGrid);
                }
                foreach (var grid in rangeGrid)
                {
                    var cutArcs = firGrid.CutArcLine(grid);
                    foreach (var cArc in cutArcs)
                    {
                        columnGrids.Add(cArc, CalCrossingColumnNum(cArc, columns));
                        mergeGrids.Add(cArc);
                    }
                }
            }
            resGrids.AddRange(mergeGrids);

            return resGrids;
        }

        /// <summary>
        /// 根据长度合并弧形轴网线
        /// </summary>
        /// <param name="mergeGrids"></param>
        /// <returns></returns>
        private static List<Arc> MergeArcGridByLength(List<Arc> mergeGrids)
        {
            var checkGrids = new List<Arc>(mergeGrids);
            var resGrids = new List<Arc>();
            while (checkGrids.Count > 0)
            {
                if (checkGrids.Count > 0)
                {
                    var firGrid = checkGrids.OrderByDescending(x => x.Length).First();
                    resGrids.Add(firGrid);
                    checkGrids.Remove(firGrid);
                    var rangeGrid = checkGrids.Where(x => Math.Abs(x.Radius - firGrid.Radius) < maxSpacing).ToList();
                    checkGrids = checkGrids.Except(rangeGrid).ToList();
                    foreach (var grid in rangeGrid)
                    {
                        checkGrids.AddRange(firGrid.CutArcLine(grid));
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
        private static List<Arc> MergeArcFirst(List<Arc> orderGrids, List<Arc> BindaryLine)
        {
            var otherGrids = orderGrids.Except(BindaryLine).ToList();
            var resGrid = new List<Arc>();
            while (otherGrids.Count > 0)
            {
                var firGrids = new List<Arc>() { otherGrids.First() };
                otherGrids.Remove(otherGrids.First());
                foreach (var bLine in BindaryLine)
                {
                    var cutResLine = new List<Arc>();
                    foreach (var firLine in firGrids)
                    {
                        cutResLine.AddRange(bLine.CutArcLine(firLine));
                    }
                    firGrids = cutResLine;
                }
                resGrid.AddRange(firGrids);
            }

            return resGrid;
        }

        /// <summary>
        /// 排序弧形轴网
        /// </summary>
        /// <param name="arcs"></param>
        /// <param name="firArc"></param>
        /// <param name="lastArc"></param>
        private static List<Arc> OrderArcLines(List<Arc> arcs, out List<Arc> firArc, out List<Arc> lastArc)
        {
            firArc = new List<Arc>();
            lastArc = new List<Arc>();
            arcs = arcs.OrderBy(x => x.Radius).ToList();
            firArc = GetArcBindaryLine(arcs);
            arcs.Reverse();
            lastArc = GetArcBindaryLine(arcs);

            return arcs;
        }

        /// <summary>
        /// 获取弧形边界轴网
        /// </summary>
        /// <param name="orderGrids"></param>
        /// <returns></returns>
        private static List<Arc> GetArcBindaryLine(List<Arc> orderGrids)
        {
            var allLines = orderGrids.Where(x => Math.Abs(x.Radius - orderGrids.First().Radius) < maxSpacing).ToList();
            var bindaryArc = new List<Arc>();
            var usedArc = new List<Arc>();
            while (allLines.Count > 0)
            {
                var line = allLines.First();
                allLines.Remove(line);
                if (usedArc.Where(x =>
                {
                    return x.ArcOverLap(line, maxSpacing);
                }).Count() <= 0)
                {
                    bindaryArc.Add(line);
                }
                usedArc.Add(line);
            }

            return bindaryArc;
        }
        #endregion

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
    }
}
