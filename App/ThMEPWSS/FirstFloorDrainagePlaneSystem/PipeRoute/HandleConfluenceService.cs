using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Service;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.PipeRoute
{
    public class HandleConfluenceService
    {
        //item1.洁具废水主管 item2.洁具污水主管 item3.其他洁具支管 item4.房间内其他立管 item5. 带权联通房间
        public List<Tuple<VerticalPipeModel, VerticalPipeModel, List<VerticalPipeModel>, List<VerticalPipeModel>, Dictionary<Polyline, int>>> pipeTuples; 
        public List<VerticalPipeModel> otherOutPoly;
        List<VerticalPipeModel> verticalPipes;
        SewageWasteWaterEnum sewageWasteWaterEnum;
        List<Dictionary<Polyline, int>> deepRooms;
        public HandleConfluenceService(SewageWasteWaterEnum _sewageWasteWaterEnum, List<VerticalPipeModel> _verticalPipes, List<Dictionary<Polyline, int>> _deepRooms)
        {
            sewageWasteWaterEnum = _sewageWasteWaterEnum;
            verticalPipes = _verticalPipes;
            deepRooms = _deepRooms;
        }

        /// <summary>
        /// 分类连管（找到连管主管）
        /// </summary>
        public void GetMainPolyVerticalPipe()
        {
            pipeTuples = new List<Tuple<VerticalPipeModel, VerticalPipeModel, List<VerticalPipeModel>, List<VerticalPipeModel>, Dictionary<Polyline, int>>>();
            foreach (var dRoom in deepRooms)
            {
                var rooms = dRoom.Select(x => x.Key).ToList();
                var roomPipeDic = verticalPipes.ToDictionary(x => x, y => dRoom.FirstOrDefault(z => z.Key.Contains(y.Position)))
                    .Where(x => !default(KeyValuePair<Polyline, int>).Equals(x))
                    .OrderByDescending(x => x.Value.Value)
                    .ToDictionary(x => x.Key, y => y.Value);
                var roomEquipementPipes = roomPipeDic.Where(x => x.Key.IsEuiqmentPipe).Select(x => x.Key).ToList(); //洁具点位
                VerticalPipeModel wasteMainPoly = null;                                 //废水主管
                VerticalPipeModel sewageMainPoly = null;                                //污水主管
                List<VerticalPipeModel> otherPolys = new List<VerticalPipeModel>();     //其他污废水支管
                List<VerticalPipeModel> otherVerPipes = new List<VerticalPipeModel>();  //其他立管

                if (roomEquipementPipes.Count > 0)
                {
                    sewageMainPoly = roomEquipementPipes.Where(x => x.PipeType == VerticalPipeType.SewagePipe).FirstOrDefault();
                    var allRoomPipes = roomPipeDic.Select(x => x.Key).ToList();
                    if (sewageWasteWaterEnum == SewageWasteWaterEnum.Confluence)
                    {
                        if (sewageMainPoly != null)
                        {
                            sewageMainPoly = roomEquipementPipes.First();
                        }
                    }
                    else if (sewageWasteWaterEnum == SewageWasteWaterEnum.Diversion)
                    {
                        wasteMainPoly = roomPipeDic.First().Key;
                    }

                    otherPolys = roomEquipementPipes.Where(x => x != sewageMainPoly && x != wasteMainPoly).ToList();
                }
                otherVerPipes = roomPipeDic.Where(x => !x.Key.IsEuiqmentPipe).Select(x => x.Key).ToList();
                var tuple = new Tuple<VerticalPipeModel, VerticalPipeModel, List<VerticalPipeModel>, List<VerticalPipeModel>, Dictionary<Polyline, int>>(sewageMainPoly, wasteMainPoly, otherPolys, otherVerPipes, dRoom);
                pipeTuples.Add(tuple);

                verticalPipes = verticalPipes.Except(roomPipeDic.Select(x => x.Key)).ToList();
            }

            otherOutPoly = verticalPipes;//.Where(x => x.PipeType == VerticalPipeType.CondensatePipe || x.PipeType == VerticalPipeType.rainPipe).ToList();
        }

        public void ConfluenceConnectPipe(VerticalPipeModel mainPipe, List<VerticalPipeModel> otherPipes, RouteModel mainRoute, Dictionary<Polyline, int> rooms, List<Polyline> outFrames, List<MPolygon> roomHoles)
        {
            var mainLine = GetMainPipeLine(mainRoute, rooms, outFrames);
            
        }

        public void ShuntConnectPipe()
        {

        }

        private void ConnectOtherPipes(Polyline mainLine , List<VerticalPipeModel> otherPipes)
        {
            var orderPipeDic = OrderPipes(mainLine, otherPipes);
            var cPipe = ClassifyPipes(orderPipeDic);
            var resPipeLines = new List<Polyline>();
            foreach (var pipes in cPipe)
            {

            }
        }

        /// <summary>
        /// 连接管线
        /// </summary>
        /// <param name="mainPoly"></param>
        /// <param name="connectLines"></param>
        /// <param name="otherPipes"></param>
        /// <param name="mainDir"></param>
        private void ConnectPipes(Polyline mainPoly, List<Polyline> connectLines, List<VerticalPipeModel> otherPipes, Vector3d mainDir)
        {
            otherPipes = otherPipes.OrderByDescending(x => mainPoly.GetClosestPointTo(x.Position, true).DistanceTo(x.Position)).ToList();
            var closePt = mainPoly.GetClosestPointTo(otherPipes.First().Position, true);
            var createDir = (closePt - otherPipes.First().Position).GetNormal();
            Polyline resConnectLine = new Polyline();
            resConnectLine.AddVertexAt(resConnectLine.NumberOfVertices, otherPipes.First().Position.ToPoint2D(), 0, 0, 0);
            if (otherPipes.Count > 1)
            {
                var firPt = otherPipes.First().Position;
                var mainClosetPt = mainPoly.GetClosestPointTo(firPt, false);
                if (connectLines.Count > 0)
                {
                    var otherClosetPt = connectLines.Select(x => x.GetClosestPointTo(firPt, false)).OrderBy(x => x.DistanceTo(firPt)).First();
                    if (firPt.DistanceTo(mainClosetPt) > firPt.DistanceTo(otherClosetPt))
                    {
                        var tempLine = new Line(firPt, firPt + createDir * 100);
                        var tempPt = tempLine.GetClosestPointTo(otherPipes.Last().Position, true);
                        resConnectLine.AddVertexAt(resConnectLine.NumberOfVertices, tempPt.ToPoint2D(), 0, 0, 0);
                        resConnectLine.AddVertexAt(resConnectLine.NumberOfVertices, otherPipes.Last().Position.ToPoint2D(), 0, 0, 0);
                        var lastPt = otherPipes.Last().Position;
                        var lastClosetPt = connectLines.Select(x => x.GetClosestPointTo(lastPt, false)).OrderBy(x => x.DistanceTo(lastPt)).First();
                        resConnectLine.AddVertexAt(resConnectLine.NumberOfVertices, lastClosetPt.ToPoint2D(), 0, 0, 0);
                    }
                    else
                    {
                        resConnectLine.AddVertexAt(resConnectLine.NumberOfVertices, mainClosetPt.ToPoint2D(), 0, 0, 0);
                    }
                }
                else
                {
                    resConnectLine.AddVertexAt(resConnectLine.NumberOfVertices, mainClosetPt.ToPoint2D(), 0, 0, 0);
                }
            }
             
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
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
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
                foreach (var pDic in pipeDic.Where(x => Math.Abs(x.Value.X - firDic.Value.X) <= 200))
                {
                    pipes.Add(pDic.Key);
                    pipeDic.Remove(pDic.Key);
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
        private Line GetMainPipeLine(RouteModel mainRoute, Dictionary<Polyline, int> deepRooms, List<Polyline> outFrames)
        {
            var outRooms = deepRooms.Select(x => x.Key).ToList(); 
            var frames = outFrames.Where(x =>
            {
                var bufferFrame = x.Buffer(100)[0] as Polyline;
                return outRooms.Where(y => y.Intersects(bufferFrame)).Count() == 1;
            }).ToList();
            var mainLine = mainRoute.route.GetAllLineByPolyline().FirstOrDefault(x=> frames.Any(y =>y.IsIntersects(x)));
            var extendMainLine = ExtendMainPipeLine(mainLine, outRooms, frames);
            return extendMainLine;
        }

        /// <summary>
        /// 延申主管线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="rooms"></param>
        /// <param name="outFrames"></param>
        /// <returns></returns>
        private Line ExtendMainPipeLine(Line line, List<Polyline> rooms, List<Polyline> outFrames)
        {
            var bufferOutFrame = outFrames.Select(x => x.ExtendByLengthLine(100)).ToList();
            var sPt = line.StartPoint;
            var dir = (line.EndPoint - line.StartPoint).GetNormal();
            if (rooms.Any(x => x.Contains(line.StartPoint)))
            {
                sPt = line.EndPoint;
                dir = (line.StartPoint - line.EndPoint).GetNormal();
            }

            Ray ray = new Ray() { BasePoint = sPt, UnitDir = dir };
            var trimRooms = rooms.SelectMany(x => bufferOutFrame.SelectMany(y => y.Trim(x).OfType<Polyline>().ToList())).ToList();
            var intersectPts = trimRooms.SelectMany(x =>
            {
                var ptCollection = new Point3dCollection();
                ray.IntersectWith(x, Intersect.OnBothOperands, ptCollection, (IntPtr)0, (IntPtr)0);
                return ptCollection.Cast<Point3d>();
            }).OrderBy(x => x.DistanceTo(sPt)).ToList();
            if (intersectPts.Count > 0)
            {
                return new Line(sPt, intersectPts[0]);
            }

            return line; 
        }
    }
}