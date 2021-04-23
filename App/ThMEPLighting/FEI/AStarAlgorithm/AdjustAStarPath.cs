using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.FEI.AStarAlgorithm
{
    public class AdjustAStarPath
    {
        public List<Point> AdjustPath(List<Point> path, RoutePlanData routePlanData)
        {
            if (path == null || path.Count <= 0)
            {
                return path;
            }
            var inflectionPoints = path.Where(x => x.IsInflectionPoint).ToList();

            return ReduceInflectionPoint(inflectionPoints, routePlanData);
        }

        /// <summary>
        /// 减少拐点
        /// </summary>
        /// <param name="inflectionPoints"></param>
        /// <param name="routePlanData"></param>
        /// <returns></returns>
        private List<Point> ReduceInflectionPoint(List<Point> inflectionPoints, RoutePlanData routePlanData)
        {
            List<Point> path = new List<Point>();
            while (inflectionPoints.Count > 3)
            {
                var firPt = inflectionPoints.First();
                inflectionPoints.Remove(firPt);
                path.Add(firPt);
                var midPt = inflectionPoints.First();
                inflectionPoints.Remove(midPt);

                List<Point> midPts = new List<Point>() { midPt };
                if (AdjustInflectionPoint(inflectionPoints, firPt, routePlanData, midPts, out Point nextPt))
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
        private bool AdjustInflectionPoint(List<Point> inflectionPoints, Point firPt, RoutePlanData routePlanData, List<Point> midPts, out Point useNextPt)
        {
            useNextPt = inflectionPoints.First();
            foreach (var nextPt in inflectionPoints)
            {
                if (CheckAdjustPoint(firPt, midPts, nextPt, routePlanData))
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
        private bool CheckAdjustPoint(Point firPt, List<Point> midPts, Point nextPt, RoutePlanData routePlanData)
        {
            int yValue = midPts.Where(x => x.Y == firPt.Y).Count() == 0 ? firPt.Y : nextPt.Y;
            int xDir = firPt.X - nextPt.X < 0 ? 1 : -1;
            int xLength = Math.Abs(firPt.X - nextPt.X);
            for (int i = 1; i <= xLength; i++)
            {
                if (routePlanData.CellMap.obstacles[firPt.X + xDir * i][yValue])
                {
                    return false;
                }
            }

            int xValue = midPts.Where(x => x.X == firPt.X).Count() == 0 ? firPt.X : nextPt.X;
            int yDir = firPt.Y - nextPt.Y < 0 ? 1 : -1;
            int yLength = Math.Abs(firPt.Y - nextPt.Y);
            for (int i = 1; i <= yLength; i++)
            {
                if (routePlanData.CellMap.obstacles[xValue][firPt.Y + yDir * i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
