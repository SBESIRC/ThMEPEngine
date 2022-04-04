using QuikGraph;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Project.Module
{
    public class ThPDSProjectGraphEdge<T> : EquatableTaggedEdge<T, ThPDSProjectGraphEdgeTag> where T : ThPDSProjectGraphNode
    {
        public ThPDSCircuit Circuit { get; set; }
        public CircuitDetails Details { get; set; }
        public ThPDSProjectGraphEdge(T source, T target) : base(source, target, null)
        {
            //
        }
    }
}
