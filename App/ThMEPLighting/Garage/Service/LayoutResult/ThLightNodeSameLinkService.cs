using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using Dreambuild.AutoCAD;

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

        public List<ThLightNodeLink> FindLightNodeLinkOnBranchCorner()
        {
            var results = new List<ThLightNodeLink>();
            // 分支拐弯处
            Links.ForEach(l =>
            {
                var lightNodeLinks = FindCornerLightNodeLink(l);
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
                var currentPath = ToPolyline(current.Select(o => o.Edge).Reverse().ToList(), linkPt.Value);
                var nextPath = ToPolyline(next.Select(o => o.Edge).ToList(), linkPt.Value);

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
                    if(preNodes[i].Number== preNodes[j].Number)
                    {
                        results.Add(Tuple.Create(preNodes[i], preNodes[j]));
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
                for (int j = i + 1; j < sameLinkEdges.Count; j++)
                {
                    if (sameLinkEdges[j].Edge.IsLessThan45Degree(sameLink.Last().Edge))
                    {
                        sameLink.Add(sameLinkEdges[j]);
                    }
                    else
                    {
                        i = j - 1;
                        break;
                    }
                }
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

        private Polyline ToPolyline(List<Line> lines,Point3d startPt)
        {
            var path = lines.ToPolyline(ThGarageLightCommon.RepeatedPointDistance);
            bool isCloseStart = startPt.DistanceTo(path.StartPoint) <= ThGarageLightCommon.RepeatedPointDistance;
            bool isCloseEnd = startPt.DistanceTo(path.EndPoint) <= ThGarageLightCommon.RepeatedPointDistance;
            if (isCloseStart || isCloseEnd)
            {
                if(isCloseEnd)
                {
                    return path.Reverse();
                }
            }
            return path;
        }

        private List<List<ThLightEdge>> BuildStraitLinks(List<ThLightEdge> edges)
        {
            var results = new List<List<ThLightEdge>>();
            var lines = edges.Select(o => o.Edge).ToList();
            var mergeLines = ThMergeLightLineService.Merge(lines);
            mergeLines.ForEach(link =>
            {
                var subEdges = link.Select(e => edges[lines.IndexOf(e)]).ToList();
                results.Add(subEdges);
            });
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

        private List<ThLightNodeLink> FindCornerLightNodeLink(ThLinkPath link)
        {
            var results = new List<ThLightNodeLink>();
            // 遇到分支上的点
            if (link.PreEdge == null || string.IsNullOrEmpty(link.PreEdge.Id))
            {
                return results;
            }
            var linkEdges = link.Edges;
            var lines = linkEdges.Select(e => e.Edge).ToList();
            lines = GetSamePathEdges(lines);
            linkEdges = linkEdges.Where(l => lines.Contains(l.Edge)).ToList();

            var path = lines.ToPolyline(ThGarageLightCommon.RepeatedPointDistance);
            if (link.Start.DistanceTo(path.StartPoint) > link.Start.DistanceTo(path.EndPoint))
            {
                path = path.Reverse();
            }

            var nodes = linkEdges.SelectMany(e => e.LightNodes).ToList();
            nodes = nodes.OrderBy(n => n.Position.DistanceTo(path)).ToList();

            var preEdgeLink = FindLinkPath(link.PreEdge);
            var preLines = FindEdges(preEdgeLink.Edges[0].Edge, link.PreEdge.Edge, preEdgeLink.Edges.Select(o => o.Edge).ToList());
            preLines.Reverse();
            var preSamePathLines = GetSamePathEdges(preLines);
            var preSamePathEdges = preEdgeLink.Edges.Where(e => preSamePathLines.Contains(e.Edge)).ToList();
            var prePath = preSamePathLines.ToPolyline(ThGarageLightCommon.RepeatedPointDistance);
            if (link.Start.DistanceTo(prePath.StartPoint) > link.Start.DistanceTo(prePath.EndPoint))
            {
                prePath = prePath.Reverse();
            }
            var prePathNodes = preSamePathEdges.SelectMany(e => e.LightNodes).ToList();
            prePathNodes = prePathNodes.OrderBy(n => n.Position.DistanceTo(prePath)).ToList();
            var canFindCornerNodes = FindDifferNumberNodes(prePathNodes, true);
            for (int i = 0; i < canFindCornerNodes.Count; i++)
            {
                if (i > 0)
                {
                    // 目前只处理第一个
                    break;
                }
                var iNodeOwner = LightNodePositionDict[canFindCornerNodes[i].Id];
                var edges = FindEdges(preLines[0], iNodeOwner.Item2.Edge, preLines);
                edges.Reverse();
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (nodes[j].Number == canFindCornerNodes[i].Number)
                    {
                        var jNodeOwner = LightNodePositionDict[nodes[j].Id];
                        var branchEdges = FindEdges(lines[0], jNodeOwner.Item2.Edge, lines);
                        edges.AddRange(branchEdges);
                        var nodeLink = new ThLightNodeLink()
                        {
                            First = canFindCornerNodes[i],
                            Second = nodes[j],
                            Edges = edges,
                        };
                        results.Add(nodeLink);
                        break;
                    }
                }
            }
            return results;
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

        private List<ThLightNodeLink> FindBranchSameNumberCode(Point3d portPt, ThLightNode currentNode, ThLightEdge lightEdge)
        {
            var results = new List<ThLightNodeLink>();
            var branches = lightEdge.MultiBranch.Where(b => b.Item1.DistanceTo(portPt) <= 5.0).ToList();
            branches = RemoveDuplicatedBranches(branches);
            branches.ForEach(b =>
            {
                var bLink = FindLinkPath(b.Item2);
                if (bLink != null)
                {
                    var lines = bLink.Edges.Select(e => e.Edge).ToList();
                    var link = lines.ToPolyline(ThGarageLightCommon.RepeatedPointDistance);
                    var halfs = Split(link, b.Item1);

                    var preEdges = bLink.Edges.Where(e => halfs.Item1.GeometryContains(e.Edge, ThMEPEngineCoreCommon.GEOMETRY_TOLERANCE)).ToList();
                    var nextEdges = bLink.Edges.Where(e => halfs.Item2.GeometryContains(e.Edge, ThMEPEngineCoreCommon.GEOMETRY_TOLERANCE)).ToList();

                    var preLink = FindSubBranchSameNumberNode(currentNode, b.Item1, preEdges);
                    var nextLink = FindSubBranchSameNumberNode(currentNode, b.Item1, nextEdges);
                    if (preLink != null)
                    {
                        results.Add(preLink);
                    }
                    if (nextLink != null)
                    {
                        results.Add(nextLink);
                    }
                }
            });
            return results;
        }

        private ThLightNodeLink FindSubBranchSameNumberNode(ThLightNode currentNode, Point3d portPt, List<ThLightEdge> edges)
        {
            if (edges.Count == 0)
            {
                return null;
            }
            var lines = edges.Select(e => e.Edge).ToList();
            var link = lines.ToPolyline(ThGarageLightCommon.RepeatedPointDistance);
            if (portPt.DistanceTo(link.StartPoint) > portPt.DistanceTo(link.EndPoint))
            {
                link = link.Reverse();
            }
            var nodes = edges.SelectMany(e => e.LightNodes).ToList();
            nodes = nodes.OrderBy(n => n.Position.DistanceTo(link)).ToList();
            var bFindRes = FindSameNumberNodeFromStart(currentNode, nodes);
            if (bFindRes.Item1 != -1)
            {
                return new ThLightNodeLink()
                {
                    First = currentNode,
                    Second = nodes[bFindRes.Item1],
                    Edges = bFindRes.Item2,
                };
            }
            else
            {
                return null;
            }
        }

        private Tuple<List<Line>, List<Line>> Split(Polyline poly, Point3d pt)
        {
            var preLines = new List<Line>();
            var nextLines = new List<Line>();
            var ptIndex = -1;
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                if (poly.GetPoint3dAt(i).DistanceTo(pt) <= 1.0)
                {
                    ptIndex = i;
                    break;
                }
            }
            if (ptIndex == -1)
            {
                for (int i = 0; i < poly.NumberOfVertices - 1; i++)
                {
                    var lineSeg = poly.GetLineSegmentAt(i);
                    preLines.Add(new Line(lineSeg.StartPoint, lineSeg.EndPoint));
                }
            }
            else
            {
                for (int i = 0; i < ptIndex; i++)
                {
                    var lineSeg = poly.GetLineSegmentAt(i);
                    preLines.Add(new Line(lineSeg.StartPoint, lineSeg.EndPoint));
                }
                for (int i = ptIndex; i < poly.NumberOfVertices - 1; i++)
                {
                    var lineSeg = poly.GetLineSegmentAt(i);
                    nextLines.Add(new Line(lineSeg.StartPoint, lineSeg.EndPoint));
                }
            }
            return Tuple.Create(preLines, nextLines);
        }

        private List<Tuple<Point3d, ThLightEdge>> RemoveDuplicatedBranches(List<Tuple<Point3d, ThLightEdge>> branches)
        {
            var results = new List<Tuple<Point3d, ThLightEdge>>();
            var bIds = new List<string>();
            foreach (var item in branches)
            {
                if (!bIds.Contains(item.Item2.Id))
                {
                    results.Add(item);
                    bIds.Add(item.Item2.Id);
                }
            }
            return results;
        }

        private Tuple<int, List<Line>> FindSameNumberNodeFromStart(ThLightNode currentNode, List<ThLightNode> nodes)
        {
            // 找灯右边相连的灯
            int findIndex = -1;
            var edges = new List<Line>();
            var currentNodeEdge = LightNodePositionDict[currentNode.Id].Item2;
            edges.Add(currentNodeEdge.Edge);
            for (int i = 0; i < nodes.Count; i++)
            {
                var lightEdge = LightNodePositionDict[nodes[i].Id].Item2;
                if (!edges.Contains(lightEdge.Edge))
                {
                    edges.Add(lightEdge.Edge);
                }
                if (nodes[i].Number == currentNode.Number)
                {
                    findIndex = i;
                    break;
                }
            }
            return Tuple.Create(findIndex, edges);
        }

        private ThLinkPath FindLinkPath(ThLightEdge lightEdge)
        {
            foreach (var link in Links)
            {
                if (link.Edges.Where(o => o.Id == lightEdge.Id).Any())
                {
                    return link;
                }
            }
            return null;
        }
        private bool IsExisted(ThLightNodeLink other, List<ThLightNodeLink> links)
        {
            foreach (var link in links)
            {
                if (link.IsSameLink(other))
                {
                    return true;
                }
            }
            return false;
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
