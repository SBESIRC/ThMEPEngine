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
        public BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> Graph;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ThPDSProjectGraph(BidirectionalGraph<ThPDSProjectGraphNode, ThPDSProjectGraphEdge> graph)
        {
            Graph = graph;
        }
    }
}
