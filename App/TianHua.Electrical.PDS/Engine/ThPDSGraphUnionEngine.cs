using System.Collections.Generic;

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
                var distBoxID = cabletrayEdgeList[i].Target.Loads[0].ID.LoadID;
                var circuitID = cabletrayEdgeList[i].Circuit.ID.CircuitNumber;

                for (var j = i + 1; j < cabletrayEdgeList.Count; j++)
                {
                    var otherDistBoxID = cabletrayEdgeList[j].Target.Loads[0].ID.LoadID;
                    var otherCircuitID = cabletrayEdgeList[j].Circuit.ID.CircuitNumber;

                    if (circuitID.IndexOf(otherDistBoxID) == 0)
                    {
                        var edge = ThPDSGraphService.CreateEdge(cabletrayEdgeList[j].Target, cabletrayEdgeList[i].Target,
                            new List<string> { circuitID }, DistBoxKey, true);
                        addEdgeList.Add(edge);
                    }
                }
            }

            var unionGraph = new AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>>();
            addEdgeList.ForEach(edge =>
            {
                unionGraph.AddVertex(edge.Source);
                unionGraph.AddVertex(edge.Target);
                unionGraph.AddEdge(edge);
            });

            return unionGraph;
        }
    }
}
