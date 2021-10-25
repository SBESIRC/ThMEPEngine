using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPLighting.DSFEI.ThEmgPilotLamp
{
    class GraphUtils
    {
        /// <summary>
        /// 根据路径，获取从起点到终点的距离
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        public static double GetRouteDisToEnd(GraphRoute route)
        {
            double dis = 0;
            if (null == route)
                return dis;
            if (null != route && route.nextRoute != null)
                dis += route.node.nodePoint.DistanceTo(route.nextRoute.node.nodePoint);
            dis += GetRouteDisToEnd(route.nextRoute);
            return dis;
        }

        public static GraphRoute GraphRouteFromNode(GraphRoute graphRoute, GraphNode node)
        {
            if (null == graphRoute || null == node)
                return null;
            var tempRoute = graphRoute;
            while (tempRoute != null)
            {
                if (tempRoute.node.nodePoint.IsEqualTo(node.nodePoint, new Tolerance(1, 1)))
                    return tempRoute;
                tempRoute = tempRoute.nextRoute;
            }
            return null;
        }
        /// <summary>
        /// 获取路径中的终点节点
        /// </summary>
        /// <param name="graphRoute"></param>
        /// <returns></returns>
        public static GraphNode GraphRouteEndNode(GraphRoute graphRoute)
        {
            if (graphRoute == null)
                return null;
            if (graphRoute.nextRoute != null)
                return GraphRouteEndNode(graphRoute.nextRoute);
            return graphRoute.node;
        }
        /// <summary>
        /// 获取经过一个节点的所有路径
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static List<GraphRoute> GetGraphNodeRoutes(List<GraphRoute> targetRoutes,GraphNode node, bool isStart)
        {
            List<GraphRoute> resRoute = new List<GraphRoute>();
            foreach (var route in targetRoutes)
            {
                if (isStart && !route.node.nodePoint.IsEqualTo(node.nodePoint, new Tolerance(1, 1)))
                    continue;
                else
                {
                    if (!NodeInRoute(route, node))
                        continue;
                }
                resRoute.Add(route);
            }
            return resRoute;
        }
        /// <summary>
        /// 判断节点是否在路径中
        /// </summary>
        /// <param name="route"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool NodeInRoute(GraphRoute route, GraphNode node)
        {
            if (route == null)
                return false;
            if (route.node.nodePoint.DistanceTo(node.nodePoint) < 5)
                return true;
            return NodeInRoute(route.nextRoute, node);
        }
        /// <summary>
        /// 判断路径是否经过某些线段
        /// </summary>
        /// <param name="route"></param>
        /// <param name="targetLines"></param>
        /// <returns></returns>
        public static bool RouteThroughLines(GraphRoute route, List<Line> targetLines)
        {
            if (null == route || targetLines == null || targetLines.Count < 1)
                return false;
            Point3d point = route.node.nodePoint;
            var tempRoute = route.nextRoute;
            while (tempRoute != null)
            {
                Point3d nextPt = tempRoute.node.nodePoint;
                Line line = new Line(point, nextPt);
                if (targetLines.Any(c => c.IsCollinear(line)))
                    return true;
                tempRoute = tempRoute.nextRoute;
            }
            return false;
        }
    }
}
