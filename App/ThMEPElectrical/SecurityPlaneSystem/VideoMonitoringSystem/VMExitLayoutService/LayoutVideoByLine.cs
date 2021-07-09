using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.SecurityPlaneSystem.Utls;
using ThMEPElectrical.SecurityPlaneSystem.VideoMonitoringSystem.Model;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Algorithm.DijkstraAlgorithm;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.VideoMonitoringSystem.VMExitLayoutService
{
    public class LayoutVideoByLine
    {
        public double distance = 5000;
        double tol = 10;

        public List<LayoutModel> Layout(List<Line> lanes, List<Polyline> doors, ThIfcRoom thRoom)
        {
            List<LayoutModel> models = new List<LayoutModel>();
            GetLayoutStructureService getLayoutStructureService = new GetLayoutStructureService();
            var room = thRoom.Boundary as Polyline;
            //获取需要的构建信息
            var bufferRoom = room.Buffer(tol)[0] as Polyline;
            var needDoors = getLayoutStructureService.GetNeedDoors(doors, bufferRoom);
            var doorPts = needDoors.Select(x => x.GetRectangleCenterPt()).ToList();
            var nLanes = getLayoutStructureService.GetNeedLanes(lanes, room);
            if (nLanes.Count <= 0)
            {
                return models;
            }

            //计算延申线（用于计算最短路径）
            var extendLines = CreateExtendLines(doorPts, nLanes);
            var allLines = new List<Line>(nLanes);
            allLines.AddRange(extendLines);
            allLines = UtilService.GetNodedLines(allLines, room.Buffer(distance)[0] as Polyline);

            //获取布置点位信息
            var layoutInfo = GetLayoutPts(nLanes);

            //计算布置点朝向
            var layoutPtInfo = CalLayoutPtDir(layoutInfo, allLines, doorPts);

            foreach (var info in layoutPtInfo)
            {
                LayoutModel layoutModel = new LayoutModel();
                layoutModel.layoutPt = info.Key;
                layoutModel.layoutDir = info.Value;
                models.Add(layoutModel);
            }

            return models;
        }

        /// <summary>
        /// 计算布置点朝向
        /// </summary>
        /// <param name="layoutInfo"></param>
        /// <param name="allLines"></param>
        /// <param name="doorPts"></param>
        /// <returns></returns>
        private Dictionary<Point3d, Vector3d> CalLayoutPtDir(Dictionary<Line, List<Point3d>> layoutInfo, List<Line> allLines, List<Point3d> doorPts)
        {
            Dictionary<Point3d, Vector3d> layoutPointInfo = new Dictionary<Point3d, Vector3d>();
            var ptInf = CalPtDistanceToExit(allLines, doorPts);
            foreach (var info in layoutInfo)
            {
                var linePtInfo = ptInf.Where(x => UtilService.CheckPointIsOnLine(info.Key, x.Key, 3)).ToList();
                if (linePtInfo.Count <= 0)
                {
                    continue;
                }

                foreach (var layoutPt in info.Value)
                {
                    var closetPtInfo = linePtInfo.OrderBy(x =>
                    {
                        var dis = x.Key.DistanceTo(layoutPt);
                        return dis + x.Value;
                    }).First();
                    var dir = (closetPtInfo.Key - layoutPt).GetNormal();

                    layoutPointInfo.Add(layoutPt, dir);
                }
            }

            return layoutPointInfo;
        }

        /// <summary>
        /// 计算当前点到最近的出口的距离
        /// </summary>
        /// <param name="allLines"></param>
        /// <param name="doorPts"></param>
        /// <returns></returns>
        private Dictionary<Point3d, double> CalPtDistanceToExit(List<Line> allLines, List<Point3d> doorPts)
        {
            Dictionary<Point3d, double> ptInfo = new Dictionary<Point3d, double>();
            DijkstraAlgorithm dijkstra = new DijkstraAlgorithm(allLines.Cast<Curve>().ToList());
            var pts = allLines.GetAllPoints().OrderBy(x => x.X).ThenBy(y => y.Y).ToList();
            foreach (var pt in pts)
            {
                var pathInfo = dijkstra.FindingAllPathMinNodeLength(pt)
                    .Where(x => doorPts.Any(y => y.IsEqualTo(x.Key, new Tolerance(3, 3))))
                    .OrderBy(x => x.Value)
                    .First();
                ptInfo.Add(pt, pathInfo.Value);
            }

            return ptInfo;
        }

        /// <summary>
        /// 获取布置点位信息
        /// </summary>
        /// <param name="lanes"></param>
        /// <returns></returns>
        private Dictionary<Line, List<Point3d>> GetLayoutPts(List<Line> lanes)
        {
            Dictionary<Line, List<Point3d>> layoutInfo = new Dictionary<Line, List<Point3d>>();
            foreach (var lane in lanes)
            {
                var sp = lane.StartPoint;
                var ep = lane.EndPoint;
                var dir = (ep - sp).GetNormal();
                var num = Math.Ceiling(lane.Length / distance);
                var space = lane.Length / num;

                var firPt = sp + space / 2 * dir;
                List<Point3d> layoutPts = new List<Point3d>() { firPt };
                for (int i = 1; i < num; i++)
                {
                    firPt = firPt + space * dir;
                    layoutPts.Add(firPt);
                }

                layoutInfo.Add(lane, layoutPts);
            }

            return layoutInfo;
        }

        /// <summary>
        /// 创建延申线
        /// </summary>
        /// <param name="doorPts"></param>
        /// <param name="lanes"></param>
        /// <returns></returns>
        private List<Line> CreateExtendLines(List<Point3d> doorPts, List<Line> lanes)
        {
            List<Line> extendLines = new List<Line>();
            foreach (var pt in doorPts)
            {
                Line line = lanes.OrderBy(x => x.GetClosestPointTo(pt, false).DistanceTo(pt)).First();
                extendLines.Add(new Line(pt, line.GetClosestPointTo(pt, false)));
            }

            return extendLines;
        }
    }
}
