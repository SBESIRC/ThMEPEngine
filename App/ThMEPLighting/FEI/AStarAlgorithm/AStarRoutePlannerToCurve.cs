using System;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.FEI.AStarAlgorithm
{
    public class AStarRoutePlannerToCurve
    {
        CostGetter costGetter = new CostGetter();
        Map map;
        List<CompassDirections> allCompassDirections = CompassDirectionsHelper.GetAllCompassDirections();

        public AStarRoutePlannerToCurve(Polyline polyline, Vector3d dir, double step = 400, double avoidDistance = 800)
        {
            map = new Map(polyline, dir, step, avoidDistance);
        }

        /// <summary>
        /// 设置障碍物
        /// </summary>
        public void SetObstacle(List<Polyline> holes)
        {
            //设置障碍物
            map.SetObstacle(holes);
        }

        #region Plan
        //public Polyline Plan(Point3d start, Line endLine)
        //{
        //    //初始化起点终点
        //    map.SetStartAndEndPoint(start, destination);

        //    if ((!map.ContainsPt(map.startPt)) || (!map.ContainsPt(map.endPt)))
        //    {
        //        throw new Exception("StartPoint or Destination not in the current map!");
        //    }

        //    RoutePlanData routePlanData = new RoutePlanData(map, map.endPt);

        //    //设置起点
        //    AStarNode startNode = new AStarNode(map.startPt, null, 0, 0);
        //    routePlanData.OpenedList.Enqueue(startNode);

        //    AStarNode currenNode = startNode;

        //    //从起始节点开始进行路径查找
        //    var lastNode = DoPlan(routePlanData, currenNode);

        //    //获取路径点位
        //    var resPt = GetPath(routePlanData, lastNode);

        //    //调整路径
        //    AdjustAStarPath adjustAStarPath = new AdjustAStarPath();
        //    resPt = adjustAStarPath.AdjustPath(resPt, routePlanData);

        //    var path = map.CreatePath(resPt);
        //    return path;
        //}
        #endregion

        #region DoPlan
        /// <summary>
        /// 寻路
        /// </summary>
        /// <param name="routePlanData"></param>
        /// <param name="currenNode"></param>
        /// <returns></returns>
        private AStarNode DoPlan(RoutePlanData routePlanData, AStarNode currenNode)
        {
            while (routePlanData.OpenedList.Count != 0)
            {
                //取得当前节点
                currenNode = this.GetMinCostNode(routePlanData.OpenedList);

                foreach (CompassDirections direction in allCompassDirections)
                {
                    Point nextCell = GeometryHelper.GetAdjacentPoint(currenNode.Location, direction);
                    if (!routePlanData.CellMap.ContainsPt(nextCell)) //相邻点已经在地图之外
                    {
                        continue;
                    }

                    if (routePlanData.CellMap.obstacles[nextCell.X][nextCell.Y]) //下一个Cell为障碍物
                    {
                        continue;
                    }

                    AStarNode nextNode = this.GetNodeOnLocation(nextCell, routePlanData);
                    int costG = this.costGetter.GetCost(currenNode, direction);   //计算G值
                    int costH = (Math.Abs(nextCell.X - routePlanData.Destination.X) + Math.Abs(nextCell.Y - routePlanData.Destination.Y)) * 10;    //计算H值
                    if (costH == 0) //costH为0，表示相邻点就是目的点，规划完成，构造结果路径
                    {
                        return currenNode;
                    }

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
                        nextNode = new AStarNode(nextCell, currenNode, costG, costH);
                        routePlanData.OpenedList.Enqueue(nextNode);
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
        private List<Point> GetPath(RoutePlanData routePlanData, AStarNode lastNode)
        {
            List<Point> route = new List<Point>();
            routePlanData.Destination.IsInflectionPoint = true;
            route.Add(routePlanData.Destination);
            route.Insert(0, lastNode.Location);
            AStarNode tempNode = lastNode;
            while (tempNode.ParentNode != null)
            {
                //判断点是否是拐点
                tempNode.ParentNode.Location.IsInflectionPoint = CheckInflectionPoint(tempNode, tempNode.ParentNode);

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
        private AStarNode GetNodeOnLocation(Point location, RoutePlanData routePlanData)
        {
            var node = routePlanData.OpenedList.Find(new AStarNode(location, null, 0, 0));
            if (node != null)
            {
                return node;
            }

            foreach (AStarNode temp in routePlanData.ClosedList)
            {
                if (temp.Location.X == location.X && temp.Location.Y == location.Y)
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
        private AStarNode GetMinCostNode(MinHeap<AStarNode> openedList)
        {
            if (openedList.Count == 0)
            {
                return null;
            }

            AStarNode target = openedList.Dequeue();
            return target;
        }
        #endregion

        #region IsInflectionPoint
        private bool CheckInflectionPoint(AStarNode currNode, AStarNode nextNode)
        {
            if (nextNode.ParentNode == null)
            {
                return true;
            }

            // 第一个点或直线点
            if (nextNode.ParentNode.Location.X == currNode.Location.X || nextNode.ParentNode.Location.Y == currNode.Location.Y)
            {
                return false;
            }

            return true;
        }
        #endregion
    }
}
