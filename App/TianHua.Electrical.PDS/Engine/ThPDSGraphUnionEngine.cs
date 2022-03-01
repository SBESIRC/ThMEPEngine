using System.Collections.Generic;
using System.Linq;
using Dreambuild.AutoCAD;
using QuickGraph;

using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThPDSGraphUnionEngine
    {
        /// <summary>
        /// 配电箱关键字
        /// </summary>
        public List<string> DistBoxKey { get; set; }

        public ThPDSGraphUnionEngine(List<string> distBoxKey)
        {
            DistBoxKey = distBoxKey;
        }

        public AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> GraphUnion(
            List<AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>> graphList,
            ThPDSCircuitGraphNode cabletrayNode)
        {
            var cabletrayEdgeList = new List<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
            var addEdgeList = new List<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
            graphList.ForEach(graph =>
            {
                graph.Edges.ForEach(e =>
                {
                    if (e.Source == cabletrayNode)
                    {
                        cabletrayEdgeList.Add(e);
                    }
                    else
                    {
                        addEdgeList.Add(e);
                    }
                });
            });

            for (var i = 0; i < cabletrayEdgeList.Count - 1; i++)
            {
                var circuitID = cabletrayEdgeList[i].Circuit.ID.CircuitNumber;

                for (var j = i + 1; j < cabletrayEdgeList.Count; j++)
                {
                    var otherDistBoxID = "";
                    if (cabletrayEdgeList[j].Target.Loads.Count > 0)
                    {
                        otherDistBoxID = cabletrayEdgeList[j].Target.Loads[0].ID.LoadID;
                    }
                    if (circuitID.IndexOf(otherDistBoxID) == 0 && !string.IsNullOrEmpty(otherDistBoxID))
                    {
                        var edge = ThPDSGraphService.UnionEdge(cabletrayEdgeList[i].Target, cabletrayEdgeList[j].Target,
                            new List<string> { circuitID });
                        edge.Circuit.ViaCableTray = true;
                        if (cabletrayEdgeList[i].Circuit.ViaConduit || cabletrayEdgeList[j].Circuit.ViaConduit)
                        {
                            edge.Circuit.ViaConduit = true;
                        }
                        addEdgeList.Add(edge);
                    }
                }
            }

            var unionGraph = new AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
            cabletrayEdgeList.ForEach(edge =>
            {
                unionGraph.AddVertex(edge.Target);
            });
            addEdgeList.ForEach(edge =>
            {
                unionGraph.AddVertex(edge.Source);
                unionGraph.AddVertex(edge.Target);
                unionGraph.AddEdge(edge);
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
    }
}
