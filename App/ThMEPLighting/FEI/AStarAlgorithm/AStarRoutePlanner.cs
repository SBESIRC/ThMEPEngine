using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        CostGetter costGetter = new CostGetter();
        Map map;

        public AStarRoutePlanner(Polyline polyline, Vector3d dir)
        {
            map = new Map(polyline, dir, 1000);
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
        public Polyline Plan(Point3d start, Point3d destination)
        {
            //初始化起点终点
            map.SetStartAndEndPoint(start, destination);

            if ((!map.ContainsPt(map.startPt)) || (!map.ContainsPt(map.endPt)))
            {
                throw new Exception("StartPoint or Destination not in the current map!");
            }

            RoutePlanData routePlanData = new RoutePlanData(map, map.endPt);

            //设置起点
            AStarNode startNode = new AStarNode(map.startPt, null, 0, 0);
            routePlanData.OpenedList.Add(startNode);

            AStarNode currenNode = startNode;

            //从起始节点开始进行递归调用
            var resPt = DoPlan(routePlanData, currenNode);
            return map.CreatePath(resPt);
        }
        #endregion

        #region DoPlan
        private List<Point> DoPlan(RoutePlanData routePlanData, AStarNode currenNode)
        {
            List<CompassDirections> allCompassDirections = CompassDirectionsHelper.GetAllCompassDirections();
            foreach (CompassDirections direction in allCompassDirections)
            {
                Point nextCell = GeometryHelper.GetAdjacentPoint(currenNode.Location, direction);
                if (!routePlanData.CellMap.ContainsPt(nextCell)) //相邻点已经在地图之外
                {
                    continue;
                }

                if (routePlanData.CellMap.obstacles[nextCell.X][nextCell.Y]) //下一个Cell为障碍物
                {
                    continue;
                }

                AStarNode existNode = this.GetNodeOnLocation(nextCell, routePlanData);
                int costG = this.costGetter.GetCost(currenNode, direction);   //计算G值
                int costH = Math.Abs(nextCell.X - routePlanData.Destination.X) + Math.Abs(nextCell.Y - routePlanData.Destination.Y);    //计算H值
                if (costH == 0) //costH为0，表示相邻点就是目的点，规划完成，构造结果路径
                {
                    List<Point> route = new List<Point>();
                    route.Add(routePlanData.Destination);
                    route.Insert(0, currenNode.Location);
                    AStarNode tempNode = currenNode;
                    while (tempNode.ParentNode != null)
                    {
                        route.Insert(0, tempNode.ParentNode.Location);
                        tempNode = tempNode.ParentNode;
                    }

                    return route;
                }

                if (existNode != null)
                {
                    if (existNode.CostG > costG)
                    {
                        //如果新的路径代价更小，则更新该位置上的节点的原始路径
                        existNode.ResetParentNode(currenNode, costG);
                    }
                }
                else
                {
                    AStarNode newNode = new AStarNode(nextCell, currenNode, costG, costH);
                    routePlanData.OpenedList.Add(newNode);
                }
            }

            //将已遍历过的节点从开放列表转移到关闭列表
            routePlanData.OpenedList.Remove(currenNode);
            routePlanData.ClosedList.Add(currenNode);

            AStarNode minCostNode = this.GetMinCostNode(routePlanData.OpenedList);
            if (minCostNode == null) //表明从起点到终点之间没有任何通路。
            {
                return null;
            }

            //对开放列表中的下一个代价最小的节点作递归调用
            return this.DoPlan(routePlanData, minCostNode);
        }
        #endregion

        #region GetNodeOnLocation
        /// <summary>
        /// 目标位置location是否已存在于开放列表或关闭列表中
        /// </summary>       
        private AStarNode GetNodeOnLocation(Point location, RoutePlanData routePlanData)
        {
            foreach (AStarNode temp in routePlanData.OpenedList)
            {
                if (temp.Location.X == location.X && temp.Location.Y == location.Y)
                {
                    return temp;
                }
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
        private AStarNode GetMinCostNode(List<AStarNode> openedList)
        {
            if (openedList.Count == 0)
            {
                return null;
            }

            AStarNode target = openedList[0];
            foreach (AStarNode temp in openedList)
            {
                if (temp.CostF < target.CostF)
                {
                    target = temp;
                }
            }

            return target;
        }
        #endregion
    }
}
