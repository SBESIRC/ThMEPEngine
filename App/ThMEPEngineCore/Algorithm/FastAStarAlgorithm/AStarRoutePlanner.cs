using System;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm.FastAStarAlgorithm.CostGetterService;
using ThMEPEngineCore.Algorithm.FastAStarAlgorithm.AStarModel;
using ThMEPEngineCore.Algorithm.FastAStarAlgorithm.MapService;
using Dreambuild.AutoCAD;
using System.Linq;
using ThMEPEngineCore.FastAStarAlgorithm.FastAStarAlgorithm;

namespace ThMEPEngineCore.Algorithm.FastAStarAlgorithm
{
    /// <summary>
    /// AStarRoutePlanner A*路径规划。每个单元格Cell的位置用Point表示
    /// F = G + W * H 。
    /// G = 从起点A，沿着产生的路径，移动到网格上指定方格的移动耗费。
    /// H = 从网格上那个方格移动到终点B的预估移动耗费。使用曼哈顿方法，它计算从当前格到目的格之间水平和垂直的方格的数量总和，忽略对角线方向。
    /// W = 为 H 附加权重，使A*寻路更快且配合剪枝操作，优化空间，提高速率
    /// 增加了“沼泽”的概念，障碍物分化为必躲的障碍Holes和尽量躲的沼泽Swaps
    /// 可以认为这是一个兼容了正常A*和带躲避A*的一个兼容且Fast的版本
    /// 优化的核心是通过跳点和剪枝极大的减少OpenList的列表长度，以减少算法的计算时间
    /// 但是这样可能也会导致一个问题，在某些case上可能会导致算法无法找到最短路径，But，找到的路径看起来仍是“没问题的”
    /// </summary>
    public class AStarRoutePlanner<T>
    {
        Map<T> map;
        public ICostGetter costGetter { set; get; }
        public AdjustAStarPath PathAdjuster { set; get; }
        List<CompassDirections> allCompassDirections = CompassDirectionsHelper.GetAllCompassDirections();

        public AStarRoutePlanner(Polyline polyline, Vector3d dir, T end, double step = 400, double avoidFrameDistance = 200, double avoidHoleDistance = 800)
        {
            map = new Map<T>(polyline, dir, end, step, avoidFrameDistance, avoidHoleDistance);

            if (typeof(T) == typeof(Line))
            {
                costGetter = new ToLineCostGetter();
            }
            else if (typeof(T) == typeof(Point3d))
            {
                costGetter = new ToPointCostGetter();
            }
            PathAdjuster = new AdjustAStarPath();
        }

        /// <summary>
        /// 设置沼泽
        /// </summary>
        public void SetSwamp(List<Polyline> swaps)
        {
            //设置沼泽
            map.SetSwap(swaps);
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
                return null;
            }
            if(costGetter is ToLineCostGetterEx cGetter)
            {
                cGetter.RoomCast = map.roomCast;
                costGetter = cGetter;
            }
            RoutePlanData<T> routePlanData = new RoutePlanData<T>(map);

            //设置起点
            int costH = costGetter.GetHCost(map.startPt, routePlanData.CellMap.mapHelper.endEntity);    //计算H值
            AStarNode startNode = new AStarNode(map.startPt, null, CompassDirections.NotSet, 0, costH);
            routePlanData.OpenedList.Enqueue(startNode);

            AStarNode currenNode = startNode;

            //从起始节点开始进行路径查找
            var lastNode = DoPlan(routePlanData, currenNode);

            //获取路径点位
            var resPt = GetPath(lastNode);

