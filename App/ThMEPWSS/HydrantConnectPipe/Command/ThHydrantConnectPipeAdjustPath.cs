using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.OriginAStarModel;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.MapService;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.PublicMethod;

namespace ThMEPWSS.HydrantConnectPipe.Command
{
    public class ThHydrantConnectPipeAdjustPath : AdjustAStarPath
    {
        override public List<Point> AdjustPath<T>(List<Point> path, Map<T> map)
        {
            if (path == null || path.Count <= 1)
            {
                return path;
            }
            var inflectionPoints = path.Where(x => x.IsInflectionPoint).ToList();

            return ReduceInflectionPoint<T>(inflectionPoints, map);
        }

        /// <summary>
        /// 减少拐点
        /// </summary>
        /// <param name="inflectionPoints"></param>
        /// <param name="routePlanData"></param>
        /// <returns></returns>
        override protected List<Point> ReduceInflectionPoint<T>(List<Point> inflectionPoints, Map<T> map)
        {
            List<Point> path = new List<Point>();
            if(inflectionPoints.Count == 3)
            {
                inflectionPoints.Reverse();
                return inflectionPoints;
            }
            
            while(inflectionPoints.Count >= 3)
            {
                var firPt = inflectionPoints.First();
                inflectionPoints.Remove(firPt);
                path.Add(firPt);
                var midPt = inflectionPoints.First();
                inflectionPoints.Remove(midPt);
                var lasPt = inflectionPoints.First();
                bool isCollinear = IsPointCollinear(firPt, midPt, lasPt);
                if (isCollinear)//如果三点同线
                {
                    inflectionPoints.Insert(0, firPt);
                    continue;
                }
                else
                {
                    //检测firPt-midPt 是否穿越墙
                    var isCrossWall = CheckPointWall(firPt, midPt,map);
                    if(isCrossWall)
                    {
                        inflectionPoints.Insert(0, midPt);
                    }
                    else
                    {
                        //计算出midpt的对角点diapt
                        var diaPt = GetMidDiaPoint(firPt, midPt, lasPt);
                        //检测firpt-diapt和diapt-laspt构成的两条线，是否穿越洞或者墙
                        var isCrossHoleWall1 = CheckPointHoleOrWall(firPt, diaPt,map);
                        var isCrossHoleWall2 = CheckPointHoleOrWall(diaPt, lasPt,map);
                        //如果不穿墙且不穿洞 那么  inflectionPoints.Insert(0, diapt);
                        if (isCrossHoleWall1 || isCrossHoleWall2)
                        {
                            inflectionPoints.Insert(0, midPt);
                        }
                        else
                        {
                            inflectionPoints.Insert(0, diaPt);
                        }
                    }
                }
            }
            
            foreach (var pt in inflectionPoints)
            {
                path.Add(pt);
            }
            path = path.Distinct().ToList();

            var tmpPath = new List<Point>();
            //去除拐点
            while (path.Count >= 3)
            {
                var firPt = path.First();
                path.Remove(firPt);
                tmpPath.Add(firPt);
                var midPt = path.First();
                path.Remove(midPt);
                var lasPt = path.First();
                if (IsPointCollinear(firPt, midPt, lasPt))
                {
                    path.Insert(0, firPt);
                    continue;
                }
                else
                {
                    path.Insert(0, midPt);
                }
            }
            foreach (var pt in path)
            {
                tmpPath.Add(pt);
            }
            tmpPath = tmpPath.Distinct().ToList();
            tmpPath.Reverse();
            return tmpPath;
        }

