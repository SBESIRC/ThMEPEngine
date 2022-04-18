using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.BFSAlgorithm;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class CreateDrainagePipeRoute
    {
        Polyline frame;                                     //外框线
        List<Polyline> mainSewagePipes;                     //排水主管
        List<Polyline> mainRainPipes;                       //雨水主管
        List<VerticalPipeModel> verticalPipes;              //排雨水立管
        List<Polyline> wallPolys;                           //墙线
        List<Polyline> outUserPoly;                         //出户框线
        Dictionary<Polyline, List<string>> rooms;           //房间框线
        List<Curve> gridLines;                              //轴网线
        ParamSettingViewModel paramSetting = null;          //参数
        readonly double step = 50;                          //步长
        readonly double lineDis = 210;                      //连接线区域范围
        readonly double lineWieght = 5;                     //连接线区域权重
        double angleTolerance = 1 * Math.PI / 180.0;
        public CreateDrainagePipeRoute(Polyline polyline, List<Polyline> sewagePolys, List<Polyline> rainPolys, List<VerticalPipeModel> verticalPipesModel, List<Polyline> walls, 
            List<Curve> grids, List<Polyline> _outUserPoly, Dictionary<Polyline, List<string>> _rooms, ParamSettingViewModel _paramSetting)
        {
            frame = polyline;
            mainSewagePipes = sewagePolys;
            mainRainPipes = rainPolys;
            verticalPipes = verticalPipesModel;
            wallPolys = walls;
            gridLines = grids;
            outUserPoly = _outUserPoly;
            rooms = _rooms;
            paramSetting = _paramSetting;
        }

        /// <summary>
        /// 计算路由
        /// </summary>
        /// <returns></returns>
        public List<RouteModel> Routing()
        {
            var resRoutes = new List<RouteModel>();
            RoomPolyService roomPolyService = new RoomPolyService();
            var roomDeep = roomPolyService.GetRoomDeep(rooms, outUserPoly);

            HandleConfluenceService handleConfluenceService = new HandleConfluenceService(paramSetting, verticalPipes, roomDeep);
            handleConfluenceService.GetMainPolyVerticalPipe();
            foreach (var pipeTuple in handleConfluenceService.pipeTuples)
            {
                var mainPipes = new List<VerticalPipeModel>(pipeTuple.Item1);
                mainPipes.Add(pipeTuple.Item2);
                mainPipes.AddRange(pipeTuple.Item5);
                mainPipes = mainPipes.Where(x => x != null).ToList();
                var routing = RoutingMainPipe(mainPipes);
                ReprocessingPipe reprocessingPipe = new ReprocessingPipe(routing, outUserPoly);     //后处理间距
                routing = reprocessingPipe.Reprocessing();

                if (paramSetting.SingleRowSetting == SingleRowSettingEnum.DrawDetail)
                {
                    if (paramSetting.SewageWasteWater == SewageWasteWaterEnum.Confluence)
                    {
                        var otherPipes = new List<VerticalPipeModel>(pipeTuple.Item3);
                        otherPipes.AddRange(pipeTuple.Item4);
                        var mainRoute = routing.Where(x => x.startPosition.DistanceTo(pipeTuple.Item1.First().Position) < 0.01).FirstOrDefault();
                        resRoutes.AddRange(handleConfluenceService.ConnectPipe(frame, otherPipes, wallPolys, mainRoute, pipeTuple.Item6, outUserPoly));
                    }
                    else if (paramSetting.SewageWasteWater == SewageWasteWaterEnum.Diversion)
                    {
                        var mainWasteRoute = routing.Where(x => x.startPosition.DistanceTo(pipeTuple.Item1.First().Position) < 0.01).FirstOrDefault();
                        resRoutes.AddRange(handleConfluenceService.ConnectPipe(frame, pipeTuple.Item3, wallPolys, mainWasteRoute, pipeTuple.Item6, outUserPoly));

                        var mainSewageRoute = routing.Where(x => x.startPosition.DistanceTo(pipeTuple.Item2.Position) < 0.01).FirstOrDefault();
                        resRoutes.AddRange(handleConfluenceService.ConnectPipe(frame, pipeTuple.Item4, wallPolys, mainSewageRoute, pipeTuple.Item6, outUserPoly));
                    }
                }
                else if (paramSetting.SingleRowSetting == SingleRowSettingEnum.ReservedPlug)
                {
                    if (pipeTuple.Item1 != null)
                    {
                        CreateReservedPlug(routing, pipeTuple.Item1);
                    }
                }
                
                resRoutes.AddRange(routing);
            }
            resRoutes.AddRange(RoutingOutPipe(handleConfluenceService.otherOutPoly, resRoutes));

            return resRoutes;
        }

        /// <summary>
        /// 计算堵头
        /// </summary>
        /// <param name="routes"></param>
        /// <param name="point"></param>
        private void CreateReservedPlug(List<RouteModel> routes, List<VerticalPipeModel> pipes)
        {
            foreach (var pipe in pipes)
            {
                var point = pipe.Position;
                var mainWasteRoute = routes.Where(x => x.startPosition.DistanceTo(point) < 0.01).FirstOrDefault();
                if (mainWasteRoute != null)
                {
                    var poly = mainWasteRoute.route;
                    var frame = GeometryUtils.FindOutFrame(poly, outUserPoly, point, false);
                    var intersectLine = GeometryUtils.FindRouteIntersectLine(poly, frame);
                    var sp = intersectLine.StartPoint.DistanceTo(point) > intersectLine.EndPoint.DistanceTo(point) ? intersectLine.StartPoint : intersectLine.EndPoint;
                    var pts = new Point3dCollection();
                    intersectLine.IntersectWith(frame, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
                    if (pts.Count > 0)
                    {
                        var lastPt = pts.Cast<Point3d>().OrderByDescending(x => x.DistanceTo(sp)).FirstOrDefault();
                        var dir = (lastPt - sp).GetNormal();
                        var ep = lastPt + dir * 200;
                        var resPoly = GeometryUtils.GetBreakLine(poly, sp, ep);
                        mainWasteRoute.route = resPoly;
                    }
                }
            }
        }

        /// <summary>
        /// 连接主管（连接污水、废水主管和立管）
        /// </summary>
        /// <param name="mainPipes"></param>
        /// <returns></returns>
        private List<RouteModel> RoutingMainPipe(List<VerticalPipeModel> mainPipes)
        {
            var resRoutes = new List<RouteModel>();
            var sewageLines = mainSewagePipes.SelectMany(x => x.GetAllLineByPolyline()).ToList();
            var rainLines = mainRainPipes.SelectMany(x => x.GetAllLineByPolyline()).ToList();
            var connecLines = new List<Line>(sewageLines);
            connecLines.AddRange(rainLines);
            var gridInfo = ClassifyGridInfo();
            var holeConnectLines = new List<Polyline>();

            var connectPipes = OrderPipeConnect(connecLines, mainPipes);
            foreach (var pipe in connectPipes)
            {
                var allLines = sewageLines;
                if (pipe.PipeType == VerticalPipeType.rainPipe || pipe.PipeType == VerticalPipeType.CondensatePipe)
                {
                    //allLines = rainLines;
                }
                if (allLines.Count <= 0)
                {
                    continue;
                }
                var closetLine = GetClosetLane(allLines, pipe.Position, frame);
                CreateConnectPipesService connectPipesService = new CreateConnectPipesService(step, gridInfo);
                Dictionary<List<Polyline>, double> weightHoles = new Dictionary<List<Polyline>, double>();
                weightHoles.Add(wallPolys, double.MaxValue);
                weightHoles.Add(CreateOtherPipeHoles(connectPipes, pipe, closetLine.Key), double.MaxValue);
                weightHoles.Add(holeConnectLines, lineWieght);
                var connectLine = connectPipesService.CreatePipes(frame, closetLine.Key, pipe.Position, weightHoles);
                holeConnectLines.AddRange(CreateConnectLineHoles(connectLine));
                foreach (var line in connectLine)
                {
                    RouteModel route = new RouteModel(line, pipe.PipeType, pipe.Position);
                    if (pipe.IsEuiqmentPipe)
                    {
                        route.printCircle = pipe.PipeCircle;
                    }
                    route.connecLine = closetLine.Key;
                    resRoutes.Add(route);
                }
            }

            return resRoutes;
        }

        /// <summary>
        /// 连接主管（连接房间外的各种管线）
        /// </summary>
        /// <param name="mainPipes"></param>
        /// <returns></returns>
        private List<RouteModel> RoutingOutPipe(List<VerticalPipeModel> outPipes, List<RouteModel> routes)
        {
            var resRoutes = new List<RouteModel>();
            var sewageLines = mainSewagePipes.SelectMany(x => x.GetAllLineByPolyline()).ToList();
            var rainLines = mainRainPipes.SelectMany(x => x.GetAllLineByPolyline()).ToList();
            var holeConnectLines = routes.Select(x => x.route).ToList();
            foreach (var pipe in outPipes)
            {
                var allLines = sewageLines;
                if (pipe.PipeType == VerticalPipeType.rainPipe || pipe.PipeType == VerticalPipeType.CondensatePipe)
                {
                    //allLines = rainLines;
                }
                if (allLines.Count <= 0)
                {
                    continue;
                }
                var closetLine = GetClosetLane(allLines, pipe.Position, frame);
                CreateConnectPipesService connectPipesService = new CreateConnectPipesService(step, new Dictionary<Vector3d, List<Line>>());
                Dictionary<List<Polyline>, double> weightHoles = new Dictionary<List<Polyline>, double>();
                weightHoles.Add(wallPolys, double.MaxValue);
                weightHoles.Add(CreateOtherPipeHoles(outPipes, pipe, closetLine.Key), double.MaxValue);
                weightHoles.Add(holeConnectLines, lineWieght);
                var connectLine = connectPipesService.CreatePipes(frame, closetLine.Key, pipe.Position, weightHoles);
                holeConnectLines.AddRange(CreateConnectLineHoles(connectLine));
                foreach (var line in connectLine)
                {
                    RouteModel route = new RouteModel(line, pipe.PipeType, pipe.Position);
                    if (pipe.IsEuiqmentPipe)
                    {
                        route.printCircle = pipe.PipeCircle;
                    }
                    route.connecLine = closetLine.Key;
                    resRoutes.Add(route);
                }
            }

            return resRoutes;
        }

        /// <summary>
        /// 创建连接线加权区域
        /// </summary>
        /// <param name="polylines"></param>
        /// <returns></returns>
        private List<Polyline> CreateConnectLineHoles(List<Polyline> polylines)
        {
            var resLines = new List<Polyline>();
            foreach (var polyline in polylines)
            {
                resLines.AddRange(polyline.BufferPL(lineDis).Cast<Polyline>().ToList());
            }
            return resLines;
        }

        /// <summary>
        /// 将其他点创建成洞口（不允许通过其他点）
        /// </summary>
        /// <param name="pipes"></param>
        /// <param name="thisPipe"></param>
        /// <param name="closeLine"></param>
        /// <returns></returns>
        private List<Polyline> CreateOtherPipeHoles(List<VerticalPipeModel> pipes, VerticalPipeModel thisPipe, Line closeLine)
        {
            var dir = (closeLine.EndPoint - closeLine.StartPoint).GetNormal();
            var otherPipes = pipes.Except(new List<VerticalPipeModel>() { thisPipe }).ToList();
            var pipeHoles = new List<Polyline>();
            foreach (var pipe in otherPipes)
            {
                pipeHoles.Add(pipe.Position.CreatePolylineByPt(step / 2, dir));
            }
            
            return pipeHoles;
        }

        /// <summary>
        /// 排序连接顺序
        /// </summary>
        /// <param name="allLines"></param>
        /// <returns></returns>
        private List<VerticalPipeModel> OrderPipeConnect(List<Line> allLines, List<VerticalPipeModel> mainPipes)
        {
            var longLine = allLines.OrderByDescending(x => x.Length).FirstOrDefault();
            if (longLine != null)
            {
                var closePoly = outUserPoly.OrderBy(x => x.Distance(longLine)).FirstOrDefault();
                if (closePoly != null)
                {
                    var dir = StructGeoService.GetPolylineDir(closePoly);
                    var matrix = GeometryUtils.GetMatrix((longLine.EndPoint - longLine.StartPoint).GetNormal());
                    var classifyMatrix = GeometryUtils.GetMatrix(dir);
                    var polyPts = closePoly.GetAllLineByPolyline().SelectMany(x => new List<Point3d>() { x.StartPoint, x.EndPoint }).Select(x => x.TransformBy(classifyMatrix.Inverse())).ToList();
                    double minX = polyPts.OrderBy(x => x.X).First().X;
                    double maxX = polyPts.OrderByDescending(x => x.X).First().X;
                    var classifyPipeDic = mainPipes.ToDictionary(x => x, y => y.Position.TransformBy(classifyMatrix.Inverse())).OrderBy(x => x.Value.X).ToDictionary(x => x.Key, y => y.Value);
                    var orderPipeDic = mainPipes.ToDictionary(x => x, y => y.Position.TransformBy(matrix.Inverse())).OrderBy(x => x.Value.X).ToDictionary(x => x.Key, y => y.Value);
                    var indexX = minX + 200;
                    var midIndec = (minX + maxX) / 2;
                    var leftPipes = new Dictionary<VerticalPipeModel, Point3d>();
                    var rightPipes = new Dictionary<VerticalPipeModel, Point3d>();
                    foreach (var pipe in classifyPipeDic)
                    {
                        if (pipe.Value.X < indexX || pipe.Value.X < midIndec)
                        {
                            leftPipes.Add(pipe.Key, orderPipeDic[pipe.Key]);
                            indexX += 300;
                            midIndec -= 300;
                        }
                        else
                        {
                            rightPipes.Add(pipe.Key, orderPipeDic[pipe.Key]);
                        }
                    }

                    var resPipes = new List<VerticalPipeModel>();
                    resPipes.AddRange(OrderDistance(matrix, leftPipes, longLine, true));
                    resPipes.AddRange(OrderDistance(matrix, rightPipes, longLine, false));
                    return resPipes;
                }
            }

            return mainPipes;
        }

        /// <summary>
        /// 根据方向按距离排序管线连接顺序
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="pipes"></param>
        /// <param name="polyline"></param>
        /// <returns></returns>
        private List<VerticalPipeModel> OrderDistance(Matrix3d matrix, Dictionary<VerticalPipeModel, Point3d> pipes, Line polyline, bool isLeft)
        {
            if (pipes.Count <= 0)
            {
                return new List<VerticalPipeModel>();
            }
            var pipePt = pipes.First().Key.Position;
            var checkDir = (pipePt - polyline.GetClosestPointTo(pipePt, true)).GetNormal();
            if (checkDir.DotProduct(matrix.CoordinateSystem3d.Yaxis) < 0)
            {
                if (isLeft)
                    return pipes.OrderByDescending(x => Math.Floor(x.Value.Y)).ThenBy(x => Math.Floor(x.Value.X)).Select(x => x.Key).ToList();
                else
                    return pipes.OrderByDescending(x => Math.Floor(x.Value.Y)).ThenByDescending(x => Math.Floor(x.Value.X)).Select(x => x.Key).ToList();
            }
            else
            {
                if (isLeft)
                    return pipes.OrderBy(x => Math.Floor(x.Value.Y)).ThenBy(x => Math.Floor(x.Value.X)).Select(x => x.Key).ToList();
                else
                    return pipes.OrderBy(x => Math.Floor(x.Value.Y)).ThenByDescending(x => Math.Floor(x.Value.X)).Select(x => x.Key).ToList();
            }
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

        /// <summary>
        /// 计算轴网方向信息并分类
        /// </summary>
        /// <returns></returns>
        private Dictionary<Vector3d, List<Line>> ClassifyGridInfo()
        {
            var lineGrids = new List<Line>();
            foreach (var grid in gridLines)
            {
                if (grid is Polyline polyline)
                {
                    lineGrids.AddRange(polyline.GetAllLineByPolyline());
                }
                else if (grid is Line line)
                {
                    lineGrids.Add(line);
                }
            }

            var lineGroup = new Dictionary<Vector3d, List<Line>>();
            foreach (var line in lineGrids)
            {
                var dir = (line.EndPoint - line.StartPoint).GetNormal();
                var compareKey = lineGroup.Keys.Where(x =>
                {
                    var angle = x.GetAngleTo(dir);
                    angle %= Math.PI;
                    if (angle <= angleTolerance || angle >= Math.PI - angleTolerance)
                    {
                        return true;//平行
                    }
                    return false;
                }).ToList();
                if (compareKey.Count > 0)
                {
                    lineGroup[compareKey.First()].Add(line);
                }
                else
                {
                    lineGroup.Add(dir, new List<Line>() { line });
                }
            }
            return lineGroup;
        }
    }
}