using QuikGraph;
using System.Collections.Generic;
using System.Linq;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSCircuitNumberSeacher
    {
        public static List<string> Seach(ThPDSProjectGraphNode node,
             AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> graph)
        {
            return graph.Edges.Where(e => e.Target.Equals(node))
                .Select(e => e.Circuit.ID.CircuitNumber.Last())
                .OrderBy(str => str)
                .ToList();
        }
    }
}
