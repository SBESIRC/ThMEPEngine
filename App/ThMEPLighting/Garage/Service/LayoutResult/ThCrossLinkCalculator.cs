using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThCrossLinkCalculator
    {
        /// <summary>
        /// 图的边
        /// 来源于双排布置
        /// Edges中的线和CenterSideDicts没有对应关系关系，
        /// </summary>
        protected List<ThLightEdge> Edges { get; set; } = new List<ThLightEdge>();
        /// <summary>
        /// 车道中心和1号线的Binding
        /// CenterSideDicts.Key 和CenterGroupLines.Dictionary.Key有映射关系
        /// 它们来源于双排布置返回的结果
        /// </summary>
        protected Dictionary<Line, Tuple<List<Line>, List<Line>>> CenterSideDicts { get; set; } //Key->灯线中心线,Value->车道中心线按Buffer之后长生的线
        /// <summary>
        /// 某个区域灯线按连通性分组的结果
        /// </summary>
        private List<Tuple<Point3d, Dictionary<Line, Vector3d>>> CenterGroupLines { get; set; }
        protected ThQueryLineService CenterQuery { get; set; }
        protected ThQueryLineService EdgeQuery { get; set; }
        public ThCrossLinkCalculator(List<ThLightEdge> edges,
            Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts)
        {
            Edges = edges;
            CenterSideDicts = centerSideDicts;
            EdgeQuery = ThQueryLineService.Create(Edges.Select(o => o.Edge).ToList());
            CenterQuery = ThQueryLineService.Create(CenterSideDicts.Select(o => o.Key).ToList());
        }
        public ThCrossLinkCalculator(
            Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts,
            List<Tuple<Point3d, Dictionary<Line, Vector3d>>> centerGroupLines)
        {
            CenterSideDicts = centerSideDicts;
            CenterGroupLines = centerGroupLines;
            CenterQuery = ThQueryLineService.Create(CenterSideDicts.Select(o => o.Key).ToList());
        }

        public List<List<Line>> LinkCableTrayCross()
        {
            var results = new List<List<Line>>();
            var crosses = GetCrosses();
            crosses.ForEach(c =>
            {
                var res = Sort(c);
                // 分区
                var partitions = CreatePartition(res);

                // 收集拐角点
                var turnerPts = new List<Point3d>();
                // 只有分区为偶数
                if (partitions.Count == 4)
                {
                    // 获取中心线附带的边线
                    var sides = GetCenterSides(c);
                    for (int i = 0; i < partitions.Count; i++)
                    {
                        var current = partitions[i];
                        var inters = ThGeometryTool.IntersectWithEx(current.Item1, current.Item2, Intersect.ExtendBoth);
                        if (inters.Count == 0)
                        {
                            continue;
                        }
                        var currentArea = CreateParallelogram(current.Item1, current.Item2);
                        var currentSides = GroupSides(currentArea, sides); // 分组
                        var lineRoadService = new ThLineRoadQueryService(currentSides);
                        var cornerPts = lineRoadService.GetCornerPoints();
                        if (cornerPts.Count > 0)
                        {
                            turnerPts.Add(cornerPts.OrderBy(p => p.DistanceTo(inters[0])).First());
                        }
                    }
                }

                // 连线
                results.Add(Link(c, turnerPts));
            });
            return results.Where(o => o.Count > 0).ToList();
        }

        public List<List<Line>> LinkCableTrayTType()
        {
            var results = new List<List<Line>>();
            var threeways = GetThreeWays();
            threeways.ForEach(o =>
            {
                var pairs = GetLinePairs(o);
                var mainPair = pairs.OrderBy(k => GetLineOuterAngle(k.Item1, k.Item2)).First();
                if (IsMainBranch(mainPair.Item1, mainPair.Item2))
                {
                    var branch = FindBranch(o, mainPair.Item1, mainPair.Item2);
                    results.Add(LinkTType(mainPair.Item1, mainPair.Item2, branch));
                }
            });
            return results.Where(o => o.Count > 0).ToList();
        }

        private List<Point3d> GetCornerPts(Line adjacentA, Line adjacentB, List<Line> sides)
        {
            var area = CreateParallelogram(adjacentA, adjacentB);
            var groupSides = GroupSides(area, sides); // 分组
            var lineRoadService = new ThLineRoadQueryService(groupSides);
            return lineRoadService.GetCornerPoints();
        }

        protected Line FindBranch(List<Line> threeways, Line first, Line second)
        {
            int firstIndex = threeways.IndexOf(first);
            int secondIndex = threeways.IndexOf(second);
            for (int i = 0; i < threeways.Count; i++)
            {
                if (i != firstIndex && i != secondIndex)
                {
                    return threeways[i];
                }
            }
            return null;
        }

        protected bool IsMainBranch(Line first, Line second)
        {
            return ThGarageUtils.IsLessThan45Degree(
                first.StartPoint, first.EndPoint,
                second.StartPoint, second.EndPoint);
        }
        protected List<List<Line>> GetCrosses()
        {
            var centerSidesQuery = new ThLineRoadQueryService(CenterSideDicts.Select(o => o.Key).ToList());
            return centerSidesQuery.GetCross();
        }
        protected List<List<Line>> GetThreeWays()
        {
            var centerSidesQuery = new ThLineRoadQueryService(CenterSideDicts.Select(o => o.Key).ToList());
            return centerSidesQuery.GetThreeWay();
        }
        protected List<List<Line>> FilterByCenterWithoutSides(List<List<Line>> threeWays)
        {
            var centers = FindCentersWithoutSides();
            var garbage = new List<List<Line>>();
            centers.ForEach(o =>
            {
                var subResults = threeWays.Where(w => w.Contains(o)).ToList();
                if(subResults.Count>1)
                {
                    for(int i=1;i< subResults.Count;i++)
                    {
                        garbage.Add(subResults[i]);
                    }
                }
            });
            return threeWays.Where(o=>!garbage.Contains(o)).ToList();
        }

        private List<Line> FindCentersWithoutSides()
        {
            return CenterSideDicts.Where(o => o.Value.Item1.Count + o.Value.Item2.Count == 0).Select(o => o.Key).ToList();
        }

        private List<Line> LinkTType(Line mainLine1, Line mainLine2, Line branch)
        {
            var results = new List<Line>();
            var line1CornerPt = GetCornerPt(mainLine1, branch);
            var line2CornerPt = GetCornerPt(mainLine2, branch);
            if (line1CornerPt.HasValue && line2CornerPt.HasValue)
            {
                var mainLine1EdgeVec = Query(mainLine1);
                var maineLine2EdgeVec = Query(mainLine2);
                var cornerLinkVec = line1CornerPt.Value.GetVectorTo(line2CornerPt.Value);
                if (mainLine1EdgeVec.HasValue && maineLine2EdgeVec.HasValue)
                {
                    if (mainLine1EdgeVec.Value.IsSameDirection(maineLine2EdgeVec.Value))
                    {
                        if (cornerLinkVec.IsSameDirection(mainLine1EdgeVec.Value))
                        {
                            var projectionpt = GetCornerProjectionPt(mainLine1, line1CornerPt.Value);
                            if (projectionpt.HasValue)
                            {
                                results.Add(new Line(line1CornerPt.Value, line2CornerPt.Value));
                                results.Add(new Line(line1CornerPt.Value, projectionpt.Value));
                            }
                        }
                        else
                        {
                            var projectionpt = GetCornerProjectionPt(mainLine2, line2CornerPt.Value);
                            if (projectionpt.HasValue)
                            {
                                results.Add(new Line(line1CornerPt.Value, line2CornerPt.Value));
                                results.Add(new Line(line2CornerPt.Value, projectionpt.Value));
                            }
                        }
                    }
                }
                else
                {
                    //TODO
                }
            }
            return results;
        }

        private Point3d? GetCornerProjectionPt(Line mainLine, Point3d cornerPt)
        {
            var sideLines = GetCenterSides(new List<Line> { mainLine });
            sideLines = sideLines.Where(o => o.IsParallelToEx(mainLine)).ToList();
            if (sideLines.Count > 0)
            {
                return sideLines
                .Select(o => cornerPt.GetProjectPtOnLine(o.StartPoint, o.EndPoint))
                .OrderByDescending(o => cornerPt.DistanceTo(o)).First();
            }
            else
            {
                return null;
            }
        }

        private Point3d? GetCornerPt(Line adjacentA, Line adjacentB)
        {
            var sides = GetCenterSides(new List<Line> { adjacentA, adjacentB });
            var cornerPts = GetCornerPts(adjacentA, adjacentB, sides);
            var inters = ThGeometryTool.IntersectWithEx(adjacentA, adjacentB, Intersect.ExtendBoth);
            if (inters.Count > 0 && cornerPts.Count > 0)
            {
                return cornerPts.OrderBy(o => o.DistanceTo(inters[0])).First();
            }
            return null;
        }

        protected double GetLineOuterAngle(Line first, Line second)
        {
            return ThGarageUtils.CalculateTwoLineOuterAngle(
                first.StartPoint, first.EndPoint, second.StartPoint, second.EndPoint);
        }

        private List<Line> Link(List<Line> crosses, List<Point3d> pts)
        {
            var results = new List<Line>();
            if (crosses.Count == 4 && pts.Count == 4)
            {
                var lineDirs = crosses
                    .Select(o => Tuple.Create(o, Query(o)))
                    .Where(o => o.Item2.HasValue)
                    .ToList();
                if (lineDirs.Count == 4)
                {
                    var mainBranch = FindMainBranch(lineDirs.Select(o => Tuple.Create(o.Item1, o.Item2.Value)).ToList());
                    if (mainBranch != null)
                    {
                        var frame = CreatePolyline(pts);
                        return DrawCrossLinkLines(mainBranch, frame);
                    }
                }
            }
            return results;
        }

        private List<Line> DrawCrossLinkLines(Tuple<Line, Vector3d, Line, Vector3d> mainBranch, Polyline frame)
        {
            var results = new List<Line>();
            var firstIntersPt = mainBranch.Item1.IntersectWithEx(frame);
            var secondIntersPt = mainBranch.Item3.IntersectWithEx(frame);
            if (firstIntersPt.Count == 1 && secondIntersPt.Count == 1)
            {
                var dir = firstIntersPt[0].GetVectorTo(secondIntersPt[0]);
                if (dir.IsSameDirection(mainBranch.Item2))
                {
                    results = SubtractPointOwnerEdge(frame, secondIntersPt[0]);
                }
                else
                {
                    results = SubtractPointOwnerEdge(frame, firstIntersPt[0]);
                }
            }
            return results;
        }

        private List<Line> SubtractPointOwnerEdge(Polyline polyline, Point3d pt)
        {
            var results = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                var lineSeg = polyline.GetLineSegmentAt(i);
                if (ThGeometryTool.IsPointOnLine(lineSeg.StartPoint, lineSeg.EndPoint, pt))
                {
                    continue;
                }
                else
                {
                    results.Add(new Line(lineSeg.StartPoint, lineSeg.EndPoint));
                }
            }
            return results;
        }

        private Polyline CreatePolyline(List<Point3d> pts, bool isClosed = true)
        {
            var newPts = new Point3dCollection();
            pts.ForEach(p => newPts.Add(p));
            return newPts.CreatePolyline(isClosed);
        }

        private Tuple<Line, Vector3d, Line, Vector3d> FindMainBranch(List<Tuple<Line, Vector3d>> lineDirs)
        {
            for (int i = 0; i < lineDirs.Count - 1; i++)
            {
                for (int j = i + 1; j < lineDirs.Count; j++)
                {
                    if (IsMainBranch(lineDirs[i].Item1, lineDirs[i].Item2, lineDirs[j].Item1, lineDirs[j].Item2))
                    {
                        return Tuple.Create(lineDirs[i].Item1, lineDirs[i].Item2, lineDirs[j].Item1, lineDirs[j].Item2);
                    }
                }
            }
            return null;
        }

        private bool IsMainBranch(Line first, Vector3d firstDir, Line second, Vector3d secondDir)
        {
            return ThGarageUtils.IsLessThan45Degree(first.StartPoint, first.EndPoint,
                second.StartPoint, second.EndPoint) && firstDir.IsSameDirection(secondDir);
        }

        private Vector3d? Query(Line line)
        {
            foreach (var item in CenterGroupLines)
            {
                if (item.Item2.ContainsKey(line))
                {
                    return item.Item2[line];
                }
            }
            return null;
        }

        protected Line MergeNeibour(Line current, Dictionary<Line,Line> neibourDict)
        {
            if (neibourDict.ContainsKey(current))
            {
                var pair = ThGeometryTool.GetCollinearMaxPts(new List<Line> { current, neibourDict[current] });
                return new Line(pair.Item1, pair.Item2);
            }
            return current;
        }
        /// <summary>
        /// 创建平行四边形
        /// </summary>
        /// <param name="a">邻边a</param>
        /// <param name="b">邻边b</param>
        /// <returns></returns>
        protected Polyline CreateParallelogram(Line a, Line b)
        {
            var pts = a.IntersectWithEx(b, Intersect.ExtendBoth);
            if (pts.Count == 0)
            {
                return new Polyline();
            }
            var first = pts[0];
            var second = first.GetNextLinkPt(a.StartPoint, a.EndPoint);
            var four = first.GetNextLinkPt(b.StartPoint, b.EndPoint);
            var vec1 = first.GetVectorTo(second);
            var vec2 = first.GetVectorTo(four);
            var third = first + vec1 + vec2;
            var points = new Point3dCollection() { first, second, third, four };
            return points.CreatePolyline();
        }
        protected List<Line> Sort(List<Line> centers)
        {
            // 把十字路口车道线按照逆时针排序
            var lines = AdjustCrossLines(centers);
            return lines
                .OrderBy(l => NewAngle(l.Value.Angle.RadToAng()))
                .Select(o => o.Key)
                .ToList();
        }
        protected Dictionary<Line, Line> AdjustCrossLines(List<Line> crosses)
        {
            /*            
             *            ^
             *            |
             *         <-- -->
             *            |
             *            v
             */
            var result = new Dictionary<Line, Line>();
            var centerPt = GetCenter(crosses);
            if (centerPt.HasValue)
            {
                crosses.ForEach(c =>
                {
                    var farwayPt = centerPt.Value.GetNextLinkPt(c.StartPoint, c.EndPoint);
                    var closePt = farwayPt.GetNextLinkPt(c.StartPoint, c.EndPoint);
                    result.Add(c, new Line(closePt, farwayPt));
                });
            }
            else
            {
                crosses.ForEach(c => result.Add(c, new Line(c.StartPoint, c.EndPoint)));
            }
            return result;
        }
        protected Point3d? GetCenter(List<Line> crosses)
        {
            /*            
             *            ^
             *            |
             *    <----(center)---->
             *            |
             *            v
             */
            Point3d? centerPt = null;
            for (int i = 1; i < crosses.Count; i++)
            {
                var linkPt = crosses[0].FindLinkPt(crosses[i], ThGarageLightCommon.RepeatedPointDistance);
                if (linkPt.HasValue)
                {
                    centerPt = linkPt;
                    break;
                }
            }
            return centerPt;
        }
        protected double NewAngle(double ang)
        {
            return Math.Floor(ang + 0.5) % 360.0;
        } 
        protected List<Line> GetCenterSides(List<Line> centers)
        {
            return centers.SelectMany(c => GetCenterSides(c)).ToList();
        }

        protected List<Line> GetCenterSides(Line center)
        {
            var results = new List<Line>();
            if (IsContains(center))
            {
                results.AddRange(CenterSideDicts[center].Item1);
                results.AddRange(CenterSideDicts[center].Item2);
            }
            return results;
        }

        protected bool IsContains(Line center)
        {
            return CenterSideDicts.ContainsKey(center);
        }

        protected List<Line> FindNeibours(Line center, Point3d port)
        {
            var neibours = CenterQuery.Query(port, ThGarageLightCommon.RepeatedPointDistance).ToList();
            neibours.Remove(center);
            return neibours;
        }

        protected Line FindCollinearNeibour(Line center,Point3d port)
        {
            var neibours = FindNeibours(center, port);
            var tolerance = ThGarageLightCommon.RepeatedPointDistance;
            neibours = neibours
                .Where(o => IsCollinear(center, o, tolerance))
                .OrderBy(o => GetOuterAngle(center, o))
                .ToList();
            if(neibours.Count>0)
            {
                return neibours[0];
            }
            return null;
        }

        private bool IsCollinear(Line first,Line second,double tolerance)
        {
            return ThGeometryTool.IsCollinearEx(
                first.StartPoint, first.EndPoint, second.StartPoint, second.EndPoint, tolerance);
        }

        private double GetOuterAngle(Line first, Line second)
        {
            return ThGarageUtils.CalculateTwoLineOuterAngle(
                first.StartPoint, first.EndPoint, second.StartPoint, second.EndPoint);
        }

        protected List<Tuple<Line, Line>> CreatePartition(List<Line> lines)
        {
            var partitions = new List<Tuple<Line, Line>>();
            int count = lines.Count;
            for (int i = 0; i < count; i++)
            {
                var adjacentEdgeA = lines[i];
                var adjacentEdgeB = lines[(i + 1) % count];
                if (!adjacentEdgeA.IsParallelToEx(adjacentEdgeB))
                {
                    partitions.Add(Tuple.Create(adjacentEdgeA, adjacentEdgeB));
                }
            }
            return partitions;
        }
        protected virtual List<Line> GroupSides(Polyline partition, List<Line> sides)
        {
            return sides
                .Where(e => partition.EntityContains(e.StartPoint) || partition.EntityContains(e.EndPoint))
                .ToList();
        }
        protected List<Tuple<Line, Line>> GetLinePairs(List<Line> lines)
        {
            var results = new List<Tuple<Line, Line>>();
            for (int i = 0; i < lines.Count - 1; i++)
            {
                for (int j = i + 1; j < lines.Count; j++)
                {
                    results.Add(Tuple.Create(lines[i], lines[j]));
                }
            }
            return results;
        }
    }
}
