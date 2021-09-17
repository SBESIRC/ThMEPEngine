using System.Collections.Generic;
using System.Linq;

namespace ThMEPEngineCore.Algorithm.GraphDomain
{
    public abstract class GraphBase
    {
        /// <summary>
        /// 图的所有节点（基础数据）
        /// </summary>
        protected List<IGraphNode> _allNodes { get; }
        /// <summary>
        /// 图节点的所有连接关系（基础数据）
        /// </summary>
        protected List<GraphNodeRelation> _allNodeRelations { get; }
        /// <summary>
        /// 图的构造函数
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="nodeRelations"></param>
        public GraphBase(IEnumerable<IGraphNode> nodes, List<GraphNodeRelation> nodeRelations)
        {
            _allNodes = new List<IGraphNode>();
            _allNodeRelations = new List<GraphNodeRelation>();
            if (null != nodes && nodes.Count() > 0)
            {
                foreach (var item in nodes)
                {
                    if (item == null || item.GraphNode == null)
                        continue;
                    _allNodes.Add(item);
                }
            }
            if (null != nodeRelations && nodeRelations.Count > 0)
            {
                foreach (var item in nodeRelations)
                {
                    if (item == null || item.StartNode == null || item.EndNode == null)
                        continue;
                    _allNodeRelations.Add(item);
                }
            }
        }
        public void AddGraphNodes(List<IGraphNode> nodes) 
        {
            if (null == nodes || nodes.Count < 1)
                return;
            foreach (var item in nodes) 
            {
                if (null == item || item.GraphNode == null)
                    continue;
                _allNodes.Add(item);
            }
        }
        public void AddRealtions(List<GraphNodeRelation> nodeRelations) 
        {
            if (null == nodeRelations || nodeRelations.Count < 1)
                return;
            foreach (var item in nodeRelations) 
            {
                if (null == item || item.StartNode == null || item.EndNode == null)
                    continue;
                _allNodeRelations.Add(item);
            }
        }
        /// <summary>
        /// 获取节点的关系
        /// （如果是有向，注意传入的起始节点的顺序）
        /// </summary>
        /// <param name="sNode"></param>
        /// <param name="eNode"></param>
        /// <param name="precision"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        protected GraphNodeRelation GetNodeToNodeRelation(IGraphNode sNode, IGraphNode eNode, object precision = null, object parameter = null)
        {
            if (null == sNode || null == eNode || _allNodeRelations == null || _allNodeRelations.Count < 1)
                return null;
            foreach (var item in _allNodeRelations)
            {
                if (item == null)
                    continue;
                if (item.StartNode.NodeIsEqual(sNode, precision, parameter) && item.EndNode.NodeIsEqual(eNode, precision, parameter))
                    return item;
                else if (item.EndNode.NodeIsEqual(sNode, precision, parameter) && !item.IsOneWay && item.StartNode.NodeIsEqual(eNode, precision, parameter))
                    return item;
            }
            return null;
        }
        /// <summary>
        /// 获取某个节点的所有连接关系
        /// </summary>
        /// <param name="node"></param>
        /// <param name="precision"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        protected List<GraphNodeRelation> GetNodeRelations(IGraphNode node, object precision = null, object parameter = null)
        {
            List<GraphNodeRelation> relations = new List<GraphNodeRelation>();
            if (null == node || _allNodeRelations == null || _allNodeRelations.Count < 1)
                return relations;
            foreach (var item in _allNodeRelations)
            {
                if (item == null || item.StartNode == null || item.EndNode == null)
                    continue;
                if (item.StartNode.NodeIsEqual(node, precision, parameter))
                    relations.Add(item);
                else if (item.EndNode.NodeIsEqual(node, precision, parameter) && !item.IsOneWay)
                    relations.Add(item);
            }
            return relations;
        }
        /// <summary>
        /// 获取某个节点和目标节点的连接关系
        /// </summary>
        /// <param name="node"></param>
        /// <param name="targetNodes"></param>
        /// <param name="precision"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        protected List<GraphNodeRelation> GetNodeRelations(IGraphNode node, List<IGraphNode> targetNodes, object precision = null, object parameter = null)
        {
            List<GraphNodeRelation> relations = new List<GraphNodeRelation>();
            if (null == node || _allNodeRelations == null || _allNodeRelations.Count < 1 || targetNodes == null || targetNodes.Count < 1)
                return relations;
            foreach (var item in _allNodeRelations)
            {
                if (item == null || item.StartNode == null || item.EndNode ==null)
                    continue;
                if (item.StartNode.NodeIsEqual(node, precision, parameter))
                {
                    if (targetNodes.Any(c => c.NodeIsEqual(item.EndNode, precision, parameter)))
                        relations.Add(item);
                }
                else if (item.EndNode.NodeIsEqual(node, precision, parameter) && !item.IsOneWay)
                {
                    if (targetNodes.Any(c => c.NodeIsEqual(item.StartNode, precision, parameter)))
                        relations.Add(item);
                }
            }
            return relations;
        }
    }
}
