using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.ConnectPipe.Dijkstra;

namespace ThMEPElectrical.ConnectPipe.Service
{
    public class PathfindingWithDirServce
    {
        readonly double distance = 1500;      //1.5m内能可以连接
        readonly double moveLength = 200;     //副车道连接线要移动200  
        readonly double tolAngle = 0.2;       //0.2以内可以认为两条线平行
        public Polyline Pathfinding(KeyValuePair<Polyline, List<Polyline>> holeInfo, List<List<Polyline>> mainPolys, List<List<Polyline>> endingPolys, 
            List<Polyline> fingdingPoly, List<Point3d> otherPLineBroadcast, List<Vector3d> dirs)
        {
            var sPts = GeUtils.FindingPolyPoints(fingdingPoly);
            var polyPts = mainPolys.Select(x => GeUtils.FindingPolyPoints(x)).ToList();
            var checkPolys = endingPolys.SelectMany(x => x).ToList();

            List<Polyline> firstConnectPoly = new List<Polyline>();
            List<Polyline> secondConnectPoly = new List<Polyline>();
            List<Polyline> LowestConnectPoly = new List<Polyline>();
            //计算连接线
            for (int i = 0; i < polyPts.Count; i++)
            {
                var polyDir = (polyPts[i].First() - polyPts[i].Last()).GetNormal();
                polyPts[i] = GeUtils.OrderPoints(polyPts[i], polyDir);

                foreach (var sPt in sPts)
                {
                    var connectPt = polyPts[i].OrderBy(x => x.DistanceTo(sPt)).First();
                    var spDir = (sPt - connectPt).GetNormal();
                    dirs = dirs.Select(x => x.DotProduct(spDir) < 0 ? x : -x).ToList();
                    var dir = dirs.OrderBy(x => x.GetAngleTo(spDir) > Math.PI / 2 ? Math.PI / 2 - x.GetAngleTo(spDir) : x.GetAngleTo(spDir)).First();

                    var firstPt = sPt;
                    var secondPt = connectPt;
                    if ((spDir.DotProduct(polyDir) > 0 && connectPt.IsEqualTo(polyPts[i].First())) || (spDir.DotProduct(polyDir) < 0 && connectPt.IsEqualTo(polyPts[i].Last())))
                    {
                        firstPt = connectPt;
                        secondPt = sPt;
                    }

                    if (GeUtils.GetDistanceByDir(firstPt, secondPt, dir, out Point3d projectPt) < (distance - moveLength))
                    {
                        Polyline connectPoly = new Polyline();
                        connectPoly.AddVertexAt(0, connectPt.ToPoint2D(), 0, 0, 0);
                        connectPoly.AddVertexAt(1, projectPt.ToPoint2D(), 0, 0, 0);
                        connectPoly.AddVertexAt(2, sPt.ToPoint2D(), 0, 0, 0);

                        if (!CheckService.CheckConnectLines(holeInfo, connectPoly, checkPolys))
                        {
                            continue;
                        }

                        if (CalFirstConnectByDir(checkPolys, connectPoly))
                        {
                            firstConnectPoly.Add(connectPoly);
                        }
                        if (CalFirstConnectByOtherPLines(otherPLineBroadcast, connectPoly))
                        {
                            secondConnectPoly.Add(connectPoly);
                        }
                        LowestConnectPoly.Add(connectPoly);
                    }
                }
            }

            //计算连接线中最短连接线（优先上下连接，其次根据副车道点位连接，最低优先级没有规则）
            if (firstConnectPoly.Count > 0)
            {
                return CalShortestLength(firstConnectPoly, checkPolys);
            }
            else if (secondConnectPoly.Count > 0)
            {
                return CalShortestLength(secondConnectPoly, checkPolys);
            }
            else
            {
                return CalShortestLength(LowestConnectPoly, checkPolys);
            }
        }

        /// <summary>
        /// 找到所有连接线中最短的路径
        /// </summary>
        /// <param name="connectPolys"></param>
        /// <param name="pathPolys"></param>
        /// <returns></returns>
        private Polyline CalShortestLength(List<Polyline> connectPolys, List<Polyline> pathPolys)
        {
            double maxLength = double.PositiveInfinity;
            Polyline maxPoly = null;
            connectPolys = connectPolys.OrderBy(x => x.Length).ToList();
            foreach (var poly in connectPolys)
            {
                var findingCheckPolys = new List<Polyline>(pathPolys);
                findingCheckPolys.Add(poly);
                DijkstraAlgorithm dijkstra = new DijkstraAlgorithm(findingCheckPolys.Cast<Curve>().ToList());
                var length = dijkstra.FindingAllPathMinLength(poly.EndPoint).OrderByDescending(x => x).First();
                if (length < maxLength)
                {
                    maxLength = length;
                    maxPoly = poly;
                }
            }

            return maxPoly;
        }

        /// <summary>
        /// 优先根据副车道点位选出连接线（如果连接线穿过副车道点位优先选择）
        /// </summary>
        /// <param name="broadcasts"></param>
        /// <param name="connectPoly"></param>
        /// <returns></returns>
        private bool CalFirstConnectByOtherPLines(List<Point3d> broadcasts, Polyline connectPoly)
        {
            var connectPt = broadcasts.Where(x =>
            {
                var closetPt = connectPoly.GetClosestPointTo(x, false);
                if (closetPt.DistanceTo(x) < distance)
                {
                    return true;
                }
                return false;
            }).ToList();
            if (connectPt.Count > 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 优先根据方向和连接关系选出连接线
        /// </summary>
        /// <param name="polylines"></param>
        /// <param name="connectPoly"></param>
        /// <returns></returns>
        private bool CalFirstConnectByDir(List<Polyline> polylines, Polyline connectPoly)
        {
            List<Line> lines = new List<Line>();
            for (int i = 0; i < connectPoly.NumberOfVertices - 1; i++)
            {
                lines.Add(new Line(connectPoly.GetPoint3dAt(i), connectPoly.GetPoint3dAt(i + 1)));
            }

            Point3d connectPt = connectPoly.StartPoint;
            Line connectLine = lines.OrderByDescending(x => x.Length).First();
            Vector3d connectDir = (connectLine.EndPoint - connectLine.StartPoint).GetNormal();
            var prevPolys = polylines.Where(x => x.StartPoint.IsEqualTo(connectPt, new Tolerance(1, 1)) || x.EndPoint.IsEqualTo(connectPt, new Tolerance(1, 1))).ToList();
            foreach (var poly in prevPolys)
            {
                List<Line> polyLines = new List<Line>();
                for (int i = 0; i < poly.NumberOfVertices - 1; i++)
                {
                    polyLines.Add(new Line(poly.GetPoint3dAt(i), poly.GetPoint3dAt(i + 1)));
                }

                Line polyLine = polyLines.OrderByDescending(x => x.Length).First();
                Vector3d polyDir = (polyLine.EndPoint - polyLine.StartPoint).GetNormal();
                polyDir = polyDir.DotProduct(connectDir) < 0 ? -polyDir : polyDir;
                double angle = polyDir.GetAngleTo(connectDir);
                angle = angle > Math.PI / 2 ? Math.PI / 2 - angle : angle;
                if (angle < tolAngle)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
