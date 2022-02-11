using System;
using QuickGraph;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSCircuitGraphNode : IEquatable<ThPDSCircuitGraphNode>
    {
        public bool Equals(ThPDSCircuitGraphNode other)
        {
            throw new NotImplementedException();
        }
    }

    public class ThPDSCircuitGraphEdge<T> : Edge<T> where T : ThPDSCircuitGraphNode
    {
        public ThPDSCircuitGraphEdge(T source, T target) : base(source, target)
        {
            //
        }
    }

    public class ThPDSCircuitGraph
    {
        private AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> Graph { get; set; }
    }
}
