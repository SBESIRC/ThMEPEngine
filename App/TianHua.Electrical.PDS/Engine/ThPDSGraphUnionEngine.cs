using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using QuikGraph;

using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThPDSGraphUnionEngine
    {
        private List<ThPDSEdgeMap> EdgeMapList;

        public ThPDSGraphUnionEngine(List<ThPDSEdgeMap> edgeMapList)
        {
            EdgeMapList = edgeMapList;
        }

        public BidirectionalGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> GraphUnion(
            List<BidirectionalGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>> graphList,
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

            for (var i = 0; i < cabletrayEdgeList.Count; i++)
            {
                var srcpanelID = cabletrayEdgeList[i].Circuit.ID.SourcePanelIDList;
                var circuitID = cabletrayEdgeList[i].Circuit.ID.CircuitIDList;
                var circuitNumber = cabletrayEdgeList[i].Circuit.ID.CircuitNumber;
                if (string.IsNullOrEmpty(circuitNumber))
                {
                    continue;
                }

                for (var j = 0; j < cabletrayEdgeList.Count; j++)
                {
                    var otherDistBoxID = "";
                    if (cabletrayEdgeList[j].Target.Loads.Count > 0)
                    {
                        otherDistBoxID = cabletrayEdgeList[j].Target.Loads[0].ID.LoadID;
                    }
                    if (!string.IsNullOrEmpty(otherDistBoxID) && srcpanelID.Last().Equals(otherDistBoxID))
                    {
                        var edge = ThPDSGraphService.UnionEdge(cabletrayEdgeList[j].Target, cabletrayEdgeList[i].Target,
                            srcpanelID, circuitID);
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
            }

            var unionGraph = new BidirectionalGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
            cabletrayEdgeList.ForEach(edge =>
            {
                if (!IsContains(unionGraph, edge.Target, out var originalNode))
                {
                    unionGraph.AddVertex(edge.Target);
                }
            });
            addEdgeList.ForEach(edge =>
            {
                var sourceCheck = IsContains(unionGraph, edge.Source, out var originalSourceNode);
                var targetCheck = IsContains(unionGraph, edge.Target, out var originalTargetNode);
                if (!sourceCheck)
                {
                    unionGraph.AddVertex(edge.Source);
                }
                if (!targetCheck)
                {
                    unionGraph.AddVertex(edge.Target);
                }

                if (!sourceCheck && !targetCheck)
                {
                    unionGraph.AddEdge(edge);
                }
                else
                {
                    var newEdge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(originalSourceNode, originalTargetNode)
                    {
                        Circuit = edge.Circuit,
                    };
                    if (!ThPDSEdgeContainsService.EdgeContainsEx(newEdge, unionGraph))
                    {
                        unionGraph.AddEdge(newEdge);
                    }
                }
            });

            // 将无连接关系的散点加入到图中
            graphList.ForEach(graph =>
            {
                graph.Vertices.ForEach(vertex =>
                {
                    if (vertex.NodeType != PDSNodeType.CableCarrier
                        && graph.OutDegree(vertex) == 0 && graph.InDegree(vertex) == 0)
                    {
                        if(!IsContains(unionGraph, vertex, out var originalSourceNode))
                        {
                            unionGraph.AddVertex(vertex);
                        }
                    }
                });
            });

            unionGraph.Edges.ForEach(edge =>
            {
                if(edge.Target.Loads[0].InstalledCapacity.IsDualPower
                && !edge.Source.Loads[0].InstalledCapacity.IsDualPower)
                {
                    edge.Source.Loads[0].InstalledCapacity.IsDualPower = true;
                }
            });

            return unionGraph;
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
            if (node.NodeType != PDSNodeType.Unkown && !node.Loads[0].Location.IsStandardStorey)
            {
                if (!string.IsNullOrEmpty(node.Loads[0].ID.LoadID))
                {
                    foreach (var vertex in graph.Vertices)
                    {
                        // id、楼层、位置判断
                        if (LoadIDCheck(vertex, node) && StoreyCheck(vertex, node) && PositionCheck(vertex, node)
                            && DescriptionCheck(vertex, node) && PowerCheck(vertex, node) && TypeCheck(vertex, node))
                        {
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
            return vertex.Loads[0].Location.FloorNumber == node.Loads[0].Location.FloorNumber;
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
                && first.FloorNumber.Equals(second.FloorNumber)
                && first.RoomType.Equals(second.RoomType)
                && ThPDSPoint3dService.PDSPoint3dToPoint3d(first.StoreyBasePoint).DistanceTo(ThPDSPoint3dService.PDSPoint3dToPoint3d(second.StoreyBasePoint)) < 1.0
                && ThPDSPoint3dService.PDSPoint3dToPoint3d(first.BasePoint).DistanceTo(ThPDSPoint3dService.PDSPoint3dToPoint3d(second.BasePoint)) < 1.0;
        }
    }
}