using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.BFSAlgorithm;
using ThMEPEngineCore.CAD;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Data;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class ConnectMainPipeService
    {
        Polyline frame;                                     //最大框线
        List<Polyline> mainSewagePipes;                     //排水主管
        List<Polyline> mainRainPipes;                       //雨水主管
        List<Curve> gridLines;                              //轴网线
        List<Polyline> outUserPoly;                         //出户框线
        List<Polyline> wallPolys;                           //墙线
        double step = 100;                                  //步长
        double lineWieght = 10;                             //连接线区域权重
        readonly double lineDis = 210;                      //连接线区域范围
        readonly double extendLength = 250;                 //连线到出户框线上的管线先延长一定长度（避免贴着出户框线）         
        double angleTolerance = 1 * Math.PI / 180.0;
        public ConnectMainPipeService(Polyline _frame, List<Polyline> sewagePolys, List<Polyline> rainPolys, List<Curve> grids, List<Polyline> _outUserPoly,
            List<Polyline> _wallPolys, double _step, double _lineWieght)
        {
            frame = _frame;
            mainSewagePipes = sewagePolys;
            mainRainPipes = rainPolys;
            gridLines = grids;
            outUserPoly = _outUserPoly;
            wallPolys = _wallPolys;
            step = _step;
            lineWieght = _lineWieght;
        }

        public List<RouteModel> Connect(List<VerticalPipeModel> mainPipes, Dictionary<KeyValuePair<Polyline, List<string>>, int> deepRooms)
        {
            var resRoutes = new List<RouteModel>();         //1.step1：先将管线连接到出户框线上
            var needOutFrames = GetOneDeepOutFrame(deepRooms);          //最后出户的用户画的出乎框线
            if (needOutFrames.Count < 0)
            {
                return resRoutes;
            }
            var roomFrame = HandleStructService.GetMaxFrame(deepRooms.Select(x => x.Key.Key).ToList(), new List<Polyline>(), new List<Polyline>());
            var pipeDic = ClassifyPipeByOutFrame(needOutFrames, mainPipes, roomFrame);
            var gridInfo = ClassifyGridInfo();
            var roomWallPolys = wallPolys.Where(x => x.Intersects(roomFrame) || roomFrame.Contains(x)).ToList();
            var holeConnectLines = new List<Polyline>();
            foreach (var dic in pipeDic)
            {
                var outFrameLines = dic.Key.GetAllLineByPolyline().OrderByDescending(x => x.Length).ToList();
                var closetLine = outFrameLines.First();
                var connectPipes = OrderPipeConnect(closetLine, mainPipes);
                var frameConnectLines = new Dictionary<VerticalPipeModel, Polyline>();
                foreach (var pipe in connectPipes)
                {
                    CreateConnectPipesService connectPipesService = new CreateConnectPipesService(step, gridInfo);
                    Dictionary<List<Polyline>, double> weightHoles = new Dictionary<List<Polyline>, double>();
                    weightHoles.Add(roomWallPolys, double.MaxValue);
                    weightHoles.Add(CreateRouteHelper.CreateOtherPipeHoles(connectPipes, pipe, closetLine, step), double.MaxValue);
                    weightHoles.Add(holeConnectLines, lineWieght);
                    var connectLine = connectPipesService.CreatePipes(roomFrame, closetLine, pipe.Position, weightHoles);
                    holeConnectLines.AddRange(CreateRouteHelper.CreateConnectLineHoles(connectLine, lineDis));
                    if (connectLine.Count > 0)
                    {
                        var line = connectLine.First();
                        if (pipe.Position.DistanceTo(line.EndPoint) < pipe.Position.DistanceTo(line.StartPoint))
                        {
                            line.ReverseCurve();
                        }
                        frameConnectLines.Add(pipe, GeometryUtils.ShortenPolyline(line, -extendLength));
                    }
                }

                ReprocessingPipe reprocessingPipe = new ReprocessingPipe(frameConnectLines, outUserPoly);     //后处理间距
                frameConnectLines = reprocessingPipe.Reprocessing();

                //step2：将连接到出户框线上的管线连接到户外主管上
                resRoutes.AddRange(ConnectToEndPipeLines(frameConnectLines, outFrameLines, gridInfo, deepRooms.Select(x => x.Key.Key).ToList()));
            }

            return resRoutes;
        }

        /// <summary>
        /// 将管线连接到户外主管上
        /// </summary>
        /// <param name="frameConnectLines"></param>
        /// <param name="outFrameLines"></param>
        /// <param name="gridInfo"></param>
        /// <param name="rooms"></param>
        /// <returns></returns>
        private List<RouteModel> ConnectToEndPipeLines(Dictionary<VerticalPipeModel, Polyline> frameConnectLines, List<Line> outFrameLines, Dictionary<Vector3d, List<Line>> gridInfo, List<Polyline> rooms)
        {
            var resRoutes = new List<RouteModel>();
            var sewageLines = mainSewagePipes.SelectMany(x => x.GetAllLineByPolyline()).ToList();
            var rainLines = mainRainPipes.SelectMany(x => x.GetAllLineByPolyline()).ToList();
            GetClosetLineInfo(sewageLines, rainLines, frameConnectLines, out Line swageClosetLine, out Line rainClosetLine);
            var frameHoleLines = frameConnectLines.Select(x => GeometryUtils.ShortenPolyline(x.Value, 50)).ToList();
            var holeConnectLines = new List<Polyline>(CreateRouteHelper.CreateConnectLineHoles(frameHoleLines, lineDis));
            frameConnectLines = OrderOutPipeConnect(swageClosetLine, rainClosetLine, frameConnectLines);
            foreach (var pipeLine in frameConnectLines)
            {
                var closetLine = swageClosetLine;
                if (pipeLine.Key.PipeType == VerticalPipeType.rainPipe || pipeLine.Key.PipeType == VerticalPipeType.CondensatePipe)
                {
                    closetLine = rainClosetLine;
                }
                if (closetLine == null)
                {
                    continue;
                }
                var closetPt = pipeLine.Value.EndPoint;
                var outFrame = HandleStructService.GetNeedFrame(closetLine, rooms);
                var poly = pipeLine.Value;
                if (!outFrameLines[1].IsIntersects(poly))
                {
                    var length = outFrameLines.Last().Length;
                    poly = GeometryUtils.ShortenPolyline(poly, -length);
                }
                
                CreateConnectPipesService connectPipesService = new CreateConnectPipesService(step, gridInfo);
                Dictionary<List<Polyline>, double> weightHoles = new Dictionary<List<Polyline>, double>();
                weightHoles.Add(wallPolys, double.MaxValue);
                weightHoles.Add(holeConnectLines, lineWieght);
                var connectLine = connectPipesService.CreatePipes(outFrame, closetLine, pipeLine.Value.EndPoint, weightHoles);
                if (connectLine.Count > 0)
                {
                    holeConnectLines.AddRange(CreateRouteHelper.CreateConnectLineHoles(connectLine, lineDis));
                    var line = CreateRouteHelper.MergeRouteLine(connectLine.First(), pipeLine.Value);
                    RouteModel route = new RouteModel(line, pipeLine.Key.PipeType, pipeLine.Key.Position, pipeLine.Key.IsEuiqmentPipe);
                    if (pipeLine.Key.IsEuiqmentPipe)
                    {
                        route.printCircle = pipeLine.Key.PipeCircle;
                    }
                    route.connecLine = closetLine;
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
        private List<VerticalPipeModel> OrderPipeConnect(Line longLine, List<VerticalPipeModel> mainPipes)
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

            return mainPipes;
        }

        /// <summary>
        /// 排序连接到户外主管的连接顺序
        /// </summary>
        /// <param name="allLines"></param>
        /// <returns></returns>
        private Dictionary<VerticalPipeModel, Polyline> OrderOutPipeConnect(Line swageClosetLine, Line rainClosetLine, Dictionary<VerticalPipeModel, Polyline> frameConnectLines)
        {
            var closePoly = outUserPoly.OrderBy(x => x.Distance(frameConnectLines.First().Value.EndPoint)).FirstOrDefault();
            var closetLine = swageClosetLine;
            if (closetLine == null)
            {
                closetLine = rainClosetLine;
            }
            if (closePoly != null && closetLine != null)
            {
                frameConnectLines = frameConnectLines.OrderBy(x => Math.Round(closetLine.GetClosestPointTo(x.Value.EndPoint, true).DistanceTo(x.Value.EndPoint)))
                    .ThenBy(x => Math.Round(closePoly.GetClosestPointTo(x.Value.EndPoint, false).DistanceTo(x.Value.EndPoint)))
                    .ToDictionary(x => x.Key, y => y.Value);
            }

            return frameConnectLines;
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
            if (isLeft)
                return pipes.OrderBy(x => Math.Floor(polyline.GetClosestPointTo(x.Value, false).DistanceTo(x.Value))).ThenBy(x => Math.Floor(x.Value.X)).Select(x => x.Key).ToList();
            else
                return pipes.OrderBy(x => Math.Floor(polyline.GetClosestPointTo(x.Value, false).DistanceTo(x.Value))).ThenByDescending(x => Math.Floor(x.Value.X)).Select(x => x.Key).ToList();
            //var pipePt = pipes.First().Key.Position;
            //var checkDir = (pipePt - polyline.GetClosestPointTo(pipePt, true)).GetNormal();
            //if (checkDir.DotProduct(matrix.CoordinateSystem3d.Yaxis) < 0)
            //{
            //    if (isLeft)
            //        return pipes.OrderByDescending(x => Math.Floor(x.Value.Y)).ThenBy(x => Math.Floor(x.Value.X)).Select(x => x.Key).ToList();
            //    else
            //        return pipes.OrderByDescending(x => Math.Floor(x.Value.Y)).ThenByDescending(x => Math.Floor(x.Value.X)).Select(x => x.Key).ToList();
            //}
            //else
            //{
            //    if (isLeft)
            //        return pipes.OrderBy(x => Math.Floor(x.Value.Y)).ThenBy(x => Math.Floor(x.Value.X)).Select(x => x.Key).ToList();
            //    else
            //        return pipes.OrderBy(x => Math.Floor(x.Value.Y)).ThenByDescending(x => Math.Floor(x.Value.X)).Select(x => x.Key).ToList();
            //}
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

        /// <summary>
        /// 获取深度为1的出户框线（最后出户的框线）
        /// </summary>
        /// <param name="deepRooms"></param>
        /// <returns></returns>
        private List<Polyline> GetOneDeepOutFrame(Dictionary<KeyValuePair<Polyline, List<string>>, int> deepRooms)
        {
            var oneDeepRooms = deepRooms.Where(x => x.Value == 1).Select(x => x.Key.Key).Select(x => x.Buffer(100)[0] as Polyline).ToList();
            var otherDeepRooms = deepRooms.Where(x => x.Value != 1).Select(x => x.Key.Key).Select(x => x.Buffer(100)[0] as Polyline).ToList();
            var needOutFrames = outUserPoly.Where(x => oneDeepRooms.Any(y => y.IsIntersects(x)) && !otherDeepRooms.Any(y => y.IsIntersects(x))).ToList();
            return needOutFrames;
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

        /// <summary>
        /// 分别计算污水和排雨水管的最近线
        /// </summary>
        /// <param name="swageLines"></param>
        /// <param name="rainLines"></param>
        /// <param name="frameConnectLines"></param>
        /// <param name="swageCloseLine"></param>
        /// <param name="rainCloseLine"></param>
        private void GetClosetLineInfo(List<Line> swageLines, List<Line> rainLines, Dictionary<VerticalPipeModel, Polyline> frameConnectLines, out Line swageClosetLine, out Line rainClosetLine)
        {
            swageClosetLine = null;
            rainClosetLine = null;
            var rainConnectLines = frameConnectLines.Where(x => x.Key.PipeType == VerticalPipeType.rainPipe || x.Key.PipeType == VerticalPipeType.CondensatePipe).ToDictionary(x => x.Key, y => y.Value);
            var swageConnectLines = frameConnectLines.Except(rainConnectLines).ToDictionary(x => x.Key, y => y.Value);
            if (rainConnectLines.Count > 0 && rainLines.Count > 0)
            {
                var closetLine = CreateRouteHelper.GetClosetLane(rainLines, rainConnectLines.First().Value.EndPoint, frame, wallPolys, 400);
                rainClosetLine = closetLine.Key;
            }
            if (swageConnectLines.Count > 0 && swageLines.Count > 0)
            {
                var closetLine = CreateRouteHelper.GetClosetLane(swageLines, swageConnectLines.First().Value.EndPoint, frame, wallPolys, 400);
                swageClosetLine = closetLine.Key;
            }
        }
    }
}