﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Algorithm.BFSAlgorithm;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class CreateDrainagePipeRoute
    {
        Polyline frame;                                     //外框线
        List<Polyline> mainSewagePipes;                     //排水主管
        List<Polyline> mainRainPipes;                       //雨水主管
        List<VerticalPipeModel> verticalPipes;              //排雨水立管
        List<Polyline> wallPolys;                           //墙线
        readonly double step = 90;                          //步长
        public CreateDrainagePipeRoute(Polyline polyline, List<Polyline> sewagePolys, List<Polyline> rainPolys, List<VerticalPipeModel> verticalPipesModel, List<Polyline> walls)
        {
            frame = polyline;
            mainSewagePipes = sewagePolys;
            mainRainPipes = rainPolys;
            verticalPipes = verticalPipesModel;
            wallPolys = walls;
        }

        /// <summary>
        /// 计算路由
        /// </summary>
        /// <returns></returns>
        public List<RouteModel> Routing()
        {
            var resRoutes = new List<RouteModel>();
            var sewageLines = mainSewagePipes.SelectMany(x => x.GetAllLineByPolyline()).ToList();
            var rainLines = mainRainPipes.SelectMany(x => x.GetAllLineByPolyline()).ToList();
            foreach (var pipe in verticalPipes)
            {
                var allLines = sewageLines;
                if (pipe.PipeType == VerticalPipeType.rainPipe)
                {
                    allLines = rainLines;
                }
                if (allLines.Count <= 0)
                {
                    continue;
                }
                var closetLine = GetClosetLane(allLines, pipe.Position, frame);
                CreateConnectPipesService connectPipesService = new CreateConnectPipesService(step);
                var connectLine = connectPipesService.CreatePipes(frame, closetLine.Key, pipe.Position, wallPolys);
                foreach (var line in connectLine)
                {
                    RouteModel route = new RouteModel(line, pipe.PipeType, pipe.Position);
                    if (pipe.IsEuiqmentPipe)
                    {
                        route.printCircle = pipe.PipeCircle;
                    }
                    resRoutes.Add(route);
                }
            }

            return resRoutes;
        }

        /// <summary>
        /// 获取最近的线信息
        /// </summary>
        /// <param name="lanes"></param>
        /// <param name="startPt"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private KeyValuePair<Line, Point3d> GetClosetLane(List<Line> lines, Point3d startPt, Polyline polyline)
        {
            var closeInfo = GeometryUtils.GetClosetLine(lines, startPt);
            Line checkLine = new Line(startPt, closeInfo.Value);
            if (!CheckService.CheckIntersectWithFrame(checkLine, polyline))
            {
                var checkDir = (closeInfo.Value - startPt).GetNormal();
                var lineDir = Vector3d.ZAxis.CrossProduct((closeInfo.Key.EndPoint - closeInfo.Key.StartPoint).GetNormal());
                if (checkDir.IsEqualTo(lineDir, new Tolerance(0.001, 0.001)))
                {
                    return closeInfo;
                }
            }

            BFSPathPlaner pathPlaner = new BFSPathPlaner(step);
            var closetLine = pathPlaner.FindingClosetLine(startPt, lines, polyline);
            var closetPt = closetLine.GetClosestPointTo(startPt, false);

            return new KeyValuePair<Line, Point3d>(closetLine, closetPt);
        }
    }
}
