using System;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    /// <summary>
    /// 用于连接十字路口的灯
    /// </summary>
    internal sealed class ThLightNodeBranchLinkService
    {
        private ThLightGraphService Graph { get; set; }
        public string DefaultStartNumber { get; set; }
        public int NumberLoop { get; set; } = 3;
        private List<ThLightEdge> Edges => Graph.GraphEdges;
        private List<ThLinkPath> Links => Graph.Links;
        public List<Tuple<Line, Point3d>> BranchPtPairs { get; private set; } //获取分支点,用于过滤
        public ThLightNodeBranchLinkService(ThLightGraphService graph)
        {
            Graph = graph;
            DefaultStartNumber = "";
            BranchPtPairs = new List<Tuple<Line, Point3d>>();
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
            var preIds = FindPreEdgeIds();
            preIds.ForEach(p =>
            {
                var branches = FindBranches(p);
                results.AddRange(LinkMainBranch(p, branches));
            });
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

        private List<ThLightNodeLink> LinkMainBranch(string preEdgeId,List<string> linkPathFirstEdgeIds)
        {
            if(linkPathFirstEdgeIds.Count==1)
            {
                // 有一个分支
                return LinkMainBranch(preEdgeId, linkPathFirstEdgeIds[0]);
            }
            else if (linkPathFirstEdgeIds.Count == 2)
            {
                // 有两个分支
                return LinkMainBranch(preEdgeId, linkPathFirstEdgeIds[0], linkPathFirstEdgeIds[1]);
            }
            else
            {
                return new List<ThLightNodeLink>();
            }
        }
        private List<ThLightNodeLink> LinkMainBranch(string preEdgeId, string firstEdgeId)
        {
            var results = new List<ThLightNodeLink>();
            var currentLinkPath = FindLinkPath(preEdgeId);
            var firstLinkPath = FindLinkPath(firstEdgeId);
            var linkNodePairs = FindPreNextNodes(currentLinkPath, preEdgeId, firstLinkPath.Start);
            var firstLinkNodes = TakeLightNodes(GetSamePathEdges(firstLinkPath.Edges), firstLinkPath.Start, NumberLoop);
            results.AddRange(FindNodeLinks(linkNodePairs.Item1, firstLinkNodes, new List<ThLightNode>()));
            results.AddRange(FindNodeLinks(linkNodePairs.Item2, firstLinkNodes, new List<ThLightNode>()));

            //
            if (firstLinkNodes.Count>0)
            {
                var firstEdge = FindEdge(firstEdgeId);
                AddToBranchDirectionRecord(firstEdge.Edge,firstLinkPath.Start);
            }
            return results;
        }

        private List<ThLightNodeLink> LinkMainBranch(string preEdgeId,string firstEdgeId,string secondEdgeId)
        {
            var results=new List<ThLightNodeLink>();
            var currentLinkPath = FindLinkPath(preEdgeId);
            var firstLinkPath = FindLinkPath(firstEdgeId);
            var secondLinkPath = FindLinkPath(secondEdgeId);
            var linkNodePairs= FindPreNextNodes(currentLinkPath, preEdgeId, firstLinkPath.Start);
            var firstLinkNodes = TakeLightNodes(GetSamePathEdges(firstLinkPath.Edges), firstLinkPath.Start, NumberLoop);
            var secondLinkNodes = TakeLightNodes(GetSamePathEdges(secondLinkPath.Edges), secondLinkPath.Start, NumberLoop);
            results.AddRange(FindNodeLinks(linkNodePairs.Item1, firstLinkNodes, secondLinkNodes));
            results.AddRange(FindNodeLinks(linkNodePairs.Item2, firstLinkNodes, secondLinkNodes));

            // 
            results.ForEach(o =>
            {
                var secondLightEdge = FindEdgeByNode(firstLinkPath.Edges.Union(secondLinkPath.Edges).ToList(), o.Second.Id);
                var secondLightLinkPath = FindLinkPath(secondLightEdge.Id);
                var secondEdge = secondLightLinkPath.Edges.First();
                AddToBranchDirectionRecord(secondEdge.Edge, secondLightLinkPath.Start);
            });
            return results;
        }

        private void AddToBranchDirectionRecord(Line branch,Point3d crossPt)
        {
            if(BranchPtPairs.Select(o=>o.Item1).Contains(branch))
            {
                BranchPtPairs.Add(Tuple.Create(branch, crossPt));
            }
        }

        private Tuple<List<ThLightNode>, List<ThLightNode>> FindPreNextNodes(
            ThLinkPath linkPath,string preEdgeId,Point3d branchPt)
        {
            var preEdge = FindEdge(preEdgeId);
            var currentEdges = GetEdgeSamePathSegment(linkPath.Edges, preEdge.Id);
            var twoHalfs = Split(currentEdges.Select(o => o.Edge).ToList(), preEdge.Edge);
            var prevHalfEdges = FindEdges(currentEdges, twoHalfs.Item1);
            var nextHalfEdges = FindEdges(currentEdges, twoHalfs.Item2);

            var prevLinkNodes = TakeLightNodes(prevHalfEdges, branchPt, NumberLoop);
            var nextLinkNodes = TakeLightNodes(nextHalfEdges, branchPt, NumberLoop);
            prevLinkNodes = FilterPrevLinkNodes(prevLinkNodes);
            prevLinkNodes = Different(prevLinkNodes);
            nextLinkNodes = Different(nextLinkNodes);
            nextLinkNodes = nextLinkNodes
                .Where(o => !prevLinkNodes.Select(n => n.Number)
                .Contains(o.Number))
                .ToList();
            return Tuple.Create(prevLinkNodes, nextLinkNodes);
        }

        private List<ThLightNodeLink> FindNodeLinks(List<ThLightNode> nodes,
            List<ThLightNode> firstBranchNodes, 
            List<ThLightNode> secondBranchNodes)
        {
            var results = new List<ThLightNodeLink>(); 
            for(int i=0;i< nodes.Count;i++)
            {
                int firstIndex = -1;
                for (int j = 0; j < firstBranchNodes.Count; j++)
                {
                    if(nodes[i].Number == firstBranchNodes[j].Number)
                    {
                        firstIndex = j;
                        break;
                    }
                }
                int secondIndex = -1; 
                for (int j = 0; j < secondBranchNodes.Count; j++)
                {
                    if (nodes[i].Number == secondBranchNodes[j].Number)
                    {
                        secondIndex = j;
                        break;
                    }
                }
                if(firstIndex>=0 && secondIndex>=0)
                {
                    if(nodes[i].Position.DistanceTo(firstBranchNodes[firstIndex].Position)<=
                        nodes[i].Position.DistanceTo(secondBranchNodes[secondIndex].Position))
                    {
                        results.Add(BuildLightNodeLink(nodes[i], firstBranchNodes[firstIndex]));
                    }
                    else
                    {
                        results.Add(BuildLightNodeLink(nodes[i], secondBranchNodes[secondIndex]));
                    }
                }
                else if (firstIndex >= 0)
                {
                    results.Add(BuildLightNodeLink(nodes[i], firstBranchNodes[firstIndex]));
                }
                else if(secondIndex >= 0)
                {
                    results.Add(BuildLightNodeLink(nodes[i], secondBranchNodes[secondIndex]));
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
            var results = new List<ThLightNode>();
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
                for (int i = 0; i <= index ; i++)
                {
                    results.Add(nodes[i]);
                }
            }            
            return results;
        }

        private List<ThLightNode> TakeLightNodes(List<ThLightEdge> edges, Point3d startPt,int number)
        {
            var results = new List<ThLightNode>();
            var nodes = SortNodes(edges, startPt);
            for (int i = 0; i < nodes.Count && i< number; i++)
            {
                results.Add(nodes[i]);
            }
            return results;
        }

        private List<ThLightNode> SortNodes(List<ThLightEdge> edges,Point3d startPt)
        {
            // 1、根据edges所在的路径（从startPt开始）
            // 2、对边上的点进行排序
            var results = new List<ThLightNode>();
            if (edges.Count==0)
            {
                return results;
            }
            var poly = edges.Select(o => o.Edge).ToList().ToPolyline(startPt);
            var nodes = edges.SelectMany(e => e.LightNodes).ToList();
            nodes = nodes.OrderBy(n => n.Position.DistanceTo(poly)).ToList();
            return nodes;
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

        private List<ThLightNode> Different(List<ThLightNode> lightNodes)
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