            //int index = 0;
            //var pl = new Polyline();
            //pl.ColorIndex = 6;
            //foreach (var pt in resPt)
            //{
            //    if (pt.IsInflectionPoint)
            //    {
            //        var cellpt = map.mapHelper.TransformMapPoint(pt);
            //        pl.AddVertexAt(index,cellpt.ToPoint2d(),0,0,0);
            //    }
            //}
            //Draw.AddToCurrentSpace(pl);
            //调整路径
            //AdjustAStarPath adjustAStarPath = new AdjustAStarPath();
            resPt = PathAdjuster.AdjustPath<T>(resPt, routePlanData.CellMap);

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
        private AStarNode DoPlan(RoutePlanData<T> routePlanData, AStarNode currenNode)
        {
            while (routePlanData.OpenedList.Count != 0)
            {
                //取得当前节点
                currenNode = this.GetMinCostNode(routePlanData.OpenedList);
                
                foreach (CompassDirections direction in allCompassDirections)
                {
                    Point nextCell = GeometryHelper.GetAdjacentPoint(currenNode.Location, direction);
                    if (!routePlanData.CellMap.IsInBounds(nextCell)) //相邻点已经在地图之外
                    {
                        continue;
                    }
                    if (routePlanData.CellMap.IsObstacle(nextCell))
                        continue;
                    AStarNode nextNode = this.GetNodeOnLocation(nextCell, routePlanData);
                    int costG = costGetter.GetGCost(currenNode, direction);   //计算G值
                    if (routePlanData.CellMap.IsSwap(nextCell))
                        costG += 100;
                    int costH = costGetter.GetHCost(nextCell, routePlanData.CellMap.mapHelper.endEntity);    //计算H值
                    if (nextNode != null)
                    {
                        if (nextNode.CostG > costG)
                        {
                            //如果新的路径代价更小，则更新该位置上的节点的原始路径
                            nextNode.ResetParentNode(currenNode, costG, direction);
                        }
                    }
                    else
                    {
                        nextNode = new AStarNode(nextCell, currenNode, direction, costG, costH);
                        if (currenNode.CostG + currenNode.CostH == costG + costH)
                        {
                            nextNode = PruneNode(routePlanData, nextNode,5);
                        }
                        routePlanData.OpenedList.Enqueue(nextNode);
                    }
                    if (nextNode.CostH == 0) //costH为0，表示相邻点就是目的点，规划完成，构造结果路径
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
        /// 剪枝与跳点
        /// </summary>
        /// <param name="routePlanData"></param>
        /// <param name="currenNode"></param>
        /// <param name="tentacleStepSize"></param>
        /// <returns></returns>
        private AStarNode PruneNode(RoutePlanData<T> routePlanData, AStarNode node, int tentacleStepSize)
        {
            int pathCount = 0;
            bool prune = true;
            var parentdirection = node.Direction;
            AStarNode currenNode = node;
            while (prune)
            {
                Point nextCell = GeometryHelper.GetAdjacentPoint(currenNode.Location, parentdirection);
                if (!routePlanData.CellMap.IsInBounds(nextCell)) //相邻点已经在地图之外
                {
                    break;
                }
                if (routePlanData.CellMap.IsObstacle(nextCell))
                {
                    break;
                }
                AStarNode nextNode = this.GetNodeOnLocation(nextCell, routePlanData);
                int costG = costGetter.GetGCost(currenNode, parentdirection);   //计算G值
                int costH = costGetter.GetHCost(nextCell, routePlanData.CellMap.mapHelper.endEntity);    //计算H值
                if (routePlanData.CellMap.IsSwap(nextCell))
                    costG += 100;
                if (nextNode != null)
                {
                    if (nextNode.CostG > costG)
                    {
                        //如果新的路径代价更小，则更新该位置上的节点的原始路径
                        nextNode.ResetParentNode(currenNode, costG, parentdirection);
                    }
                }
                else
                {
                    nextNode = new AStarNode(nextCell, currenNode, parentdirection, costG, costH);
                }
                if (currenNode.CostG + currenNode.CostH == costG + costH)
                {
                    routePlanData.OpenedList.Remove(currenNode);
                    routePlanData.ClosedList.Add(currenNode);
                    currenNode = nextNode;
                    pathCount++;
                }
                else
                {
                    prune = false;
                }
            }
            if (pathCount > tentacleStepSize)
            {
                GenerateTentacles(routePlanData, node, tentacleStepSize);
            }
            return currenNode;
        }

        /// <summary>
        /// 生成触手
        /// </summary>
        /// <param name="aStarNode"></param>
        /// <param name="node"></param>
        /// <param name="v"></param>
        /// <param name="tentacleStepSize"></param>
        private void GenerateTentacles(RoutePlanData<T> routePlanData, AStarNode node, int tentacleStepSize)
        {
            var tentacleNode = node;
            for (int i = 0; i < tentacleStepSize; i++)
            {
                tentacleNode = tentacleNode.ParentNode;
            }
            routePlanData.OpenedList.Remove(tentacleNode);
            routePlanData.OpenedList.Enqueue(tentacleNode);
            routePlanData.ClosedList.Remove(tentacleNode);
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
        private AStarNode GetNodeOnLocation(Point location, RoutePlanData<T> routePlanData)
        {
            var node = routePlanData.OpenedList.Find(new AStarNode(location, null, CompassDirections.NotSet, 0, 0));
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
