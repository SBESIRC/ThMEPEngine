using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThShapeAnalysisService
    {
        public ShapeCode Analysis(Polyline poly)
        {
            // 不支持弧
            if(!ThMEPFrameService.IsClosed(poly,1.0) || HasArc(poly))
            {
                return ShapeCode.Unknown;
            }
            if(IsRectType(poly))
            {
                return ShapeCode.Rect;
            }
            else if(IsLType(poly))
            {
                return ShapeCode.L;
            }
            else if (IsTType(poly))
            {
                return ShapeCode.T;
            }
            else
            {
                return ShapeCode.Unknown;
            }
        }

        private bool HasArc(Polyline poly)
        {
            for (int i = 0; i < poly.NumberOfVertices - 1; i++)
            {
                var st = poly.GetSegmentType(i);
                if (st == SegmentType.Arc)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsLType(Polyline poly)
        {
            /*          L3
             *         -----
             *         |   | 
             *         |   |
             *         |   | L4
             *  (L2)   |   |     L5
             *         |   ------------
             *         |              | L6
             *         ----------------
             *             （L1）
             */
            // 识别条件：
            // 有6条边
            // 相邻边是互相垂直的
            // 有一个凹点
            // 有两组边满足：
            // L1//L3,L3//L5,L1=L3+L5, L3和L5不共线
            // L2//L4,L2//L6,L2=L4+L6, L4和L6不共线
            var lines = ToLines(poly);
            if(lines.Count != 6 || !IsAllAnglesVertical(lines))
            {
                return false;
            }
            var concavePoints = GetConcavePoints(poly);
            if(concavePoints.Count!=1)
            {
                return false;
            }
            var l1l2Edges = FindLTypeEdge(lines);
            return l1l2Edges.Count == 2;
        }

        private bool IsTType(Polyline poly)
        {
            /*                  (L5)
             *                 -----
             *                 |   | 
             *                 |   |
             *            (L4) |   | (L6)
             *          (L3)   |   |    (L7)
             *       |----------   ------------|
             *   (L2)|                         | (L8)
             *       |-------------------------|
             *                 （L1）
             */
            // 识别条件
            // 8条边
            // 相邻边是互相垂直的
            // 两个凹点
            // 其中有一组平行边满足 L1//L3,L1//L5,L1//L7 L1=L3+l5+l7, L3与L7共线，L5与L3,L7不共线
            var lines = ToLines(poly);
            if (lines.Count != 8 || !IsAllAnglesVertical(lines))
            {
                return false;
            }
            var concavePoints = GetConcavePoints(poly);
            if (concavePoints.Count != 2)
            {
                return false;
            }
            var l1l2Edges = FindTTypeMainEdge(lines); //<L1,(L3,L5,L7)>
            return l1l2Edges.Count == 1;
        }

        private List<Point3d> GetConcavePoints(Polyline polyline)
        {
            var result = polyline.PointClassify();
            return result.Where(o => o.Value == 2).Select(o => o.Key).ToList();
        }
        private List<Point3d> GetConvexPoints(Polyline polyline)
        {
            var result = polyline.PointClassify();
            return result.Where(o => o.Value == 1).Select(o => o.Key).ToList();
        }

        private List<Tuple<int, List<int>>> FindLTypeEdge(List<Tuple<Point3d, Point3d>> lines)
        {
            // 返回 <L2,(L4,L6)>,<L1,(L3,L5)>
            var results = new List<Tuple<int, List<int>>>();
            for (int i = 0; i < lines.Count; i++)
            {
                var currentDir = GetLineDirection(lines[i]);
                var parallels = new List<int>();
                for (int j = 0; j < lines.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    var nextDir = GetLineDirection(lines[j]);
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
                    IsEdgeIndexValid(parallels, lines.Count))
                {
                    results.Add(Tuple.Create(i, parallels));
                }
            }
            return results;
        }

        private List<Tuple<int, List<int>>> FindTTypeMainEdge(List<Tuple<Point3d, Point3d>> lines)
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
                var currentDir = GetLineDirection(lines[i]);
                var parallels = new List<int>();
                for (int j = 0; j < lines.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    var nextDir = GetLineDirection(lines[j]);
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
                    parallels = parallels.OrderBy(o =>
                    {
                        var seg = lines[o];
                        var midPt = seg.Item1.GetMidPt(seg.Item2);
                        var projectionPt = midPt.GetProjectPtOnLine(lines[i].Item1, lines[i].Item2);
                        return lines[i].Item1.DistanceTo(projectionPt);
                    }).ToList();
                    if(ThGeometryTool.IsCollinearEx(
                        lines[parallels.First()].Item1, lines[parallels.First()].Item2,
                        lines[parallels.Last()].Item1, lines[parallels.Last()].Item2) &&
                        IsParallelIntervalGreaterThan(lines[parallels[0]], lines[parallels[1]],10.0))
                    {
                        results.Add(Tuple.Create(i, parallels));
                    }
                }
            }
            return results;
        }

        private bool IsEdgeIndexValid(List<int> indexes,int count)
        {
            if(indexes.Count==2)
            {
                var first = indexes[0];
                var second = indexes[1];
                return (first + 1) % count == second || (second + 1) % count == first;
            }
            else
            {
                return false;
            }
        }

        private bool IsProjectionLengthEqual(
            Tuple<Point3d,Point3d> mainSeg, 
            Tuple<Point3d, Point3d> firstSeg, 
            Tuple<Point3d, Point3d> secondSeg,
            double tolerance =1.0
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
            var firstSp = firstSeg.Item1.GetProjectPtOnLine(mainSeg.Item1,mainSeg.Item2);
            var firstEp = firstSeg.Item2.GetProjectPtOnLine(mainSeg.Item1, mainSeg.Item2);
            var secondSp = secondSeg.Item1.GetProjectPtOnLine(mainSeg.Item1, mainSeg.Item2);
            var secondEp = secondSeg.Item2.GetProjectPtOnLine(mainSeg.Item1, mainSeg.Item2);
            var mainDis = GetLineDistance(mainSeg);
            var pts = new List<Point3d>() { firstSp, firstEp, secondSp, secondEp };
            var maxPts = pts.GetCollinearMaxPts();
            var totalLength = GetLineDistance(maxPts);
            return IsEqual(mainDis, totalLength, tolerance);
        }

        private bool IsProjectionLengthEqual(
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

            var mainDis = GetLineDistance(mainSeg);
            var pts = new List<Point3d>() { firstSp, firstEp, secondSp, secondEp, thirdSp, thirdEp };
            var maxPts = pts.GetCollinearMaxPts();
            var totalLength = GetLineDistance(maxPts);
            return IsEqual(mainDis, totalLength, tolerance);
        }

        private bool IsParallelIntervalGreaterThan(
            Tuple<Point3d,Point3d> first, 
            Tuple<Point3d, Point3d> second,double distance)
        {
            // 检查两根平行线间距是否大于distance
            var firstDir = GetLineDirection(first);
            var secondDir = GetLineDirection(second);
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

        private bool IsEqual(double first,double second,double tolerance=1e-6)
        {
            return Math.Abs(first - second) <= tolerance;
        }

        private bool IsRectType(Polyline poly)
        {
            // 特点：四条边、对边平行、相邻边垂直
            // 识别条件
            // 多段线是闭合的
            // 有4条边
            // 相邻边是互相垂直的
            var lines = ToLines(poly);
            return lines.Count == 4 && IsAllAnglesVertical(lines);
        }

        private bool IsAllAnglesVertical(List<Tuple<Point3d, Point3d>> lines)
        {
            for(int i=0;i<lines.Count;i++)
            {
                var currentDir = GetLineDirection(lines[i% lines.Count]);
                var nextDir = GetLineDirection(lines[(i+1)%lines.Count]);
                if (!currentDir.IsVertical(nextDir))
                {
                    return false;
                }
            }
            return true;
        }

        private Vector3d GetLineDirection(Tuple<Point3d, Point3d> linePtPair)
        {
            return linePtPair.Item1.GetVectorTo(linePtPair.Item2).GetNormal();
        }

        private double GetLineDistance(Tuple<Point3d, Point3d> linePtPair)
        {
            return linePtPair.Item1.DistanceTo(linePtPair.Item2);
        }

        private List<Tuple<Point3d, Point3d>> ToLines(Polyline poly)
        {
            var results = new List<Tuple<Point3d, Point3d>>();
            for(int i =0;i<poly.NumberOfVertices-1;i++)
            {
                var st = poly.GetSegmentType(i);
                if(st == SegmentType.Line)
                {
                    var lineSeg = poly.GetLineSegmentAt(i);
                    results.Add(Tuple.Create(lineSeg.StartPoint,lineSeg.EndPoint));
                }
            }
            return results;
        }
    }
    internal enum ShapeCode
    {
        L,
        T,
        Rect,
        Unknown
    }
}
