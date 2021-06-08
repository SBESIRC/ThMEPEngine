﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPElectrical.VideoMonitoringSystem.Model;
using ThMEPElectrical.VideoMonitoringSystem.Utls;
using ThMEPEngineCore.Algorithm.DijkstraAlgorithm;

namespace ThMEPElectrical.VideoMonitoringSystem.VMExitLayoutService
{
    public class LayoutVideoByLine
    {
        public double distance = 500;
        double tol = 10;

        public List<KeyValuePair<Point3d, Vector3d>> Layout(List<Line> lanes, List<Polyline> doors, List<Polyline> rooms)
        {
            List<KeyValuePair<Point3d, Vector3d>> resLayout = new List<KeyValuePair<Point3d, Vector3d>>();
            foreach (var room in rooms)
            {
                //获取需要的构建信息
                var bufferRoom = room.Buffer(tol)[0] as Polyline;
                var needDoors = GetNeedDoors(doors, bufferRoom);
                var doorPts = needDoors.Select(x => x.GetRectangleCenterPt()).ToList();
                var nLanes = GetNeedLanes(lanes, room);

                //计算延申线（用于计算最短路径）
                var extendLines = CreateExtendLines(doorPts, lanes);
                var allLines = new List<Line>(lanes);
                allLines.AddRange(extendLines);
                allLines = UtilService.GetNodedLines(allLines, room);

                //获取布置点位信息
                var layoutInfo = GetLayoutPts(lanes);

                //计算布置点朝向
                var layoutPtInfo = CalLayoutPtDir(layoutInfo, allLines, doorPts);

                resLayout.AddRange(layoutPtInfo);
            }

            return resLayout;
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
                var linePtInfo = ptInf.Where(x => UtilService.CheckPointIsOnLine(info.Key, x.Key, 1)).ToList();
                if (linePtInfo.Count < 0)
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
            var pts = allLines.GetAllPoints();
            foreach (var pt in pts)
            {
                var pathInfo = dijkstra.FindingAllPathMinNodeLength(pt)
                    .Where(x => doorPts.Any(y => y.IsEqualTo(x.Key, new Tolerance(1, 1))))
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
                for (int i = 1; i <= num; i++)
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
                Line line = lanes.OrderBy(x => x.GetClosestPointTo(pt, false)).First();
                extendLines.Add(new Line(pt, line.GetClosestPointTo(pt, false)));
            }

            return extendLines;
        }

        /// <summary>
        /// 找到需要的与房间相交的门
        /// </summary>
        /// <param name="doors"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        private List<Polyline> GetNeedDoors(List<Polyline> doors, Polyline room)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(doors.ToCollection());
            return thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room).Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 获取空间内的车道线或者中心线
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        private List<Line> GetNeedLanes(List<Line> lanes, Polyline room)
        {
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(lanes.ToCollection());
            var needLanes = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(room).Cast<Polyline>().ToList();
            return needLanes.SelectMany(x => room.Trim(x).Cast<Line>().ToList()).ToList();
        }
    }
}
