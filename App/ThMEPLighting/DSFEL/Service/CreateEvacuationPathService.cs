using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.AStarRoutingEngine.RoutePlannerService;
using ThMEPLighting.DSFEL.Model;

namespace ThMEPLighting.DSFEL.Service
{
    public class CreateEvacuationPathService
    {
        double distance = 400;
        public List<RoomInfoModel> CreatePath(List<ExitModel> exits, List<Line> centerLines, List<Polyline> holes)
        {
            var roomModels = new List<RoomInfoModel>();
            foreach (var exitInfo in exits.GroupBy(x => x.room))
            {
                var room = exitInfo.Key;
                var roomCenterLines = GetCenterLinesByRoom(centerLines, room);
                if (roomCenterLines.Count <= 0)
                {
                    continue;
                }

                RoomInfoModel roomInfoModel = new RoomInfoModel();
                roomInfoModel.room = room;
                roomInfoModel.exitModels = exitInfo.Select(x => x).ToList();
                roomInfoModel.evacuationPaths = new List<Line>();
                foreach (var exit in exitInfo)
                {
                    var closetLine = roomCenterLines.OrderBy(x => x.GetClosestPointTo(exit.positin, false).DistanceTo(exit.positin)).First();
                    var dir = (closetLine.EndPoint - closetLine.StartPoint).GetNormal();
                    Point3d sp = CreateDistancePoint(room, exit.positin);
                    AStarRoutePlanner<Line> aStarRoute = new AStarRoutePlanner<Line>(room, dir, closetLine, 100, 100, 100);
                    aStarRoute.SetObstacle(holes);
                    var path = aStarRoute.Plan(sp);

                    if (path != null)
                    {
                        path.AddVertexAt(0, exit.positin.ToPoint2D(), 0, 0, 0);
                        roomInfoModel.evacuationPaths.AddRange(TransPolylineToLine(path));
                    }
                }
                roomModels.Add(roomInfoModel);
            }

            return roomModels;
        }

        /// <summary>
        /// 获取polyline上的所有线
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<Line> TransPolylineToLine(Polyline polyline)
        {
            List<Line> lines = new List<Line>();
            for (int i = 0; i < polyline.NumberOfVertices - 1; i++)
            {
                var line = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices));
                if (line.Length > 0)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        /// <summary>
        /// 找到当前房间内的中心线
        /// </summary>
        /// <param name="centerLines"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        private List<Line> GetCenterLinesByRoom(List<Line> centerLines, Polyline room)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(centerLines.ToCollection());
            var roomLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room).Cast<Line>().ToList();

            return roomLines;
        }


        /// <summary>
        /// 计算起始点离外框线大于800距离
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="blockPt"></param>
        /// <returns></returns>
        private Point3d CreateDistancePoint(Polyline frame, Point3d blockPt)
        {
            Point3d resPt = blockPt;
            int i = 0;
            while (i <= 4)
            {
                i++;
                var closetPt = frame.GetClosestPointTo(resPt, false);
                var ptDistance = resPt.DistanceTo(closetPt);
                if (ptDistance >= distance)
                {
                    break;
                }

                var moveDir = (resPt - closetPt).GetNormal();
                resPt = resPt + moveDir * (distance - ptDistance);
            }

            return resPt;
        }
    }
}
