using QuikGraph;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Project.Module
{
    public class ThPDSProjectGraphEdge : EquatableTaggedEdge<ThPDSProjectGraphNode, ThPDSProjectGraphEdgeCompareTag>
    {
        public ThPDSCircuit Circuit { get; set; }
        public CircuitDetails Details { get; set; }
        public ThPDSProjectGraphEdge(ThPDSProjectGraphNode source, ThPDSProjectGraphNode target) : base(source, target, null)
        {
            //
        }
    }
}
