using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Data;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class ConenctOutdoorPipeService
    {
        Polyline frame;                                     //最大框线
        List<Line> sewageLines;                             //排水主管
        List<Line> rainLines;                               //雨水主管
        List<Polyline> wallPolys;                           //墙线
        readonly double lineDis = 210;                      //连接线区域范围
        readonly double step = 50;                          //步长
        readonly double lineWieght = 5;                     //连接线区域权重
        public ConenctOutdoorPipeService(Polyline _frame, List<Line> sewagePolys, List<Line> rainPolys, List<Polyline> walls) 
        {
            sewageLines = sewagePolys;
            rainLines = rainPolys;
            frame = _frame;
            wallPolys = walls;
        }

        /// <summary>
        /// 连接主管（连接房间外的各种管线）
        /// </summary>
        /// <param name="mainPipes"></param>
        /// <returns></returns>
        private List<RouteModel> RoutingOutPipe(List<VerticalPipeModel> outPipes, List<RouteModel> routes)
        {
            var resRoutes = new List<RouteModel>();
            var holeConnectLines = CreateRouteHelper.CreateConnectLineHoles(routes.Select(x => x.route).ToList(), lineDis);
            foreach (var pipe in outPipes)
            {
                var allLines = sewageLines;
                if (pipe.PipeType == VerticalPipeType.rainPipe || pipe.PipeType == VerticalPipeType.CondensatePipe)
                {
                    allLines = rainLines;
                }
                if (allLines.Count <= 0 || !frame.Contains(pipe.Position))
                {
                    continue;
                }

                var closetLine = CreateRouteHelper.GetClosetLane(allLines, pipe.Position, frame, wallPolys, step);
                CreateConnectPipesService connectPipesService = new CreateConnectPipesService(step, new Dictionary<Vector3d, List<Line>>());
                Dictionary<List<Polyline>, double> weightHoles = new Dictionary<List<Polyline>, double>();
                weightHoles.Add(wallPolys, double.MaxValue);
                weightHoles.Add(CreateRouteHelper.CreateOtherPipeHoles(outPipes, pipe, closetLine.Key, step), double.MaxValue);
                weightHoles.Add(holeConnectLines, lineWieght);
                var needFrame = HandleStructService.GetNeedFrame(closetLine.Key, pipe.Position);
                var connectLine = connectPipesService.CreatePipes(needFrame, closetLine.Key, pipe.Position, weightHoles);
                holeConnectLines.AddRange(CreateRouteHelper.CreateConnectLineHoles(connectLine, lineDis));
                foreach (var line in connectLine)
                {
                    RouteModel route = new RouteModel(line, pipe.PipeType, pipe.Position, pipe.IsEuiqmentPipe);
                    if (pipe.IsEuiqmentPipe)
                    {
                        route.printCircle = pipe.PipeCircle;
                    }
                    else
                    {
                        route.originCircle = pipe.PipeCircle;
                    }
                    route.connecLine = closetLine.Key;
                    resRoutes.Add(route);
                }
            }

            return resRoutes;
        }

        /// <summary>
        /// 根据通过的出户框线分类点位
        /// </summary>
        /// <param name="outFrame"></param>
        /// <param name="pipes"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        private Dictionary<Polyline, List<VerticalPipeModel>> ClassifyPipeByOutFrame(List<Polyline> outFrame, List<VerticalPipeModel> pipes, Polyline frame)
        {
            var resPipes = new Dictionary<Polyline, List<VerticalPipeModel>>();
            if (outFrame.Count == 1)
            {
                resPipes.Add(outFrame.First(), pipes);
                return resPipes;
            }
            var outFrameDic = outFrame.ToDictionary(x => x.GetAllLineByPolyline().OrderBy(z => z.Length).First(), y => y);
            var outFrameLines = outFrameDic.Select(x => x.Key).ToList();
            foreach (var pipe in pipes)
            {
                var closeLine = CreateRouteHelper.GetClosetLane(outFrameLines, pipe.Position, frame, wallPolys, step).Key;
                var resKey = outFrameDic[closeLine];
                if (resPipes.Keys.Contains(resKey))
                {
                    resPipes[resKey].Add(pipe);
                }
                else
                {
                    resPipes.Add(resKey, new List<VerticalPipeModel>() { pipe });
                }
            }

            return resPipes;
        }
    }
}
