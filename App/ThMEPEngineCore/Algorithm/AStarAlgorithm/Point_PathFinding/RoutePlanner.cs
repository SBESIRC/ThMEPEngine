using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.Point_PathFinding.CostGetterService;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.Point_PathFinding.Model;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm.Point_PathFinding
{
    public class RoutePlanner
    {
        PointMap map;
        CotterGetterService costGetter = new CotterGetterService();
        public RoutePlanner(Polyline frame, Point3d endPt)
        {
            map = new PointMap(frame, endPt);
        }

        /// <summary>
        /// 设置障碍物
        /// </summary>
        public void SetObstacle(List<Polyline> holes)
        {
            //设置障碍物
            map.SetObstacle(holes);
        }

        public Polyline Plan(Point3d startPt)
        {
            RoutePlanMap routePlan = new RoutePlanMap(map);

            //设置起点
            NodeModel startNode = new NodeModel(startPt, null, 0, 0);
            routePlan.OpenedList.Enqueue(startNode);

            //从起始节点开始进行路径查找
            var lastNode = DoPlan(routePlan, startNode);

            //获取路径点位
            var resPt = GetPath(lastNode);

            var path = map.CreatePath(resPt);
            return path;
        }

        #region DoPlan
        /// <summary>
        /// 寻路
        /// </summary>
        /// <param name="routePlanData"></param>
        /// <param name="currenNode"></param>
        /// <returns></returns>
        private NodeModel DoPlan(RoutePlanMap routePlanData, NodeModel currenNode)
        {
            while (routePlanData.OpenedList.Count != 0)
            {
                //取得当前节点
                currenNode = this.GetMinCostNode(routePlanData.OpenedList);

                foreach (Point3d pt in routePlanData.CellMap.GetClostNode(currenNode.Location))
                {
                    NodeModel nextNode = this.GetNodeOnLocation(pt, routePlanData);
                    double costG = costGetter.GetGCost(currenNode, pt);   //计算G值
                    double costH = costGetter.GetHCost(pt, routePlanData.CellMap.endPoint);    //计算H值
                    if (nextNode != null)
                    {
                        if (nextNode.CostG > costG)
                        {
                            //如果新的路径代价更小，则更新该位置上的节点的原始路径
                            nextNode.ResetParentNode(currenNode, costG);
                        }
                    }
                    else
                    {
                        nextNode = new NodeModel(pt, currenNode, costG, costH);
                        routePlanData.OpenedList.Enqueue(nextNode);
                    }

                    if (costH == 0) //costH为0，表示相邻点就是目的点，规划完成，构造结果路径
                    {
                        return nextNode;
                    }
                }

                //将已遍历过的节点从开放列表转移到关闭列表
                routePlanData.OpenedList.Remove(currenNode);
                routePlanData.ClosedList.Add(currenNode);
            }

            return currenNode;
        }

        /// <summary>
        /// 获取路径点位
        /// </summary>
        /// <param name="routePlanData"></param>
        /// <param name="lastNode"></param>
        /// <returns></returns>
        private List<Point3d> GetPath(NodeModel lastNode)
        {
            List<Point3d> route = new List<Point3d>();
            //lastNode.Location.IsInflectionPoint = true;
            route.Insert(0, lastNode.Location);
            NodeModel tempNode = lastNode;
            while (tempNode.ParentNode != null)
            {
                route.Insert(0, tempNode.ParentNode.Location);
                tempNode = tempNode.ParentNode;
            }

            return route;
        }
        #endregion

        #region GetNodeOnLocation
        /// <summary>
        /// 目标位置location是否已存在于开放列表或关闭列表中
        /// </summary>       
        private NodeModel GetNodeOnLocation(Point3d location, RoutePlanMap routePlanMap)
        {
            var node = routePlanMap.OpenedList.Find(new NodeModel(location, null, 0, 0));
            if (node != null)
            {
                return node;
            }

            foreach (NodeModel temp in routePlanMap.ClosedList)
            {
                if (temp.Location.IsEqualTo(location))
                {
                    return temp;
                }
            }

            return null;
        }
        #endregion

        #region GetMinCostNode
        /// <summary>
        /// 从开放列表中获取代价F最小的节点，以启动下一次递归
        /// </summary>      
        private NodeModel GetMinCostNode(MinHeap<NodeModel> openedList)
        {
            if (openedList.Count == 0)
            {
                return null;
            }

            NodeModel target = openedList.Dequeue();
            return target;
        }
        #endregion
    }
}
