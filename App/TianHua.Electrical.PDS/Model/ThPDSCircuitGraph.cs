using System;
using System.Collections.Generic;
using System.Linq;
using QuikGraph;
using System.ComponentModel;

namespace TianHua.Electrical.PDS.Model
{
    public enum PDSNodeType
    {
        [Description("配电箱")]
        DistributionBox,
        [Description("负载")]
        Load,
        [Description("桥架")]
        Cabletray,
        [Description("未知")]
        None
    }

    public class ThPDSCircuitGraphNode : IEquatable<ThPDSCircuitGraphNode>
    {
        public ThPDSCircuitGraphNode()
        {
            Loads = new List<ThPDSLoad>();
            IsStartVertexOfGraph = false;
        }
        public PDSNodeType NodeType { get; set; }
        public bool IsStartVertexOfGraph { get; set; }
        public List<ThPDSLoad> Loads { get; set; }
        public bool Equals(ThPDSCircuitGraphNode other)
        {
            return this.NodeType == other.NodeType && this.Loads.SequenceEqual(other.Loads);
        }
    }

    public class ThPDSCircuitGraphEdge<T> : EquatableEdge<T> where T : ThPDSCircuitGraphNode
    {
        public ThPDSCircuit Circuit { get; set; }
        public ThPDSCircuitGraphEdge(T source, T target) : base(source, target)
        {
            Circuit = new ThPDSCircuit();
        }
    }

    public class ThPDSCircuitGraph
    {
        private AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> graph { get; set; }
        public AdjacencyGraph<ThPDSCircuitGraphNode, ThPDSCircuitGraphEdge<ThPDSCircuitGraphNode>> Graph
        {
            get
            {
                return graph;
            }
            set
            {
                graph = value;
            }
        }
    }
}
