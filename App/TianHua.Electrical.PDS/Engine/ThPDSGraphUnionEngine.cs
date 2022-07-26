using System.Linq;
using System.Collections.Generic;

using QuikGraph;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThPDSGraphUnionEngine
    {
        private List<ThPDSEdgeMap> EdgeMapList;

        public BidirectionalGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> UnionGraph;

        public ThPDSGraphUnionEngine(List<ThPDSEdgeMap> edgeMapList)
        {
            EdgeMapList = edgeMapList;
            UnionGraph = new BidirectionalGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
        }

        public void GraphUnion(List<BidirectionalGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>> graphList,
            ThPDSCircuitGraphNode cableTrayNode)
        {
            var cabletrayEdgeList = new List<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
            var addEdgeList = new List<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
            graphList.ForEach(graph =>
            {
                graph.Edges.ForEach(e =>
                {
                    if (e.Source.Equals(cableTrayNode))
                    {
                        cabletrayEdgeList.Add(e);
                    }
                    else
                    {
                        addEdgeList.Add(e);
                    }
                });
            });

            // 将无连接关系的散点加入到图中
            graphList.ForEach(graph =>
            {
                graph.Vertices.ForEach(vertex =>
                {
                    if (vertex.NodeType != PDSNodeType.CableCarrier
                        && graph.OutDegree(vertex) == 0 && graph.InDegree(vertex) == 0)
                    {
                        if (!IsContains(UnionGraph, vertex, out var originalSourceNode))
                        {
                            UnionGraph.AddVertex(vertex);
                        }
                    }
                });
            });

            UnionGraph.Vertices.ForEach(vertex =>
            {
                if (vertex.Loads[0].ID.CircuitNumberList.Count == 1)
                {
                    return;
                }

                for (var i = 1; i < vertex.Loads[0].ID.SourcePanelIDList.Count; i++)
                {
                    var sourcePanel = UnionGraph.Vertices.Where(v => v.Loads[0].ID.LoadID.Equals(vertex.Loads[0].ID.SourcePanelIDList[i])).ToList();
                    if (sourcePanel.Count == 1)
                    {
                        var edge = ThPDSGraphService.UnionEdge(sourcePanel[0], vertex, vertex.Loads[0].ID.SourcePanelIDList[i],
                            vertex.Loads[0].ID.CircuitIDList[i]);
                        edge.Circuit.ViaCableTray = true;
                        addEdgeList.Add(edge);
                    }
                }
            });

            for (var i = 0; i < cabletrayEdgeList.Count; i++)
            {
                var srcPanelID = cabletrayEdgeList[i].Circuit.ID.SourcePanelIDList;
                var circuitID = cabletrayEdgeList[i].Circuit.ID.CircuitIDList;
                var circuitNumber = cabletrayEdgeList[i].Circuit.ID.CircuitNumber;
                if (string.IsNullOrEmpty(circuitNumber))
                {
                    continue;
                }

                var j = 0;
                for (; j < cabletrayEdgeList.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    var otherDistBoxID = "";
                    if (cabletrayEdgeList[j].Target.Loads.Count > 0)
                    {
                        otherDistBoxID = cabletrayEdgeList[j].Target.Loads[0].ID.LoadID;
                    }
                    if (!string.IsNullOrEmpty(otherDistBoxID) && srcPanelID.Last().Equals(otherDistBoxID))
                    {
                        // 对末端配电箱做特殊处理
                        if (ThPDSTerminalPanelService.IsTerminalPanel(cabletrayEdgeList[i].Target)
                            && (cabletrayEdgeList[j].Target.Loads[0].Location.StoreyNumber != cabletrayEdgeList[i].Target.Loads[0].Location.StoreyNumber
                            || cabletrayEdgeList[j].Target.Loads[0].Location.StoreyTypeString != cabletrayEdgeList[i].Target.Loads[0].Location.StoreyTypeString))
                        {
                            continue;
                        }

                        var edge = ThPDSGraphService.UnionEdge(cabletrayEdgeList[j].Target, cabletrayEdgeList[i].Target,
                            srcPanelID, circuitID);
                        edge.Circuit.ViaCableTray = true;
                        if (cabletrayEdgeList[i].Circuit.ViaConduit || cabletrayEdgeList[j].Circuit.ViaConduit)
                        {
                            edge.Circuit.ViaConduit = true;
                        }
                        if (!addEdgeList.Contains(edge))
                        {
                            addEdgeList.Add(edge);
                            var objectIds = new List<ObjectId>();
                            var targetMap = EdgeMapList.FirstOrDefault(e => e.ReferenceDWG == edge.Target.Loads[0].Location.ReferenceDWG);
                            if (targetMap != null)
                            {
                                objectIds.AddRange(targetMap.EdgeMap[cabletrayEdgeList[i]]);
                            }
                            var sourceMap = EdgeMapList.FirstOrDefault(e => e.ReferenceDWG == edge.Source.Loads[0].Location.ReferenceDWG);
                            if (targetMap != null && targetMap.ReferenceDWG == sourceMap.ReferenceDWG)
                            {
                                objectIds.AddRange(targetMap.EdgeMap[cabletrayEdgeList[j]]);
                            }
                            if (!targetMap.EdgeMap.ContainsKey(edge))
                            {
                                targetMap.EdgeMap.Add(edge, objectIds);
                            }
                            else
                            {
                                targetMap.EdgeMap[edge].AddRange(objectIds);
                                targetMap.EdgeMap[edge] = targetMap.EdgeMap[edge].Distinct().ToList();
                            }
                        }

                        break;
                    }
                }

                if (j == cabletrayEdgeList.Count)
                {
                    var sourcePanel = UnionGraph.Vertices.Where(v => v.Loads[0].ID.LoadID.Equals(srcPanelID.Last())).ToList();
                    if (sourcePanel.Count == 1)
                    {
                        var edge = ThPDSGraphService.UnionEdge(sourcePanel[0], cabletrayEdgeList[i].Target, srcPanelID, circuitID);
                        edge.Circuit.ViaCableTray = true;
                        if (cabletrayEdgeList[i].Circuit.ViaConduit)
                        {
                            edge.Circuit.ViaConduit = true;
                        }
                        addEdgeList.Add(edge);
                    }
                }
            }

            cabletrayEdgeList.ForEach(edge =>
            {
                if (!IsContains(UnionGraph, edge.Target, out var originalNode))
                {
                    UnionGraph.AddVertex(edge.Target);
                }
            });

            addEdgeList.ForEach(edge =>
            {
                var sourceCheck = IsContains(UnionGraph, edge.Source, out var originalSourceNode);
                var targetCheck = IsContains(UnionGraph, edge.Target, out var originalTargetNode);
                if (!sourceCheck)
                {
                    UnionGraph.AddVertex(edge.Source);
                }
                if (!targetCheck)
                {
                    UnionGraph.AddVertex(edge.Target);
                }

                if (!sourceCheck && !targetCheck)
                {
                    UnionGraph.AddEdge(edge);
                }
                else
                {
                    var newEdge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(originalSourceNode, originalTargetNode)
                    {
                        Circuit = edge.Circuit,
                    };
                    if (!ThPDSEdgeContainsService.EdgeContainsEx(newEdge, UnionGraph))
                    {
                        UnionGraph.AddEdge(newEdge);
                    }
                }
            });

            UnionGraph.Edges.ForEach(edge =>
            {
                if (edge.Target.Loads[0].InstalledCapacity.IsDualPower
                && !edge.Source.Loads[0].InstalledCapacity.IsDualPower)
                {
                    edge.Source.Loads[0].InstalledCapacity.IsDualPower = true;
                }
            });
        }

        public void SplitSeriesConnection()
        {
            var addEdges = new List<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
            var removeEdges = new List<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();

            UnionGraph.Edges.ForEach(edge =>
            {
                if (ThPDSTerminalPanelService.IsTwowayTerminalPanel(edge))
                {
                    // 上级节点搜索
                    var sourceEdges = UnionGraph.InEdges(edge.Source).ToList();
                    if (sourceEdges.Count > 0)
                    {
                        while (ThPDSTerminalPanelService.IsTwowayTerminalPanel(sourceEdges[0]))
                        {
                            sourceEdges = UnionGraph.InEdges(sourceEdges[0].Source).ToList();
                            if (sourceEdges.Count == 0)
                            {
                                break;
                            }
                        }
                        if (sourceEdges.Count > 0)
                        {
                            var newEdge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(sourceEdges[0].Source, edge.Target);
                            newEdge.Circuit = sourceEdges[0].Circuit;
                            removeEdges.Add(edge);
                            addEdges.Add(newEdge);
                            UpdateEdgeMap(edge, newEdge);
                            return;
                        }
                    }

                    // 下级节点搜索
                    var targetEdges = UnionGraph.OutEdges(edge.Target).ToList();
                    if (targetEdges.Count > 0)
                    {
                        while (ThPDSTerminalPanelService.IsTwowayTerminalPanel(targetEdges[0]))
                        {
                            targetEdges = UnionGraph.OutEdges(targetEdges[0].Target).ToList();
                            if (targetEdges.Count == 0)
                            {
                                break;
                            }
                        }
                        if (targetEdges.Count > 0)
                        {
                            var newEdge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(targetEdges[0].Target, edge.Source);
                            newEdge.Circuit = targetEdges[0].Circuit;
                            removeEdges.Add(edge);
                            addEdges.Add(newEdge);
                            UpdateEdgeMap(edge, newEdge);
                            return;
                        }
                    }
                }
            });

            removeEdges.ForEach(edge => UnionGraph.RemoveEdge(edge));
            addEdges.ForEach(edge => UnionGraph.AddEdge(edge));
        }

        private void UpdateEdgeMap(ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> edge, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> newEdge)
        {
            EdgeMapList.ForEach(map =>
            {
                if (map.EdgeMap.ContainsKey(edge))
                {
                    map.EdgeMap.Add(newEdge, map.EdgeMap[edge]);
                    map.EdgeMap.Remove(edge);
                }
            });
        }

        public void UnionSubstation(List<THPDSSubstation> substationList)
        {
            substationList.ForEach(substation =>
            {
                substation.Transformers.ForEach(transform =>
                {
                    transform.LowVoltageCabinets.ForEach(lowVoltageCabinet =>
                    {
                        lowVoltageCabinet.Edges.ForEach(edge =>
                        {
                            if (IsContains(UnionGraph, edge.Target, out var originalNode))
                            {
                                edge.Target = originalNode;
                            }
                        });
                    });
                });
            });
        }

        /// <summary>
        /// 判断图中是否已包含该节点，若包含则返回true
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="originalNode"></param>
        /// <returns></returns>
        private bool IsContains(BidirectionalGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> graph,
            ThPDSCircuitGraphNode node, out ThPDSCircuitGraphNode originalNode)
        {
            if (node.NodeType != PDSNodeType.Unkown && !node.Loads[0].Location.IsStandardStorey
                && !ThPDSTerminalPanelService.IsTerminalPanel(node))
            {
                if (!string.IsNullOrEmpty(node.Loads[0].ID.LoadID))
                {
                    foreach (var vertex in graph.Vertices)
                    {
                        // id、楼层、位置判断
                        if (LoadIDCheck(vertex, node) && StoreyCheck(vertex, node) && PositionCheck(vertex, node) && TypeCheck(vertex, node))
                        {
                            if (!PowerCheck(vertex, node))
                            {
                                vertex.Loads[0].InstalledCapacity = vertex.Loads[0].InstalledCapacity.HighPower
                                    > node.Loads[0].InstalledCapacity.HighPower ? vertex.Loads[0].InstalledCapacity : node.Loads[0].InstalledCapacity;
                            }
                            if (!DescriptionCheck(vertex, node))
                            {
                                if (vertex.Loads[0].ID.Description.Equals(vertex.Loads[0].ID.DefaultDescription))
                                {
                                    vertex.Loads[0].ID.Description = node.Loads[0].ID.Description;
                                }
                            }

                            originalNode = vertex;
                            if (!LocationEquals(node.Loads[0].Location, originalNode.Loads[0].Location))
                            {
                                originalNode.Loads[0].SetLocation(node.Loads[0].Location);
                            }
                            return true;
                        }
                    }
                }
            }
            originalNode = node;
            return false;
        }

        private Point3d ToPoint3d(Vector3d vector)
        {
            return new Point3d(vector.X, vector.Y, 0);
        }

        /// <summary>
        /// 负载编号校核
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool LoadIDCheck(ThPDSCircuitGraphNode vertex, ThPDSCircuitGraphNode node)
        {
            return vertex.Loads[0].ID.LoadID == node.Loads[0].ID.LoadID;
        }

        /// <summary>
        /// 楼层校核
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool StoreyCheck(ThPDSCircuitGraphNode vertex, ThPDSCircuitGraphNode node)
        {
            return vertex.Loads[0].Location.StoreyNumber == node.Loads[0].Location.StoreyNumber
                && vertex.Loads[0].Location.StoreyTypeString == node.Loads[0].Location.StoreyTypeString;
        }

        /// <summary>
        /// 相对位置校核
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool PositionCheck(ThPDSCircuitGraphNode vertex, ThPDSCircuitGraphNode node)
        {
            return ToPoint3d(ThPDSPoint3dService.PDSPoint3dToPoint3d(vertex.Loads[0].Location.StoreyBasePoint)
                - ThPDSPoint3dService.PDSPoint3dToPoint3d(vertex.Loads[0].Location.BasePoint))
                .DistanceTo(ToPoint3d(ThPDSPoint3dService.PDSPoint3dToPoint3d(node.Loads[0].Location.StoreyBasePoint)
                - ThPDSPoint3dService.PDSPoint3dToPoint3d(node.Loads[0].Location.BasePoint)))
                < ThPDSCommon.STOREY_TOLERANCE;
        }

        /// <summary>
        /// 描述校核
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool DescriptionCheck(ThPDSCircuitGraphNode vertex, ThPDSCircuitGraphNode node)
        {
            return vertex.Loads[0].ID.Description == node.Loads[0].ID.Description;
        }

        /// <summary>
        /// 功率校核
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool PowerCheck(ThPDSCircuitGraphNode vertex, ThPDSCircuitGraphNode node)
        {
            return vertex.Loads[0].InstalledCapacity.EqualsTo(node.Loads[0].InstalledCapacity);
        }

        /// <summary>
        /// 负载类型校核
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool TypeCheck(ThPDSCircuitGraphNode vertex, ThPDSCircuitGraphNode node)
        {
            return vertex.Loads[0].LoadTypeCat_1 == node.Loads[0].LoadTypeCat_1
                && vertex.Loads[0].LoadTypeCat_2 == node.Loads[0].LoadTypeCat_2
                && vertex.Loads[0].LoadTypeCat_3 == node.Loads[0].LoadTypeCat_3;
        }

        /// <summary>
        /// 位置信息校核
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private bool LocationEquals(ThPDSLocation first, ThPDSLocation second)
        {
            return first.ReferenceDWG.Equals(second.ReferenceDWG)
                && first.StoreyNumber.Equals(second.StoreyNumber)
                && first.StoreyTypeString.Equals(second.StoreyTypeString)
                && first.RoomType.Equals(second.RoomType)
                && ThPDSPoint3dService.PDSPoint3dToPoint3d(first.StoreyBasePoint).DistanceTo(ThPDSPoint3dService.PDSPoint3dToPoint3d(second.StoreyBasePoint)) < 1.0
                && ThPDSPoint3dService.PDSPoint3dToPoint3d(first.BasePoint).DistanceTo(ThPDSPoint3dService.PDSPoint3dToPoint3d(second.BasePoint)) < 1.0;
        }
    }
}