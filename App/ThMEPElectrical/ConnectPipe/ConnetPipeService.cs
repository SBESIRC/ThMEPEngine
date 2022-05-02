using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.ConnectPipe.Model;
using ThMEPElectrical.ConnectPipe.Service;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Service;

namespace ThMEPElectrical.ConnectPipe
{
    public class ConnetPipeService
    {
        readonly double tol = 8000;

        public List<Polyline> ConnetPipe(KeyValuePair<Polyline, List<Polyline>> plInfo, List<Line> parkingLines, List<BlockReference> broadcasts)
        {
            if (broadcasts.Count <= 0)
            {
                return new List<Polyline>();
            }
            broadcasts = broadcasts.Distinct().ToList();

            //分类车道线，将车道分为主车道和副车道
            var parkingLinesService = new ParkingLinesService();
            var mainPLines = parkingLinesService.CreateNodedParkingLines(plInfo.Key, parkingLines, out List<List<Line>> otherPLines);

            //将车道线做成polyline
            var mainParkingPolys = mainPLines.Select(x => parkingLinesService.CreateParkingLineToPolylineByTol(x)).ToList();
            var otherParkingPolys = otherPLines.Select(x => parkingLinesService.CreateParkingLineToPolylineByTol(x)).ToList();
            
            //排序车道线
            mainParkingPolys = OrderBlocklines(mainParkingPolys);
            otherParkingPolys = OrderBlocklines(otherParkingPolys);

            //找到车道上布置的广播
            var mainParkingPolysDic = GetBroadcastWithParkingLine(mainParkingPolys, broadcasts);
            var otherParkingPolysDic = GetBroadcastWithParkingLine(otherParkingPolys, broadcasts);
            MatchAllBroadcats(mainParkingPolysDic, otherParkingPolysDic, mainParkingPolys, otherParkingPolys, broadcasts);
            mainParkingPolysDic = mainParkingPolysDic.Where(x => x.Value != null && x.Value.Count > 0).ToDictionary(x => x.Key, y => y.Value);
            otherParkingPolysDic = otherParkingPolysDic.Where(x => x.Value != null && x.Value.Count > 0).ToDictionary(x => x.Key, y => y.Value);

            //连接车道线广播
            ConnectBroadcastService connectBroadcastService = new ConnectBroadcastService();
            var connectPolys = connectBroadcastService.ConnectBroadcast(plInfo, mainParkingPolysDic, otherParkingPolysDic);

            //修正连管线
            CorrectPipeConnectService correctPipeConnectService = new CorrectPipeConnectService();
            var correctPipe = correctPipeConnectService.CorrectPipe(connectPolys);
            
            //创建真实连管线
            CreatePipeLineService createPipeLineService = new CreatePipeLineService();
            var resPolys = createPipeLineService.CreatePipe(correctPipe, broadcasts);
           
            return resPolys;
        }

        /// <summary>
        /// 找到车道布置的广播
        /// </summary>
        /// <param name="parkingPoly"></param>
        /// <param name="broadcasts"></param>
        /// <returns></returns>
        private Dictionary<Polyline, List<BlockReference>> GetBroadcastWithParkingLine(List<Polyline> parkingPoly, List<BlockReference> broadcasts)
        {
            var bcLists = new List<BlockReference>(broadcasts);
            Dictionary<Polyline, List<BlockReference>> broadcastDic = new Dictionary<Polyline, List<BlockReference>>();
            foreach (var pPoly in parkingPoly)
            {
                var mathcBroadcasts = GetMatchBroadcasts(pPoly, bcLists);
                bcLists = bcLists.Except(mathcBroadcasts).ToList();
                broadcastDic.Add(pPoly, mathcBroadcasts);
            }

            return broadcastDic;
        }

