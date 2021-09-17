using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Algorithm.GraphDomain
{
    public class GraphUtils
    {
        public static GraphRoute GraphNodeToRoute(List<IGraphNode> graphNodes) 
        {
            if (null == graphNodes || graphNodes.Count < 1)
                return null;
            var node = graphNodes.FirstOrDefault();
            graphNodes.Remove(node);
            GraphRoute nextRoute = null;
            if (graphNodes.Count > 0) 
                nextRoute = GraphNodeToRoute(graphNodes);
            double weightToNext = 0.0;
            if (nextRoute != null)
                weightToNext = node.NodeDistanceToNode(nextRoute.currentNode);
            GraphRoute graphRoute = new GraphRoute(node, weightToNext);
            graphRoute.nextRoute = nextRoute;
            return graphRoute;
        }
        public static GraphRoute GraphNodeToRoute(List<IGraphNode> graphNodes, List<GraphNodeRelation> graphNodeRelations, object precision, object parameter) 
        {
            if (null == graphNodes || graphNodes.Count < 1)
                return null;
            var node = graphNodes.First();
            graphNodes.Remove(node);
            GraphRoute nextRoute = null;
            if (graphNodes.Count > 0)
                nextRoute = GraphNodeToRoute(graphNodes,graphNodeRelations,precision,parameter);
            double weightToNext = 0.0;
            if (nextRoute != null)
            {
                var relation = GetNodeRealtion(node, nextRoute.currentNode, graphNodeRelations, precision, parameter);
                if(null == relation)
                    weightToNext = relation.Weight;
            }
            GraphRoute graphRoute = new GraphRoute(node, weightToNext);
            graphRoute.nextRoute = nextRoute;
            return graphRoute;
        }
        public static GraphNodeRelation GetNodeRealtion(IGraphNode startNode,IGraphNode endNode, List<GraphNodeRelation> targetRelations,object precision,object parameter) 
        {
            if (null == targetRelations || targetRelations.Count < 1)
                return null;
            if (null == startNode || endNode == null)
                return null;
            foreach (var item in targetRelations) 
            {
                if (item == null || item.StartNode == null || item.EndNode == null)
                    continue;
                if (item.StartNode.NodeIsEqual(startNode, precision, parameter) && item.EndNode.NodeIsEqual(endNode, precision, parameter))
                    return item;
                else if (item.EndNode.NodeIsEqual(startNode, precision, parameter) && !item.IsOneWay && item.StartNode.NodeIsEqual(endNode, precision, parameter))
                    return item;
            }
            return null;
        }

        /// <summary>
        /// 判断路径是否是以某个特定的开始节点，结束节点的路径
        /// </summary>
        /// <param name="sNode"></param>
        /// <param name="eNode"></param>
        /// <param name="route"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public static bool CheckRouteStartEndNode(IGraphNode startNode, IGraphNode endNode, GraphRoute route, object precision = null, object parameter = null)
        {
            if (null == startNode || null == endNode || null == route)
                return false;
            return CheckRouteStartEndNode(startNode, endNode, route, precision,parameter, true);
        }
        /// <summary>
        /// 判断图路径是否是以某个固定节点开始，固定节点结束
        /// </summary>
        /// <param name="sNode">开始节点</param>
        /// <param name="eNode">结束节点</param>
        /// <param name="route">路径</param>
        /// <param name="precision">精度（不同的节点方式不同）</param>
        /// <param name="isStart">判断当前是判断开始还是结束节点</param>
        /// <returns></returns>
        static bool CheckRouteStartEndNode(IGraphNode sNode, IGraphNode eNode, GraphRoute route, object precision, object parameter, bool isStart)
        {
            if (null == sNode || null == eNode || null == route)
                return false;
            if (isStart)
            {
                if (route.currentNode.NodeIsEqual(sNode, precision, parameter))
                    return CheckRouteStartEndNode(sNode, eNode, route.nextRoute, precision,parameter, false);
                return false;
            }
            if (null == route.nextRoute)
                return route.currentNode.NodeIsEqual(eNode, precision, parameter);
            return CheckRouteStartEndNode(sNode, eNode, route.nextRoute, precision, parameter, false);
        }
        /// <summary>
        /// 判断图的路径中是否包含某两个特定的节点，节点有先后的顺序
        /// </summary>
        /// <param name="firstNode"></param>
        /// <param name="secondNode"></param>
        /// <param name="route"></param>
        /// <param name="precision"></param>
        /// <param name="isFirst"></param>
        /// <returns></returns>
        static bool CheckFirstSecondNodeInRoute(IGraphNode firstNode, IGraphNode secondNode, GraphRoute route, object precision, object parameter, bool isFirst, bool secondIsEnd)
        {
            if (null == firstNode || null == secondNode || null == route)
                return false;
            if (isFirst)
            {
                if (route.currentNode.NodeIsEqual(firstNode, precision, parameter))
                    return CheckFirstSecondNodeInRoute(firstNode, secondNode, route.nextRoute, precision, parameter, false, secondIsEnd);
            }
            else
            {
                if (secondIsEnd)
                {
                    if (null == route.nextRoute)
                        return route.currentNode.NodeIsEqual(secondNode, precision, parameter);
                }
                else
                {
                    if (route.currentNode.NodeIsEqual(secondNode, precision, parameter))
                        return true;
                }
            }
            return CheckFirstSecondNodeInRoute(firstNode, secondNode, route.nextRoute, precision, parameter, isFirst, secondIsEnd);
        }
    }
}
