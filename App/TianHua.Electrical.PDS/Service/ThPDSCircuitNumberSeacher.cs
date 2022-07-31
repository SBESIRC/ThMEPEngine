using System.Linq;
using System.Collections.Generic;

using QuikGraph;

using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSCircuitNumberSeacher
    {
        public static List<string> Seach(ThPDSProjectGraphNode node,
             BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph)
        {
            return graph.InEdges(node)
                .Select(e => e.Circuit.ID.CircuitNumber)
                .OrderBy(str => str)
                .ToList();
        }
    }
}
