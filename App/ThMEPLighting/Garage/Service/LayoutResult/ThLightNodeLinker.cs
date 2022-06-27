using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThLightNodeLinker
    {
        #region ---------- Input ---------
        private List<ThLightEdge> Edges { get; set; }
        private List<Line> Links { get; set; }
        #endregion
        #region ---------- Global ----------
        private Dictionary<ThLightNode,ThLightEdge> LightNodeDict { get; set; }
        private double PointTolerance = 5.0;
        private ThCADCoreNTSSpatialIndex LineSpatialIndex { get; set; }
        #endregion
        public ThLightNodeLinker(List<ThLightEdge> edges, List<Line> links)
        {
            Edges = edges;
            Links = links;
            Init();
        }
        private void Init()
        {
            LightNodeDict = new Dictionary<ThLightNode, ThLightEdge>();
            Edges.ForEach(edge =>
            {
                edge.LightNodes.ForEach(node => LightNodeDict.Add(node, edge));
            });

            var lineObjs = new DBObjectCollection();
            Links.ForEach(link => lineObjs.Add(link));
            Edges.ForEach(edge => lineObjs.Add(edge.Edge));
            LineSpatialIndex = new ThCADCoreNTSSpatialIndex(lineObjs);
        }
        public List<ThLightNodeLink> Link(ThLightNode first, ThLightNode second)
        {
            var results = new List<ThLightNodeLink>();
            var edge = GetLightEdge(first);
            if(edge.LightNodes.IndexOf(second)>=0)
            {
                
                var firstEdge = GetLightEdge(first);
                var nextNodes = GetBetweenNodes(firstEdge.LightNodes, first.Position, firstEdge.EndPoint);
                if (nextNodes.Contains(second))
                {
                    var lightNodeLink = new ThLightNodeLink()
                    {
                        First = first,
                        Second = second,
                        Edges = new List<Line> { firstEdge.Edge },
                    };
                    Append(results, lightNodeLink);
                }
                else
                {
                    var lightNodeLink = new ThLightNodeLink()
                    {
                        First = first,
                        Second = second,
                    };
                    var nextLinks = TraverseTarget(lightNodeLink, edge.EndPoint);
                    nextLinks.ForEach(link => Append(results, link));
                }

                var prevNodes = GetBetweenNodes(firstEdge.LightNodes, first.Position, firstEdge.StartPoint);
                if (prevNodes.Contains(second))
                {
                    var lightNodeLink = new ThLightNodeLink()
                    {
                        First = first,
                        Second = second,
                        Edges = new List<Line> { firstEdge.Edge },
                    };
                    Append(results, lightNodeLink);
                }
                else
                {
                    var lightNodeLink = new ThLightNodeLink()
                    {
                        First = first,
                        Second = second,
                    };
                    var prevLinks = TraverseTarget(lightNodeLink, edge.StartPoint);
                    prevLinks.ForEach(link => Append(results, link));
                }
            }
            return results;
        }

        public List<ThLightNodeLink> Link()
        {
            var results = new List<ThLightNodeLink>();
            var lightNodes = Edges.SelectMany(o => o.LightNodes).ToList();
            lightNodes.ForEach(node =>
            {
                var edge  = GetLightEdge(node);
                var nextNode = FindSameNumberOnOwnerEdge(node, edge.EndPoint);
                if(nextNode!=null)
                {
                    var lightNodeLink = new ThLightNodeLink()
                    {
                        First = node,
                        Second = nextNode,
                        Edges = new List<Line> { edge.Edge }, // 把当前灯所在的线
                    };
                    Append(results, lightNodeLink);
                }
                else
                {
                    var lightNodeLink = new ThLightNodeLink()
                    {
                        First = node,                       
                        Edges = new List<Line> { edge.Edge }, // 把当前灯所在的线
                    };
                    var nextLinks = Traverse(lightNodeLink, edge.Edge.EndPoint);
                    nextLinks.ForEach(link => Append(results, link));
                }

                var prevNode = FindSameNumberOnOwnerEdge(node, edge.StartPoint);
                if(prevNode!=null)
                {
                    var lightNodeLink = new ThLightNodeLink()
                    {
                        First = node,
                        Second = prevNode,
                        Edges = new List<Line> { edge.Edge }, // 把当前灯所在的线
                    };
                    Append(results, lightNodeLink);
                }
                else
                {
                    var lightNodeLink = new ThLightNodeLink()
                    {
                        First = node,
                        Edges = new List<Line> { edge.Edge }, // 把当前灯所在的线
                    };
                    var prevLinks = Traverse(lightNodeLink, edge.Edge.StartPoint);
                    prevLinks.ForEach(link => Append(results, link));
                }                
            });
            return results;
        }

        private List<ThLightNodeLink> TraverseTarget(ThLightNodeLink link, Point3d nextPt)
        {
            var results = new List<ThLightNodeLink>();
            var links = Query(nextPt);
            link.Edges.ForEach(o => links.Remove(o));
            links.OfType<Line>().ForEach(o =>
            {
                var newLink = new ThLightNodeLink()
                {
                    First = link.First,
                    Second = link.Second,
                };
                link.Edges.ForEach(edge => newLink.Edges.Add(edge));
                var newNextPt = nextPt.GetNextLinkPt(o.StartPoint, o.EndPoint);
                if (IsLink(o))
                {
                    newLink.Edges.Add(o);
                    results.AddRange(TraverseTarget(newLink, newNextPt));
                }
                else
                {
                    var lightEdge = GetLightEdge(o);
                    newLink.Edges.Add(lightEdge.Edge);
                    if(lightEdge.LightNodes.Contains(link.Second))
                    {
                        results.Add(newLink);
                    }
                    else
                    {
                        results.AddRange(TraverseTarget(newLink, newNextPt));
                    }                    
                }
            });
            return results;
        }

        private List<ThLightNodeLink> Traverse(ThLightNodeLink link, Point3d nextPt)
        {
            var results = new List<ThLightNodeLink>();
            var links = Query(nextPt);
            link.Edges.ForEach(o => links.Remove(o));
            links.OfType<Line>().ForEach(o =>
            {
                var newLink = new ThLightNodeLink()
                {
                    First = link.First,
                };
                link.Edges.ForEach(edge => newLink.Edges.Add(edge));
                var newNextPt = nextPt.GetNextLinkPt(o.StartPoint, o.EndPoint);
                if (IsLink(o))
                {
                    newLink.Edges.Add(o);
                    results.AddRange(Traverse(newLink, newNextPt));
                }
                else
                {
                    var lightEdge = GetLightEdge(o);
                    newLink.Edges.Add(lightEdge.Edge);
                    var secondNode = FindSameNumberOnOtherEdge(newLink.First.Number, lightEdge, nextPt);
                    if(secondNode!=null)
                    {
                        newLink.Second = secondNode;
                        results.Add(newLink);
                    }
                    else
                    {
                        results.AddRange(Traverse(newLink, newNextPt));
                    }
                }
            });
            return results;
        }

        private DBObjectCollection Query(Point3d port)
        {
            var outline = port.CreateSquare(PointTolerance);
            var results = LineSpatialIndex.SelectCrossingPolygon(outline);
            outline.Dispose();
            return results; 
        }

        private ThLightNode FindSameNumberOnOwnerEdge(ThLightNode current, Point3d portPt)
        {
            var edge = GetLightEdge(current);
            var betweenNodes = GetBetweenNodes(edge.LightNodes, current.Position, portPt);
            betweenNodes = SortNodes(betweenNodes.Where(o => o.Id != current.Id).ToList(), current.Position);
            for(int i =0;i<betweenNodes.Count;i++)
            {
                if(current.Number == betweenNodes[i].Number)
                {
                    return betweenNodes[i];
                }
            }
            return null;
        }

        private List<ThLightNode> GetBetweenNodes(List<ThLightNode> nodes, Point3d sp,Point3d ep)
        {
            return nodes
                .Where(o => ThGeometryTool.IsPointInLine(sp, ep, o.Position))
                .ToList();
        }

        private List<ThLightNode> SortNodes(List<ThLightNode> nodes,Point3d pt)
        {
            return nodes
                .OrderBy(o => o.Position.DistanceTo(pt))
                .ToList();
        }

        private ThLightNode FindSameNumberOnOtherEdge(string nodeNumber,ThLightEdge otherEdge , Point3d portPt)
        {
            var nodes = SortNodes(otherEdge.LightNodes, portPt);
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodeNumber == nodes[i].Number)
                {
                    return nodes[i];
                }
            }
            return null;
        }

        private ThLightEdge GetLightEdge(ThLightNode node)
        {
            return LightNodeDict[node];
        }

        private ThLightEdge GetLightEdge(Line line)
        {
           var index = Edges.Select(o => o.Edge).ToList().IndexOf(line);
            if(index!=-1)
            {
                return Edges[index];
            }
            else
            {
                return null;
            }
        }

        private bool IsLink(Line line)
        {
            return Links.Contains(line);    
        }

        private void Append(List<ThLightNodeLink> links, ThLightNodeLink link)
        {
            if(!IsExist(links, link))
            {
                links.Add(link);
            }
        }

        private bool IsExist(List<ThLightNodeLink> links, ThLightNodeLink link)
        {
            return links.Where(o => IsSameLink(o, link)).Any();
        }
        private bool IsSameLink(ThLightNodeLink first,ThLightNodeLink second)
        {
            if((first.First.Number == second.First.Number && first.Second.Number == second.Second.Number) ||
                (first.First.Number == second.Second.Number && first.Second.Number == second.First.Number))
            {
                if (IsSequence(first.Edges, second.Edges))
                {
                    return true;
                }
                else
                {
                    var newSecondEdges = second.Edges.Select(o => o).ToList();
                    newSecondEdges.Reverse();
                    return IsSequence(first.Edges, newSecondEdges);
                }               
            }
            return false;
        }
        private bool IsSequence(List<Line> firstLines,List<Line> secondLines)
        {
            if(firstLines.Count != secondLines.Count)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < firstLines.Count; i++)
                {
                    var index = firstLines.IndexOf(secondLines[i]);
                    if (index != i)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
