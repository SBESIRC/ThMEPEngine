using System;
using QuikGraph;

namespace TianHua.Electrical.PDS.Project.Module
{
    [Serializable]
    public class ThPDSProjectGraph
    {
        /// <summary>
        /// 回路图
        /// </summary>
        public readonly AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> Graph;
        /// <summary>
        /// 构造函数
        /// </summary>
        public ThPDSProjectGraph(AdjacencyGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge<ThPDSProjectGraphNode>> graph)
        {
            Graph = graph;
        }
    }
}
