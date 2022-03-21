using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.AStarModel;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.CostGetterService;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.MapService;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm.GlobleAStarAlgorithm
{
    public class GlobleAStarRoutePlanner<T>
    {
        double _inflectionWeight = 0;       //拐点权重
        GlobleMap<T> map;
        public ICostGetter costGetter { set; get; }
        public GlobleAdjustAStarPath PathAdjuster { set; get; }
        List<CompassDirections> allCompassDirections = CompassDirectionsHelper.GetAllCompassDirections();

        public GlobleAStarRoutePlanner(Polyline polyline, Vector3d dir, T end, double step = 400, double avoidFrameDistance = 200, double avoidHoleDistance = 800, double inflectionWeight = 1.5)
        {
            _inflectionWeight = inflectionWeight;
            map = new GlobleMap<T>(polyline, dir, end, step, avoidFrameDistance, avoidHoleDistance);

            if (typeof(T) == typeof(Line))
            {
                costGetter = new ToLineCostGetter();
            }
            else if (typeof(T) == typeof(Point3d))
            {
                costGetter = new ToPointCostGetter();
            }
            PathAdjuster = new GlobleAdjustAStarPath();
        }

        /// <summary>
        /// 设置障碍物
        /// </summary>
        public void SetObstacle(List<Polyline> holes, double wieght)
        {
            //设置障碍物
            map.SetObstacle(holes, wieght);
        }

        #region Plan
        public Polyline Plan(Point3d start)
        {
            //初始化起点终点信息
            map.SetStartAndEndInfo(start);

            if (!map.IsInBounds(map.startPt))
            {
                return null;
            }

            GlobleRoutePlanData<T> routePlanData = new GlobleRoutePlanData<T>(map);

            //设置起点
            GlobleNode startNode = new GlobleNode(map.startPt, null, 0, 0);
            routePlanData.OpenedList.Add(startNode);

            GlobleNode currenNode = startNode;
            //从起始节点开始进行路径查找
            var lastNode = DoPlan(routePlanData, currenNode);

            //获取路径点位
            var resPt = GetPath(lastNode);

            //调整路径
            //resPt = PathAdjuster.AdjustPath<T>(resPt, routePlanData.CellMap);

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
        private GlobleNode DoPlan(GlobleRoutePlanData<T> routePlanData, GlobleNode currenNode)
        {
            var insertX = map.mapHelper.insertPts.Select(x => x.X).Distinct().ToDictionary(x => x, y => (int)Math.Floor(y)).Where(x => x.Key != x.Value).ToDictionary(x => x.Key, y => y.Value);
            var insertY = map.mapHelper.insertPts.Select(x => x.Y).Distinct().ToDictionary(x => x, y => (int)Math.Floor(y)).Where(x => x.Key != x.Value).ToDictionary(x => x.Key, y => y.Value);
            while (routePlanData.OpenedList.Count != 0)
            {
                //取得当前节点
                currenNode = this.GetMinCostNode(routePlanData.OpenedList);

                foreach (CompassDirections direction in allCompassDirections)
                {
                    GloblePoint nextCell = GeometryHelper.GetAdjacentPoint(currenNode.Location, direction, insertX, insertY);
                    if (!routePlanData.CellMap.IsInBounds(nextCell)) //相邻点已经在地图之外
                        continue;

                    var weight = routePlanData.CellMap.GetObstacleWeight(nextCell);
                    if (weight == Double.MaxValue)
                        continue;

                    GlobleNode nextNode = this.GetNodeOnLocation(nextCell, routePlanData);
                    double costG = costGetter.GetGCost(currenNode, nextCell) + weight;   //计算G值(加上权重)
                    if (currenNode.Directions != CompassDirections.NotSet && currenNode.Directions != direction)
                    {
                        costG = costG + _inflectionWeight;
                    }
                    double costH = costGetter.GetHCost(nextCell, routePlanData.CellMap.mapHelper.endEntity);    //计算H值
                    if (nextNode != null)
                    {
                        if (nextNode.CostG >= costG)
                        {
                            var nodeCostG = nextNode.CostG;
                            //如果新的路径代价更小，则更新该位置上的节点的原始路径
                            nextNode.ResetParentNode(currenNode, costG, direction);
                        }
                    }
                    else
                    {
                        nextNode = new GlobleNode(nextCell, currenNode, costG, costH);
                        nextNode.Directions = direction;
                        routePlanData.OpenedList.Add(nextNode);
                    }

                    if (Math.Abs(costH) < 0.1) //costH为0，表示相邻点就是目的点，规划完成，构造结果路径(给一点点误差)
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
        private List<GloblePoint> GetPath(GlobleNode lastNode)
        {
            List<GloblePoint> route = new List<GloblePoint>();
            if (lastNode.CostH > 0.1)
            {
                return route;
            }
            lastNode.Location.IsInflectionPoint = true;
            route.Insert(0, lastNode.Location);
            GlobleNode tempNode = lastNode;
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
        private GlobleNode GetNodeOnLocation(GloblePoint location, GlobleRoutePlanData<T> routePlanData)
        {
            var node = routePlanData.OpenedList.Find(x=>x.Location.X == location.X && x.Location.Y == location.Y);
            if (node != null)
            {
                return node;
            }

            foreach (GlobleNode temp in routePlanData.ClosedList)
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
        private GlobleNode GetMinCostNode(List<GlobleNode> openedList)
        {
            if (openedList.Count == 0)
            {
                return null;
            }

            GlobleNode target = openedList.OrderBy(x => x.CostF).First();// openedList.Dequeue();
            return target;
        }
        #endregion

        #region IsInflectionPoint
        private bool CheckInflectionPoint(GlobleNode currNode, GlobleNode nextNode)
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
