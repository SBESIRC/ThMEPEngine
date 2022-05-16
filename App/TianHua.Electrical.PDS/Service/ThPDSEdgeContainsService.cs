using System.Linq;
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

        public static bool EdgeContainsEx(ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode> edge, CircuitGraph circuitGraph)
        {
            var sourceEdge = circuitGraph.Edges.Where(e => e.Equals(edge)).ToList();
            if (sourceEdge.Count > 0)
            {
                if (edge.Circuit.ID.CircuitNumber.Equals(sourceEdge[0].Circuit.ID.CircuitNumber))
                {
                    return true;
                }
            }
            var inverseEdge = new ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>(edge.Target, edge.Source);
            var sourceInverseEdge = circuitGraph.Edges.Where(e => e.Equals(inverseEdge)).ToList();
            if (sourceInverseEdge.Count > 0)
            {
                if (inverseEdge.Circuit.ID.CircuitNumber.Equals(sourceInverseEdge[0].Circuit.ID.CircuitNumber))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
