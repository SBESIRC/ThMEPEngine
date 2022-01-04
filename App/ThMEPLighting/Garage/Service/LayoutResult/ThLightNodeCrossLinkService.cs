using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using Dreambuild.AutoCAD;
using ThCADExtension;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    /// <summary>
    /// 用于连接十字路口的灯
    /// </summary>
    internal sealed class ThLightNodeCrossLinkService: ThCrossLinkCalculator
    {
        public ThLightNodeCrossLinkService(List<ThLightEdge> edges, 
            Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts):base(edges,centerSideDicts)
        {
            
        }
        public List<ThLightNodeLink> LinkOppositeCross()
        {
            var results = new List<ThLightNodeLink>();
            var crosses = GetCrosses();
            crosses.Where(o => o.Count == 4).ForEach(c => results.AddRange(LinkOppositeCross(c)));
            return results;
        }

        public List<ThLightNodeLink> LinkAdjacentCross()
        {
            var results = new List<ThLightNodeLink>();
            var crosses = GetCrosses();
            crosses.Where(o => o.Count == 4).ForEach(c => results.AddRange(LinkAdjacentCross(c)));
            return results;
        }

        public List<ThLightNodeLink> LinkOppositeThreeWay()
        {
            /*
                        |
                        |(branch)
                        |
             ___________|___________
               (main1)     (main2)
            */
            var results = new List<ThLightNodeLink>();
            var threeWays = GetThreeWays();
            threeWays = FilterByCenterWithoutSides(threeWays);
            threeWays.ForEach(o =>
            {
                var pairs = GetLinePairs(o);
                var mainPair = pairs.OrderBy(k => GetLineOuterAngle(k.Item1,k.Item2)).First();
                if(IsMainBranch(mainPair.Item1, mainPair.Item2))
                {
                    var branch = FindBranch(o, mainPair.Item1, mainPair.Item2);
                    var oppositeBranch = GetOpposite(mainPair.Item1, branch);
                    var orders = new List<Line> { mainPair.Item1, branch, mainPair.Item2, oppositeBranch };
                    results.AddRange(LinkOppositeCross(orders));
                }
            });
            return results;
        }

        public List<ThLightNodeLink> LinkAdjacentThreeWay()
        {
            var results = new List<ThLightNodeLink>();
            var threeWays = GetThreeWays();
            threeWays = FilterByCenterWithoutSides(threeWays);
            threeWays.ForEach(o =>
            {
                var pairs = GetLinePairs(o);
                var mainPair = pairs.OrderBy(k => GetLineOuterAngle(k.Item1, k.Item2)).First();
                if (IsMainBranch(mainPair.Item1, mainPair.Item2))
                {
                    var branch = FindBranch(o, mainPair.Item1, mainPair.Item2);
                    results.AddRange(LinkAdjacentThreeway(mainPair.Item1, mainPair.Item2, branch));
                }
            });
            return results;
        }

        private Line GetOpposite(Line main,Line branch)
        {
            var linkPtRes = main.FindLinkPt(branch);
            var farawayPt = linkPtRes.Value.GetNextLinkPt(branch.StartPoint,branch.EndPoint);
            var vec = farawayPt.GetVectorTo(linkPtRes.Value).GetNormal();
            return new Line(linkPtRes.Value, linkPtRes.Value + vec.MultiplyBy(branch.Length));
        }

        private List<ThLightNodeLink> LinkOppositeCross(List<Line> cross)
        {
            var results = new List<ThLightNodeLink>();
            var res = Sort(cross);
            // 对于没有边线的中心线，获取其符合条件的邻居
            var neibourDict = CreateNeibourDict(res);
            var allNeibourDict = CreateAllNeibourDict(res);

            // 分区
            var partitions = CreatePartition(res);

            // 获取中心线附带的边线
            var sides = new List<Line>();
            sides.AddRange(GetCenterSides(cross));
            sides.AddRange(GetCenterSides(allNeibourDict.SelectMany(o => o.Value).ToList()));
            // 通过sides找到Edges中的边
            var edgeLines = sides.SelectMany(o => GetEdges(o)).ToList();
            // 创建对角区域的灯Link
            var edges = Edges.Where(o => edgeLines.Contains(o.Edge)).ToList();
            var half = partitions.Count / 2;
            var bufferService = new ThMEPEngineCore.Service.ThNTSBufferService();
            var bufferTolerance = 1.0; //解决点在区域边界上的问题
            for (int i = 0; i < half; i++)
            {
                var current = partitions[i];
                var currentAdjacentA = MergeNeibour(current.Item1, neibourDict);
                var currentAdjacentB = MergeNeibour(current.Item2, neibourDict);
                var currentArea = CreateParallelogram(currentAdjacentA, currentAdjacentB);
                var currentEdges = GroupEdges(currentArea, edges); // 分组
                var currentNodes = GetPartitionCloseNodes(current, currentEdges, currentArea);
                var currentNeibourLinkPt = current.Item1.FindLinkPt(current.Item2);

                var opposite = partitions[i + half];
                var oppositeAdjacentA = MergeNeibour(opposite.Item1, neibourDict);
                var oppositeAdjacentB = MergeNeibour(opposite.Item2, neibourDict);
                var oppositeArea = CreateParallelogram(oppositeAdjacentA, oppositeAdjacentB);
                var oppositeEdges = GroupEdges(oppositeArea, edges);
                var newOppositeArea = bufferService.Buffer(oppositeArea, bufferTolerance) as Polyline;
                var oppositeNodes = GetPartitionCloseNodes(opposite, oppositeEdges, newOppositeArea);
                var oppositeNeibourLinkPt = opposite.Item1.FindLinkPt(opposite.Item2);

                // 添加转接点
                var linkRes = Link(currentNodes, oppositeNodes);
                if (currentNeibourLinkPt.HasValue)
                {
                    linkRes.ForEach(l => l.CrossIntersectionPt = currentNeibourLinkPt.Value);
                }
                else if (oppositeNeibourLinkPt.HasValue)
                {
                    linkRes.ForEach(l => l.CrossIntersectionPt = oppositeNeibourLinkPt.Value);
                }
                results.AddRange(linkRes);
            }
            return results;
        }

        private List<ThLightNodeLink> LinkAdjacentCross(List<Line> cross)
        {
            var results = new List<ThLightNodeLink>();
            var res = Sort(cross);
            for (int i = 0; i < 4; i++)
            {
                var first = res[i];
                var second = res[(i + 2) % 4];
                var branch = res[(i + 1) % 4];
                results.AddRange(GetLightNodeLinks(first, second, branch));
            }
            return results;
        }

        private List<ThLightNodeLink> GetLightNodeLinks(Line first,Line second, Line branch)
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
            var results= new List<ThLightNodeLink>();
            var centers = new List<Line>() { first, second };
            var edges = GetCenterSideEdges(centers);
            // 对于没有边线的中心线，获取其符合条件的邻居
            var neibourDict = CreateNeibourDict(centers);

            var firstEdge = MergeNeibour(first, neibourDict);
            var secondEdge = MergeNeibour(second, neibourDict);
            var branchEdge = MergeNeibour(branch, neibourDict);

            var firstArea = CreateParallelogram(firstEdge, branchEdge);
            var secondArea = CreateParallelogram(secondEdge, branchEdge);

            var firstEdges = GroupEdges(firstArea, edges); // 分组
            var secondEdges = GroupEdges(secondArea, edges);// 分组
            firstEdges = firstEdges.Where(o => o.Direction.IsParallelToEx(first.LineDirection())).ToList();
            secondEdges = secondEdges.Where(o => o.Direction.IsParallelToEx(second.LineDirection())).ToList();

            var linkPt = first.FindLinkPt(second,ThGarageLightCommon.RepeatedPointDistance);

            if(linkPt.HasValue)
            {
                var firstFarwayPt = linkPt.Value.GetNextLinkPt(first.StartPoint, first.EndPoint);
                firstEdges = Sort(firstEdges, linkPt.Value, firstFarwayPt);
                var secondFarwayPt = linkPt.Value.GetNextLinkPt(second.StartPoint, second.EndPoint);
                secondEdges = Sort(secondEdges, linkPt.Value, secondFarwayPt);
                firstEdges = Filter(firstEdges);
                secondEdges = Filter(secondEdges);
                if(firstEdges.Count==0 || secondEdges.Count==0)
                {
                    return results;
                }
                if(firstEdges[0].EdgePattern!= secondEdges[0].EdgePattern)
                {
                    return results;
                }
                var firstClosePt = GetSameDirectionClosestPt(firstEdges.Select(o => o.Edge).ToList(), linkPt.Value, firstFarwayPt);
                var secondClosePt = GetSameDirectionClosestPt(secondEdges.Select(o => o.Edge).ToList(), linkPt.Value, secondFarwayPt);

                var passEdge = CreateEdge(firstClosePt, secondClosePt);
                passEdge.EdgePattern = firstEdges[0].EdgePattern;

                var pathEdges = new List<ThLightEdge>();
                firstEdges.Reverse();
                pathEdges.AddRange(firstEdges);
                pathEdges.Add(passEdge);
                pathEdges.AddRange(secondEdges);

                var linkPath = new ThLinkPath()
                {
                    Start = firstFarwayPt,
                    Edges = pathEdges,
                };
                var linkService = new ThLightNodeSameLinkService(new List<ThLinkPath> { linkPath });
                var lightNodeLinks = linkService.FindLightNodeLink1();
            }
            return results;
        }

        private ThLightEdge CreateEdge(Point3d sp,Point3d ep)
        {
            return new ThLightEdge(new Line(sp, ep));
        }

        private List<ThLightEdge> GetCenterSideEdges(List<Line> centers)
        {
            // 获取中心线附带的边线
            var sides = new List<Line>();
            sides.AddRange(GetCenterSides(centers));

            // 通过sides找到Edges中的边
            var edgeLines = sides.SelectMany(o => GetEdges(o)).ToList();
            // 创建对角区域的灯Link
            return Edges.Where(o => edgeLines.Contains(o.Edge)).ToList();
        }

        private List<ThLightEdge> Sort(List<ThLightEdge> edges,Point3d sp,Point3d ep)
        {
            // 根据sp到ep的方向
            return edges
                .OrderBy(e => GetMidPt(e.Edge).GetProjectPtOnLine(sp, ep).DistanceTo(sp))
                .ToList();
        }
        private Point3d GetSameDirectionClosestPt(List<Line> edges, Point3d sp, Point3d ep)
        {
            // 根据sp到ep的方向
            var vec = sp.GetVectorTo(ep);
            return edges
                .SelectMany(o => GetPoints(o))
                .Where(o => sp.GetVectorTo(o)
                .IsSameDirection(vec))
                .OrderBy(o => o.DistanceTo(sp))
                .FirstOrDefault();
        }

        private List<Point3d> GetPoints(Line line)
        {
            return new List<Point3d> { line.StartPoint, line.EndPoint };
        }

        private List<ThLightEdge> Filter(List<ThLightEdge> edges)
        {
            // 过滤连续的具有相同EdgePattern的边
            var results = new List<ThLightEdge>();
            if(edges.Count>0)
            {
                results.Add(edges[0]);
                for (int i = 1; i < edges.Count; i++)
                {
                    if (edges[i].EdgePattern!= results[0].EdgePattern)
                    {
                        break;
                    }
                    results.Add(edges[i]);
                }
            }            
            return results;
        }

        private Point3d GetMidPt(Line line)
        {
            return line.StartPoint.GetMidPt(line.EndPoint);
        }

        private List<ThLightNodeLink> LinkAdjacentThreeway(Line main1, Line main2, Line branch)
        {
            return GetLightNodeLinks(main1, main2, branch);
        }

        private Dictionary<Line, Line> CreateNeibourDict(List<Line> crosses)
        {
            // 对于中心线没有边线的，获取其共线的邻居
            var results = new Dictionary<Line, Line>();
            var centerPt = GetCenter(crosses);
            if (centerPt.HasValue)
            {
                crosses.Where(o => IsContains(o)).Where(o => GetCenterSides(o).Count == 0).ForEach(o =>
                     {
                        var port = centerPt.Value.GetNextLinkPt(o.StartPoint, o.EndPoint);
                        var neibour = FindCollinearNeibour(o, port);
                         if(neibour!=null)
                         {
                             results.Add(o, neibour);
                         }
                     });
            }
            return results;
        }

        private Dictionary<Line, List<Line>> CreateAllNeibourDict(List<Line> crosses)
        {
            // 对于中心线没有边线的，获取其共线的邻居
            var results = new Dictionary<Line, List<Line>>();
            var centerPt = GetCenter(crosses);
            if (centerPt.HasValue)
            {
                crosses.Where(o => IsContains(o)).Where(o => GetCenterSides(o).Count == 0).ForEach(o =>
                {
                    var port = centerPt.Value.GetNextLinkPt(o.StartPoint, o.EndPoint);
                    var neibours = FindNeibours(o, port);
                    results.Add(o, neibours);
                });
            }
            return results;
        }

        private List<ThLightNodeLink> Link(List<ThLightNode> firstNodes, List<ThLightNode> secondNodes)
        {
            var results = new List<ThLightNodeLink>();
            firstNodes.ForEach(f =>
            {
                secondNodes.ForEach(s =>
                {
                    if (f.Number == s.Number)
                    {
                        var nodeLink = new ThLightNodeLink()
                        {
                            First = f,
                            Second = s,
                            IsCrossLink = true,
                        };
                        // 这儿的Edges是连不起来的,用于创建跳线使用
                        nodeLink.Edges.Add(FindLightNodeEdge(f.Id));
                        nodeLink.Edges.Add(FindLightNodeEdge(s.Id));
                        results.Add(nodeLink);
                    }
                });
            });
            return Filter(results); //只保留一个
        }

        private List<ThLightNodeLink> Filter(List<ThLightNodeLink> nodeLinks)
        {
            var results = new List<ThLightNodeLink>();
            var numbers = nodeLinks.Select(o => o.First.Number).Distinct().ToList();
            numbers.ForEach(o =>
            {
                var sorts = nodeLinks
                 .Where(l => l.First.Number == o)
                 .OrderBy(l => l.First.Position.DistanceTo(l.Second.Position))
                 .ToList();
                if (sorts.Count > 0)
                {
                    results.Add(sorts.First());
                }
            });
            return results;
        }

        private Line FindLightNodeEdge(string lightNodeId)
        {
           return Edges
                .Where(o => o.LightNodes.Select(n => n.Id).Contains(lightNodeId))
                .FirstOrDefault().Edge;
        }

        private List<ThLightNode> GetPartitionCloseNodes(
            Tuple<Line,Line> partition,List<ThLightEdge> edges,Polyline partitionArea)
        {
            var results = new List<ThLightNode>();
            var item1Nodes = GetClosestNodes(partition.Item1, partition.Item2, edges, partitionArea);
            var item2Nodes = GetClosestNodes(partition.Item2, partition.Item1, edges, partitionArea);
            results.AddRange(item1Nodes);
            results.AddRange(item2Nodes);
            return results;
        }

        private List<ThLightNode> GetClosestNodes(Line adjacentA, Line adjacentB, List<ThLightEdge> edges, Polyline partitionArea)
        {
            var results = new List<ThLightNode>();
            var inters = adjacentA.IntersectWithEx(adjacentB);
            if (inters.Count == 0)
            {
                return results;
            }
            var projectionAxis = GetCenterProjectionAxis(adjacentA.StartPoint, adjacentA.EndPoint, inters[0]);
            var parallels = GetParallels(adjacentA, edges.Select(o => o.Edge).ToList());
            var parallelEdges = edges.Where(o => parallels.Contains(o.Edge)).ToList();
            var lightNodes = parallelEdges.SelectMany(o => o.LightNodes).ToList();
            lightNodes = lightNodes.Where(o=>partitionArea.EntityContains(o.Position)).ToList(); // 过滤在分区里的灯

            lightNodes = lightNodes
                .OrderByDescending(o=> o.Position.GetProjectPtOnLine(
                    projectionAxis.Item1, projectionAxis.Item2)
                .DistanceTo(projectionAxis.Item1)).ToList();
            lightNodes.ForEach(o =>
            {
                if(!results.Select(r=>r.Number).Contains(o.Number))
                {
                    results.Add(o);
                }
            });
            return results;
        }

        private Tuple<Point3d,Point3d> GetCenterProjectionAxis(Point3d lineSp,Point3d lineEp, Point3d cornerPt)
        {
            var farwayPt = cornerPt.GetNextLinkPt(lineSp, lineEp);
            return Tuple.Create(farwayPt, cornerPt);
        }

        private List<Line> GetParallels(Line line,List<Line> lines)
        {
            return lines.Where(o => line.IsParallelToEx(o)).ToList();
        }

        private List<Line> GetEdges(Line line,double width=1.0)
        {
            var lines = EdgeQuery.QueryCollinearLines(line.StartPoint, line.EndPoint, width);
            return lines.Where(o => line.HasCommon(o)).ToList();
        }
        private List<ThLightEdge> GroupEdges(Polyline partition, List<ThLightEdge> edges)
        {
            var results = new List<ThLightEdge>();
            edges.ForEach(e =>
            {
                if (e.LightNodes.Select(n => n.Position).Where(n => partition.EntityContains(n)).Any())
                {
                    results.Add(e);
                }
            });
            return results;
        }
    }
}
