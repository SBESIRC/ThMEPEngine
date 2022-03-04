using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.CAD;

namespace ThMEPStructure.Reinforcement.Service
{
    internal static class ThShapeAnalysisUtils
    {
        public static List<Tuple<int, List<int>>> FindLTypeEdge(this List<Tuple<Point3d, Point3d>> lines)
        {
            // 返回 <L2,(L4,L6)>,<L1,(L3,L5)>
            var results = new List<Tuple<int, List<int>>>();
            for (int i = 0; i < lines.Count; i++)
            {
                var currentDir = lines[i].GetLineDirection();
                var parallels = new List<int>();
                for (int j = 0; j < lines.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    var nextDir = lines[j].GetLineDirection();
                    if (IsParallelIntervalGreaterThan(lines[i], lines[j], 10.0))
                    {
                        parallels.Add(j);
                    }
                }
                if (parallels.Count != 2)
                {
                    continue;
                }
                if (IsProjectionLengthEqual(lines[i], lines[parallels[0]], lines[parallels[1]]) &&
                    IsAdjacentEdgeIndex(parallels[0], parallels[1], lines.Count))
                {
                    results.Add(Tuple.Create(i, parallels));
                }
            }
            return results;
        }
        public static List<Tuple<int, List<int>>> FindTTypeMainEdge(this List<Tuple<Point3d, Point3d>> lines)
        {
            /*                   (L3)
             *          (L2)    -----   (L4)
             *       -----------     -----------
             *       ---------------------------
             *                  (L1)
             *       返回 <L1,(L2,L3,L4)>
             */
            var results = new List<Tuple<int, List<int>>>();
            for (int i = 0; i < lines.Count; i++)
            {
                var currentDir = lines[i].GetLineDirection();
                var parallels = new List<int>();
                for (int j = 0; j < lines.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    var nextDir = lines[j].GetLineDirection();
                    if (IsParallelIntervalGreaterThan(lines[i], lines[j], 10.0))
                    {
                        parallels.Add(j);
                    }
                }
                if (parallels.Count != 3)
                {
                    continue;
                }
                if (IsProjectionLengthEqual(lines[i], lines[parallels[0]], lines[parallels[1]], lines[parallels[2]]))
                {
                    parallels = SortEdgeIndexes(parallels, lines, lines[i].Item1);
                    if (ThGeometryTool.IsCollinearEx(
                        lines[parallels.First()].Item1, lines[parallels.First()].Item2,
                        lines[parallels.Last()].Item1, lines[parallels.Last()].Item2) &&
                        IsParallelIntervalGreaterThan(lines[parallels[0]], lines[parallels[1]], 10.0))
                    {
                        results.Add(Tuple.Create(i, parallels));
                    }
                }
            }
            return results;
        }
        private static bool IsProjectionLengthEqual(
            Tuple<Point3d, Point3d> mainSeg,
            Tuple<Point3d, Point3d> firstSeg,
            Tuple<Point3d, Point3d> secondSeg,
            Tuple<Point3d, Point3d> thirdSeg,
             double tolerance = 1.0
            )
        {
            /* 
             *             (L1)
             *  -----------————————----------
             *      (L2)          (L3)
             *  -----------     -------------
             *                         
             *             —————
             *             （L4）
             */
            // L1 // L2, L1 // L3，L1//L4
            var firstSp = firstSeg.Item1.GetProjectPtOnLine(mainSeg.Item1, mainSeg.Item2);
            var firstEp = firstSeg.Item2.GetProjectPtOnLine(mainSeg.Item1, mainSeg.Item2);
            var secondSp = secondSeg.Item1.GetProjectPtOnLine(mainSeg.Item1, mainSeg.Item2);
            var secondEp = secondSeg.Item2.GetProjectPtOnLine(mainSeg.Item1, mainSeg.Item2);
            var thirdSp = thirdSeg.Item1.GetProjectPtOnLine(mainSeg.Item1, mainSeg.Item2);
            var thirdEp = thirdSeg.Item2.GetProjectPtOnLine(mainSeg.Item1, mainSeg.Item2);

            var mainDis = mainSeg.GetLineDistance();
            var pts = new List<Point3d>() { firstSp, firstEp, secondSp, secondEp, thirdSp, thirdEp };
            var maxPts = pts.GetCollinearMaxPts();
            var totalLength = maxPts.GetLineDistance();
            return mainDis.IsEqual(totalLength, tolerance);
        }
        private static bool IsParallelIntervalGreaterThan(
            Tuple<Point3d, Point3d> first,
            Tuple<Point3d, Point3d> second, double distance)
        {
            // 检查两根平行线间距是否大于distance
            var firstDir = first.GetLineDirection();
            var secondDir = second.GetLineDirection();
            if (firstDir.IsParallelToEx(secondDir))
            {
                var projectPt = first.Item1.GetProjectPtOnLine(second.Item1, second.Item2);
                var interval = first.Item1.DistanceTo(projectPt);
                return interval > distance;
            }
            else
            {
                return false;
            }
        }
        private static bool IsProjectionLengthEqual(
            Tuple<Point3d, Point3d> mainSeg,
            Tuple<Point3d, Point3d> firstSeg,
            Tuple<Point3d, Point3d> secondSeg,
            double tolerance = 1.0
            )
        {
            /* 
             *          (L1)
             *  ---------------------
             *      (L2)
             *  -----------
             *                 (L3)
             *             ----------
             */
            // L1 // L2, L1 // L3
            var firstSp = firstSeg.Item1.GetProjectPtOnLine(mainSeg.Item1, mainSeg.Item2);
            var firstEp = firstSeg.Item2.GetProjectPtOnLine(mainSeg.Item1, mainSeg.Item2);
            var secondSp = secondSeg.Item1.GetProjectPtOnLine(mainSeg.Item1, mainSeg.Item2);
            var secondEp = secondSeg.Item2.GetProjectPtOnLine(mainSeg.Item1, mainSeg.Item2);
            var mainDis = mainSeg.GetLineDistance();
            var pts = new List<Point3d>() { firstSp, firstEp, secondSp, secondEp };
            var maxPts = pts.GetCollinearMaxPts();
            var totalLength = maxPts.GetLineDistance();
            return mainDis.IsEqual(totalLength, tolerance);
        }
        private static bool IsAdjacentEdgeIndex(int first, int second, int count)
        {
            return (first + 1) % count == second || (second + 1) % count == first;
        }

