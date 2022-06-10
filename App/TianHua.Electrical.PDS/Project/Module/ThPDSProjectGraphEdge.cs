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
                return this.Circuit.CircuitUID == edge.Circuit.CircuitUID;
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ThPDSProjectGraphEdge);
        }

        public override int GetHashCode()
        {
            return Circuit.ID.CircuitIDList.GetHashCode();
        }
        #endregion
    }
}
