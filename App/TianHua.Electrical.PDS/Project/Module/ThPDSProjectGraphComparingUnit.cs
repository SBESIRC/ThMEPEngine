using PDSGraph = QuikGraph.BidirectionalGraph<
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode,
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.Project.Module
{
    public abstract class ThPDSProjectGraphComparingUnit
    {
        public abstract void DoCompare(PDSGraph source, PDSGraph target);

        protected bool NodeEquals(ThPDSProjectGraphNode node, ThPDSProjectGraphNode other)
        {
            return node.Load.ID.LoadID == other.Load.ID.LoadID;
        }

        protected bool EdgeEquals(ThPDSProjectGraphEdge edge, ThPDSProjectGraphEdge other)
        {
            return edge.Circuit.ID.CircuitID.Equals(other.Circuit.ID.CircuitID);
        }
    }
}