        public static int FindMiddleEdgeIndex(this int preEdgeIndex,int nextEdgeIndex,int count)
        {
            var middleIndex = (preEdgeIndex + 1)% count;
            if(IsAdjacentEdgeIndex(middleIndex, nextEdgeIndex, count))
            {
                return middleIndex;
            }
            middleIndex = (preEdgeIndex - 1+ count) % count;
            if (IsAdjacentEdgeIndex(middleIndex, nextEdgeIndex, count))
            {
                return middleIndex;
            }
            return -1;
        }

        public static List<int> SortEdgeIndexes(this List<int> edgeIndexes, 
            List<Tuple<Point3d, Point3d>> lines, Point3d startPt)
        {
            /*
             *          -----
             *   -------     ------
             *   把上方三条平行线根据给定的起点排序
             */
            var firstSeg = lines[edgeIndexes[0]];
            var newStartPt = startPt.GetProjectPtOnLine(firstSeg.Item1, firstSeg.Item2);
            return edgeIndexes.OrderBy(o =>
            {
                var seg = lines[o];
                var midPt = seg.Item1.GetMidPt(seg.Item2);
                var projectionPt = midPt.GetProjectPtOnLine(firstSeg.Item1, firstSeg.Item2);
                return newStartPt.DistanceTo(projectionPt);
            }).ToList();
        }
    }
}
