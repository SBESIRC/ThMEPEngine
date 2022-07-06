using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThCADCore.NTS;

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
        protected ThCrossLinkCalculator()
        {
        }
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
        protected List<Line> CenterLines
        {
            get
            {
                return CenterSideDicts.Select(o => o.Key).ToList();
            }
        }
        public List<List<Line>> LinkCableTrayCross()
        {
            var results = new List<List<Line>>();
            var crosses = CenterLines.GetCrosses();
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
                        var currentArea = current.Item1.CreateParallelogram(current.Item2);
                        var currentSides = currentArea.GroupSides(sides); // 分组
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
            var threeways = CenterLines.GetThreeWays();
            threeways.ForEach(o =>
            {
                var pairs = o.GetLinePairs();
                var mainPair = pairs.OrderBy(k => k.Item1.GetLineOuterAngle(k.Item2)).First();
                if (mainPair.Item1.IsLessThan45Degree(mainPair.Item2))
                {
                    var branch = o.FindBranch(mainPair.Item1, mainPair.Item2);
                    results.Add(LinkTType(mainPair.Item1, mainPair.Item2, branch));
                }
            });
            return results.Where(o => o.Count > 0).ToList();
        }

        public List<ThLightEdge> BuildCrossLinkEdges()
        {
            // 把十字型单边同为1 or 2号线 连起来
            var crosses = CenterLines.GetCrosses();
            return crosses
                .Where(o => o.Count == 4)
                .SelectMany(c => BuildCrossLinkEdges(c))
                .ToList();
        }

        public List<ThLightEdge> BuildThreeWayLinkEdges()
        {
            // 把T字型单边同为1 or 2号线 连起来
            var results = new List<ThLightEdge>();
            var threeways = CenterLines.GetThreeWays();
            threeways.Where(o=>o.Count==3).ForEach(o =>
            {
                var pairs = o.GetLinePairs();
                var mainPair = pairs.OrderBy(k => k.Item1.GetLineOuterAngle(k.Item2)).First();
                if (mainPair.Item1.IsLessThan45Degree(mainPair.Item2))
                {
                    var branch = o.FindBranch(mainPair.Item1, mainPair.Item2);
                    results.AddRange(BuildLinkEges(mainPair.Item1, mainPair.Item2, branch));
                }
            });
            return results;
        }

        private List<ThLightEdge> BuildCrossLinkEdges(List<Line> cross)
        {
            var results = new List<ThLightEdge>();
            var res = Sort(cross);
            for (int i = 0; i < 4; i++)
            {
                var first = res[i];
                var second = res[(i + 2) % 4];
                var branch = res[(i + 1) % 4];
                results.AddRange(BuildLinkEges(first, second, branch));
            }
            return results;
        }

        private List<ThLightEdge> BuildLinkEges(Line first, Line second, Line branch)
        {
            /*          
             *            |
             *            | < branch
             *            | 
             *  ---------------------
             *      ^     |    ^
             *   first    |  second
             *            |
             */
            var results = new List<ThLightEdge>();
            var centers = new List<Line>() { first, second };
            // 对于没有边线的中心线，获取其符合条件的邻居
            var neibourDict = CreateNeibourDict(centers);
            var edges = GetCenterSideEdges(centers);
            edges.AddRange(GetCenterSideEdges(neibourDict.Values.ToList()));
            var firstLines = MergeNeibour(first, neibourDict);
            var secondLines = MergeNeibour(second, neibourDict);
            var branchLines = MergeNeibour(branch, neibourDict);

            var firstArea = firstLines.CreateParallelogram(branchLines);
            var secondArea = secondLines.CreateParallelogram(branchLines);
            var firstEdges = GroupEdges(firstArea, edges); // 分组
            var secondEdges = GroupEdges(secondArea, edges);// 分组
            
            firstEdges = firstEdges.Where(o => o.Direction.IsParallelToEx(first.LineDirection())).ToList();
            secondEdges = secondEdges.Where(o => o.Direction.IsParallelToEx(second.LineDirection())).ToList();

            var linkPt = first.FindLinkPt(second, ThGarageLightCommon.RepeatedPointDistance);
            if (linkPt.HasValue)
            {
                var firstFarwayPt = linkPt.Value.GetNextLinkPt(first.StartPoint, first.EndPoint);
                firstEdges = Sort(firstEdges, linkPt.Value, firstFarwayPt);
                var secondFarwayPt = linkPt.Value.GetNextLinkPt(second.StartPoint, second.EndPoint);
                secondEdges = Sort(secondEdges, linkPt.Value, secondFarwayPt);
                if (firstEdges.Count == 0 || secondEdges.Count == 0)
                {
                    return results;
                }
                if (firstEdges[0].EdgePattern != secondEdges[0].EdgePattern)
                {
                    return results;
                }
                var firstClosePt = GetSameDirectionClosestPt(new List<Line> { firstEdges[0].Edge }, linkPt.Value, firstFarwayPt);
                var secondClosePt = GetSameDirectionClosestPt(new List<Line> { secondEdges[0].Edge }, linkPt.Value, secondFarwayPt);

                var passEdge = CreateEdge(firstClosePt, secondClosePt);
                passEdge.EdgePattern = firstEdges[0].EdgePattern;
                if(firstEdges[0].Direction.IsSameDirection(secondEdges[0].Direction))
                {
                    passEdge.Direction = firstEdges[0].Direction;
                }
                else
                {
                    //
                }
                results.Add(passEdge);
            }
            return results;
        }
     

        private List<Point3d> GetCornerPts(Line adjacentA, Line adjacentB, List<Line> sides)
        {
            var area = adjacentA.CreateParallelogram(adjacentB);
            var groupSides = area.GroupSides(sides); // 分组
            var lineRoadService = new ThLineRoadQueryService(groupSides);
            return lineRoadService.GetCornerPoints();
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
                        var frame = pts.CreatePolyline();
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

        //protected Tuple<Line,Polyline> MergeNeibour(Line current, Dictionary<Line,Line> neibourDict)
        //{
        //    if (neibourDict.ContainsKey(current))
        //    {
        //        var res = Merge(current,neibourDict[current]);
        //        var triangle = current.GetTwoLinkLineMergeTriangle(neibourDict[current]);
        //        return Tuple.Create(res, triangle);
        //    }
        //    return Tuple.Create(current, new Polyline() { Closed=true});
        //}

        protected List<Line> MergeNeibour(Line current, Dictionary<Line, Line> neibourDict)
        {
            var results = new List<Line>() { current};
            if (neibourDict.ContainsKey(current))
            {
                results.Add(neibourDict[current]);
            }
            return results;
        }

        private Line Merge(Line first, Line second)
        {
            var pts = new List<Point3d>();
            pts.Add(second.StartPoint.GetProjectPtOnLine(first.StartPoint, first.EndPoint));
            pts.Add(second.EndPoint.GetProjectPtOnLine(first.StartPoint, first.EndPoint));
            pts.Add(first.StartPoint);
            pts.Add(first.EndPoint);
            var pair = pts.GetCollinearMaxPts();
            return new Line(pair.Item1, pair.Item2);
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
                .Where(o => center.IsCollinear(o, tolerance))
                .OrderBy(o => center.CalculateTwoLineOuterAngle(o))
                .ToList();
            if(neibours.Count>0)
            {
                return neibours[0];
            }
            return null;
        }

        protected Line FindSmallestOutAngleNeibour(Line center, Point3d port)
        {
            var neibours = FindNeibours(center, port);
            var tolerance = ThGarageLightCommon.RepeatedPointDistance;
            if (neibours.Count > 0)
            {
                return neibours.OrderBy(o => center.CalculateTwoLineOuterAngle(o)).First();
            }
            return null;
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

        protected Dictionary<Line, Line> CreateNeibourDict(List<Line> crosses)
        {
            // 对于中心线没有边线的，获取其共线的邻居
            var results = new Dictionary<Line, Line>();
            var centerPt = GetCenter(crosses);
            if (centerPt.HasValue)
            {
                crosses.Where(o => IsContains(o)).ForEach(o =>
                {
                    var port = centerPt.Value.GetNextLinkPt(o.StartPoint, o.EndPoint);
                    var neibour = FindCollinearNeibour(o, port);
                    if (neibour != null)
                    {
                        results.Add(o, neibour);
                    }
                    else
                    {
                        neibour = FindSmallestOutAngleNeibour(o, port);
                        if(neibour!=null && o.IsLessThan45Degree(neibour))
                        {
                            results.Add(o, neibour);
                        }
                    }
                });
            }
            return results;
        }
        
        protected Point3d GetSameDirectionClosestPt(List<Line> edges, Point3d sp, Point3d ep)
        {
            // 根据sp到ep的方向
            var vec = sp.GetVectorTo(ep);
            return edges
                .SelectMany(o => o.GetPoints())
                .Where(o => sp.GetVectorTo(o)
                .IsSameDirection(vec))
                .OrderBy(o => o.DistanceTo(sp))
                .FirstOrDefault();
        }

        #region --------- 对边的操作 ----------
        protected List<ThLightEdge> GetCenterSideEdges(List<Line> centers)
        {
            // 获取中心线附带的边线
            var sides = new List<Line>();
            sides.AddRange(GetCenterSides(centers));

            // 通过sides找到Edges中的边
            var edgeLines = sides.SelectMany(o => GetEdges(o)).ToList();
            // 创建对角区域的灯Link
            return Edges.Where(o => edgeLines.Contains(o.Edge)).ToList();
        }
        protected List<Line> GetEdges(Line line, double width = 1.0)
        {
            var lines = EdgeQuery.QueryCollinearLines(line.StartPoint, line.EndPoint, width);
            return lines.Where(o => line.HasCommon(o)).ToList();
        }
        protected List<ThLightEdge> GetEdges(List<Line> lines)
        {
            // 传入ThLightEdge的几何中心线，返回所在的边
            return Edges.Where(o => lines.Contains(o.Edge)).ToList();
        }
        protected List<ThLightEdge> GroupEdges(Polyline partition, List<ThLightEdge> edges)
        {
            var results = new List<ThLightEdge>();
            edges.ForEach(e =>
            {
                if (e.LightNodes.Select(n => n.Position).Where(n => partition.Contains(n)).Any())
                {
                    results.Add(e);
                }
                else if (partition.Contains(e.Edge.StartPoint) || partition.Contains(e.Edge.EndPoint))
                {
                    results.Add(e);
                }
            });
            return results;
        }
        private List<Line> FilterEdgesByTriangle(List<Polyline> triangles, List<Line> edges)
        {
            triangles = triangles.Where(o => o.Area > 1.0).ToList();
            if(triangles.Count==0)
            {
                return edges;
            }
            return edges.Where(o => !triangles.Where(p => p.IsIntersects(o)).Any()).ToList();
        }

        protected List<ThLightEdge> FilterEdgesByTriangle(List<Polyline> triangles, List<ThLightEdge> edges)
        {
            //var lines = FilterEdgesByTriangle(triangles, edges.Select(o => o.Edge).ToList());
            //return edges.Where(o=> lines.Contains(o.Edge)).ToList();
            // 暂不处理
            return edges;
        }

        protected List<ThLightEdge> Sort(List<ThLightEdge> edges, Point3d sp, Point3d ep)
        {
            // 根据sp到ep的方向
            return edges
                .OrderBy(e => e.Edge.GetMidPt().GetProjectPtOnLine(sp, ep).DistanceTo(sp))
                .ToList();
        }
        protected ThLightEdge CreateEdge(Point3d sp, Point3d ep)
        {
            return new ThLightEdge(new Line(sp, ep));
        }
        #endregion
    }
}
