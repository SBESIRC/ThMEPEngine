using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    /// <summary>
    /// 用于连接十字路口的灯
    /// </summary>
    internal sealed class ThLightNodeCrossLinkService : ThCrossLinkCalculator
    {
        private List<List<Line>> EdgeGroups { get; set; }
        public ThLightNodeCrossLinkService(List<ThLightEdge> edges,
            Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts) : base(edges, centerSideDicts)
        {
            EdgeGroups = new List<List<Line>>();
            EdgeGroups.AddRange(ThMergeLightLineService.Merge(edges.Where(o => o.EdgePattern == EdgePattern.First).Select(o => o.Edge).ToList()));
            EdgeGroups.AddRange(ThMergeLightLineService.Merge(edges.Where(o => o.EdgePattern == EdgePattern.Second).Select(o => o.Edge).ToList()));
        }

        public List<ThLightNodeLink> LinkCross()
        {
            var results = new List<ThLightNodeLink>();
            var crosses = CenterLines.GetCrosses();
            crosses.Where(o => o.Count == 4).ForEach(c => results.AddRange(LinkCrossCorner(c)));
            return results;
        }
        public List<ThLightNodeLink> LinkThreeWay()
        {
            var results = new List<ThLightNodeLink>();
            var threeWays = CenterLines.GetThreeWays();
            threeWays.Where(o => o.Count == 3).ForEach(o =>
                {
                    var pairs = o.GetLinePairs();
                    var mainPair = pairs.OrderBy(k => k.Item1.GetLineOuterAngle(k.Item2)).First();
                    if (mainPair.Item1.IsLessThan45Degree(mainPair.Item2))
                    {
                        var branch = o.FindBranch(mainPair.Item1, mainPair.Item2);
                        results.AddRange(LinkThreewayCross(mainPair.Item1, mainPair.Item2, branch));
                    }
                });
            return results;
        }
        public List<ThLightNodeLink> LinkElbow()
        {
            // 连接弯头拐角处，
            var results = new List<ThLightNodeLink>();
            var elbows = CenterLines.GetElbows();
            elbows.Where(o => o.Count == 2).ForEach(o =>
            {
                results.AddRange(LinkElbowCross(o[0], o[1]));
            });
            return results;
        }
        private List<ThLightNode> GetLinkNodes(Line line1, Point3d linkPt, List<ThLightEdge> line1Edges)
        {
            // 获取可以连接的灯节点
            var results = new List<ThLightNode>();
            var line1FarwayPt = linkPt.GetNextLinkPt(line1.StartPoint, line1.EndPoint);
            var edges = Sort(line1Edges, linkPt, line1FarwayPt);
            //edges = Filter(edges);
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
        private List<ThLightNodeLink> LinkElbowCross(Line first, Line second)
        {
            /*          
             *     |
             *     | < second
             *     | 
             *     ----------
             *         ^          
             *       first                  
             */
            var results = new List<ThLightNodeLink>();
            if (IsCrossLink(first, second))
            {
                var nodeLinks = GetElblowLightNodeLinks(first, second);
                results.AddRange(nodeLinks);
            }
            return results;
        }
        private List<ThLightNodeLink> LinkThreewayCross(Line first, Line second, Line branch)
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
            var firstNodeLinks = new List<ThLightNodeLink>();
            var secondNodeLinks = new List<ThLightNodeLink>();
            if (IsCrossLink(first, branch))
            {
                firstNodeLinks = GetLightNodeLinks(first, second, branch);
            }
            if (IsCrossLink(second, branch))
            {
                secondNodeLinks = GetLightNodeLinks(second, first, branch);
            }
            results.AddRange(firstNodeLinks);
            results.AddRange(secondNodeLinks);
            return results;
        }

        private List<ThLightNodeLink> GetElblowLightNodeLinks(Line first, Line second)
        {
            /*          
             *  |
             *  | < second
             *  | 
             *  -----------
             *      ^        
             *    first            
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
            var firstLines = MergeNeibour(first, neibourDict);
            var secondLines = MergeNeibour(second, neibourDict);
            var cornerArea = firstLines.CreateParallelogram(secondLines);

            // 获取与first、second平行的边
            var includeEdges = GroupEdges(cornerArea, sideEdges); // 分组

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
            if (firstEdges[0].EdgePattern == secondEdges[0].EdgePattern)
            {
                return results;
            }

            var firstSecondNodes = new List<ThLightNode>(); // 由 first,second形成的区域包括的灯节点
            firstSecondNodes.AddRange(GetLinkNodes(first, linkPt.Value, firstEdges));
            firstSecondNodes.AddRange(GetLinkNodes(second, linkPt.Value, secondEdges));

            // 获取与first、second下方且平行的边
            var downNodes = new List<ThLightNode>();
            var downEdges = sideEdges.Where(o => !includeEdges.Select(f => f.Id).Contains(o.Id)).ToList();
            var firstOuterEdges = FilterEdges(downEdges, first, neibourDict);
            var secondOuterEdges = FilterEdges(downEdges, second, neibourDict);

            //
            var firstEdgeLines = firstOuterEdges.SelectMany(o => QueryLink(o.Edge)).ToList();
            var secondEdgeLines = secondOuterEdges.SelectMany(o => QueryLink(o.Edge)).ToList();
            downNodes.AddRange(GetEdges(Distinct(firstEdgeLines)).SelectMany(o => o.LightNodes));
            downNodes.AddRange(GetEdges(Distinct(secondEdgeLines)).SelectMany(o => o.LightNodes));

            results = Link(firstSecondNodes, downNodes);
            results.ForEach(l => l.CrossIntersectionPt = linkPt.Value);
            return results;
        }

        private List<Line> Distinct(List<Line> lines)
        {
            var results = new List<Line>();
            foreach (var line in lines)
            {
                if (!results.Contains(line))
                {
                    results.Add(line);
                }
            }
            return results;
        }

        private List<Line> QueryLink(Line line)
        {
            foreach (var link in EdgeGroups)
            {
                if (link.Contains(line))
                {
                    return link;
                }
            }
            return new List<Line>();
        }

        private List<ThLightNodeLink> GetLightNodeLinks(Line first, Line second)
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
            var firstLines = MergeNeibour(first, neibourDict);
            var secondLines = MergeNeibour(second, neibourDict);
            var cornerArea = firstLines.CreateParallelogram(secondLines);

            // 获取与first、second平行的边
            var includeEdges = GroupEdges(cornerArea, sideEdges); // 分组
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

            return FindCornerStraitLinks(firstEdges, secondEdges);
        }

        private bool IsCrossLink(Line first, Line second)
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
            var results = new List<ThLightNodeLink>();
            var linkPt = first.FindLinkPt(second, ThGarageLightCommon.RepeatedPointDistance);
            if (!linkPt.HasValue)
            {
                return false;
            }
            var centers = new List<Line>() { first, second };
            // 对于没有边线的中心线，获取其符合条件的邻居
            var neibourDict = CreateNeibourDict(centers);

            var sideEdges = new List<ThLightEdge>();
            sideEdges.AddRange(GetCenterSideEdges(centers));
            sideEdges.AddRange(GetCenterSideEdges(neibourDict.Values.ToList()));

            // 把有Sides的中心线与其相邻的线合并
            var firstLines = MergeNeibour(first, neibourDict);
            var secondLines = MergeNeibour(second, neibourDict);
            var cornerArea = firstLines.CreateParallelogram(secondLines);

            // 获取与first、second平行的边
            var includeEdges = GroupEdges(cornerArea, sideEdges); // 分组
            var firstEdges = FilterEdges(includeEdges, first, neibourDict);
            var secondEdges = FilterEdges(includeEdges, second, neibourDict);

            firstEdges.SelectMany(o => o.LightNodes).Select(o => o.Position);

            var firstFarwayPt = linkPt.Value.GetNextLinkPt(first.StartPoint, first.EndPoint);
            var secondFarwayPt = linkPt.Value.GetNextLinkPt(second.StartPoint, second.EndPoint);
            firstEdges = firstEdges.OrderBy(o => o.Edge.GetMidPt().GetProjectPtOnLine(linkPt.Value, firstFarwayPt).DistanceTo(linkPt.Value)).ToList();
            secondEdges = secondEdges.OrderBy(o => o.Edge.GetMidPt().GetProjectPtOnLine(linkPt.Value, secondFarwayPt).DistanceTo(linkPt.Value)).ToList();
            if (firstEdges.Count == 0 || secondEdges.Count == 0)
            {
                return false;
            }
            if (firstEdges.Count == 1 && secondEdges.Count == 1)
            {
                return firstEdges[0].EdgePattern != secondEdges[0].EdgePattern;
            }
            return true;
        }

        private bool IsHasIsolatedEdge(List<ThLightEdge> edges)
        {
            return edges.GroupBy(o => o.EdgePattern).Where(o => o.ToList().Count == 1).Any();
        }

        private bool IsElbow(Line first, Line second)
        {
            return !first.IsLessThan45Degree(second);
        }

        private List<ThLightNodeLink> GetLightNodeLinks(Line first, Line second, Line branch)
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
            if (!linkPt.HasValue)
            {
                return results;
            }
            var centers = new List<Line>() { first, second, branch };
            // 对于没有边线的中心线，获取其符合条件的邻居
            var neibourDict = CreateNeibourDict(centers);

            var sideEdges = new List<ThLightEdge>();
            sideEdges.AddRange(GetCenterSideEdges(centers));
            sideEdges.AddRange(GetCenterSideEdges(neibourDict.Values.ToList()));

            // 把有Sides的中心线与其相邻的线合并(后期再优化)
            var firstLines = MergeNeibour(first, neibourDict);
            var branchLines = MergeNeibour(branch, neibourDict);
            var secondLines = MergeNeibour(second, neibourDict);

            var firstArea = firstLines.CreateParallelogram(branchLines);
            var firstIncludeEdges = GroupEdges(firstArea, sideEdges); // firstArea包含的边

            var secondArea = secondLines.CreateParallelogram(branchLines);
            var secondIncludeEdges = GroupEdges(secondArea, sideEdges); // secondArea包含的边

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
            downNodes.AddRange(GetLinkNodes(first, linkPt.Value, downFirstEdges));
            downNodes.AddRange(GetLinkNodes(second, linkPt.Value, downSecondEdges));

            results = Link(firstBranchNodes, downNodes);
            results.ForEach(l => l.CrossIntersectionPt = linkPt.Value);
            return results;
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

        private List<ThLightEdge> FilterEdges(List<ThLightEdge> edges, Line center, Dictionary<Line, Line> neibourDict)
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
        private List<ThLightEdge> Filter(List<ThLightEdge> edges)
        {
            // 过滤连续的具有相同EdgePattern的边
            var results = new List<ThLightEdge>();
            if (edges.Count > 0)
            {
                results.Add(edges[0]);
                for (int i = 1; i < edges.Count; i++)
                {
                    if (edges[i].EdgePattern != results[0].EdgePattern)
                    {
                        break;
                    }
                    if (!edges[i].Edge.FindLinkPt(results.Last().Edge).HasValue)
                    {
                        break;
                    }
                    results.Add(edges[i]);
                }
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
    }
}
