using System;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm.AStarAlgorithm_New.MapService;
using ThMEPEngineCore.Algorithm.AStarAlgorithm_New.CostGetterService;
using ThMEPEngineCore.Algorithm.AStarAlgorithm_New.Model;
using System.Linq;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm_New
{
    /*
     * 用障碍物的点位建网格，障碍物较少的情况下速度会更快 
     */
    /// <summary>
    /// AStarRoutePlanner A*路径规划。每个单元格Cell的位置用Point表示
    /// F = G + H 。
    /// G = 从起点A，沿着产生的路径，移动到网格上指定方格的移动耗费。
    /// H = 从网格上那个方格移动到终点B的预估移动耗费。使用曼哈顿方法，它计算从当前格到目的格之间水平和垂直的方格的数量总和，忽略对角线方向。
    /// </summary>
    public class AStarOptimizeRoutePlanner
    {
        Map map;
        ICostGetter costGetter = null;

        public AStarOptimizeRoutePlanner(Polyline polyline, Vector3d dir, double avoidFrameDistance = 200, double avoidHoleDistance = 800)
        {
            map = new Map(polyline, dir, avoidFrameDistance, avoidHoleDistance);
            costGetter = new ToLineCostGetter();
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
        public Polyline Plan(Point3d start, Point3d endPt)
        {
            //判断起点和终点是否在框线内
            if (!map.ContainsPt(start) || !map.ContainsPt(endPt))
            {
                return null;
            }

            //初始化起点终点信息
            map.SetStartAndEndInfo(start, endPt);

            RoutePlanData routePlanData = new RoutePlanData(map);

            //设置起点
            AStarNode startNode = new AStarNode(map.startPt, null, 0, 0);
            routePlanData.OpenedList.Add(startNode);

            AStarNode currenNode = startNode;

            //从起始节点开始进行路径查找
            var lastNode = DoPlan(routePlanData, currenNode);

            //获取路径点位
            var resNodes = GetPath(lastNode);

            //调整路径
            AdjustAStarPath adjustAStarPath = new AdjustAStarPath();
            var resPts = adjustAStarPath.AdjustPath(resNodes, routePlanData.CellMap.holes, routePlanData.CellMap.polyline);

            var path = map.CreatePath(resPts);
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
                if (currenNode == null)
                {
                    return currenNode;
                }

                List<Point3d> nextCells = GeometryHelper.GetAdjacentPoint(currenNode.Location, routePlanData.CellMap.cellLines);
                foreach (var nextCell in nextCells)
                {
                    AStarNode nextNode = this.GetNodeOnLocation(nextCell, routePlanData);
                    //var index = GetCoefficient(currenNode, nextCell);
                    double costG = costGetter.GetGCost(currenNode, nextCell);   //计算G值
                    double costH = costGetter.GetHCost(nextCell, routePlanData.CellMap.endPt);    //计算H值
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
                        routePlanData.OpenedList.Add(nextNode);
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
        private List<AStarNode> GetPath(AStarNode lastNode)
        {
            List<AStarNode> route = new List<AStarNode>();
            lastNode.IsInflectionPoint = true;
            route.Insert(0, lastNode);
            AStarNode tempNode = lastNode;
            while (tempNode.ParentNode != null)
            {
                //判断点是否是拐点
                tempNode.ParentNode.IsInflectionPoint = CheckInflectionPoint(tempNode, tempNode.ParentNode);

                route.Insert(0, tempNode.ParentNode);
                tempNode = tempNode.ParentNode;
            }

            return route;
        }
        #endregion

        #region GetNodeOnLocation
        /// <summary>
        /// 目标位置location是否已存在于开放列表或关闭列表中
        /// </summary>       
        private AStarNode GetNodeOnLocation(Point3d location, RoutePlanData routePlanData)
        {
            var node = routePlanData.OpenedList.FirstOrDefault(x => x.Location.IsEqualTo(location));
            if (node != null)
            {
                return node;
            }

            return routePlanData.ClosedList.FirstOrDefault(x => x.Location.IsEqualTo(location));
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

            AStarNode target = openedList.OrderBy(x => x.CostF).First();
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
            if (Math.Abs(nextNode.ParentNode.Location.X - currNode.Location.X) < 0.1 || Math.Abs(nextNode.ParentNode.Location.Y - currNode.Location.Y) < 0.1)
            {
                return false;
            }

            return true;
        }
        #endregion

        #region CallCoefficient
        private double GetCoefficient(AStarNode currentNode, Point3d nextCell)
        {
            if (currentNode == null || currentNode.ParentNode == null)
            {
                return 1;
            }

            var preDir = (currentNode.Location - currentNode.ParentNode.Location).GetNormal();
            var nextDir = (nextCell - currentNode.Location).GetNormal();
            if (preDir.IsEqualTo(nextDir, new Tolerance(0.01, 0.01)))
            {
                return 0.8;
            }

            return 1;
        }
        #endregion
    }
}
