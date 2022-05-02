using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Algorithm.DijkstraAlgorithm;

namespace ThMEPElectrical.ConnectPipe.Service
{
    public class PathfindingByPointService
    {
        double overLength = 10000;
        public List<Polyline> Pathfinding(KeyValuePair<Polyline, List<Polyline>> holeInfo, List<Polyline> mainConnectPolys,
            List<BlockReference> broadcasts, ref List<BlockReference> otherBroadcasts)
        {
            if (mainConnectPolys.Count <= 0)
            {
                return new List<Polyline>();
            }
            var connectPts = FindingPolyPoints(mainConnectPolys);
            var broadcastPts = broadcasts.OrderBy(x => connectPts.Select(y => y.DistanceTo(x.Position)).OrderBy(y => y).First()).ToList();   //根据现有连接主车道距离排序

            var connectPolys = new List<Polyline>(mainConnectPolys);
            //连接剩余其他副车道点
            foreach (var bPt in broadcastPts)
            {
                connectPts = FindingPolyPoints(connectPolys);
                var maxLength = CalMostUnfavorableValue(connectPolys, connectPts);
                var connectPoly = CalConnectPoly(holeInfo, connectPolys, connectPts, bPt.Position, maxLength);
                if (connectPoly != null)
                {
                    connectPolys.Add(connectPoly);
                }
                else
                {
                    otherBroadcasts.Add(bPt);
                }
            }

            return connectPolys;
        }

        /// <summary>
        /// 计算连接线
        /// </summary>
        /// <param name="holeInfo"></param>
        /// <param name="mainConnectPolys"></param>
        /// <param name="connectPts"></param>
        /// <param name="broadcastPt"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        private Polyline CalConnectPoly(KeyValuePair<Polyline, List<Polyline>> holeInfo, List<Polyline> mainConnectPolys,
            List<Point3d> connectPts, Point3d broadcastPt, double maxLength)
        {
            connectPts = connectPts.OrderBy(x => x.DistanceTo(broadcastPt)).ToList();
            bool isFirst = true;
            Polyline firPoly = null;
            foreach (var pt in connectPts)
            {
                Polyline connectPoly = new Polyline();
                connectPoly.AddVertexAt(0, broadcastPt.ToPoint2D(), 0, 0, 0);
                connectPoly.AddVertexAt(1, pt.ToPoint2D(), 0, 0, 0);

                if (CheckService.CheckOtherConnectLines(holeInfo, connectPoly, mainConnectPolys))
                {
                    var connectPolys = new List<Polyline>(mainConnectPolys);
                    connectPolys.Add(connectPoly);
                    DijkstraAlgorithm dijkstra = new DijkstraAlgorithm(connectPolys.Cast<Curve>().ToList());
                    var length = dijkstra.FindingAllPathMinLength(broadcastPt).OrderByDescending(x => x).First();
                    if (length > maxLength + overLength)
                    {
                        if (!isFirst)
                        {
                            return firPoly;
                        }

                        firPoly = connectPoly;
                        isFirst = false;
                        continue;
                    }

                    return connectPoly;
                }
            }

            return firPoly;
        }

        /// <summary>
        /// 计算整个系统最不利回路的最大值
        /// </summary>
        /// <param name="mainConnectPolys"></param>
        /// <param name="connectPts"></param>
        /// <returns></returns>
        private double CalMostUnfavorableValue(List<Polyline> mainConnectPolys, List<Point3d> connectPts)
        {
            double maxLength = 0;
            DijkstraAlgorithm dijkstra = new DijkstraAlgorithm(mainConnectPolys.Cast<Curve>().ToList());
            foreach (var pt in connectPts)
            {
                var length = dijkstra.FindingAllPathMinLength(pt).OrderByDescending(x => x).First();
                if (maxLength < length)
                {
                    maxLength = length;
                }
            }

            return maxLength;
        }

        /// <summary>
        /// 找到点位
        /// </summary>
        /// <param name="fingdingPoly"></param>
        /// <returns></returns>
        private List<Point3d> FindingPolyPoints(List<Polyline> fingdingPoly)
        {
            List<Point3d> sPts = new List<Point3d>();
            foreach (var poly in fingdingPoly)
            {
                if (!sPts.Any(x => x.IsEqualTo(poly.StartPoint, new Tolerance(1, 1))))
                {
                    sPts.Add(poly.StartPoint);
                }

                if (!sPts.Any(x => x.IsEqualTo(poly.EndPoint, new Tolerance(1, 1))))
                {
                    sPts.Add(poly.EndPoint);
                }
            }

            return sPts;
        }
    }
}
