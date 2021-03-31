﻿using System;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.FEI.AStarAlgorithm.MapService;
using ThMEPLighting.FEI.AStarAlgorithm.AStarModel;
using ThMEPLighting.FEI.AStarAlgorithm.CostGetterService;

namespace ThMEPLighting.FEI.AStarAlgorithm
{
    /// <summary>
    /// AStarRoutePlanner A*路径规划。每个单元格Cell的位置用Point表示
    /// F = G + H 。
    /// G = 从起点A，沿着产生的路径，移动到网格上指定方格的移动耗费。
    /// H = 从网格上那个方格移动到终点B的预估移动耗费。使用曼哈顿方法，它计算从当前格到目的格之间水平和垂直的方格的数量总和，忽略对角线方向。
    /// </summary>
    public class AStarRoutePlanner
    {
        ICostGetter costGetter;
        Map map;
        List<CompassDirections> allCompassDirections = CompassDirectionsHelper.GetAllCompassDirections();

        public AStarRoutePlanner(Polyline polyline, Vector3d dir, EndModel endInfo, double step = 400, double avoidFrameDistance = 200, double avoidHoleDistance = 800)
        {
            map = new Map(polyline, dir, endInfo, step, avoidFrameDistance, avoidHoleDistance);

            if (endInfo.type == EndInfoType.line)
            {
                costGetter = new ToLineCostGetter();
            }
            else if (endInfo.type == EndInfoType.point)
            {
                costGetter = new ToPointCostGetter();
            }
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
        public Polyline Plan(Point3d start)
        {
            //初始化起点终点信息
            map.SetStartAndEndInfo(start);

            if (!map.ContainsPt(map.startPt))
            {
                throw new Exception("StartPoint or Destination not in the current map!");
            }

            RoutePlanData routePlanData = new RoutePlanData(map);

            //设置起点
            AStarNode startNode = new AStarNode(map.startPt, null, 0, 0);
            routePlanData.OpenedList.Enqueue(startNode);

            AStarNode currenNode = startNode;

            //从起始节点开始进行路径查找
            var lastNode = DoPlan(routePlanData, currenNode);

            //获取路径点位
            var resPt = GetPath(lastNode);

            //调整路径
            AdjustAStarPath adjustAStarPath = new AdjustAStarPath();
            resPt = adjustAStarPath.AdjustPath(resPt, routePlanData);

            var path = map.CreatePath(resPt);
            return path;
        }
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
                    int costG = this.costGetter.GetGCost(currenNode, direction);   //计算G值
                    int costH = this.costGetter.GetHCost(nextCell, routePlanData.CellMap.endInfo);    //计算H值
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
        private List<Point> GetPath(AStarNode lastNode)
        {
            List<Point> route = new List<Point>();
            lastNode.Location.IsInflectionPoint = true;
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
