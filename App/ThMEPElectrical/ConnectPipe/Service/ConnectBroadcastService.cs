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
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (var item in connectPolys.SelectMany(x => x))
                {
                    acdb.ModelSpace.Add(item);
                }
            }
        }

        /// <summary>
        /// 连接主车道（最不利路劲长度最短）
        /// </summary>
        /// <param name="mainBlockLines"></param>
        /// <returns></returns>
        private List<List<Polyline>> ConnectMainParkingLines(KeyValuePair<Polyline, List<Polyline>> holeInfo, List<List<Polyline>> mainBlockLines, Dictionary<Polyline, List<BlockReference>> otherParkingBroadcast)
        {
            var orderBlockLines = OrderBlocklines(mainBlockLines);
            var otherBlockLines = otherParkingBroadcast.Select(x => x.Key).ToList();

            PathfindingUitlsService pathfindingUitls = new PathfindingUitlsService();
            PathfindingWithDirUtils pathfindingWithDirUtils = new PathfindingWithDirUtils();
            List<List<Polyline>> resPolys = new List<List<Polyline>>() { orderBlockLines.First() };
            List<List<Polyline>> findingPolys = new List<List<Polyline>>() { orderBlockLines.First() };
            for (int i = 1; i < orderBlockLines.Count; i++)
            {
                var dirs = CalConnectDirByOtherLanes(orderBlockLines[i], otherBlockLines);
                Polyline connectPoly = pathfindingWithDirUtils.Pathfinding(holeInfo, findingPolys, resPolys, orderBlockLines[i], dirs);
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
    }
}
