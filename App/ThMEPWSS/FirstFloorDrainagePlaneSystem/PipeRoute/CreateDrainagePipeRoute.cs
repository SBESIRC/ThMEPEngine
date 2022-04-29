using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.BFSAlgorithm;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Data;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class CreateDrainagePipeRoute
    {
        Polyline frame;                                     //最大框线
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
        //double angleTolerance = 1 * Math.PI / 180.0;
        public CreateDrainagePipeRoute(List<Polyline> sewagePolys, List<Polyline> rainPolys, List<VerticalPipeModel> verticalPipesModel, List<Polyline> walls, 
            List<Curve> grids, List<Polyline> _outUserPoly, Dictionary<Polyline, List<string>> _rooms, ParamSettingViewModel _paramSetting)
        {
            mainSewagePipes = sewagePolys;
            mainRainPipes = rainPolys;
            verticalPipes = verticalPipesModel;
            gridLines = grids;
            outUserPoly = _outUserPoly;
            rooms = _rooms;
            paramSetting = _paramSetting;
            frame = HandleStructService.GetMaxFrame(rooms.Select(x => x.Key).ToList(), mainSewagePipes, mainRainPipes);
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(walls.ToCollection());
            wallPolys = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(frame).Cast<Polyline>().ToList();
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
                // 连接主管（连接污水、废水主管和立管）
                ConnectMainPipeService connectMainPipeService = new ConnectMainPipeService(frame, mainSewagePipes, mainRainPipes, gridLines, outUserPoly, wallPolys, step, lineWieght);
                var routing = connectMainPipeService.Connect(mainPipes, pipeTuple.Item6);

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
            resRoutes.AddRange(RoutingOutPipe(handleConfluenceService.otherOutPoly, resRoutes)); //连接户外管

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
                    var outFrame = GeometryUtils.FindOutFrame(poly, outUserPoly, point, false);
                    var intersectLine = GeometryUtils.FindRouteIntersectLine(poly, outFrame);
                    var sp = intersectLine.StartPoint.DistanceTo(point) > intersectLine.EndPoint.DistanceTo(point) ? intersectLine.StartPoint : intersectLine.EndPoint;
                    var pts = new Point3dCollection();
                    intersectLine.IntersectWith(outFrame, Intersect.OnBothOperands, pts, (IntPtr)0, (IntPtr)0);
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
                    route.connecLine = closetLine.Key;
                    resRoutes.Add(route);
                }
            }

            return resRoutes;
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
    }
}