using System.Linq;
using System.Collections.Generic;

using TianHua.Electrical.PDS.Model;
using CircuitGraph = QuikGraph.BidirectionalGraph<TianHua.Electrical.PDS.Model.ThPDSCircuitGraphNode,
    TianHua.Electrical.PDS.Model.ThPDSCircuitGraphEdge<TianHua.Electrical.PDS.Model.ThPDSCircuitGraphNode>>;

namespace TianHua.Electrical.PDS.Service
{
    public static class ThPDSPowerTransformerService
    {
        public static bool Contains(List<ThPDSCircuitGraphNode> nodes, string powerTransformer)
        {
            return nodes.Any(v => v.Loads[0].ID.LoadID.Equals(powerTransformer));
        }
    }
}
