using System;
using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    public class ThCrossLightNodeLinker
    {
        public List<ThLightNodeLink> LightLinks { get; set; }
        private List<ThLightEdge> FirstEdges { get; set; }
        private List<ThLightEdge> SecondEdges { get; set; }
        private List<ThLightingLinkInfo> LightingLinkInfo { get; set; }
        private double DoubleRowOffsetDis { get; set; }

        public ThCrossLightNodeLinker(List<ThLightEdge> edges, double doubleRowOffsetDis)
        {
            DoubleRowOffsetDis = doubleRowOffsetDis;
            LightLinks = new List<ThLightNodeLink>();
            LightingLinkInfo = new List<ThLightingLinkInfo>();
            FirstEdges = edges.Where(e => e.EdgePattern == EdgePattern.First).ToList();
            SecondEdges = edges.Where(e => e.EdgePattern == EdgePattern.Second).ToList();
        }

        public void Link()
        {
            Link(FirstEdges);
            Link(SecondEdges);
        }

        private void Link(List<ThLightEdge> edges)
        {
            var linkSourceLines = new List<List<Line>>();
            var linkTargetLines = new List<List<Line>>();
            var lines = edges.Select(e => e.Edge).ToList();
            var spatial = new ThCADCoreNTSSpatialIndex(lines.ToCollection());
            lines.ForEach(e =>
            {
                var direction = e.LineDirection();
                // 端点无延伸线则尝试跨线连接
                if (spatial.SelectCrossingPolygon( e.StartPoint.CreateSquare(10.0)).Count == 1)
                {
                    var startExtandLine = new Line(e.StartPoint - DoubleRowOffsetDis / 2 * direction, e.StartPoint - DoubleRowOffsetDis * direction);
                    Search(e, startExtandLine, spatial, direction, linkSourceLines, linkTargetLines);
                }

                if (spatial.SelectCrossingPolygon(e.EndPoint.CreateSquare(10.0)).Count == 1)
                {
                    var endExtandLine = new Line(e.EndPoint + DoubleRowOffsetDis / 2 * direction, e.EndPoint + DoubleRowOffsetDis * direction);
                    Search(e, endExtandLine, spatial, direction, linkSourceLines, linkTargetLines);
                }
            });

            for (var i = 0; i < linkSourceLines.Count; i++)
            {
                var numbers = new List<string>();
                for (var j = 0; j < linkTargetLines[i].Count; j++)
                {
                    var sourceEdge = edges.Where(e => e.Edge.Equals(linkSourceLines[i][0])).FirstOrDefault();
                    var targetEdge = edges.Where(e => e.Edge.Equals(linkTargetLines[i][j])).FirstOrDefault();
                    if (!sourceEdge.IsNull() && !targetEdge.IsNull())
                    {
                        var sourceDict = NodeSort(sourceEdge.LightNodes);
                        var targetDict = NodeSort(targetEdge.LightNodes);
                        sourceDict.ForEach(pair =>
                        {
                            var targetDictClone = targetDict;
                            var linkTargetLineClone = linkTargetLines[i][j];
                            if (!targetDictClone.ContainsKey(pair.Key))
                            {
                                if (j != 0)
                                {
                                    return;
                                }
                                var lineBuffer = linkTargetLineClone.BufferSquare(10.0);
                                var direction = linkTargetLineClone.LineDirection();
                                var filter = lineBuffer.SelectCrossingEntities(spatial)
                                    .Where(l => Math.Abs(l.LineDirection().DotProduct(direction)) > Math.Cos(45 / 180.0 * Math.PI))
                                    .Except(new List<Line> { linkTargetLineClone }).ToList();
                                if (filter.Count == 0)
                                {
                                    return;
                                }
                                targetEdge = edges.Where(e => e.Edge.Equals(filter[0])).FirstOrDefault();
                                if (targetEdge.IsNull())
                                {
                                    return;
                                }
                                targetDictClone = NodeSort(targetEdge.LightNodes);
                                linkTargetLineClone = filter[0];
                                if (!targetDictClone.ContainsKey(pair.Key))
                                {
                                    return;
                                }
                            }
                            var sourceNode = pair.Value.OrderBy(o => linkTargetLineClone.DistanceTo(o.Position, false)).First();
                            if (numbers.Contains(sourceNode.Number))
                            {
                                return;
                            }
                            var targetNode = targetDictClone[pair.Key].OrderBy(o => linkSourceLines[i][0].DistanceTo(o.Position, false)).First();
                            var linkInfo = new ThLightingLinkInfo(linkSourceLines[i][0], linkTargetLineClone, sourceNode.Number);
                            numbers.Add(sourceNode.Number);
                            if (!LightingLinkInfo.Any(info => info.Equals(linkInfo)))
                            {
                                var nodeLink = new ThLightNodeLink
                                {
                                    First = sourceNode,
                                    Second = targetNode,
                                    Edges = new List<Line> { linkSourceLines[i][0], linkTargetLineClone },
                                };
                                LightLinks.Add(nodeLink);
                                LightingLinkInfo.Add(linkInfo);
                            }
                        });

                        if (j != 0)
                        {
                            continue;
                        }
                        targetDict.ForEach(pair =>
                        {
                            if (sourceDict.ContainsKey(pair.Key))
                            {
                                return;
                            }
                            var lineBuffer = linkSourceLines[i][0].BufferSquare(10.0);
                            var direction = linkSourceLines[i][0].LineDirection();
                            var filter = lineBuffer.SelectCrossingEntities(spatial)
                                .Where(l => Math.Abs(l.LineDirection().DotProduct(direction)) > Math.Cos(45 / 180.0 * Math.PI))
                                .Except(new List<Line> { linkSourceLines[i][0] }).ToList();
                            if (filter.Count == 0)
                            {
                                return;
                            }
                            var attachedEdge = edges.Where(e => e.Edge.Equals(filter[0])).FirstOrDefault();
                            if (attachedEdge.IsNull())
                            {
                                return;
                            }
                            var attachedDict = NodeSort(attachedEdge.LightNodes);
                            if (!attachedDict.ContainsKey(pair.Key))
                            {
                                return;
                            }
                            var sourceNode = pair.Value.OrderBy(o => filter[0].DistanceTo(o.Position, false)).First();
                            var targetNode = attachedDict[pair.Key].OrderBy(o => linkTargetLines[i][j].DistanceTo(o.Position, false)).First();
                            var linkInfo = new ThLightingLinkInfo(linkSourceLines[i][0], linkTargetLines[i][j], sourceNode.Number);
                            if (!LightingLinkInfo.Any(info => info.Equals(linkInfo)))
                            {
                                var nodeLink = new ThLightNodeLink
                                {
                                    First = sourceNode,
                                    Second = targetNode,
                                    Edges = new List<Line> { linkTargetLines[i][j], filter[0] },
                                };
                                LightLinks.Add(nodeLink);
                                LightingLinkInfo.Add(linkInfo);
                            }
                        });
                    }
                }
            }
        }

        private void Search(Line line, Line extandLine, ThCADCoreNTSSpatialIndex spatial, Vector3d direction,
            List<List<Line>> linkSourceLines, List<List<Line>> linkTargetLines)
        {
            var searchFrame = extandLine.BufferSquare(10.0);
            var searchLines = searchFrame.SelectCrossingEntities(spatial)
                .Where(l => l.Distance(line) > DoubleRowOffsetDis / 2).ToList();
            if (searchLines.Count == 0)
            {
                return;
            }
            searchLines = Filter(searchLines, line);
            Search(line, spatial, direction, searchLines, linkSourceLines, linkTargetLines);
        }

        private List<Line> Filter(List<Line> searchLines, Line line)
        {
            var direction = line.LineDirection();
            var verticalLines = searchLines.Where(l => Math.Abs(l.LineDirection().DotProduct(direction)) < Math.Sin(1 / 180.0 * Math.PI))
                .OrderByDescending(l => l.Length).ToList();
            if (verticalLines.Count < 2)
            {
                return searchLines;
            }
            var results = searchLines.Except(verticalLines).ToList();
            results.Add(verticalLines.First());
            return results;
        }

        private void Search(Line line, ThCADCoreNTSSpatialIndex spatial, Vector3d direction, List<Line> searchLines,
            List<List<Line>> linkSourceLines, List<List<Line>> linkTargetLines)
        {
            if (searchLines.Count > 0)
            {
                if (searchLines.Count > 1)
                {
                    var targetLines = searchLines.OrderByDescending(l => Math.Abs(l.LineDirection().DotProduct(direction)))
                        .ThenBy(l => l.Distance(line)).ToList();
                    Add(linkSourceLines, linkTargetLines, new List<Line> { line }, targetLines);
                }
                else
                {
                    var target = searchLines.First();
                    Add(linkSourceLines, linkTargetLines, new List<Line> { line }, new List<Line> { target });
                }
            }
        }

        private void Add(List<List<Line>> linkSourceLines, List<List<Line>> linkTargetLines, List<Line> source, List<Line> target)
        {
            var firstLinkSourceLines = linkSourceLines.Select(list => list[0]).ToList();
            var firstLinkTargetLines = linkTargetLines.Select(list => list[0]).ToList();
            if (!Contains(firstLinkSourceLines, firstLinkTargetLines, source[0], target[0]))
            {
                linkSourceLines.Add(source);
                linkTargetLines.Add(target);
            }
        }

        private bool Contains(List<Line> linkSourceLines, List<Line> linkTargetLines, Line source, Line target)
        {
            var isContains = false;
            var sourceIndex = linkSourceLines.IndexOf(source);
            while (sourceIndex != -1)
            {
                if (linkTargetLines[sourceIndex].Equals(target))
                {
                    isContains = true;
                    break;
                }
                else
                {
                    sourceIndex = linkSourceLines.IndexOf(source, sourceIndex + 1);
                }
            }
            return isContains;
        }

        private Dictionary<string, List<ThLightNode>> NodeSort(List<ThLightNode> nodes)
        {
            var results = new Dictionary<string, List<ThLightNode>>();
            nodes.ForEach(node =>
            {
                if (!results.ContainsKey(node.Number))
                {
                    results.Add(node.Number, new List<ThLightNode> { node });
                }
                else
                {
                    results[node.Number].Add(node);
                }
            });
            return results;
        }
    }
}
