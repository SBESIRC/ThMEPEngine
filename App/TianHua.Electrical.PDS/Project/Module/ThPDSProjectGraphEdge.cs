using QuikGraph;
using System;
using System.Linq;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Project.Module
{
    [Serializable]
    public class ThPDSProjectGraphEdge : EquatableTaggedEdge<ThPDSProjectGraphNode, ThPDSProjectGraphEdgeTag>
    {
        public ThPDSCircuit Circuit { get; set; }
        public CircuitDetails Details { get; set; }
        public ThPDSProjectGraphEdge(ThPDSProjectGraphNode source, ThPDSProjectGraphNode target) : base(source, target, null)
        {
            //
        }

        #region
        public override bool Equals(EquatableEdge<ThPDSProjectGraphNode> other)
        {
            if (other is ThPDSProjectGraphEdge edge)
            {
                return Circuit.ID.CircuitNumber.Last().Equals(edge.Circuit.ID.CircuitNumber.Last());
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is ThPDSProjectGraphEdge edge)
            {
                return Circuit.ID.CircuitNumber.Last().Equals(edge.Circuit.ID.CircuitNumber.Last());
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
