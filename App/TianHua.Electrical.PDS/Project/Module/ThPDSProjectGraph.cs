using System;
using QuikGraph;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Project.Module
{
    public class ThPDSProjectGraphNode : IEquatable<ThPDSProjectGraphNode>
    {
        public ThPDSLoad Load { get; set; }
        public PDSNodeType Type { get; set; }
        public NodeDetails Details { get; set; }
        public bool IsStartVertexOfGraph { get; set; }
        public ThPDSProjectGraphNode()
        {
            Load = new ThPDSLoad();
            IsStartVertexOfGraph = false;
            Details = new NodeDetails();
        }
        public bool Equals(ThPDSProjectGraphNode other)
        {
            return this.Type == other.Type && this.Load.Equals(other.Load);
        }
    }

    public class ThPDSProjectGraphEdge<T> : EquatableEdge<T> where T : ThPDSProjectGraphNode
    {
        public ThPDSCircuit Circuit { get; set; }
        public CircuitDetails Details { get; set; }
        public ThPDSProjectGraphEdge(T source, T target) : base(source, target)
        {
            //
        }
    }

    [Serializable]
    public class ThPDSProjectGraph
    {
        /// <summary>
        /// 回路图
        /// </summary>
        public AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> Graph { get; set; }
        /// <summary>
        /// 回路信息
        /// </summary>
        public List<PDSDWGLoopInfo> LoopInfo { get; set; }
        /// <summary>
        /// 负载信息
        /// </summary>
        public List<PDSDWGLoadInfo> LoadInfo { get; set; }
        /// <summary>
        /// 构造函数
        /// </summary>
        public ThPDSProjectGraph()
        {
            Graph = new AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>>();
            LoopInfo = new List<PDSDWGLoopInfo>();
            LoadInfo = new List<PDSDWGLoadInfo>();
        }
    }
}
