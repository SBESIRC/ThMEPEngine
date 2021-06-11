using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.AStarAlgorithm;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Algorithm
{
    public class ThFindingPathByRegion
    {
        public double step = 400;
        public double avoidFrameDistance = 250;
        public double avoidHoleDistance = 250;
        public List<Polyline> FindingPath(Polyline polyline, Point3d sPt, Point3d ePt, List<Polyline> holes)
        {
            var dir = GetPolylineDir(polyline);
            AStarRoutePlanner<Point3d> aStarRoute = new AStarRoutePlanner<Point3d>(polyline, dir, ePt, step, avoidFrameDistance, avoidHoleDistance);
            aStarRoute.SetObstacle(holes);
            var res = aStarRoute.Plan(sPt);

            ThRegionDivisionService thRegionDivision = new ThRegionDivisionService();
            thRegionDivision.tol = 0.1;
            var divPolys = thRegionDivision.DivisionRegion(polyline);
            var resPolys = thRegionDivision.MergePolygon(divPolys);

            Polyline lastPoly = null;
            foreach (var poly in resPolys)
            {
                if (poly.Contains(ePt))
                {
                    lastPoly = poly;
                    break;
                }
            }

            resPolys.Remove(lastPoly);
            var resPaths = FindingPathCrosingRegions(sPt, res, polyline, resPolys, holes);
            if (lastPoly != null && resPolys.Count > 0)
            {
                var lastPath = FindLastPath(resPaths, ePt, polyline, lastPoly, holes);
                if (lastPath != null)
                {
                    resPaths.Add(lastPath);
                }
            }

            return resPaths;
        }

        /// <summary>
        /// 最后一段区域寻路
        /// </summary>
        /// <param name="sPt"></param>
        /// <param name="ePt"></param>
        /// <param name="resPolys"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        private Polyline FindLastPath(List<Polyline> resPaths, Point3d ePt, Polyline polyline, Polyline lastPoly, List<Polyline> holes)
        {
            Point3d sPt = resPaths.Last().EndPoint;
            if (lastPoly != null)
            {
                var dir = GetPolylineDir(lastPoly);
                AStarRoutePlanner<Point3d> aStarRoute = new AStarRoutePlanner<Point3d>(polyline, dir, ePt, step, avoidFrameDistance, avoidHoleDistance);
                aStarRoute.SetObstacle(holes);
                var res = aStarRoute.Plan(sPt);

                if (resPaths.Count > 0 && res != null)
                {
                    res = AdjustPath(polyline, resPaths, res, dir, holes);
                }

                return res;
            }

            return null;
        }

        /// <summary>
        /// 寻路穿越框线
        /// </summary>
        /// <param name="sPt"></param>
        /// <param name="resPath"></param>
        /// <param name="resPolys"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        private List<Polyline> FindingPathCrosingRegions(Point3d sPt, Polyline resPath, Polyline polyline, List<Polyline> resPolys, List<Polyline> holes)
        {
            List<Polyline> resPoly = new List<Polyline>();
            var interPolys = FindIntersectRegion(resPath, resPolys);
            if (interPolys.Count <= 0)
            {
                return new List<Polyline>() { resPath };
            }

            foreach (var polyInfo in interPolys)
            {
                Polyline poly = polyInfo.Value;
                var dir = GetPolylineDir(poly);

                AStarRoutePlanner<Point3d> aStarRoute = new AStarRoutePlanner<Point3d>(polyline, dir, polyInfo.Key, step, avoidFrameDistance, avoidHoleDistance);
                aStarRoute.SetObstacle(holes);
                var res = aStarRoute.Plan(sPt);
                if (resPoly.Count > 0)
                {
                    res = AdjustPath(polyline, resPoly, res, dir, holes);
                }

                resPoly.Add(res);
                sPt = res.EndPoint;
            }

            return resPoly;
        }

        /// <summary>
        /// 调整生成线
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="prePath"></param>
        /// <param name="path"></param>
        /// <param name="dir"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        private Polyline AdjustPath(Polyline frame, List<Polyline> resPaths, Polyline path, Vector3d dir, List<Polyline> holes)
        {
            var prePath = resPaths.Last();
            Point3d sPt = path.GetPoint3dAt(0);
            Ray ray = new Ray();
            ray.BasePoint = path.GetPoint3dAt(1);
            ray.UnitDir = (sPt - ray.BasePoint).GetNormal();

            Point3dCollection pts = new Point3dCollection();
            prePath.IntersectWith(ray, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
            var ptLts = pts.Cast<Point3d>().Where(x => !x.IsEqualTo(sPt)).ToList();
            if (ptLts.Count > 0)
            {
                var sp = ptLts.OrderBy(x => x.DistanceTo(sPt)).Last();
                var ep = path.EndPoint;

                AStarRoutePlanner<Point3d> aStarRoute = new AStarRoutePlanner<Point3d>(frame, dir, ep, step, avoidFrameDistance, avoidHoleDistance);
                aStarRoute.SetObstacle(holes);
                var res = aStarRoute.Plan(sp);
                resPaths[resPaths.Count - 1] = AjustPrePath(prePath, sp);

                return res;
            }

            return path;
        }

        /// <summary>
        /// 调整前一条路径
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        private Polyline AjustPrePath(Polyline path, Point3d pt)
        {
            Polyline newPoly = new Polyline();
            newPoly.AddVertexAt(0, path.GetPoint3dAt(0).ToPoint2D(), 0, 0, 0);
            for (int i = 1; i < path.NumberOfVertices; i++)
            {
                Line line = new Line(path.GetPoint3dAt(i - 1), path.GetPoint3dAt(i));
                if (line.GetClosestPointTo(pt, false).DistanceTo(pt) < 0.1)
                {
                    newPoly.AddVertexAt(i, pt.ToPoint2D(), 0, 0, 0);
                    break;
                }

                newPoly.AddVertexAt(i, path.GetPoint3dAt(i).ToPoint2D(), 0, 0, 0);
            }
            return newPoly;
        }

        /// <summary>
        /// 找到相交的框线
        /// </summary>
        /// <param name="resPath"></param>
        /// <param name="resPolys"></param>
        /// <returns></returns>
        private Dictionary<Point3d, Polyline> FindIntersectRegion(Polyline resPath, List<Polyline> resPolys)
        {
            var interInfo = new Dictionary<Point3d, Polyline>();
            List<(double, double)> checkIndex = new List<(double, double)>();
            foreach (var poly in resPolys)
            {
                var bPolys = poly.Buffer(-avoidFrameDistance);
                if (bPolys.Count <= 0)
                {
                    continue;
                }

                var bufferPoly = bPolys[0] as Polyline;
                Point3dCollection pts = new Point3dCollection();
                resPath.IntersectWith(bufferPoly, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                if (pts.Count > 0)
                {
                    var ptLst = pts.Cast<Point3d>().OrderBy(x => resPath.GetDistAtPoint(x)).ToList();
                    if (!interInfo.Keys.Contains(ptLst.Last()) && interInfo.Keys.Where(x => x.IsEqualTo(ptLst.Last(), new Tolerance(1, 1))).Count() <= 0)
                    {
                        double maxLength = resPath.GetDistAtPoint(ptLst.Last());
                        double minLength = resPath.GetDistAtPoint(ptLst.First());
                        if (ptLst.Count == 1)
                        {
                            minLength = 0;
                        }
                        foreach (var index in checkIndex)
                        {
                            if (maxLength >= index.Item1 && maxLength <= index.Item2)
                            {
                                continue;
                            }
                        }
                        checkIndex.Add((minLength, maxLength));
                        interInfo.Add(ptLst.Last(), bufferPoly);
                    }
                }
            }
            interInfo = CheckUsefulPoly(checkIndex, interInfo).OrderBy(x => resPath.GetDistAtPoint(x.Key)).ToDictionary(x => x.Key, y => y.Value);

            return interInfo;
        }

        /// <summary>
        /// 筛选掉不符合要求的polyline
        /// </summary>
        /// <param name="checkIndex"></param>
        /// <param name="interInfo"></param>
        /// <returns></returns>
        private Dictionary<Point3d, Polyline> CheckUsefulPoly(List<(double, double)> checkIndex, Dictionary<Point3d, Polyline> interInfo)
        {
            List<Point3d> removeKeys = new List<Point3d>();
            for (int i = 0; i < checkIndex.Count; i++)
            {
                var thisIndex = checkIndex[i];
                foreach (var index in checkIndex)
                {
                    if ((thisIndex.Item1 < index.Item2 && thisIndex.Item1 > index.Item1) &&
                        (thisIndex.Item2 < index.Item2 && thisIndex.Item2 > index.Item1))
                    {
                        removeKeys.Add(interInfo.ElementAt(i).Key);
                        break;
                    }
                }
            }

            foreach (var key in removeKeys)
            {
                interInfo.Remove(key);
            }

            return interInfo;
        }

        /// <summary>
        /// 计算polyline大致走向
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private Vector3d GetPolylineDir(Polyline polyline)
        {
            var obbPoly = polyline.CalObb();
            using (Linq2Acad.AcadDatabase db = Linq2Acad.AcadDatabase.Active())
            {
                //db.ModelSpace.Add(obbPoly);
            }
            List<Line> allLines = GetAllLinesInPolyline(obbPoly);
            var line = allLines.OrderByDescending(x => x.Length).First();
            var polyDir = (line.EndPoint - line.StartPoint).GetNormal();

            return polyDir;
        }

        /// <summary>
        /// 获取polyline上所有线
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<Line> GetAllLinesInPolyline(Polyline polyline)
        {
            List<Line> allLines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                allLines.Add(new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices)));
            }
            return allLines;
        }
    }
}