        /// <summary>
        /// 找到符合的广播
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="broadcasts"></param>
        /// <returns></returns>
        private List<BlockReference> GetMatchBroadcasts(Polyline polyline, List<BlockReference> broadcasts)
        {
            var bufferPoly = polyline.BufferPoly(tol);
            var broads = broadcasts.Where(x => bufferPoly.Contains(x.Position) || bufferPoly.Distance(x.Position) < 300).ToList();
            var dir = (polyline.EndPoint - polyline.StartPoint).GetNormal();
            var otherDir = Vector3d.ZAxis.CrossProduct(dir);

            return broads.Where(x =>
            {
                var broadcastDir = -x.BlockTransform.CoordinateSystem3d.Xaxis.GetNormal();
                var checkBroadcastDir = -x.BlockTransform.CoordinateSystem3d.Yaxis.GetNormal();
                double yDotValue = broadcastDir.DotProduct(otherDir);
                double xDotValue = broadcastDir.DotProduct(dir);
                var checkDir = (polyline.StartPoint - x.Position).GetNormal();
                if (Math.Abs(yDotValue) < Math.Abs(xDotValue) && checkDir.DotProduct(checkBroadcastDir) > 0)
                {
                    return true;
                }

                return false;
            }).ToList();
        }

        /// <summary>
        /// 为所有广播点补全所有车道线
        /// </summary>
        /// <param name="mainParkingPolysDic"></param>
        /// <param name="otherParkingPolysDic"></param>
        /// <param name="mainParkingPoly"></param>
        /// <param name="otherParkingPoly"></param>
        /// <param name="broadcasts"></param>
        private void MatchAllBroadcats(Dictionary<Polyline, List<BlockReference>> mainParkingPolysDic, Dictionary<Polyline, List<BlockReference>> otherParkingPolysDic,
            List<Polyline> mainParkingPoly, List<Polyline> otherParkingPoly, List<BlockReference> broadcasts)
        {
            //找到剩余广播
            broadcasts = broadcasts.Except(mainParkingPolysDic.SelectMany(x => x.Value)).ToList();
            broadcasts = broadcasts.Except(otherParkingPolysDic.SelectMany(x => x.Value)).ToList();

            List<Polyline> allPolys = new List<Polyline>(mainParkingPoly);
            allPolys.AddRange(otherParkingPoly);
            foreach (var bCast in broadcasts)
            {
                var closePoly = allPolys.OrderBy(x => x.GetClosestPointTo(bCast.Position, false).DistanceTo(bCast.Position)).First();
                if (mainParkingPoly.Contains(closePoly))
                {
                    if (mainParkingPolysDic.ContainsKey(closePoly))
                    {
                        mainParkingPolysDic[closePoly].Add(bCast);
                    }
                    else
                    {
                        mainParkingPolysDic.Add(closePoly, new List<BlockReference>() { bCast });
                    }
                }
                else
                {
                    if (otherParkingPolysDic.ContainsKey(closePoly))
                    {
                        otherParkingPolysDic[closePoly].Add(bCast);
                    }
                    else
                    {
                        otherParkingPolysDic.Add(closePoly, new List<BlockReference>() { bCast });
                    }
                }
            }
        }

        /// <summary>
        /// 从上到下排序广播线
        /// </summary>
        /// <param name="mainBlockLines"></param>
        /// <returns></returns>
        private List<Polyline> OrderBlocklines(List<Polyline> mainBlockLines)
        {
            if (mainBlockLines.Count <= 0)
            {
                return mainBlockLines;
            }

            var maxLengthLine = mainBlockLines.OrderByDescending(x => x.Length).First();
            var xDir = (maxLengthLine.EndPoint - maxLengthLine.StartPoint).GetNormal();
            var zDir = Vector3d.ZAxis;
            var yDir = zDir.CrossProduct(xDir);
            yDir = yDir.Y < 0 ? -yDir : yDir;
            Matrix3d matrix = new Matrix3d(
                new double[] {
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0
            });

            return mainBlockLines.OrderByDescending(x => x.StartPoint.TransformBy(matrix.Inverse()).Y).ToList();
        }
    }
}
