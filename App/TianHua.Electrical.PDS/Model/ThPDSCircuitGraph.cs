using System;
using System.Collections.Generic;
using System.Linq;
using QuickGraph;

namespace TianHua.Electrical.PDS.Model
{
    public enum PDSNodeType
    {
        /// <summary>
        /// 配电箱
        /// </summary>
        DistributionBox,
        /// <summary>
        /// 负载
        /// </summary>
        Load,
        /// <summary>
        /// 桥架
        /// </summary>
        Cabletray,
        /// <summary>
        /// 未知
        /// </summary>
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

    public class ThPDSCircuitGraphEdge<T> : Edge<T> where T : ThPDSCircuitGraphNode
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
