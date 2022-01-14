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
            var crosses = CenterLines.GetCrosses();
            crosses.Where(o => o.Count == 4).ForEach(c => results.AddRange(LinkOppositeCross(c)));
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
            var threeWays = CenterLines.GetThreeWays();
            threeWays = FilterByCenterWithoutSides(threeWays);
            threeWays.ForEach(o =>
            {
                var pairs = o.GetLinePairs();
                var mainPair = pairs.OrderBy(k => k.Item1.GetLineOuterAngle(k.Item2)).First();
                if (mainPair.Item1.IsLessThan45Degree(mainPair.Item2))
                {
                    var branch = o.FindBranch(mainPair.Item1, mainPair.Item2);
                    var oppositeBranch = GetOpposite(mainPair.Item1, branch);
                    var orders = new List<Line> { mainPair.Item1, branch, mainPair.Item2, oppositeBranch };
                    results.AddRange(LinkOppositeCross(orders));
                }
            });
            return results;
        }
        public List<ThLightNodeLink> LinkCrossCorner()
        {
            var results = new List<ThLightNodeLink>();
            var crosses = CenterLines.GetCrosses();
            crosses.Where(o => o.Count == 4).ForEach(c => results.AddRange(LinkCrossCorner(c)));
            return results;
        }
        public List<ThLightNodeLink> LinkThreeWayCorner()
        {
            var results = new List<ThLightNodeLink>();
            var threeWays = CenterLines.GetThreeWays();
            threeWays.Where(o=>o.Count==3).ForEach(o =>
            {
                var pairs = o.GetLinePairs();
                var mainPair = pairs.OrderBy(k => k.Item1.GetLineOuterAngle(k.Item2)).First();
                if (mainPair.Item1.IsLessThan45Degree(mainPair.Item2))
                {
                    var branch = o.FindBranch(mainPair.Item1, mainPair.Item2);
                    results.AddRange(LinkThreewayCorner(mainPair.Item1, mainPair.Item2, branch));
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
 
            // 分区
            var partitions = CreatePartition(res);

            // 获取中心线附带的边线
            var sides = new List<Line>();
            sides.AddRange(GetCenterSides(cross));
            sides.AddRange(GetCenterSides(neibourDict.Values.ToList()));
            // 通过sides找到Edges中的边
            var edgeLines = sides.SelectMany(o => GetEdges(o)).ToList();
            // 创建对角区域的灯Link
            var edges = Edges.Where(o => edgeLines.Contains(o.Edge)).ToList();
            var half = partitions.Count / 2;
            var bufferService = new ThMEPEngineCore.Service.ThNTSBufferService();
            for (int i = 0; i < half; i++)
            {
                var current = partitions[i];
                var currentNodes = new List<ThLightNode>();
                var currentNeibourLinkPt = current.Item1.FindLinkPt(current.Item2);
                var currentAdjacentA = MergeNeibour(current.Item1, neibourDict);
                var currentAdjacentB = MergeNeibour(current.Item2, neibourDict);       
                var currentArea= currentAdjacentA.Item1.CreateParallelogram(currentAdjacentB.Item1);
                var currentEdges = GroupEdges(currentArea, edges); // 分组
                currentEdges = FilterEdgesByTriangle(new List<Polyline> { currentAdjacentA.Item2, currentAdjacentB.Item2},currentEdges);

                var currentItem1Edges = FilterEdges(currentEdges, current.Item1, neibourDict);
                currentNodes.AddRange(GetLinkNodes(current.Item1, currentNeibourLinkPt.Value, currentItem1Edges));
                var currentItem2Edges = FilterEdges(currentEdges, current.Item2, neibourDict);
                currentNodes.AddRange(GetLinkNodes(current.Item2, currentNeibourLinkPt.Value, currentItem2Edges));
                
                var opposite = partitions[i + half];
                var oppositeNodes = new List<ThLightNode>();
                var oppositeNeibourLinkPt = opposite.Item1.FindLinkPt(opposite.Item2);
                var oppositeAdjacentA = MergeNeibour(opposite.Item1, neibourDict);
                var oppositeAdjacentB = MergeNeibour(opposite.Item2, neibourDict);
                var oppositeArea = oppositeAdjacentA.Item1.CreateParallelogram(oppositeAdjacentB.Item1);
                var oppositeEdges = GroupEdges(oppositeArea, edges);
                oppositeEdges = FilterEdgesByTriangle(new List<Polyline> { oppositeAdjacentA.Item2, oppositeAdjacentB.Item2 }, oppositeEdges);

                var oppositeItem1Edges = FilterEdges(currentEdges, current.Item1, neibourDict);
                oppositeNodes.AddRange(GetLinkNodes(opposite.Item1, oppositeNeibourLinkPt.Value, oppositeItem1Edges));
                var oppositeItem2Edges = FilterEdges(currentEdges, current.Item2, neibourDict);
                oppositeNodes.AddRange(GetLinkNodes(opposite.Item2, oppositeNeibourLinkPt.Value, oppositeItem2Edges));

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

        private List<ThLightNode> GetLinkNodes(Line line1,Point3d linkPt,List<ThLightEdge> line1Edges)
        {
            // 获取可以连接的灯节点
            var results = new List<ThLightNode>();
            var line1FarwayPt = linkPt.GetNextLinkPt(line1.StartPoint, line1.EndPoint);
            var edges = Sort(line1Edges, linkPt, line1FarwayPt);
            edges = Filter(edges);
            var nodes = GetClosestNodes(linkPt, line1FarwayPt,
                edges.SelectMany(o => o.LightNodes).ToList());
            return GetDifferntNumberNodes(nodes);
        }

        private List<ThLightNodeLink> LinkCrossCorner(List<Line> cross)
        {
            var results = new List<ThLightNodeLink>();
            var res = Sort(cross);
            for (int i = 0; i < 4; i++)
            {
                var first = res[i];
                var second = res[(i + 1) % 4];
                results.AddRange(GetLightNodeLinks(first, second));
            }
            return results;
        }

        private List<ThLightNodeLink> LinkThreewayCorner(Line first, Line second, Line branch)
        {
            /*          
             *            |
             *            | < branch
             *            | 
             *  ---------------------
             *      ^          ^
             *   first        second          
             */
            var results = new List<ThLightNodeLink>();
            var firstNodeLinks = GetLightNodeLinks(first, branch);
            var secondNodeLinks = GetLightNodeLinks(second, branch);
            if(firstNodeLinks.Count==0)
            {
                firstNodeLinks = GetLightNodeLinks(first, second, branch);
            }
            if (secondNodeLinks.Count == 0)
            {
                secondNodeLinks = GetLightNodeLinks(second,first, branch);
            }
            results.AddRange(firstNodeLinks);
            results.AddRange(secondNodeLinks);
            return results;
        }

        private List<ThLightNodeLink> GetLightNodeLinks(Line first,Line second)
        {
            /*          
             *            |
             *            | < second
             *            | 
             *  ---------------------
             *      ^     |    
             *   first    |  
             *            |
             */
            // 连接由first、second形成的区域内具有相同编号的灯,不跨区
            // 对可以连接的边是要按条件筛选的，请仔细阅读代码
            var results = new List<ThLightNodeLink>();
            var linkPt = first.FindLinkPt(second, ThGarageLightCommon.RepeatedPointDistance);
            if (!linkPt.HasValue)
            {
                return results;
            }
            var centers = new List<Line>() { first, second };
            // 对于没有边线的中心线，获取其符合条件的邻居
            var neibourDict = CreateNeibourDict(centers);

            var sideEdges = new List<ThLightEdge>();
            sideEdges.AddRange(GetCenterSideEdges(centers));
            sideEdges.AddRange(GetCenterSideEdges(neibourDict.Values.ToList()));

            // 把有Sides的中心线与其相邻的线合并
            var firstExtent = MergeNeibour(first, neibourDict);
            var secondExtent = MergeNeibour(second, neibourDict);
            var cornerArea = firstExtent.Item1.CreateParallelogram(secondExtent.Item1);
            
            // 获取与first、second平行的边
            var includeEdges = GroupEdges(cornerArea, sideEdges); // 分组
            includeEdges = FilterEdgesByTriangle(new List<Polyline> { firstExtent.Item2, secondExtent.Item2 }, includeEdges);

            var firstEdges = FilterEdges(includeEdges, first, neibourDict);
            var secondEdges = FilterEdges(includeEdges, second, neibourDict); 

            // 寻找可以连接的灯点
            var firstFarwayPt = linkPt.Value.GetNextLinkPt(first.StartPoint, first.EndPoint);
            firstEdges = Sort(firstEdges, linkPt.Value, firstFarwayPt);
            firstEdges = Filter(firstEdges);

            var secondFarwayPt = linkPt.Value.GetNextLinkPt(second.StartPoint, second.EndPoint);
            secondEdges = Sort(secondEdges, linkPt.Value, secondFarwayPt);
            secondEdges = Filter(secondEdges);

            if (firstEdges.Count == 0 || secondEdges.Count == 0)
            {
                return results;
            }
            if (firstEdges[0].EdgePattern != secondEdges[0].EdgePattern)
            {
                return results;
            }
            firstEdges.Reverse();
            return FindCornerStraitLinks(firstEdges, secondEdges);
        }

        private List<ThLightNodeLink> GetLightNodeLinks(Line first, Line second,Line branch)
        {
            /*          
             *            |
             *            | < branch
             *            | 
             *  ---------------------
             *      ^          ^
             *   first        second
             *            
             */
            // 连接由first、branch形成的区域与first、second以下的区域进行跨区连接
            // 对可以连接的边是要按条件筛选的，请仔细阅读代码
            var results = new List<ThLightNodeLink>();
            var linkPt = first.FindLinkPt(branch, ThGarageLightCommon.RepeatedPointDistance);
            if(!linkPt.HasValue)
            {
                return results;
            }
            var centers = new List<Line>() { first, second , branch };
            // 对于没有边线的中心线，获取其符合条件的邻居
            var neibourDict = CreateNeibourDict(centers);

            var sideEdges = new List<ThLightEdge>();
            sideEdges.AddRange(GetCenterSideEdges(centers));
            sideEdges.AddRange(GetCenterSideEdges(neibourDict.Values.ToList()));

            // 把有Sides的中心线与其相邻的线合并(后期再优化)
            var firstEdge = MergeNeibour(first, neibourDict);
            var branchEdge = MergeNeibour(branch, neibourDict);
            var secondEdge = MergeNeibour(second, neibourDict);

            var firstArea = firstEdge.Item1.CreateParallelogram(branchEdge.Item1);
            var firstIncludeEdges = GroupEdges(firstArea, sideEdges); // firstArea包含的边
            firstIncludeEdges = FilterEdgesByTriangle(new List<Polyline> { firstEdge.Item2, branchEdge.Item2 }, firstIncludeEdges);

            var secondArea = secondEdge.Item1.CreateParallelogram(branchEdge.Item1);
            var secondIncludeEdges = GroupEdges(secondArea, sideEdges); // secondArea包含的边
            secondIncludeEdges = FilterEdgesByTriangle(new List<Polyline> { secondEdge.Item2, branchEdge.Item2 }, secondIncludeEdges);

            var firstEdges = FilterEdges(firstIncludeEdges, first, neibourDict); 
            var branchEdges = FilterEdges(firstIncludeEdges, branch, neibourDict);

            var firstBranchNodes = new List<ThLightNode>();      
            firstBranchNodes.AddRange(GetLinkNodes(first, linkPt.Value, firstEdges));
            firstBranchNodes.AddRange(GetLinkNodes(branch, linkPt.Value, branchEdges));

            // 获取与first、second下方且平行的边
            var downNodes = new List<ThLightNode>();
            var downEdges = sideEdges.Where(o => !firstIncludeEdges.Select(f => f.Id).Contains(o.Id) &&
            !secondIncludeEdges.Select(s => s.Id).Contains(o.Id)).ToList();
            var downFirstEdges = FilterEdges(downEdges, first, neibourDict);
            var downSecondEdges = FilterEdges(downEdges, second, neibourDict);
            downNodes.AddRange(GetLinkNodes(first,linkPt.Value, downFirstEdges));
            downNodes.AddRange(GetLinkNodes(second, linkPt.Value, downSecondEdges));

            results = Link(firstBranchNodes, downNodes);
            results.ForEach(l => l.CrossIntersectionPt = linkPt.Value);
            return results;
        }

        private List<ThLightNodeLink> FindStraitLinks(List<ThLightEdge> firstEdges, List<ThLightEdge> secondEdges)
        {
            var edges = new List<ThLightEdge>();
            edges.AddRange(firstEdges);
            edges.AddRange(secondEdges);
            var linkPath = new ThLinkPath()
            {
                Edges = edges,
            };
            var linkService = new ThLightNodeSameLinkService(new List<ThLinkPath> { linkPath });
            return linkService.FindCornerStraitLinks();
        }
        private List<ThLightNodeLink> FindCornerStraitLinks(List<ThLightEdge> firstEdges, List<ThLightEdge> secondEdges)
        {
            var edges = new List<ThLightEdge>();
            edges.AddRange(firstEdges);
            edges.AddRange(secondEdges);
            var linkPath = new ThLinkPath()
            {
                Edges = edges,
            };
            var linkService = new ThLightNodeSameLinkService(new List<ThLinkPath> { linkPath });
            return linkService.FindCornerStraitLinks();
        }

        private List<ThLightEdge> FilterEdges(List<ThLightEdge> edges,Line center,Dictionary<Line,Line> neibourDict)
        {
            var results = new List<ThLightEdge>();
            results.AddRange(edges.Where(o => o.Direction.IsParallelToEx(center.LineDirection())).ToList());
            if (neibourDict.ContainsKey(center))
            {
                var linkEdges = edges.Where(o => o.Direction.IsParallelToEx(neibourDict[center].LineDirection())).ToList();
                linkEdges = linkEdges.Where(o => !results.Select(e => e.Id).Contains(o.Id)).ToList();
                results.AddRange(linkEdges);
            }
            return results;
        }

        private bool IsOnSameSide(Point3d start,Point3d end,Line branch)
        {
            var startProjectionPt = start.GetProjectPtOnLine(branch.StartPoint, branch.EndPoint);
            var endProjectionPt = end.GetProjectPtOnLine(branch.StartPoint, branch.EndPoint);
            var startDir = startProjectionPt.GetVectorTo(start);
            var endDir = endProjectionPt.GetVectorTo(end);
            return startDir.IsSameDirection(endDir);
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
                    if(!edges[i].Edge.FindLinkPt(results.Last().Edge).HasValue)
                    {
                        break;
                    }
                    results.Add(edges[i]);
                }
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
                    var neibours = FindNeibours(o, port)
                    .Where(n=> o.IsLessThan45Degree(n))
                    .ToList();
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

        private List<ThLightNode> GetClosestNodes(Point3d sp, Point3d ep, List<ThLightNode> lightNodes)
        {
            var direction = sp.GetVectorTo(ep).GetNormal();
            return lightNodes
                .OrderBy(o => o.Position.GetProjectPtOnLine(sp, ep).DistanceTo(sp))
                .Where(o => 
                {
                    var projectionPt = o.Position.GetProjectPtOnLine(sp, ep);
                    return sp.GetVectorTo(projectionPt).IsSameDirection(direction) || projectionPt.DistanceTo(sp) <= 1.0;
                }).ToList();
        }
        private List<ThLightNode> GetDifferntNumberNodes(List<ThLightNode> lightNodes)
        {
            var results = new List<ThLightNode>();
            lightNodes.ForEach(o =>
            {
                if (!results.Select(r => r.Number).Contains(o.Number))
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
    }
}
