using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Project.Module
{
    public class ThPDSProjectGraphNode : IEquatable<ThPDSProjectGraphNode>
    {
        public ThPDSProjectGraphNode()
        {
            Load = new ThPDSLoad();
            IsStartVertexOfGraph = false;
            nodeDetails = new NodeDetails();
        }
        public NodeDetails nodeDetails { get; set; }
        public PDSNodeType NodeType { get; set; }
        public bool IsStartVertexOfGraph { get; set; }
        public ThPDSLoad Load { get; set; }
        public bool Equals(ThPDSProjectGraphNode other)
        {
            return this.NodeType == other.NodeType && this.Load.Equals(other.Load);
        }
    }

    public class ThPDSProjectGraphEdge<T> : Edge<T> where T : ThPDSProjectGraphNode
    {
        public ThPDSCircuit Circuit { get; set; }
        public CircuitDetails circuitDetails { get; set; }

        public ThPDSProjectGraphEdge(T source, T target) : base(source, target)
        {
            Circuit = new ThPDSCircuit();
            circuitDetails = new CircuitDetails();
        }

        /// <summary>
        /// 计算回路详情
        /// </summary>
        public void CalculateCircuitDetails()
        {

        }
    }
    [Serializable]
    public class ThPDSProjectGraph
    {
        public AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> Graph { get; set; }
        public Dictionary<ThPDSProjectGraphNode, CircuitDetails> CircuitInfo { get; set; }

    }
}
