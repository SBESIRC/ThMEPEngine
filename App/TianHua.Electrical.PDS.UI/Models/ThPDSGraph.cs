using QuickGraph;
using System.Collections.Generic;
using System.Linq;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.UI.Models
{
    public class ThPDSGraph
    {
        public AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> Graph { get; set; }
        private Dictionary<ThPDSProjectGraphNode, int> IdDict { get; set; }
        private List<ThPDSProjectGraphNode> GetDistributionBoxes()
        {
            return Graph.Vertices.Where(x => x.Type == Model.PDSNodeType.DistributionBox).ToList();
        }
        //private CircuitFormOutType GetCircuitFormOutType(ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge)
        //{
        //    return edge.circuitDetails?.CircuitFormType ?? default;
        //}
        //private CircuitFormInType GetCircuitFormInType(ThPDSProjectGraphNode node)
        //{
        //    return node.nodeDetails?.CircuitFormType ?? default;
        //}
    }
}
