using System;
using System.Linq;
using System.Collections.Generic;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    /// <summary>
    /// 用于连接十字路口的灯
    /// </summary>
    internal sealed class ThLightNodeBranchLinkService
    {
        private ThLightGraphService Graph { get; set; }
        public string DefaultStartNumber { get; set; }
        private List<ThLightEdge> Edges => Graph.GraphEdges;
        private List<ThLinkPath> Links => Graph.Links;
        private List<Line> EdgeLines => Edges.Select(o => o.Edge).ToList();
        public List<BranchLinkFilterPath> BranchLinkFilterPaths { get; private set; } 
        public ThLightNodeBranchLinkService(ThLightGraphService graph)
        {
            Graph = graph;
            DefaultStartNumber = "";
            BranchLinkFilterPaths = new List<BranchLinkFilterPath>();
        }
        public List<ThLightNodeLink> LinkMainBranch()
        {
            /*  
             *          | (branch)
             *          |
             * -------------------
             *  (first)   (second)
             */
            // eg.方向是从first -> second,连first->branch
            var results = new List<ThLightNodeLink>();
            results.AddRange(LinkThreewayBranch());
            results.AddRange(LinkCrossBranch());
            return results;
        }
        private List<ThLightNodeLink> LinkThreewayBranch()
        {
            var results = new List<ThLightNodeLink>();
            EdgeLines.GetThreeWays().ForEach(o =>
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
        private List<ThLightNodeLink> LinkCrossBranch()
        {
            var results = new List<ThLightNodeLink>();
            EdgeLines.GetCrosses()
                .Where(o => o.Count == 4)
                .ForEach(o =>results.AddRange(LinkCrossBranch(o)));          
            return results;
        }
        public List<ThLightNodeLink> LinkBetweenBranch()
        {
            /*      (branch1)
             *          | 
             *          |
             * -------------------
             *  (first) | (second)
             *          | 
             *      (branch2)
             */
            // 对于
            var results = new List<ThLightNodeLink>();
            var preIds = FindPreEdgeIds();
            preIds.ForEach(p =>
            {
                var branches = FindBranches(p);
                if(branches.Count==2)
                {
                    results.AddRange(LinkBetweenBranch(branches[0], branches[1]));
                }
            });
            return results;
        }
        private List<ThLightNodeLink> LinkBetweenBranch(string firstEdgeId, string secondEdgeId)
        {
            // firstEdgeId 第一个分支第一条边的Id
            // secondEdgeId 第二个分支第一条边的Id
            var results = new List<ThLightNodeLink>();
            var firstEdge = FindEdge(firstEdgeId);
            var secondEdge = FindEdge(secondEdgeId);
            if(firstEdge.Direction.IsSameDirection(secondEdge.Direction))
            {
                return results;
            }
            if(firstEdge.Edge.IsLessThan45Degree(firstEdge.Edge))
            {
                return results;
            }
            var firstLinkPath = FindLinkPath(firstEdgeId);
            var secondLinkPath = FindLinkPath(secondEdgeId);
            var firstSamePathEdges = GetSamePathEdges(firstLinkPath.Edges);
            var secondSamePathEdges = GetSamePathEdges(secondLinkPath.Edges);
            return LinkBranchEdges(firstSamePathEdges, secondSamePathEdges);
        }
        private List<ThLightNodeLink> LinkBranchEdges(
            List<ThLightEdge> firstBranchEdges, 
            List<ThLightEdge> secondBranchEdges)
        {
            /* 
             *   <-----------------  ---------------->
             *    firstBranchEdges    secondBranchEdges
             */
            if (firstBranchEdges.Count > 0 && firstBranchEdges.Count > 0)
            {
                var samePathEdges = new List<ThLightEdge>();
                firstBranchEdges.Reverse();
                samePathEdges.AddRange(firstBranchEdges);
                samePathEdges.AddRange(secondBranchEdges);
                var path = samePathEdges.Select(o => o.Edge).ToList().ToPolyline();
                var linkPath = new ThLinkPath()
                {
                    Edges = samePathEdges,
                    Start = path.StartPoint,
                };
                if(path.Length>0.0)
                {
                    path.Dispose();
                }
                var sameLinkService = new ThLightNodeSameLinkService(new List<ThLinkPath> { linkPath });
                return sameLinkService.FindLightNodeLinkOnSamePath();
            }
            return new List<ThLightNodeLink>();
        }
        private List<ThLightNodeLink> LinkThreewayCorner(Line main1,Line main2,Line branch)
        {
            var main1Edge = FindEdge(Edges, main1);
            var main2Edge = FindEdge(Edges, main2);
            var branchEdge = FindEdge(Edges, branch);
            if(main1Edge.Direction.IsSameDirection(main2Edge.Direction))
            {
                var linkPath = FindLinkPath(main1Edge.Id);
                if(IsFront(GetEdgeIds(linkPath.Edges), main1Edge.Id, main2Edge.Id))
                {
                    return LinkMainBranch(main1Edge, branchEdge);
                }
                else
                {
                    return LinkMainBranch(main2Edge, branchEdge);
                }
            }
            else
            {
                return LinkMainBranch(branchEdge, main1Edge, main2Edge);
            }
        }
        private bool IsFront(List<string> edgeIds, string edge1Id,string edge2Id)
        {
            int index1 = edgeIds.IndexOf(edge1Id);
            int index2 = edgeIds.IndexOf(edge2Id);
            return index1 < index2;
        }
        private List<string> GetEdgeIds(List<ThLightEdge> edges)
        {
            return edges.Select(o => o.Id).ToList();
        }
        private List<ThLightNodeLink> LinkCrossBranch(List<Line> lines)
        {
            var pairs = lines.GetLinePairs();
            var mainPairs = pairs.OrderBy(k => k.Item1.GetLineOuterAngle(k.Item2))
                .Where(p => p.Item1.IsLessThan45Degree(p.Item2))
                .Where(p=>FindEdge(Edges,p.Item1).Direction.IsSameDirection(FindEdge(Edges, p.Item2).Direction))
                .ToList();
            if (mainPairs.Count == 1)
            {
                var first = mainPairs.First();
                var branches = lines.FindBranches(first.Item1, first.Item2);
                return LinkCrossBranch(FindEdge(Edges, first.Item1),  
                    FindEdge(Edges, branches[0]), FindEdge(Edges, branches[1]));
            }
            else if (mainPairs.Count == 2)
            {
                var firstPair = mainPairs[0];
                var secondPair = mainPairs[1];
                var firstEdge = FindEdge(Edges, firstPair.Item1);
                var secondEdge = FindEdge(Edges, firstPair.Item2);
                var thirdEdge = FindEdge(Edges, secondPair.Item1);
                var fourthEdge = FindEdge(Edges, secondPair.Item2);
                if (IsInSameLinkPath(new List<string> { firstEdge.Id, secondEdge.Id, thirdEdge.Id, fourthEdge.Id }))
                {
                    return LinkSamePathCrossBranch(firstEdge, secondEdge, thirdEdge, fourthEdge);
                }
                else
                {
                    var branchEdgesIds = GetEdgeIds(firstEdge.MultiBranch.Select(o => o.Item2)
                        .Union(secondEdge.MultiBranch.Select(o => o.Item2)).ToList());
                    if (branchEdgesIds.Contains(thirdEdge.Id) || branchEdgesIds.Contains(fourthEdge.Id))
                    {
                        return LinkUnSamePathCrossBranch(firstEdge, secondEdge, thirdEdge, fourthEdge);
                    }
                    else
                    {
                        return LinkUnSamePathCrossBranch(thirdEdge, fourthEdge, firstEdge, secondEdge);
                    }
                }
            }
            else 
            {
                // 暂时不支持
                return new List<ThLightNodeLink>();
            }
        }
        private List<ThLightNodeLink> LinkSamePathCrossBranch(
            ThLightEdge firstEdge, ThLightEdge secondEdge, 
            ThLightEdge thirdEdge, ThLightEdge fourthEdge)
        {
            var linkPath = FindLinkPath(firstEdge.Id);
            if (IsFront(GetEdgeIds(linkPath.Edges), firstEdge.Id, thirdEdge.Id))
            {
                return LinkUnSamePathCrossBranch(firstEdge, secondEdge, thirdEdge, fourthEdge);
            }
            else
            {
                return LinkUnSamePathCrossBranch(thirdEdge, fourthEdge, firstEdge, secondEdge);
            }
        }
        private List<ThLightNodeLink> LinkUnSamePathCrossBranch(
            ThLightEdge firstEdge, ThLightEdge secondEdge,
            ThLightEdge thirdEdge, ThLightEdge fourthEdge)
        {
            var linkPath = FindLinkPath(firstEdge.Id);
            if (IsFront(GetEdgeIds(linkPath.Edges), firstEdge.Id, secondEdge.Id))
            {
                return LinkCrossBranch(firstEdge, thirdEdge, fourthEdge);
            }
            else
            {
                return LinkCrossBranch(secondEdge, thirdEdge, fourthEdge);
            }
        }
        private List<ThLightNodeLink> LinkCrossBranch(ThLightEdge mainEdge,ThLightEdge branch1Edge,ThLightEdge branch2Edge)
        {
            /*
             *          |  
             *          |  
             *   -----------------
             *          ->
             */
            var results = new List<ThLightNodeLink>();
            var linkPt = mainEdge.Edge.FindLinkPt(branch1Edge.Edge);
            if (!linkPt.HasValue)
            {
                return results;
            }
            var currentLinkPath = FindLinkPath(mainEdge.Id);
            var mainLinkNodePairs = FindPreNextNodes(currentLinkPath.Edges, mainEdge, linkPt.Value);
            var branch1LinkPath = FindLinkPath(branch1Edge.Id);
            var branch2LinkPath = FindLinkPath(branch2Edge.Id);

            var branch1LinkNodes = FindBranchLinkNodes(branch1LinkPath.Edges, branch1Edge);
            var branch2LinkNodes = FindBranchLinkNodes(branch2LinkPath.Edges, branch2Edge);

            results.AddRange(FindNodeLinks(mainLinkNodePairs.Item1, branch1LinkNodes, branch2LinkNodes));
            results.AddRange(FindNodeLinks(mainLinkNodePairs.Item2, branch1LinkNodes, branch2LinkNodes));
            return results;
        }
        private List<ThLightNodeLink> LinkMainBranch(ThLightEdge mainEdge, ThLightEdge branchEdge)
        {
            /*
             *          |  
             *          |  
             *   -----------------
             *          ->
             */
            var results = new List<ThLightNodeLink>();
            var linkPt = mainEdge.Edge.FindLinkPt(branchEdge.Edge);
            if(!linkPt.HasValue)
            {
                return results;
            }
            var currentLinkPath = FindLinkPath(mainEdge.Id);
            var mainLinkNodePairs = FindPreNextNodes(currentLinkPath.Edges, mainEdge, linkPt.Value);
            var branchLinkPath = FindLinkPath(branchEdge.Id);
            var branchLinkNodes = FindBranchLinkNodes(branchLinkPath.Edges, branchEdge);
            results.AddRange(FindNodeLinks(mainLinkNodePairs.Item1, branchLinkNodes, new List<ThLightNode>()));
            results.AddRange(FindNodeLinks(mainLinkNodePairs.Item2, branchLinkNodes, new List<ThLightNode>()));

            // 将分支需要过滤线的路由返回
            results.ForEach(o =>
            {
                var branchNewEdges = Sort(branchLinkPath.Edges, linkPt.Value);
                var edges = FindBranchPtToLightNodeEdges(branchNewEdges, o.Second);
                if(edges.Count>0)
                {
                    BranchLinkFilterPaths.Add(new BranchLinkFilterPath(o.Second.Number, linkPt.Value, o.Second.Position, edges));
                }
            });
            return results;
        }

        private List<ThLightEdge> Sort(List<ThLightEdge> edges, Point3d linkPt)
        {
            if (edges.Count > 1)
            {
                var first = edges[0].Edge;
                if (first.StartPoint.DistanceTo(linkPt) <= ThGarageLightCommon.RepeatedPointDistance ||
                    first.EndPoint.DistanceTo(linkPt) <= ThGarageLightCommon.RepeatedPointDistance)
                {
                    return edges;
                }
                else
                {
                    var results = new List<ThLightEdge>();
                    for (int i = edges.Count - 1; i >= 0; i--)
                    {
                        results.Add(edges[i]);
                    }
                    return results;
                }
            }
            else
            {
                return edges;
            }
        }

        private List<Line> FindBranchPtToLightNodeEdges(List<ThLightEdge> edges,ThLightNode node)
        {
            /*
             *  ------1-------2-------3-------4-------
             *  eg.比如3灯，返回从起始边到3灯所在的边
             *  edges是前后首尾连接的边
             */
            var results = new List<Line>();
            var res = edges.Where(o => o.LightNodes.Select(n => n.Id).Contains(node.Id));
            if(res.Count()==1)
            {
                var targetEdge = res.First();
                for(int i=0;i<edges.Count;i++)
                {
                    results.Add(edges[i].Edge);
                    if (edges[i].Id== targetEdge.Id)
                    {
                        break;
                    }
                }
            }
            return results;
        }

        private List<ThLightNodeLink> LinkMainBranch(ThLightEdge mainEdge, ThLightEdge branch1Edge, ThLightEdge branch2Edge)
        {
            /*
             *          |  
             *          |  <- mainEdge
             *          |  
             *   -----------------
             * (从分支下来，往左、往右分)
             */
            var linkPt = mainEdge.Edge.FindLinkPt(branch1Edge.Edge);
            if(!linkPt.HasValue)
            {
                return new List<ThLightNodeLink>();
            }
            var results =new List<ThLightNodeLink>();
            var mainLinkPath = FindLinkPath(mainEdge.Id);
            var branch1LinkPath = FindLinkPath(branch1Edge.Id);
            var branch2LinkPath = FindLinkPath(branch2Edge.Id);

            var mainLinkNodes = FindBranchLinkNodes(mainLinkPath.Edges, mainEdge);
            var mainPath = mainLinkPath.Edges.Select(o => o.Edge).ToList().ToPolyline(linkPt.Value);
            mainLinkNodes = SortNodes(mainLinkNodes, mainPath);
            mainLinkNodes = DifferentByNumber(mainLinkNodes);

            var branch1LinkNodes = FindBranchLinkNodes(branch1LinkPath.Edges, branch1Edge);
            var branch1Path = branch1LinkPath.Edges.Select(o => o.Edge).ToList().ToPolyline(linkPt.Value);
            branch1LinkNodes = SortNodes(branch1LinkNodes, branch1Path);
            branch1LinkNodes = DifferentByNumber(branch1LinkNodes);

            var branch2LinkNodes = FindBranchLinkNodes(branch2LinkPath.Edges, branch2Edge);
            var branch2Path = branch2LinkPath.Edges.Select(o => o.Edge).ToList().ToPolyline(linkPt.Value);
            branch2LinkNodes = SortNodes(branch2LinkNodes, branch2Path);
            branch2LinkNodes = DifferentByNumber(branch2LinkNodes);

            results.AddRange(FindNodeLinks(mainLinkNodes, branch1LinkNodes, branch2LinkNodes));
            results.ForEach(o =>
            {
                var mainNewEdges = Sort(mainLinkPath.Edges, linkPt.Value);
                var edges = FindBranchPtToLightNodeEdges(mainNewEdges, o.First);
                if (edges.Count > 0)
                {
                    BranchLinkFilterPaths.Add(new BranchLinkFilterPath(o.First.Number, linkPt.Value, o.First.Position, edges));
                }
            });
            return results;
        }
       
        private Tuple<List<ThLightNode>, List<ThLightNode>> FindPreNextNodes(
            List<ThLightEdge> linkPathEdges,ThLightEdge preEdge,Point3d branchPt)
        {
            var currentEdges = GetEdgeSamePathSegment(linkPathEdges, preEdge.Id);
            var twoHalfs = Split(currentEdges.Select(o => o.Edge).ToList(), preEdge.Edge);
            var prevHalfEdges = FindEdges(currentEdges, twoHalfs.Item1);
            var nextHalfEdges = FindEdges(currentEdges, twoHalfs.Item2);

            var prevLinkNodes = TakeLightNodes(prevHalfEdges, branchPt);
            var nextLinkNodes = TakeLightNodes(nextHalfEdges, branchPt);
            prevLinkNodes = DifferentByNumber(prevLinkNodes);
            nextLinkNodes = DifferentByNumber(nextLinkNodes);

            // prevLinkNodes: WL03 WL01 WL05 -> WL03 WL01
            var tempPrevLinkNodes = FilterPrevLinkNodes(prevLinkNodes);
            // WL05
            var restPrevLinkNodes = prevLinkNodes.Except(tempPrevLinkNodes).ToList();
            prevLinkNodes = tempPrevLinkNodes;

            // 把存在于prevLinkNodes里具有相同编号的去掉
            nextLinkNodes = nextLinkNodes
                .Where(o => !prevLinkNodes.Select(n => n.Number)
                .Contains(o.Number)).ToList();

            // 如果restPrevLinkNodes里有灯的编号不在nextLinkNodes里，要保留
            restPrevLinkNodes = restPrevLinkNodes
                .Where(o => !nextLinkNodes.Select(n => n.Number)
                .Contains(o.Number)).ToList();
            prevLinkNodes.AddRange(restPrevLinkNodes);

            return Tuple.Create(prevLinkNodes, nextLinkNodes);
        }
        private List<ThLightNode> FindBranchLinkNodes(
            List<ThLightEdge> linkPathEdges, ThLightEdge branchEdge)
        {
            var currentEdges = GetEdgeSamePathSegment(linkPathEdges, branchEdge.Id);
            var linkNodes = GetLightNodes(currentEdges);
            linkNodes = DifferentByPosition(linkNodes);
            return linkNodes;
        }
        private List<ThLightNodeLink> FindNodeLinks(List<ThLightNode> nodes,
            List<ThLightNode> firstBranchNodes, 
            List<ThLightNode> secondBranchNodes)
        {
            var results = new List<ThLightNodeLink>(); 
            for(int i=0;i< nodes.Count;i++)
            {
                var linkNodes = new List<ThLightNode>();
                for (int j = 0; j < firstBranchNodes.Count; j++)
                {
                    if(nodes[i].Number == firstBranchNodes[j].Number)
                    {
                        linkNodes.Add(firstBranchNodes[j]);
                    }
                }
                for (int j = 0; j < secondBranchNodes.Count; j++)
                {
                    if (nodes[i].Number == secondBranchNodes[j].Number)
                    {
                        linkNodes.Add(secondBranchNodes[j]);
                    }
                }
                if(linkNodes.Count>0)
                {
                    results.Add(BuildLightNodeLink(nodes[i],
                        linkNodes.OrderBy(o=> nodes[i].Position.DistanceTo(o.Position)).First()));
                }
            }
            return results;
        }
        private ThLightNodeLink BuildLightNodeLink(ThLightNode first, ThLightNode second)
        {
            var edges = new List<Line>();
            edges.Add(FindEdgeByNode(Edges, first.Id).Edge);
            edges.Add(FindEdgeByNode(Edges, second.Id).Edge);
            return new ThLightNodeLink()
            {
                First = first,
                Second = second,
                Edges= edges,
            };
        }
        private List<ThLightNode> FilterPrevLinkNodes(List<ThLightNode> nodes)
        {
            // 优先查找分支点到默认编号之间的灯号
            int index = -1;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Number == DefaultStartNumber)
                {
                    index = i;
                }
            }
            if(index>=0)
            {
                var results = new List<ThLightNode>();
                for (int i = 0; i <= index ; i++)
                {
                    results.Add(nodes[i]);
                }
                return results;
            }
            else
            {
                return nodes;
            }
        }
        private List<ThLightNode> TakeLightNodes(List<ThLightEdge> edges, Point3d startPt)
        {
            if(edges.Count==0)
            {
                return new List<ThLightNode>();
            }
            var nodes = GetLightNodes(edges);
            var poly = edges.Select(o => o.Edge).ToList().ToPolyline(startPt);
            var results = SortNodes(nodes, poly);
            poly.Dispose();
            return results;
        }

        private List<ThLightNode> GetLightNodes(List<ThLightEdge> edges)
        {
            return edges.SelectMany(e => e.LightNodes).ToList();
        }
        private List<ThLightNode> SortNodes(List<ThLightNode> nodes,Polyline path)
        {
            // 1、根据Edges生成的路径path
            // 2、对path上的点进行排序
            return nodes.OrderBy(n => n.Position.DistanceTo(path)).ToList(); 
        }
        private List<ThLightEdge> FindEdges(List<ThLightEdge> edges,List<Line> lines)
        {
            var results = new List<ThLightEdge>();
            foreach(Line line in lines)
            {
                results.Add(FindEdge(edges, line));
            }
            return results;
        }
        private List<ThLightEdge> GetEdgeSamePathSegment(List<ThLightEdge> edges,string edgeId)
        {
            // 从一个链路上再细分成几段，定义为同一段上的分段
            // 查找前一条边所在的分段（这里的边是属于同一段的）
            var segments = SplitSameLinks(edges)
                .Where(o => o.Select(e => e.Id).Contains(edgeId));
            if(segments.Count()==1)
            {
                return segments.First();
            }
            else
            {
                return new List<ThLightEdge>();
            }
        }
        private Tuple<List<Line>, List<Line>> Split(List<Line> lines, Line middle)
        {
            int index = lines.IndexOf(middle);
            var prevHalfs = new List<Line>();
            var nextHalfs = new List<Line>();
            if (index < 0)
            {
                return Tuple.Create(prevHalfs, nextHalfs);
            }
            for (int i = 0; i <= index; i++)
            {
                prevHalfs.Add(lines[i]);
            }
            for (int i = index + 1; i < lines.Count; i++)
            {
                nextHalfs.Add(lines[i]);
            }
            return Tuple.Create(prevHalfs, nextHalfs);
        }
        private List<string> FindPreEdgeIds()
        {
            return Links
                .Where(o => IsValid(o.PreEdge))
                .Select(o => o.PreEdge.Id)
                .Distinct()
                .ToList();
        }
        private ThLinkPath FindLinkPath(string id)
        {
            var res = Links.Where(o => o.Edges.Select(e => e.Id).Contains(id));
            if(res.Count()==1)
            {
                return res.First();
            }
            else
            {
                return null;
            }
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
        private List<ThLightEdge> GetSamePathEdges(List<ThLightEdge> edges)
        {
            var sameLines = GetSamePathEdges(edges.Select(o => o.Edge).ToList());
            return edges.Where(o => sameLines.Contains(o.Edge)).ToList();
        }
        private List<string> FindBranches(string preId)
        {
            return Links
                .Where(l => IsValid(l.PreEdge))
                .Where(l => l.PreEdge.Id == preId)
                .Where(l=>l.Edges.Count>0)
                .Select(l=>l.Edges[0].Id)
                .ToList();
        }
        private ThLightEdge FindEdge(string id)
        {
            return Edges.Where(o => o.Id == id).FirstOrDefault();
        }
        private ThLightEdge FindEdgeByNode(List<ThLightEdge> edges , string nodeId)
        {
            return edges
                .Where(o => o.LightNodes.Select(n=>n.Id)
                .Contains(nodeId))
                .FirstOrDefault();
        }
        private ThLightEdge FindEdge(List<ThLightEdge> edges, Line edge)
        {
            int index = edges.Select(o => o.Edge).ToList().IndexOf(edge);
            if(index>=0)
            {
                return edges[index];
            }
            else
            {
                return new ThLightEdge();
            }
        }
        private bool IsValid(ThLightEdge edge)
        {
            return edge != null && !string.IsNullOrEmpty(edge.Id);
        }
        private List<ThLightNode> SelectNumberNodes(List<ThLightEdge> edges)
        {
            return edges.SelectMany(e => e.LightNodes).ToList();
        }
        private List<ThLightNode> DifferentByNumber(List<ThLightNode> lightNodes)
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
        private List<ThLightNode> DifferentByPosition(List<ThLightNode> lightNodes)
        {
            var results = new List<ThLightNode>();
            lightNodes.ForEach(o =>
            {
                if (!results.Select(r => r.Position).ToList().IsContains(o.Position)) 
                {
                    results.Add(o);
                }
            });
            return results;
        }
        private bool IsInSameLinkPath(List<string> edgeIds)
        {
            return Links.Where(l=> GetEdgeIds(l.Edges).IsContains(edgeIds)).Any();
        }
    }
    public class BranchLinkFilterPath
    {
        /*     
         *               |
         *              WL01 ->Number, 此处能获取LightNodePosition
         *               |
         *             
         *               |
         *               |
         *  ---------BranchLinkPt----------
         */
        public string Number { get; private set; } //灯编号
        public Point3d BranchLinkPt { get; private set; } //分支连接点
        public Point3d LightNodePosition { get; private set; } //灯位置
        public List<Line> Edges { get; private set; } // BranchLinkPt到WL01这段的边
        public BranchLinkFilterPath()
        {
            Number = "";
            Edges = new List<Line>();
        }
        public BranchLinkFilterPath(string number,Point3d branchLinkPt,Point3d lightNodePosition,List<Line> edges)
        {
            Edges = edges;
            Number =number;
            BranchLinkPt = branchLinkPt;
            LightNodePosition = lightNodePosition;
        }
        public Polyline GetPath()
        {
            var result = new Polyline();
            if(Edges.Count==0)
            {
                return result;
            }
            var path = Edges.ToPolyline(BranchLinkPt);
            var pts = new Point3dCollection();
            pts.Add(path.StartPoint);
            for (int i=0;i<path.NumberOfVertices;i++)
            {
                var lineSeg = path.GetLineSegmentAt(i);
                if (ThGeometryTool.IsPointOnLine(lineSeg.StartPoint,lineSeg.EndPoint, LightNodePosition))
                {
                    pts.Add(LightNodePosition);
                    break;
                }
                pts.Add(lineSeg.EndPoint);
            }
            path.Dispose();
            return pts.CreatePolyline(false);
        }
    }
}
