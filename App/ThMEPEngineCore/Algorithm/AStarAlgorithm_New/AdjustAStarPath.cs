using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.AStarAlgorithm_New.Model;

namespace ThMEPEngineCore.Algorithm.AStarAlgorithm_New
{
    public class AdjustAStarPath
    {
        public List<Point3d> AdjustPath(List<AStarNode> path, List<Polyline> obstacles, Polyline frame)
        {
            if (path == null || path.Count <= 1)
            {
                return path.Select(x => x.Location).ToList();
            }
            var inflectionPoints = path.Where(x => x.IsInflectionPoint).Select(x => x.Location).ToList();

            var useObstacles = obstacles.Select(x => x.Buffer(-10)[0] as Polyline).ToList();
            useObstacles.Add(frame);
            return ReduceInflectionPoint(inflectionPoints, useObstacles);
        }

        /// <summary>
        /// 减少拐点
        /// </summary>
        /// <param name="inflectionPoints"></param>
        /// <param name="routePlanData"></param>
        /// <returns></returns>
        private List<Point3d> ReduceInflectionPoint(List<Point3d> inflectionPoints, List<Polyline> obstacles)
        {
            List<Point3d> path = new List<Point3d>();
            Point3d lastPt = inflectionPoints.Last();
            while (inflectionPoints.Count >= 3)
            {
                var firPt = inflectionPoints.First();
                inflectionPoints.Remove(firPt);
                path.Add(firPt);
                var midPt = inflectionPoints.First();
                inflectionPoints.Remove(midPt);

                List<Point3d> midPts = new List<Point3d>() { midPt };
                if (AdjustInflectionPoint(inflectionPoints, firPt, obstacles, midPts, out Point3d nextPt))
                {
                    double xValue = midPts.Where(x => x.X == firPt.X).Count() == 0 ? firPt.X : nextPt.X;
                    double yValue = midPts.Where(x => x.Y == firPt.Y).Count() == 0 ? firPt.Y : nextPt.Y;
                    var adjustPt = new Point3d(xValue, yValue, 0);
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
        private bool AdjustInflectionPoint(List<Point3d> inflectionPoints, Point3d firPt, List<Polyline> obstacles, List<Point3d> midPts, out Point3d useNextPt)
        {
            useNextPt = inflectionPoints.First();
            foreach (var nextPt in inflectionPoints)
            {
                if (CheckAdjustPoint(firPt, midPts, nextPt, obstacles))
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
        private bool CheckAdjustPoint(Point3d firPt, List<Point3d> midPts, Point3d nextPt, List<Polyline> obstacles)
        {
            double yValue = midPts.Where(x => x.Y == firPt.Y).Count() == 0 ? firPt.Y : nextPt.Y;
            double xValue = midPts.Where(x => x.X == firPt.X).Count() == 0 ? firPt.X : nextPt.X;
            Point3d pt = new Point3d(xValue, yValue, 0);
            Line xLine = new Line(pt, firPt);
            foreach (var hole in obstacles)
            {
                if (hole.LineIntersects(xLine))
                {
                    return false;
                }
            }

            Line yLine = new Line(pt, nextPt);
            foreach (var hole in obstacles)
            {
                if (hole.LineIntersects(yLine))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
