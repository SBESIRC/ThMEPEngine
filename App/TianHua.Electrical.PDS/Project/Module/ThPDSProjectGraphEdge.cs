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
            Circuit = new ThPDSCircuit();
            Details = new CircuitDetails();
        }

        #region
        public override bool Equals(EquatableEdge<ThPDSProjectGraphNode> other)
        {
            if (other is ThPDSProjectGraphEdge edge)
            {
                return Circuit.ID.CircuitID.Equals(edge.Circuit.ID.CircuitID);
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is ThPDSProjectGraphEdge edge)
            {
                return Circuit.ID.CircuitID.Equals(edge.Circuit.ID.CircuitID);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Circuit.ID.CircuitID.GetHashCode();
        }
        #endregion
    }
}
