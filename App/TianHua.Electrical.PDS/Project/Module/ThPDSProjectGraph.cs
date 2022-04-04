using System;
using QuikGraph;
using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Project.Module
{
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
