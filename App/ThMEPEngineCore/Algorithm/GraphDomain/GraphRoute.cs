using System;

namespace ThMEPEngineCore.Algorithm.GraphDomain
{
    /// <summary>
    /// 图的节点到节点的路线
    /// </summary>
    public class GraphRoute : ICloneable
    {
        /// <summary>
        /// 当前节点
        /// </summary>
        public IGraphNode currentNode { get; }
        /// <summary>
        /// 到下一个节点的权重
        /// </summary>
        public double weightToNext { get; set; }
        /// <summary>
        /// 当前节点到起点的权重
        /// </summary>
        public double weightToStart { get; set; }
        /// <summary>
        /// 下一个节点
        /// </summary>
        public GraphRoute nextRoute { get; set; }
        public GraphRoute(IGraphNode node, double weightToNext)
        {
            this.currentNode = node;
            this.weightToNext = weightToNext;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
