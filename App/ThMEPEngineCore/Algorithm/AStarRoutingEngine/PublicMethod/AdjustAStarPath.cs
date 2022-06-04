using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.AStarModel.OriginAStarModel;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.MapService;

namespace ThMEPEngineCore.Algorithm.AStarRoutingEngine.PublicMethod
{
    public class AdjustAStarPath
    {
        virtual public List<Point> AdjustPath<T>(List<Point> path, Map<T> map)
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
        virtual protected List<Point> ReduceInflectionPoint<T>(List<Point> inflectionPoints, Map<T> map)
        {
            List<Point> path = new List<Point>();
            Point lastPt = inflectionPoints.Last();
            while (inflectionPoints.Count >= 3)
            {
                var firPt = inflectionPoints.First();
                inflectionPoints.Remove(firPt);
                path.Add(firPt);
                var midPt = inflectionPoints.First();
                inflectionPoints.Remove(midPt);

                List<Point> midPts = new List<Point>() { midPt };
                if (AdjustInflectionPoint<T>(inflectionPoints, firPt, map, midPts, out Point nextPt))
                {
                    int xValue = midPts.Where(x => x.X == firPt.X).Count() == 0 ? firPt.X : nextPt.X;
                    int yValue = midPts.Where(x => x.Y == firPt.Y).Count() == 0 ? firPt.Y : nextPt.Y;
                    var adjustPt = new Point(xValue, yValue);
                    midPts.Add(nextPt);
                    inflectionPoints = inflectionPoints.Except(midPts).ToList();
                    inflectionPoints.Insert(0, adjustPt);
                    inflectionPoints.Insert(0, firPt);
                }
                else
                {
                    inflectionPoints.Insert(0, midPt);
                }
            }

            inflectionPoints.Add(lastPt);
            foreach (var pt in inflectionPoints)
            {
                path.Add(pt);
            }
            path.Reverse();
            return path;
        }

        /// <summary>
        /// 调整拐点线
        /// </summary>
        /// <param name="inflectionPoints"></param>
        /// <param name="firPt"></param>
        /// <param name="routePlanData"></param>
        /// <returns></returns>
        virtual protected bool AdjustInflectionPoint<T>(List<Point> inflectionPoints, Point firPt, Map<T> map, List<Point> midPts, out Point useNextPt)
        {
            useNextPt = inflectionPoints.First();
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
        virtual protected bool CheckAdjustPoint<T>(Point firPt, List<Point> midPts, Point nextPt, Map<T> map)
        {
            int yValue = midPts.Where(x => x.Y == firPt.Y).Count() == 0 ? firPt.Y : nextPt.Y;
            int xDir = firPt.X - nextPt.X < 0 ? 1 : -1;
            int xLength = Math.Abs(firPt.X - nextPt.X);
            for (int i = 1; i <= xLength; i++)
            {
                var pt = new Point(firPt.X + xDir * i, yValue);
                if (map.IsObstacle(pt) || !map.IsInBounds(pt))
                    return false;
            }

            int xValue = midPts.Where(x => x.X == firPt.X).Count() == 0 ? firPt.X : nextPt.X;
            int yDir = firPt.Y - nextPt.Y < 0 ? 1 : -1;
            int yLength = Math.Abs(firPt.Y - nextPt.Y);
            for (int i = 1; i <= yLength; i++)
            {
                var pt = new Point(xValue, firPt.Y + yDir * i);

                if (map.IsObstacle(pt) || !map.IsInBounds(pt))
                    return false;
            }

            return true;
        }
    }
}