        /// <summary>
        /// 判断三点是否同线
        /// </summary>
        /// <param name="firPt"></param>
        /// <param name="midPt"></param>
        /// <param name="lasPt"></param>
        /// <returns></returns>
        public bool IsPointCollinear(Point firPt,Point midPt,Point lasPt)
        {
            if((firPt.X == midPt.X &&firPt.X == lasPt.X)
                || (firPt.Y == midPt.Y && firPt.Y == lasPt.Y))
            {
                return true;
            }

            return false;
        }
        /// <summary>
        /// 获取中点的对角点
        /// </summary>
        /// <param name="firPt"></param>
        /// <param name="midPt"></param>
        /// <param name="lasPt"></param>
        /// <returns></returns>
        public Point GetMidDiaPoint(Point firPt, Point midPt, Point lasPt)
        {
            int xValue = (midPt.X == firPt.X) ? lasPt.X : firPt.X;
            int yValue = (midPt.Y == firPt.Y) ? lasPt.Y : firPt.Y;
            Point diaPt = new Point(xValue, yValue);
            return diaPt;
        }
        /// <summary>
        /// 检测pt1与pt2构成的线段，是否穿越洞或者墙
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns></returns>
        public bool CheckPointHole<T>(Point pt1, Point pt2, Map<T> map)
        {
            if(pt1.X == pt2.X)
            {
                int xValue = pt1.X;
                int yDir = pt1.Y - pt2.Y < 0 ? 1 : -1;
                int yLength = Math.Abs(pt1.Y - pt2.Y);
                for (int i = 1; i <= yLength; i++)
                {
                    var pt = new Point(xValue, pt1.Y + yDir * i);
                    if (map.IsObstacle(pt))
                    {
                        return true;
                    }
                }
            }
            else if(pt1.Y == pt2.Y)
            {
                int yValue = pt1.Y;
                int xDir = pt1.X - pt2.X < 0 ? 1 : -1;
                int xLength = Math.Abs(pt1.X - pt2.X);
                for (int i = 1; i <= xLength; i++)
                {
                    var pt = new Point(pt1.X + xDir * i, yValue);
                    if (map.IsObstacle(pt))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool CheckPointWall<T>(Point pt1, Point pt2, Map<T> map)
        {
            if(map.IsRoomWell(pt1,pt2))
            {
                return true;
            }
            //if (pt1.X == pt2.X)
            //{
            //    int xValue = pt1.X;
            //    int yDir = pt1.Y - pt2.Y < 0 ? 1 : -1;
            //    int yLength = Math.Abs(pt1.Y - pt2.Y);
            //    for (int i = 1; i <= yLength; i++)
            //    {
            //        var pt = new Point(xValue, pt1.Y + yDir * i);
            //        if (map.IsRoomWell(pt))
            //        {
            //            return false;
            //        }
            //    }
            //}
            //else if (pt1.Y == pt2.Y)
            //{
            //    int yValue = pt1.Y;
            //    int xDir = pt1.X - pt2.X < 0 ? 1 : -1;
            //    int xLength = Math.Abs(pt1.X - pt2.X);
            //    for (int i = 1; i <= xLength; i++)
            //    {
            //        var pt = new Point(pt1.X + xDir * i, yValue);
            //        if (map.IsRoomWell(pt))
            //        {
            //            return false;
            //        }
            //    }
            //}
            return false;
        }
        public bool CheckPointHoleOrWall<T>(Point pt1, Point pt2, Map<T> map)
        {
            bool isClossHole = CheckPointHole(pt1,pt2, map);
            bool isClossWall = CheckPointWall(pt1, pt2, map);
            if(isClossHole || isClossWall)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 调整拐点线
        /// </summary>
        /// <param name="inflectionPoints"></param>
        /// <param name="firPt"></param>
        /// <param name="routePlanData"></param>
        /// <returns></returns>
        override protected bool AdjustInflectionPoint<T>(List<Point> inflectionPoints, Point firPt, Map<T> map, List<Point> midPts, out Point useNextPt)
        {
            useNextPt = inflectionPoints.First();
            //判断是否穿墙
            //var midPt = midPts.First();
            //if(firPt.X == midPt.X)
            //{
            //    int xValue = firPt.X;
            //    int yDir = firPt.Y - midPt.Y < 0 ? 1 : -1;
            //    int yLength = Math.Abs(firPt.Y - midPt.Y);
            //    for (int i = 1; i <= yLength; i++)
            //    {
            //        var pt = new Point(xValue, firPt.Y + yDir * i);

            //        if (map.IsRoomWell(pt))
            //            return false;
            //    }
            //}
            //else if(firPt.Y == midPt.Y)
            //{
            //    int yValue = firPt.Y;
            //    int xDir = firPt.X - midPt.X < 0 ? 1 : -1;
            //    int xLength = Math.Abs(firPt.X - midPt.X);
            //    for (int i = 1; i <= xLength; i++)
            //    {
            //        var pt = new Point(firPt.X + xDir * i, yValue);

            //        if (map.IsRoomWell(pt))
            //            return false;
            //    }
            //}

            foreach (var nextPt in inflectionPoints)
            {
                if (CheckAdjustPoint<T>(firPt, midPts, nextPt, map))
                {
                    useNextPt = nextPt;
                    return true;
                }
                midPts.Add(nextPt);
            }

            return false;
        }

        /// <summary>
        /// 检查调整线上的点是否在障碍物中
        /// </summary>
        /// <param name="firPt"></param>
        /// <param name="nextPt"></param>
        /// <param name="routePlanData"></param>
        /// <returns></returns>
        override protected bool CheckAdjustPoint<T>(Point firPt, List<Point> midPts, Point nextPt, Map<T> map)
        {
            int yValue = midPts.Where(x => x.Y == firPt.Y).Count() == 0 ? firPt.Y : nextPt.Y;
            int xDir = firPt.X - nextPt.X < 0 ? 1 : -1;
            int xLength = Math.Abs(firPt.X - nextPt.X);
            for (int i = 1; i <= xLength; i++)
            {
                var pt = new Point(firPt.X + xDir * i, yValue);
                if (map.IsObstacle(pt))
                    return false;
            }

            int xValue = midPts.Where(x => x.X == firPt.X).Count() == 0 ? firPt.X : nextPt.X;
            int yDir = firPt.Y - nextPt.Y < 0 ? 1 : -1;
            int yLength = Math.Abs(firPt.Y - nextPt.Y);
            for (int i = 1; i <= yLength; i++)
            {
                var pt = new Point(xValue, firPt.Y + yDir * i);

                if (map.IsObstacle(pt))
                    return false;
            }
            return true;
        }
    }
}
