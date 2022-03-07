using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.AStarModel;
using ThMEPEngineCore.Algorithm.AStarAlgorithm.MapService;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm
{
    public class GlobleAdjustAStarPath
    {
        public List<GloblePoint> AdjustPath<T>(List<GloblePoint> path, GlobleMap<T> map)
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
        protected List<GloblePoint> ReduceInflectionPoint<T>(List<GloblePoint> inflectionPoints, GlobleMap<T> map)
        {
            List<GloblePoint> path = new List<GloblePoint>();
            GloblePoint lastPt = inflectionPoints.Last();
            while (inflectionPoints.Count >= 3)
            {
                var firPt = inflectionPoints.First();
                inflectionPoints.Remove(firPt);
                path.Add(firPt);
                var midPt = inflectionPoints.First();
                inflectionPoints.Remove(midPt);

                List<GloblePoint> midPts = new List<GloblePoint>() { midPt };
                if (AdjustInflectionPoint<T>(inflectionPoints, firPt, map, midPts, out GloblePoint nextPt))
                {
                    double xValue = midPts.Where(x => x.X == firPt.X).Count() == 0 ? firPt.X : nextPt.X;
                    double yValue = midPts.Where(x => x.Y == firPt.Y).Count() == 0 ? firPt.Y : nextPt.Y;
                    var adjustPt = new GloblePoint(xValue, yValue);
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
        protected bool AdjustInflectionPoint<T>(List<GloblePoint> inflectionPoints, GloblePoint firPt, GlobleMap<T> map, List<GloblePoint> midPts, out GloblePoint useNextPt)
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
        protected bool CheckAdjustPoint<T>(GloblePoint firPt, List<GloblePoint> midPts, GloblePoint nextPt, GlobleMap<T> map)
        {
            double yValue = midPts.Where(x => x.Y == firPt.Y).Count() == 0 ? firPt.Y : nextPt.Y;
            double xDir = firPt.X - nextPt.X < 0 ? 1 : -1;
            double xLength = Math.Abs(firPt.X - nextPt.X);
            for (int i = 1; i <= xLength; i++)
            {
                var pt = new GloblePoint(firPt.X + xDir * i, yValue);
                if (map.GetObstacleWeight(pt) == double.MaxValue || !map.IsInBounds(pt))
                    return false;
            }

            double xValue = midPts.Where(x => x.X == firPt.X).Count() == 0 ? firPt.X : nextPt.X;
            double yDir = firPt.Y - nextPt.Y < 0 ? 1 : -1;
            double yLength = Math.Abs(firPt.Y - nextPt.Y);
            for (int i = 1; i <= yLength; i++)
            {
                var pt = new GloblePoint(xValue, firPt.Y + yDir * i);
                if (map.GetObstacleWeight(pt) == double.MaxValue || !map.IsInBounds(pt))
                    return false;
            }

            return true;
        }
    }
}
