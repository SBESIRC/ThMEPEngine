using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.ConnectPipe.Service
{
    public class ConnectBroadcastService
    {
        readonly double distance = 1500;      //1.5m内能可以连接

        public void ConnectBroadcast(KeyValuePair<Polyline, List<Polyline>> holeInfo, Dictionary<Polyline, List<BlockReference>> mainParkingBroadcast, Dictionary<Polyline, List<BlockReference>> otherParkingBroadcast)
        {
            //区分连管顺序的优先级
            var firstBroadcasts = mainParkingBroadcast.Where(x => x.Value.Count() > 1).ToDictionary(x => x.Key, y => y.Value);
            var secondBroadcasts = mainParkingBroadcast.Where(x => x.Value.Count() == 1).ToDictionary(x => x.Key, y => y.Value);
            var thirdBroadcasts = otherParkingBroadcast;

            //将主车道上的广播连线
            var mainBlockLines = ConnectBroadcastsToLine(firstBroadcasts);

            //连接主车道（最不利路劲长度最短）
            var connectPolys = ConnectMainParkingLines(holeInfo, mainBlockLines, otherParkingBroadcast);

            //将副车道和单个点主车道都连接上
            var allConnectPolys = ConnectOtherParkingLines(holeInfo, connectPolys, secondBroadcasts, thirdBroadcasts);

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (var item in allConnectPolys)
                {
                    acdb.ModelSpace.Add(item);
                }
            }
        }

        #region 主车道连接
        /// <summary>
        /// 连接主车道（最不利路劲长度最短）
        /// </summary>
        /// <param name="mainBlockLines"></param>
        /// <returns></returns>
        private List<List<Polyline>> ConnectMainParkingLines(KeyValuePair<Polyline, List<Polyline>> holeInfo, 
            List<List<Polyline>> mainBlockLines, Dictionary<Polyline, List<BlockReference>> otherParkingBroadcast)
        {
            var orderBlockLines = OrderBlocklines(mainBlockLines);
            var otherBlockLines = otherParkingBroadcast.Select(x => x.Key).ToList();
            var otherBlockPts = otherParkingBroadcast.SelectMany(x => x.Value.Select(y => y.Position)).ToList();

            PathfindingUitlsService pathfindingUitls = new PathfindingUitlsService();
            PathfindingWithDirServce pathfindingWithDirUtils = new PathfindingWithDirServce();
            List<List<Polyline>> resPolys = new List<List<Polyline>>() { orderBlockLines.First() };
            List<List<Polyline>> findingPolys = new List<List<Polyline>>() { orderBlockLines.First() };
            for (int i = 1; i < orderBlockLines.Count; i++)
            {
                var dirs = CalConnectDirByOtherLanes(orderBlockLines[i], otherBlockLines);
                Polyline connectPoly = pathfindingWithDirUtils.Pathfinding(holeInfo, findingPolys, resPolys, orderBlockLines[i], otherBlockPts, dirs);
                if (connectPoly == null)
                {
                    connectPoly = pathfindingUitls.Pathfinding(holeInfo, findingPolys, resPolys, orderBlockLines[i]);
                }
                resPolys.Add(new List<Polyline>() { connectPoly });
                resPolys.Add(orderBlockLines[i]);
                findingPolys.Add(orderBlockLines[i]);
            }

            return resPolys;
        }

        /// <summary>
        /// 连接方向（为了美观）
        /// </summary>
        /// <param name="mainBlockLines"></param>
        /// <param name="otherBlockLines"></param>
        /// <returns></returns>
        private List<Vector3d> CalConnectDirByOtherLanes(List<Polyline> mainBlockLines, List<Polyline> otherBlockLines)
        {
            var directions = otherBlockLines.Select(x => (x.EndPoint - x.StartPoint).GetNormal()).ToList();
            directions.AddRange(mainBlockLines.Select(x => Vector3d.ZAxis.CrossProduct((x.EndPoint - x.StartPoint).GetNormal())));
            directions = directions.Distinct().ToList();

            return directions;
        }

        /// <summary>
        /// 将单根主车道上的广播连线
        /// </summary>
        /// <param name="parkingBroadcast"></param>
        private List<List<Polyline>> ConnectBroadcastsToLine(Dictionary<Polyline, List<BlockReference>> parkingBroadcast)
        {
            List<List<Polyline>> resLines = new List<List<Polyline>>();
            foreach (var pBroadcast in parkingBroadcast)
            {
                var poly = pBroadcast.Key;
                var broadcasts = pBroadcast.Value;

                //获取移动信息
                BlockUtils.GetLaneMoveInfo(poly, broadcasts, out double distance, out Vector3d dir);

                //移动车道线
                var newPoly = BlockUtils.movePolyline(poly, dir, distance);

                //将广播连线
                var blockLines = BlockUtils.GetBlockLines(newPoly, broadcasts);
                resLines.Add(blockLines);
            }

            return resLines;
        }

        /// <summary>
        /// 从上到下排序广播线
        /// </summary>
        /// <param name="mainBlockLines"></param>
        /// <returns></returns>
        private List<List<Polyline>> OrderBlocklines(List<List<Polyline>> mainBlockLines)
        {
            var maxLengthLine = mainBlockLines.SelectMany(x => x).OrderByDescending(x => x.Length).First();
            var xDir = (maxLengthLine.EndPoint - maxLengthLine.StartPoint).GetNormal();
            var zDir = Vector3d.ZAxis;
            var yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0
            });

            return mainBlockLines.OrderByDescending(x => x.First().StartPoint.Y).ToList();
        }
        #endregion

        #region 副车道连接
        /// <summary>
        /// 计算副车道和单个点主车道连接线
        /// </summary>
        /// <param name="holeInfo"></param>
        /// <param name="mainConnectPolys"></param>
        /// <param name="secondBroadcasts"></param>
        /// <param name="thirdBroadcasts"></param>
        /// <returns></returns>
        private List<Polyline> ConnectOtherParkingLines(KeyValuePair<Polyline, List<Polyline>> holeInfo, List<List<Polyline>> mainConnectPolys,
            Dictionary<Polyline, List<BlockReference>> secondBroadcastsInfo, Dictionary<Polyline, List<BlockReference>> thirdBroadcastsInfo)
        {
            List<BlockReference> secondBroadcasts = secondBroadcastsInfo.SelectMany(x => x.Value).ToList();
            List<BlockReference> thirdBroadcasts = thirdBroadcastsInfo.SelectMany(x => x.Value).ToList();

            var allMainConnectPolys = mainConnectPolys.SelectMany(x => x).ToList();
            PathfindingByPointService pathfindingByPoint = new PathfindingByPointService();
            //已连接线如果穿过副车道广播，则重新连接上这个副车道点位
            thirdBroadcasts = FilterBroadcastPts(allMainConnectPolys, thirdBroadcasts, out Dictionary<Polyline, List<Point3d>> singlePtDic);
            foreach (var dicInfo in singlePtDic)
            {
                allMainConnectPolys.Remove(dicInfo.Key);
                allMainConnectPolys.AddRange(CalFirstConnectByOtherPLines(dicInfo.Value, dicInfo.Key));
            }

            //单个点主车道链接上去
            var allConnectPolys = pathfindingByPoint.Pathfinding(holeInfo, allMainConnectPolys, secondBroadcasts);

            //副车道链接上去
            allConnectPolys = pathfindingByPoint.Pathfinding(holeInfo, allConnectPolys, thirdBroadcasts);

            return allConnectPolys;
        }


        /// <summary>
        /// 找到一些不需要伸出支管的广播点
        /// </summary>
        /// <param name="mainConnectPolys"></param>
        /// <param name="broadcastPts"></param>
        /// <returns></returns>
        private List<BlockReference> FilterBroadcastPts(List<Polyline> mainConnectPolys, List<BlockReference> broadcasts, out Dictionary<Polyline, List<Point3d>> singlePtDic)
        {
            List<BlockReference> resBroadcasts = new List<BlockReference>();
            bool isUseful = false;
            singlePtDic = new Dictionary<Polyline, List<Point3d>>();
            foreach (var bcast in broadcasts)
            {
                isUseful = true;
                foreach (var poly in mainConnectPolys)
                {
                    var closetPt = poly.GetClosestPointTo(bcast.Position, false);
                    if (closetPt.DistanceTo(bcast.Position) < distance)
                    {
                        if (singlePtDic.SelectMany(x => x.Value).Contains(bcast.Position))
                        {
                            break;
                        }
                        if (singlePtDic.ContainsKey(poly))
                        {
                            singlePtDic[poly].Add(bcast.Position);
                        }
                        else
                        {
                            singlePtDic.Add(poly, new List<Point3d>() { bcast.Position });
                        }
                        isUseful = false;
                    }
                }

                if (isUseful)
                {
                    resBroadcasts.Add(bcast);
                }
            }

            return resBroadcasts;
        }

        /// <summary>
        /// 优先连接副车道广播穿过之前的连接线的广播点
        /// </summary>
        /// <param name="broadcasts"></param>
        /// <param name="connectPoly"></param>
        /// <param name="resConnectPoly"></param>
        /// <returns></returns>
        private List<Polyline> CalFirstConnectByOtherPLines(List<Point3d> broadcasts, Polyline connectPoly)
        {
            var connectPtDic = broadcasts
                .ToDictionary(x => x, y => connectPoly.GetClosestPointTo(y, false))
                .Where(x => x.Key.DistanceTo(x.Value) < distance)
                .ToDictionary(x => x.Key, y => y.Value);
            var connectPtKeys = connectPtDic.Keys.ToList();
            var allPolyPts = new List<Point3d>(connectPtKeys);
            for (int i = 0; i < connectPoly.NumberOfVertices; i++)
            {
                allPolyPts.Add(connectPoly.GetPoint3dAt(i));
            }
            allPolyPts = GeUtils.OrderPoints(allPolyPts, (connectPoly.EndPoint - connectPoly.StartPoint).GetNormal());

            var resConnectPoly = new List<Polyline>();
            if (connectPtDic.Count > 0)
            {
                var sPt = allPolyPts.First();
                allPolyPts.Remove(sPt);
                while (allPolyPts.Count > 0)
                {
                    List<Point3d> usePts = new List<Point3d>() { sPt };
                    if (connectPtKeys.Contains(sPt))
                    {
                        usePts.Add(connectPtDic[sPt]);
                    }
                    for (int i = 0; i < allPolyPts.Count; i++)
                    {
                        if (connectPtKeys.Contains(allPolyPts[i]))
                        {
                            usePts.Add(connectPtDic[allPolyPts[i]]);
                            usePts.Add(allPolyPts[i]);
                            sPt = allPolyPts[i];
                            break;
                        }
                        usePts.Add(allPolyPts[i]);
                    }
                    allPolyPts = allPolyPts.Except(usePts).ToList();

                    Polyline polyline = new Polyline();
                    for (int i = 0; i < usePts.Count; i++)
                    {
                        polyline.AddVertexAt(i, usePts[i].ToPoint2D(), 0, 0, 0);
                    }
                    resConnectPoly.Add(polyline);
                }
            }

            return resConnectPoly;
        }
        #endregion
    }
}
