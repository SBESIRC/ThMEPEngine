using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Data;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class HandleConfluenceService
    {
        //item1.洁具废水主管 item2.洁具污水主管 item3.其他废水支管 item4.其他污水支管 item5.房间内其他立管 item6. 带权联通房间
        public List<Tuple<List<VerticalPipeModel>, VerticalPipeModel, List<VerticalPipeModel>, List<VerticalPipeModel>, List<VerticalPipeModel>, Dictionary<KeyValuePair<Polyline, List<string>>, int>>> pipeTuples;
        public List<VerticalPipeModel> otherOutPoly;
        List<VerticalPipeModel> verticalPipes;
        SewageWasteWaterEnum sewageWasteWaterEnum;
        SingleRowSettingEnum singleRowSettingEnum;
        List<Dictionary<KeyValuePair<Polyline, List<string>>, int>> deepRooms;
        readonly double step = 50;                          //步长
        public HandleConfluenceService(ParamSettingViewModel paramSetting, List<VerticalPipeModel> _verticalPipes, List<Dictionary<KeyValuePair<Polyline, List<string>>, int>> _deepRooms)
        {
            sewageWasteWaterEnum = paramSetting.SewageWasteWater;
            singleRowSettingEnum = paramSetting.SingleRowSetting;
            verticalPipes = _verticalPipes;
            deepRooms = _deepRooms;
        }

        /// <summary>
        /// 分类连管（找到连管主管）
        /// </summary>
        public void GetMainPolyVerticalPipe()
        {
            pipeTuples = new List<Tuple<List<VerticalPipeModel>, VerticalPipeModel, List<VerticalPipeModel>, List<VerticalPipeModel>, List<VerticalPipeModel>, Dictionary<KeyValuePair<Polyline, List<string>>, int>>>();
            foreach (var dRoom in deepRooms)
            {
                var rooms = dRoom.Select(x => x.Key).ToList();
                var roomPipeDic = verticalPipes.ToDictionary(x => x, y => dRoom.FirstOrDefault(z => z.Key.Key.Contains(y.Position)))
                    .Where(x => !default(KeyValuePair<KeyValuePair<Polyline, List<string>>, int>).Equals(x.Value))
                    .OrderByDescending(x => x.Value.Value)
                    .ToDictionary(x => x.Key, y => y.Value);
                var roomEquipementPipes = roomPipeDic.Where(x => x.Key.IsEuiqmentPipe).Select(x => x.Key).ToList(); //洁具点位
                List<VerticalPipeModel> wasteMainPolys = new List<VerticalPipeModel>();                             //废水主管
                VerticalPipeModel sewageMainPoly = null;                                                            //污水主管
                List<VerticalPipeModel> otherWastePolys = new List<VerticalPipeModel>();                            //其他废水支管
                List<VerticalPipeModel> otherSewagePolys = new List<VerticalPipeModel>();                           //其他废水支管
                List<VerticalPipeModel> otherVerPipes = new List<VerticalPipeModel>();                              //其他立管

                if (roomEquipementPipes.Count > 0)
                {
                    if (singleRowSettingEnum == SingleRowSettingEnum.ReservedPlug)
                    {
                        wasteMainPolys = GetReservedPlugMainPipes(dRoom, roomEquipementPipes);
                    }
                    else if (singleRowSettingEnum == SingleRowSettingEnum.DrawDetail)
                    {
                        wasteMainPolys = new List<VerticalPipeModel>() { roomEquipementPipes.Where(x => x.PipeType == VerticalPipeType.WasteWaterPipe).FirstOrDefault() };
                        if (sewageWasteWaterEnum == SewageWasteWaterEnum.Confluence)
                        {
                            roomEquipementPipes.ForEach(x => x.PipeType = VerticalPipeType.ConfluencePipe);
                            if (wasteMainPolys.Count <= 0)
                            {
                                wasteMainPolys = new List<VerticalPipeModel>() { roomEquipementPipes.First() };
                            }
                        }
                        else if (sewageWasteWaterEnum == SewageWasteWaterEnum.Diversion)
                        {
                            sewageMainPoly = roomEquipementPipes.First();
                        }
                    }

                    var otherPolys = roomEquipementPipes.Where(x => x != sewageMainPoly && !wasteMainPolys.Contains(x)).ToList();
                    otherWastePolys = otherPolys.Where(x => x.PipeType == VerticalPipeType.WasteWaterPipe || x.PipeType == VerticalPipeType.ConfluencePipe).ToList();
                    otherSewagePolys = otherPolys.Where(x => x.PipeType == VerticalPipeType.SewagePipe).ToList();
                }
                otherVerPipes = roomPipeDic.Where(x => !x.Key.IsEuiqmentPipe).Select(x => x.Key).ToList();
                var tuple = new Tuple<List<VerticalPipeModel>, VerticalPipeModel, List<VerticalPipeModel>, List<VerticalPipeModel>, List<VerticalPipeModel>, Dictionary<KeyValuePair<Polyline, List<string>>, int>>(wasteMainPolys, sewageMainPoly, otherWastePolys, otherSewagePolys, otherVerPipes, dRoom);
                pipeTuples.Add(tuple);

                verticalPipes = verticalPipes.Except(roomPipeDic.Select(x => x.Key)).ToList();
            }

            otherOutPoly = verticalPipes;
        }

        /// <summary>
        /// 获取堵头的主管
        /// </summary>
        /// <param name="thRooms"></param>
        /// <param name="equipmentPipes"></param>
        /// <returns></returns>
        private List<VerticalPipeModel> GetReservedPlugMainPipes(Dictionary<KeyValuePair<Polyline, List<string>>, int> thRooms, List<VerticalPipeModel> equipmentPipes)
        {
            var resMainPipes = new List<VerticalPipeModel>();
            foreach (var room in thRooms)
            {
                if (!room.Key.Value.Any(x => x == "厨房" || x == "卫生间"))
                {
                    continue;
                }

                var pipes = equipmentPipes.Where(x => room.Key.Key.Contains(x.Position)).ToList();
                var mainPipe = pipes.Where(x => x.PipeType == VerticalPipeType.WasteWaterPipe).FirstOrDefault();
                if (mainPipe == null)
                {
                    mainPipe = pipes.FirstOrDefault();
                }

                if (mainPipe != null)
                {
                    resMainPipes.Add(mainPipe);
                }
            }

            return resMainPipes;
        }

        /// <summary>
        /// 合流分流连接算法
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="otherPipes"></param>
        /// <param name="wallPolys"></param>
        /// <param name="mainRoute"></param>
        /// <param name="rooms"></param>
        /// <param name="outFrames"></param>
        /// <returns></returns>
        public List<RouteModel> ConnectPipe(Polyline frame, List<VerticalPipeModel> otherPipes, List<Polyline> wallPolys,
            RouteModel mainRoute, Dictionary<KeyValuePair<Polyline, List<string>>, int> rooms, List<Polyline> outFrames)
        {
            if (otherPipes.Count <= 0 || mainRoute == null)
            {
                return new List<RouteModel>();
            }
            var mainLine = GetMainPipeLine(mainRoute, rooms, outFrames, wallPolys);
            var roomPolys = rooms.Select(x => x.Key.Key).ToList();
            return ConnectOtherPipes(frame, mainLine, otherPipes, wallPolys, roomPolys);
        }

        /// <summary>
        /// 连接其他支管
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="mainLine"></param>
        /// <param name="otherPipes"></param>
        /// <param name="wallPolys"></param>
        /// <returns></returns>
        private List<RouteModel> ConnectOtherPipes(Polyline frame, KeyValuePair<Polyline, Line> mainLineDic, List<VerticalPipeModel> otherPipes, List<Polyline> wallPolys, List<Polyline> roomPolys)
        {
            var mainLine = mainLineDic.Key;
            var orderPipeDic = OrderPipes(mainLine, otherPipes);
            var cPipe = ClassifyPipes(orderPipeDic);
            var resPipeLines = new List<RouteModel>();
            var otherPolyline = new List<RouteModel>();
            foreach (var pipes in cPipe)
            {
                resPipeLines.Add(ConnectPipes(frame, mainLine, resPipeLines, pipes, wallPolys, roomPolys, out List<RouteModel> otherConnectPipeLines));
                otherPolyline.AddRange(otherConnectPipeLines);
            }
            var lastPipe = resPipeLines.Last();
            resPipeLines.Remove(lastPipe);
            resPipeLines.Add(ConnectLastPipe(lastPipe, mainLineDic.Value));
            var allResPolys = new List<RouteModel>(resPipeLines);
            allResPolys.AddRange(otherPolyline);
            return allResPolys;
        }

        /// <summary>
        /// 连接管线
        /// </summary>
        /// <param name="mainPoly"></param>
        /// <param name="connectLines"></param>
        /// <param name="otherPipes"></param>
        /// <param name="mainDir"></param>
        private RouteModel ConnectPipes(Polyline frame, Polyline mainPoly, List<RouteModel> connectLines, List<VerticalPipeModel> otherPipes, List<Polyline> wallPolys, List<Polyline> rooms, out List<RouteModel> otherConnectPipeLines)
        {
            otherPipes = otherPipes.OrderByDescending(x => mainPoly.GetClosestPointTo(x.Position, true).DistanceTo(x.Position)).ToList();
            var closePt = mainPoly.GetClosestPointTo(otherPipes.First().Position, true);
            var createDir = (closePt - otherPipes.First().Position).GetNormal();
            Polyline resConnectLine = new Polyline();
            resConnectLine.AddVertexAt(resConnectLine.NumberOfVertices, otherPipes.First().Position.ToPoint2D(), 0, 0, 0);

            var firPt = otherPipes.First().Position;
            var pipeType = otherPipes.First().PipeType;
            var mainClosetPt = mainPoly.GetClosestPointTo(firPt, false);
            var connectPoly = mainPoly;
            var connectPt = firPt;
            if (connectLines.Count > 0)
            {
                var connectLinesDic = connectLines.ToDictionary(x => x, y => y.route.GetClosestPointTo(firPt, false))
                    .OrderBy(x => x.Value.DistanceTo(firPt))
                    .ToDictionary(x => x.Key.route, y => y.Value);
                var otherClosetPt = connectLinesDic.First().Value;
                if (firPt.DistanceTo(mainClosetPt) > firPt.DistanceTo(otherClosetPt))
                {
                    var tempLine = new Line(firPt, firPt + createDir * 100);
                    var tempPt = tempLine.GetClosestPointTo(otherPipes.Last().Position, true);
                    resConnectLine.AddVertexAt(resConnectLine.NumberOfVertices, tempPt.ToPoint2D(), 0, 0, 0);
                    resConnectLine.AddVertexAt(resConnectLine.NumberOfVertices, otherPipes.Last().Position.ToPoint2D(), 0, 0, 0);
                    connectPoly = connectLinesDic.First().Key;
                    connectPt = otherPipes.Last().Position;
                }
            }

            CreateConnectPipesService connectPipesService = new CreateConnectPipesService(step, new Dictionary<Vector3d, List<Line>>());
            Dictionary<List<Polyline>, double> weightHoles = new Dictionary<List<Polyline>, double>();
            weightHoles.Add(wallPolys, double.MaxValue);
            var closetLine = GeometryUtils.GetClosetLane(connectPoly.GetAllLineByPolyline(), connectPt, frame, step);
            var outFrame = HandleStructService.GetNeedFrame(closetLine.Key, rooms);
            var connectLine = connectPipesService.CreatePipes(outFrame, closetLine.Key, connectPt, weightHoles);

            otherPipes.Remove(otherPipes.First());
            var resLine = CreateConnectLine(resConnectLine, connectLine, connectPt, closetLine.Key, pipeType);
            otherConnectPipeLines = CreateOtherPipes(otherPipes, resLine);
            return resLine;
        }

        /// <summary>
        /// 创建连接线
        /// </summary>
        /// <param name="resConnectLine"></param>
        /// <param name="connectLines"></param>
        /// <param name="connectPt"></param>
        /// <param name="closetLine"></param>
        /// <returns></returns>
        private RouteModel CreateConnectLine(Polyline resConnectLine, List<Polyline> connectLines, Point3d connectPt, Line closetLine, VerticalPipeType type)
        {
            if (connectLines.Count > 0)
            {
                var firLine = connectLines.First();
                if (resConnectLine.EndPoint.DistanceTo(firLine.EndPoint) < resConnectLine.EndPoint.DistanceTo(firLine.StartPoint))
                {
                    firLine.ReverseCurve();
                }
                for (int i = 0; i < firLine.NumberOfVertices; i++)
                {
                    resConnectLine.AddVertexAt(resConnectLine.NumberOfVertices, firLine.GetPoint3dAt(i).ToPoint2D(), 0, 0, 0);
                }
                return new RouteModel(resConnectLine, type, connectPt, false);
            }

            var closetP = closetLine.GetClosestPointTo(connectPt, true);
            resConnectLine.AddVertexAt(resConnectLine.NumberOfVertices, closetP.ToPoint2D(), 0, 0, 0);
            if (closetLine.GetClosestPointTo(connectPt, false).DistanceTo(closetP) > 1)
            {
                var otherP = closetLine.StartPoint.DistanceTo(closetP) < closetLine.EndPoint.DistanceTo(closetP)
                     ? closetLine.StartPoint : closetLine.EndPoint;
                resConnectLine.AddVertexAt(resConnectLine.NumberOfVertices, otherP.ToPoint2D(), 0, 0, 0);
            }
            return new RouteModel(resConnectLine, type, connectPt, false);
        }

        /// <summary>
        /// 创建其他连接支管
        /// </summary>
        /// <param name="otherPipes"></param>
        /// <param name="resLine"></param>
        /// <returns></returns>
        private List<RouteModel> CreateOtherPipes(List<VerticalPipeModel> otherPipes, RouteModel resLine)
        {
            var resPolys = new List<RouteModel>();
            foreach (var pipe in otherPipes)
            {
                var pt = resLine.route.GetClosestPointTo(pipe.Position, false);
                var connectLine = new Polyline();
                connectLine.AddVertexAt(0, pt.ToPoint2D(), 0, 0, 0);
                connectLine.AddVertexAt(1, pipe.Position.ToPoint2D(), 0, 0, 0);
                resPolys.Add(new RouteModel(connectLine, pipe.PipeType, pipe.Position, pipe.IsEuiqmentPipe));
            }
            return resPolys;
        }

        /// <summary>
        /// 将最后一段管劲连接到主管上
        /// </summary>
        private RouteModel ConnectLastPipe(RouteModel routeModel, Line originMainLine)
        {
            var connectPt = routeModel.route.EndPoint;
            if (connectPt.DistanceTo(routeModel.startPosition) < 0.1)
            {
                routeModel.route.ReverseCurve();
                connectPt = routeModel.route.EndPoint;
            }

            var mainLinePt = originMainLine.StartPoint.DistanceTo(connectPt) < originMainLine.EndPoint.DistanceTo(connectPt) ?
                originMainLine.StartPoint : originMainLine.EndPoint;
            if (originMainLine.GetClosestPointTo(connectPt, false).DistanceTo(connectPt) > 0.1)
            {
                routeModel.route.AddVertexAt(routeModel.route.NumberOfVertices, mainLinePt.ToPoint2D(), 0, 0, 0);
            }
            return routeModel;
        }

        /// <summary>
        /// 排序管点
        /// </summary>
        /// <param name="mainLine"></param>
        /// <param name="otherPipes"></param>
        /// <returns></returns>
        private Dictionary<VerticalPipeModel, Point3d> OrderPipes(Polyline mainLine, List<VerticalPipeModel> otherPipes)
        {
            var xDir = (mainLine.EndPoint - mainLine.StartPoint).GetNormal();
            var zDir = Vector3d.ZAxis;
            var yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(new double[]{
                    xDir.X, yDir.X, zDir.X, mainLine.StartPoint.X,
                    xDir.Y, yDir.Y, zDir.Y, mainLine.StartPoint.Y,
                    xDir.Z, yDir.Z, zDir.Z, mainLine.StartPoint.Z,
                    0.0, 0.0, 0.0, 1.0});
            var pipeDic = otherPipes.ToDictionary(x => x, y => y.Position.TransformBy(matrix.Inverse()));
            return pipeDic.OrderBy(x => x.Value.X).ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// 分类管线
        /// </summary>
        /// <param name="pipeDic"></param>
        /// <returns></returns>
        private List<List<VerticalPipeModel>> ClassifyPipes(Dictionary<VerticalPipeModel, Point3d> pipeDic)
        {
            var resPipe = new List<List<VerticalPipeModel>>();
            while (pipeDic.Count > 0)
            {
                var firDic = pipeDic.First();
                pipeDic.Remove(firDic.Key);
                var pipes = new List<VerticalPipeModel>() { firDic.Key };
                foreach (var pDic in pipeDic.Where(x => Math.Abs(x.Value.X - firDic.Value.X) <= 200 && x.Value.Y * firDic.Value.Y > 0))
                {
                    pipes.Add(pDic.Key);
                }
                foreach (var pipe in pipes)
                {
                    pipeDic.Remove(pipe);
                }
                resPipe.Add(pipes);
            }

            return resPipe;
        }

        /// <summary>
        /// 获取主管线段
        /// </summary>
        /// <param name="mainRoute"></param>
        /// <param name="deepRooms"></param>
        /// <param name="outFrames"></param>
        /// <returns></returns>
        private KeyValuePair<Polyline, Line> GetMainPipeLine(RouteModel mainRoute, Dictionary<KeyValuePair<Polyline, List<string>>, int> deepRooms, List<Polyline> outFrames, List<Polyline> wallPolys)
        {
            var outRooms = deepRooms.Select(x => x.Key.Key).ToList();
            var frames = outFrames.Where(x =>
            {
                var bufferFrame = x;
                var bufferCollect = x.Buffer(100);
                if (bufferCollect.Count > 0)
                {
                    bufferFrame = bufferCollect[0] as Polyline;
                }
                return outRooms.Where(y => y.Intersects(bufferFrame)).Count() == 1;
            }).ToList();
            var mainLine = mainRoute.route.GetAllLineByPolyline().FirstOrDefault(x => frames.Any(y => y.IsIntersects(x)));
            var extendMainLine = ExtendMainPipeLine(mainLine, outRooms, wallPolys);
            return new KeyValuePair<Polyline, Line>(extendMainLine, mainLine);
        }

        /// <summary>
        /// 延申主管线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="rooms"></param>
        /// <param name="outFrames"></param>
        /// <returns></returns>
        private Polyline ExtendMainPipeLine(Line line, List<Polyline> rooms, List<Polyline> wallPolys)
        {
            var sPt = line.StartPoint;
            var dir = (line.EndPoint - line.StartPoint).GetNormal();
            if (rooms.Any(x => x.Contains(line.StartPoint)))
            {
                sPt = line.EndPoint;
                dir = (line.StartPoint - line.EndPoint).GetNormal();
            }

            Ray ray = new Ray() { BasePoint = sPt, UnitDir = dir };
            var intersectPts = wallPolys.SelectMany(x =>
            {
                var ptCollection = new Point3dCollection();
                ray.IntersectWith(x, Intersect.OnBothOperands, ptCollection, (IntPtr)0, (IntPtr)0);
                return ptCollection.Cast<Point3d>();
            }).OrderBy(x => x.DistanceTo(sPt)).ToList();
            Polyline resPoly = new Polyline();
            if (intersectPts.Count > 0)
            {
                resPoly.AddVertexAt(0, sPt.ToPoint2D(), 0, 0, 0);
                resPoly.AddVertexAt(1, intersectPts[0].ToPoint2D(), 0, 0, 0);
                return resPoly;
            }

            resPoly.AddVertexAt(0, line.StartPoint.ToPoint2D(), 0, 0, 0);
            resPoly.AddVertexAt(1, line.EndPoint.ToPoint2D(), 0, 0, 0);
            return resPoly;
        }
    }
}