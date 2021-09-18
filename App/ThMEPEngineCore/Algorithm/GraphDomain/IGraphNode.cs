using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Algorithm.GraphDomain
{
    /// <summary>
    /// 图的节点基本信息
    /// </summary>
    public interface IGraphNode
    {
        /// <summary>
        /// 节点
        /// </summary>
        object GraphNode { get; }
        /// <summary>
        /// 两个节点是否相等
        /// </summary>
        /// <param name="node"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        bool NodeIsEqual(IGraphNode node, object precision, object parameter);
        /// <summary>
        /// 不同的类型数据计算中心点的方式不同
        /// （这种定义在这里不合适,调用也不方便，但又需要各自的类型去实现）
        /// </summary>
        /// <param name="graphNodes"></param>
        IGraphNode CenterGraphNode(List<IGraphNode> graphNodes);
        /// <summary>
        /// 两个节点间的距离
        /// （有些时候需要用两个节点间的权重，有些时候需要两个点间的距离，用还是不用，看具体的算法）
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        double NodeDistanceToNode(IGraphNode node);
        /// <summary>
        /// 节点权重（用还是不用，看具体的算法）
        /// </summary>
        double NodeWeight { get; }
        /// <summary>
        /// 是否是结束点（用还是不用，看具体的算法）
        /// </summary>
        bool IsEnd { get; set;}
        /// <summary>
        /// 标识数据（扩展用）
        /// </summary>
        object Tag { get; set; }
        /// <summary>
        /// 节点类型（扩展用）
        /// </summary>
        object NodeType { get; set; }
    }
}
