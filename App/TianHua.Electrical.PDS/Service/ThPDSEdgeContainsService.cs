using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

using TianHua.Electrical.PDS.Model;
using CircuitGraph = QuikGraph.BidirectionalGraph<TianHua.Electrical.PDS.Model.ThPDSCircuitGraphNode,
    TianHua.Electrical.PDS.Model.ThPDSCircuitGraphEdge<TianHua.Electrical.PDS.Model.ThPDSCircuitGraphNode>>;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSEdgeContainsService
    {
        public static bool EdgeContains(ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> edge, CircuitGraph circuitGraph)
        {
            return circuitGraph.ContainsEdge(edge)
                || circuitGraph.ContainsEdge(edge.Target, edge.Source);
        }

        /// <summary>
        /// 判断图中是否含有相同回路编号的边
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="circuitGraph"></param>
        /// <returns></returns>
        public static bool EdgeContainsEx(ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> edge, CircuitGraph circuitGraph)
        {
            var sourceEdge = circuitGraph.Edges.Where(e => e.Equals(edge)).ToList();
            if (sourceEdge.Count > 0)
            {
                foreach (var e in sourceEdge)
                {
                    if (edge.Circuit.ID.CircuitNumber.Equals(e.Circuit.ID.CircuitNumber))
                    {
                        return true;
                    }
                }
            }

            return circuitGraph.ContainsEdge(edge.Target, edge.Source);
        }

        /// <summary>
        /// 若图中包含该边，且图中回路编号为空，则进行替换并返回true，
        /// </summary>
        /// <param name="edge"></param>
        /// <param name="circuitGraph"></param>
        /// <returns></returns>
        public static bool EdgeContainsAndInstand(ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> edge, CircuitGraph circuitGraph,
            Dictionary<ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>, List<ObjectId>> edgeMap, List<ObjectId> objectIds)
        {
            var sourceEdge = circuitGraph.Edges.Where(e => e.Equals(edge)).ToList();
            foreach (var e in sourceEdge)
            {
                if (!string.IsNullOrEmpty(edge.Circuit.ID.CircuitNumber) && string.IsNullOrEmpty(e.Circuit.ID.CircuitNumber))
                {
                    circuitGraph.RemoveEdge(e);
                    circuitGraph.AddEdge(edge);
                    edgeMap.Remove(e);
                    edgeMap.Add(edge, objectIds);
                    return true;
                }
            }

            return circuitGraph.ContainsEdge(edge.Target, edge.Source);
        }
    }
}
