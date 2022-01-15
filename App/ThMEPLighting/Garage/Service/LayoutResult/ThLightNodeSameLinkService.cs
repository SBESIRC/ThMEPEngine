using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    /// <summary>
    /// 用于在同一链路上的连接
    /// </summary>
    internal sealed class ThLightNodeSameLinkService
    {
        private Dictionary<string, Tuple<ThLightNode, ThLightEdge, ThLinkPath>> LightNodePositionDict;
        private List<ThLinkPath> Links { get; set; }
        public ThLightNodeSameLinkService(List<ThLinkPath> links)
        {
            Links = links;
            // 信息打平，便于后期查询
            BuildLightNodePositionDict();
        }

        public List<ThLightNodeLink> FindLightNodeLinkOnSamePath()
        {
            var results = new List<ThLightNodeLink>();
            // 对每一个链路开始编号
            Links.ForEach(l =>
            {
                // 查询
                var lightNodeLinks = BuildLightNodeLinks(l.Edges);
                lightNodeLinks.ForEach(o => o.OnLinkPath = IsOnSamePath(o.Edges));
                results.AddRange(lightNodeLinks);
            });
            return results;
        }

        public List<ThLightNodeLink> FindCornerStraitLinks()
        {
            // 找出拐弯处直连的灯，
            var results = new List<ThLightNodeLink>();
            return Links.SelectMany(l => FindCornerStraitLink(l)).ToList();
        }

        private List<ThLightNodeLink> FindCornerStraitLink(ThLinkPath linkPath)
        {
            // 传入ThLinkPath，目的是表达其中的Edges是连续的
            var results = new List<ThLightNodeLink>();
            var links = SplitSameLinks(linkPath.Edges);
            for(int i=0;i< links.Count-1;i++)
            {
                var current = links[i];
                var next = links[i + 1];
                var linkPt = current.Last().Edge.FindLinkPt(next.First().Edge, ThGarageLightCommon.RepeatedPointDistance);
                if(!linkPt.HasValue)
                {
                    continue;
                }
                var currentPath = current.Select(o => o.Edge).Reverse().ToList().ToPolyline(linkPt.Value);
                var nextPath = next.Select(o => o.Edge).ToList().ToPolyline(linkPt.Value);

                var currentNodes = SortNumberNodes(SelectNumberNodes(current),currentPath);
                var nextNodes = SortNumberNodes(SelectNumberNodes(next), nextPath);

                var currentDifferntNodes = FindDifferNumberNodes(currentNodes, true);
                var nextDifferntNodes = FindDifferNumberNodes(nextNodes, true);

                var linkNodesPairs = FindLinkNodes(currentDifferntNodes, nextDifferntNodes);
                linkNodesPairs = FilterByCloseDistance(linkNodesPairs); // 对于有重复编号灯链，获取距离最近的

                linkNodesPairs.ForEach(o => results.Add(CreateNodeLink(o.Item1,o.Item2, linkPath.Edges)));
            }
            return results;
        }

        private ThLightNodeLink CreateNodeLink(ThLightNode first,ThLightNode second,
            List<ThLightEdge> edges)
        {
            var item1NodeEdge = FindLightNodeEdge(edges, first.Id);
            var item2NodeEdge = FindLightNodeEdge(edges, second.Id);
            return new ThLightNodeLink()
            {
                First = first,
                Second = second,
                OnLinkPath = false,
                Edges = new List<Line> { item1NodeEdge.Edge, item2NodeEdge.Edge },
            };
        }

        private List<Tuple<ThLightNode, ThLightNode>> FilterByCloseDistance(List<Tuple<ThLightNode, ThLightNode>> linkNodes)
        {
            return linkNodes
                .GroupBy(o => o.Item1.Number)
                .Where(g => g.Count() > 0)
                .Select(g => g.ToList().OrderBy(p => p.Item1.Position.DistanceTo(p.Item2.Position)).First())
                .ToList();
        }

        private ThLightEdge FindLightNodeEdge(List<ThLightEdge> edges,string lightNodeId)
        {
            return edges
                 .Where(o => o.LightNodes.Select(n => n.Id).Contains(lightNodeId))
                 .FirstOrDefault();
        }

        private List<Tuple<ThLightNode, ThLightNode>> FindLinkNodes(List<ThLightNode> preNodes, List<ThLightNode> nextNodes)
        {
            var results = new List<Tuple<ThLightNode, ThLightNode>>();
            for(int i=0;i< preNodes.Count;i++)
            {
                for (int j = 0; j < nextNodes.Count; j++)
                {
                    if(preNodes[i].Number == nextNodes[j].Number)
                    {
                        results.Add(Tuple.Create(preNodes[i], nextNodes[j]));
                    }
                }
            }
            // 增加过滤
            return results
                .Where(o=>!string.IsNullOrEmpty(o.Item1.Number))
                .Where(o=>o.Item1.Position.DistanceTo(o.Item2.Position)>5.0)
                .ToList();
        }

        private List<ThLightNode> SortNumberNodes(List<ThLightNode> nodes, Polyline edge)
        {
            return nodes.OrderBy(n => n.Position.DistanceTo(edge)).ToList();
        }

        private List<ThLightNode> SelectNumberNodes(List<ThLightEdge> edges)
        {
            return edges.SelectMany(e => e.LightNodes).ToList();
        }

        private List<List<ThLightEdge>> SplitSameLinks(List<ThLightEdge> sameLinkEdges)
        {
            var links = new List<List<ThLightEdge>>();
            for (int i = 0; i < sameLinkEdges.Count; i++)
            {
                var sameLink = new List<ThLightEdge>();
                sameLink.Add(sameLinkEdges[i]);
                int j = i + 1;
                for (; j < sameLinkEdges.Count; j++)
                {
                    if (sameLinkEdges[j].Edge.IsLessThan45Degree(sameLink.Last().Edge))
                    {
                        sameLink.Add(sameLinkEdges[j]);
                    }
                    else
                    {                        
                        break;
                    }
                }
                i = j - 1;
                links.Add(sameLink);
            }
            return links;
        }

        private bool IsOnSamePath(List<Line> lines)
        {
            for (int i = 0; i < lines.Count - 1; i++)
            {
                if (!ThGarageUtils.IsLessThan45Degree(lines[i].StartPoint,
                    lines[i].EndPoint, lines[i + 1].StartPoint, lines[i + 1].EndPoint))
                {
                    return false;
                }
            }
            return true;
        }
        private List<Line> GetSamePathEdges(List<Line> edges)
        {
            // 获取与第一段在同一段上的路线
            var results = new List<Line>();
            if (edges.Count == 0)
            {
                return results;
            }
            results.Add(edges[0]);
            for (int i = 1; i < edges.Count; i++)
            {
                if (ThGarageUtils.IsLessThan45Degree(edges[i].StartPoint,
                    edges[i].EndPoint, edges[i - 1].StartPoint, edges[i - 1].EndPoint))
                {
                    results.Add(edges[i]);
                }
                else
                {
                    break;
                }
            }
            return results;
        }

        private List<ThLightNodeLink> BuildLightNodeLinks(List<ThLightEdge> edges)
        {
            // 寻找直段上的相同编号的
            var lines = edges.Select(e => e.Edge).ToList();
            var link = lines.ToPolyline(ThGarageLightCommon.RepeatedPointDistance);
            var nodes = edges.SelectMany(e => e.LightNodes).ToList();
            nodes = nodes.OrderBy(n => n.Position.DistanceTo(link)).ToList();
            var results = new List<ThLightNodeLink>();
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    if (nodes[i].Number == nodes[j].Number)
                    {
                        var firstEdge = LightNodePositionDict[nodes[i].Id].Item2;
                        var secondEdge = LightNodePositionDict[nodes[j].Id].Item2;
                        var path = FindEdges(firstEdge.Edge, secondEdge.Edge, lines);
                        var nodeLink = new ThLightNodeLink()
                        {
                            First = nodes[i],
                            Second = nodes[j],
                            Edges = path,
                        };
                        results.Add(nodeLink);
                        break;
                    }
                }
            }
            return results;
        }

        private List<Line> FindEdges(Line firstEdge, Line secondEdge, List<Line> edges)
        {
            var result = new List<Line>();
            int firstIndex = edges.IndexOf(firstEdge);
            int secondIndex = edges.IndexOf(secondEdge);
            if (firstIndex == -1 || secondIndex == -1)
            {
                return result;
            }
            if (firstIndex <= secondIndex)
            {
                for (int i = firstIndex; i <= secondIndex; i++)
                {
                    result.Add(edges[i]);
                }
            }
            else
            {
                for (int i = firstIndex; i >= secondIndex; i--)
                {
                    result.Add(edges[i]);
                }
            }
            return result;
        }

        private List<ThLightNode> FindDifferNumberNodes(List<ThLightNode> nodes, bool isFromStart)
        {
            var results = new List<ThLightNode>();
            if (isFromStart)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (!results.Select(o => o.Number).Contains(nodes[i].Number))
                    {
                        results.Add(nodes[i]);
                    }
                }
            }
            else
            {
                for (int i = nodes.Count - 1; i >= 0; i--)
                {
                    if (!results.Select(o => o.Number).Contains(nodes[i].Number))
                    {
                        results.Add(nodes[i]);
                    }
                }
            }
            return results;
        }

        private void BuildLightNodePositionDict()
        {
            LightNodePositionDict = new Dictionary<string,
                Tuple<ThLightNode, ThLightEdge, ThLinkPath>>();
            // 把灯节点信息打平
            foreach (var link in Links)
            {
                foreach (var edge in link.Edges)
                {
                    edge.LightNodes.ForEach(n =>
                    {
                        LightNodePositionDict.Add(n.Id, Tuple.Create(n, edge, link));
                    });
                }
            }
        }
    }
}
