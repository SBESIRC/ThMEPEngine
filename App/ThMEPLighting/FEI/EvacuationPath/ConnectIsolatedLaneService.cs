using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.FEI.Model;
using ThMEPLighting.FEI.Service;

namespace ThMEPLighting.FEI.EvacuationPath
{
    public class ConnectIsolatedLaneService
    {
        readonly double tol = 10.0;
        public List<ExtendLineModel> ConnectIsolatedLane(List<ExtendLineModel> extendLines, List<List<Line>> lanes, Polyline frame, List<Polyline> holes)
        {
            List<ExtendLineModel> resExtendLines = new List<ExtendLineModel>();

            //寻找孤立车道线
            List<Polyline> checkConnectPoly = extendLines.Select(x => x.line).ToList();
            var isolatedLanes = FindIsolatedLane(checkConnectPoly, lanes, out List<List<Line>> otherLanes);

            while (isolatedLanes.Count > 0)
            {
                //找到所有连接点
                var allConnectPts = extendLines.SelectMany(x => 
                    x.priority == Priority.startExtendLine ? new List<Point3d>() { x.line.EndPoint } : new List<Point3d>() { x.line.StartPoint, x.line.EndPoint })
                    .ToList();
                allConnectPts.AddRange(otherLanes.SelectMany(x => x.SelectMany(y => new List<Point3d>() { y.StartPoint, y.EndPoint })));
                if (allConnectPts.Count() <= 0)
                {
                    break;
                }

                //找到最近的孤立车道线信息
                var closetLaneInfo = FindClosetIsolateLane(isolatedLanes, allConnectPts);

                //连接孤立车道线
                CreateExtendLineWithAStarService createExtendLine = new CreateExtendLineWithAStarService();
                var startExtendLines = createExtendLine.CreateStartLines(frame, closetLaneInfo.Value.Item1, closetLaneInfo.Value.Item2, holes);
                startExtendLines.ForEach(x => x.priority = Priority.firstLevel);
                resExtendLines.AddRange(startExtendLines);

                //更新孤立车道线信息(更新剩下的孤立车道线和新的连接点)
                isolatedLanes.Remove(closetLaneInfo.Key);
                allConnectPts.AddRange(startExtendLines.SelectMany(x => new List<Point3d>() { x.line.StartPoint, x.line.EndPoint }));
            }

            return resExtendLines;
        }

        /// <summary>
        /// 找到离当前布置信息最近的车道线信息
        /// </summary>
        /// <param name="isolatedLanes"></param>
        /// <param name="connectPts"></param>
        /// <returns></returns>
        private KeyValuePair<List<Line>, Tuple<Line, Point3d, double>> FindClosetIsolateLane(List<List<Line>> isolatedLanes, List<Point3d> connectPts)
        {
            var closetLaneInfo = isolatedLanes.ToDictionary(x => x, y =>
            {
                double minDis = double.MaxValue;
                Point3d? closetPt = null;
                Line line = null;
                foreach (var pt in connectPts)
                {
                    var closetInfo = y.ToDictionary(x => x, z => z.GetClosestPointTo(pt, false).DistanceTo(pt))
                                      .OrderBy(x => x.Value)
                                      .First();
                    if (closetInfo.Value < minDis)
                    {
                        closetPt = pt;
                        minDis = closetInfo.Value;
                        line = closetInfo.Key;
                    }
                }

                return Tuple.Create(line, closetPt.Value, minDis);
            })
            .OrderBy(x => x.Value.Item3)
            .First();

            return closetLaneInfo;
        }

        /// <summary>
        /// 寻找孤立车道线
        /// </summary>
        /// <param name="extendLines"></param>
        /// <param name="lanes"></param>
        /// <param name="otherLanes"></param>
        /// <returns></returns>
        private List<List<Line>> FindIsolatedLane(List<Polyline> allExtendLines, List<List<Line>> lanes, out List<List<Line>> otherLanes)
        {
            otherLanes = new List<List<Line>>();

            foreach (Polyline line in allExtendLines)
            {
                if (lanes.Count <= 0)
                {
                    break;
                }

                otherLanes.AddRange(FindInsectLanes(line, lanes));
            }

            List<List<Line>> isolatedLanes = new List<List<Line>>(lanes);
            return isolatedLanes;
        }

        /// <summary>
        /// 找到所有相交车道线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="lanes"></param>
        /// <returns></returns>
        private List<List<Line>> FindInsectLanes(Polyline line, List<List<Line>> lanes)
        {
            List<List<Line>> otherLanes = new List<List<Line>>();

            var laneLines = lanes.SelectMany(x => x).ToList();
            var bufferLine = line.BufferPL(tol)[0] as Polyline;
            var res = SelectService.SelelctCrossing(laneLines, bufferLine);
            if (res.Count > 0)
            {
                var checkLanes = lanes.Where(x => x.Where(y => res.Contains(y)).Count() > 0).ToList();
                otherLanes.AddRange(checkLanes);
                lanes.RemoveAll(x => checkLanes.Contains(x));

                ParkingLinesService parkingLines = new ParkingLinesService();
                foreach (var lane in checkLanes)
                {
                    Polyline poly = parkingLines.CreateParkingLineToPolyline(lane);
                    otherLanes.AddRange(FindInsectLanes(poly, lanes));
                }
            }

            return otherLanes;
        }
    }
}
