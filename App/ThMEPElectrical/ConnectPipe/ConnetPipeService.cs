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
using ThMEPEngineCore.Service;

namespace ThMEPElectrical.ConnectPipe
{
    public class ConnetPipeService
    {
        readonly double tol = 6000;

        public void ConnetPipe(KeyValuePair<Polyline, List<Polyline>> plInfo, List<Line> parkingLines, List<BlockReference> broadcasts)
        {
            if (broadcasts.Count <= 0)
            {
                return;
            }

            //分类车道线，将车道分为主车道和副车道
            var parkingLinesService = new ParkingLinesService();
            var mainPLines = parkingLinesService.CreateNodedParkingLines(plInfo.Key, parkingLines, out List<List<Line>> otherPLines);
            
            //将车道线做成polyline
            var mainParkingPolys = mainPLines.Select(x => parkingLinesService.CreateParkingLineToPolyline(x)).ToList();
            var otherParkingPolys = otherPLines.Select(x => parkingLinesService.CreateParkingLineToPolyline(x)).ToList();
            
            //找到主车道上布置的广播
            var mainParkingPolysDic = GetBroadcastWithParkingLine(mainParkingPolys, broadcasts)
                .Where(x => x.Value != null && x.Value.Count > 0)
                .ToDictionary(x => x.Key, y => y.Value);
            var otherParkingPolysDic = GetBroadcastWithParkingLine(otherParkingPolys, broadcasts);

            //连接车道线广播
            ConnectBroadcastService connectBroadcastService = new ConnectBroadcastService();
            connectBroadcastService.ConnectBroadcast(plInfo, mainParkingPolysDic, otherParkingPolysDic);

            //var s = mainParkingPolysDic.ToDictionary(x => x.Key, y => y.Value.Select(z => new BroadcastModel(z)).ToList());
            ////主车道上的连管
            //MainLanesConnectPipeSrevice mainLanesConnectPipe = new MainLanesConnectPipeSrevice();
            //mainLanesConnectPipe.ConnectPipe(s);
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
            var broads = broadcasts.Where(x => bufferPoly.Contains(x.Position)).ToList();
            var dir = (polyline.EndPoint - polyline.StartPoint).GetNormal();
            var otherDir = Vector3d.ZAxis.CrossProduct(dir);
            
            return broads.Where(x =>
            {
                var broadcastDir = -x.BlockTransform.CoordinateSystem3d.Xaxis.GetNormal();
                double yDotValue = broadcastDir.DotProduct(otherDir);
                double xDotValue = broadcastDir.DotProduct(dir);
                if (Math.Abs(yDotValue) < Math.Abs(xDotValue))
                {
                    return true;
                }

                return false;
            }).ToList();
        }
    }
}
