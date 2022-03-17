using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using QuikGraph;

using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThPDSGraphUnionEngine
    {
        public ThPDSGraphUnionEngine()
        {
            //
        }

        public AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> GraphUnion(
            List<AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>> graphList,
            ThPDSCircuitGraphNode cableTrayNode)
        {
            var cabletrayEdgeList = new List<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
            var addEdgeList = new List<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
            graphList.ForEach(graph =>
            {
                graph.Edges.ForEach(e =>
                {
                    if (e.Source == cableTrayNode)
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
                var circuitID = cabletrayEdgeList[i].Circuit.ID.CircuitNumber;

                if (string.IsNullOrEmpty(circuitID))
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
                    if (!string.IsNullOrEmpty(otherDistBoxID) && circuitID.IndexOf(otherDistBoxID) == 0)
                    {
                        var edge = ThPDSGraphService.UnionEdge(cabletrayEdgeList[j].Target, cabletrayEdgeList[i].Target,
                            new List<string> { circuitID });
                        edge.Circuit.ViaCableTray = true;
                        if (cabletrayEdgeList[i].Circuit.ViaConduit || cabletrayEdgeList[j].Circuit.ViaConduit)
                        {
                            edge.Circuit.ViaConduit = true;
                        }
                        addEdgeList.Add(edge);
                        break;
                    }
                }
            }

            var unionGraph = new AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
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
                    unionGraph.AddEdge(newEdge);
                }

            });

            // 设置图的遍历起点
            foreach (var vertice in unionGraph.Vertices)
            {
                if (!unionGraph.Edges.Any(o => o.Target.Equals(vertice)))
                {
                    vertice.IsStartVertexOfGraph = true;
                }
            }

            return unionGraph;
        }

        /// <summary>
        /// 判断图中是否已包含该节点，若包含则返回true
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="node"></param>
        /// <param name="originalNode"></param>
        /// <returns></returns>
        private bool IsContains(AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> graph,
            ThPDSCircuitGraphNode node, out ThPDSCircuitGraphNode originalNode)
        {
            if (node.NodeType != PDSNodeType.None)
            {
                if (!string.IsNullOrEmpty(node.Loads[0].ID.LoadID))
                {
                    foreach (var vertex in graph.Vertices)
                    {
                        // id、楼层、位置判断
                        if (vertex.Loads[0].ID.LoadID == node.Loads[0].ID.LoadID
                            && vertex.Loads[0].Location.FloorNumber == node.Loads[0].Location.FloorNumber
                            && ToPoint3d(vertex.Loads[0].Location.StoreyBasePoint - vertex.Loads[0].Location.BasePoint)
                                .DistanceTo(ToPoint3d(node.Loads[0].Location.StoreyBasePoint - node.Loads[0].Location.BasePoint))
                                < ThPDSCommon.STOREY_TOLERANCE)
                        {
                            originalNode = vertex;
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
    }
}