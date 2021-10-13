using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.Common;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    class KitchenSinkConnect
    {
        private string _floorId;
        private List<RoomModel> _kitchenRooms;
        private List<RoomModel> _toiletRooms;
        private List<CreateBlockInfo> _floorFLPipes;
        private List<EquipmentBlockSpace> _kitchenSinks;
        private Dictionary<string,Point3d> _sinkConvertorCenters;
        private double _sinkRadius = 25.0;//洗涤盆转换后半径
        private double _convertorDistanceToWall = 150.0;
        public KitchenSinkConnect(string floorId, List<CreateBlockInfo> createFLPipeBlocks) 
        {
            _floorId = floorId;
            _floorFLPipes = createFLPipeBlocks;
            _kitchenSinks = new List<EquipmentBlockSpace>();
            _sinkConvertorCenters = new Dictionary<string, Point3d>();
        }
        public void InitData(List<RoomModel> kitchenRooms,List<RoomModel> toiletRooms,List<EquipmentBlockSpace> sinkBlocks) 
        {
            _toiletRooms = toiletRooms;
            _kitchenRooms = kitchenRooms;
            _kitchenSinks = sinkBlocks;
        }
        public List<CreateBasicElement> SinkConvertorConnect() 
        {
            var retBasicElements = SinkConvertor();
            retBasicElements.AddRange(ConnectToFLPipe());
            return retBasicElements;
        }
        public List<CreateBasicElement> SinkConvertor() 
        {
            _sinkConvertorCenters.Clear();
            var createBasics = new List<CreateBasicElement>();
            if (null == _kitchenRooms || _kitchenRooms.Count < 1 || _kitchenSinks == null || _kitchenSinks.Count<1)
                return createBasics;
            foreach (var room in _kitchenRooms) 
            {
                var roomSinks = new List<EquipmentBlockSpace>();
                foreach (var sink in _kitchenSinks) 
                {
                    if (_sinkConvertorCenters.Any(c => c.Key.Equals(sink.uid)))
                        continue;
                    if (room.outLine.Contains(sink.blockCenterPoint))
                        roomSinks.Add(sink);
                }
                if (roomSinks.Count < 1)
                    continue;
                foreach (var item in roomSinks) 
                {
                    var pt = item.blockCenterPoint;
                    var closePoint = room.outLine.GetClosePoint(pt);
                    var dir = (pt - closePoint).GetNormal();
                    var createPoint = closePoint + dir.MultiplyBy(_convertorDistanceToWall);
                    _sinkConvertorCenters.Add(item.uid, createPoint);
                }
            }
            foreach (var center in _sinkConvertorCenters) 
            {
                var circle = new Circle(center.Value, Vector3d.ZAxis, _sinkRadius);
                createBasics.Add(new CreateBasicElement(_floorId, circle, ThWSSCommon.Layout_WastWaterPipeLayerName, center.Key, "CFXDP"));
            }
            return createBasics;
        }
        public List<CreateBasicElement> ConnectToFLPipe() 
        {
            var createBasics = new List<CreateBasicElement>();
            if (null == _kitchenRooms || _kitchenRooms.Count < 1 || _sinkConvertorCenters == null || _sinkConvertorCenters.Count < 1)
                return createBasics;
            foreach (var room in _kitchenRooms)
            {
                var roomPLine = (Polyline)room.outLine.Clone();
                var roomFLs = KitchenRoomFLs(roomPLine);
                if (roomFLs == null || roomFLs.Count < 1)
                    continue;
                var roomSinks = new List<Point3d>();
                foreach (var point in _sinkConvertorCenters) 
                {
                    if (roomPLine.Contains(point.Value))
                        roomSinks.Add(point.Value);
                }
                foreach(var sinkPoint in roomSinks) 
                {
                    var connectFL = roomFLs.OrderBy(c => c.createPoint.DistanceTo(sinkPoint)).FirstOrDefault();
                    var lines = GetConnectLines(room, sinkPoint, connectFL.createPoint);
                    foreach (var line in lines) 
                    {
                        var sp = line.StartPoint;
                        var ep = line.EndPoint;
                        var dir = (ep - sp).GetNormal();
                        if (sp.DistanceTo(sinkPoint) < 5) 
                            sp = sp + dir.MultiplyBy(_sinkRadius);
                        else if(sp.DistanceTo(connectFL.createPoint)<5)
                            sp = sp + dir.MultiplyBy(DrainSysAGCommon.GetBlockCircleRadius(connectFL, "可见性1"));
                        if (ep.DistanceTo(sinkPoint) < 5)
                            ep = ep - dir.MultiplyBy(_sinkRadius);
                        else if (ep.DistanceTo(connectFL.createPoint) < 5)
                            ep = ep - dir.MultiplyBy(DrainSysAGCommon.GetBlockCircleRadius(connectFL, "可见性1"));
                        createBasics.Add(new CreateBasicElement(_floorId, new Line(sp,ep), ThWSSCommon.Layout_PipeWastDrainConnectLayerName, "", ""));
                    }
                    
                }
            }
            return createBasics;
        }
        private List<Line> GetConnectLines(RoomModel room,Point3d sinkPoint,Point3d pipePoint) 
        {
            var resLines =new List<Line>();
            bool pipeInRoom = room.outLine.Contains(pipePoint);
            var closePoint = room.outLine.GetClosePoint(sinkPoint);
            var dic = new Dictionary<Point3d, double>();
            var mainDir = (closePoint - sinkPoint).GetNormal();
            if (pipeInRoom)
            {
                //管在厨房内部
                dic.Add(sinkPoint, 100);
                var connectPipe = new PipeDrainConnect(room.outLine, pipePoint, 100, dic);
                var lines = connectPipe.PipeDrainConnectByMainAxis(mainDir);
                if (null != lines && lines.Count > 0)
                    resLines.AddRange(lines);
            }
            else 
            {
                dic.Add(pipePoint, 50);
                var points = new List<Point3d>() { sinkPoint };
                //房间轮廓取obb
                var obbPline = room.GetRoomOBBPolyline();
                Line nearLine = null;
                if (obbPline.Contains(pipePoint))
                    nearLine = NearRoomLine(room.outLine, pipePoint, points);
                else
                {
                    var nearLine1 = NearRoomLine(obbPline, pipePoint);
                    var nearPoint1 = pipePoint.PointToLine(nearLine1);
                    var nearLine2 = NearRoomLine(room.outLine, pipePoint);
                    var nearPoint2 = pipePoint.PointToLine(nearLine2);
                    if (PointVectorUtil.PointInLineSegment(nearPoint2, new Line(pipePoint, nearPoint1), 10, 10))
                        nearLine = NearRoomLine(room.outLine, pipePoint, new List<Point3d>() { sinkPoint });
                }

                Point3d crossPoint = new Point3d();
                Vector3d outDir = new Vector3d();
                if (nearLine == null)
                {
                    //不能直接穿过，根据点位信息，进一步计算弯折信息
                    var roomNearLine = NearRoomLine(room.outLine, pipePoint, mainDir, false);
                    var tempPoint = pipePoint.PointToLine(roomNearLine);
                    roomNearLine = NearRoomLine(room.outLine, tempPoint, points);
                    var orderPts = PointVectorUtil.PointsOrderByDirection(points, mainDir, tempPoint);
                    var nearPoint = orderPts.First().Key;
                    crossPoint = nearPoint.PointToLine(roomNearLine);
                    
                    var connectPipeInner = new PipeDrainConnect(room.outLine, crossPoint, 0, dic);
                    outDir = (crossPoint - nearPoint).GetNormal();
                    var lines = connectPipeInner.PipeDrainConnectByMainAxis(outDir);
                    if (null != lines && lines.Count > 0)
                    {
                        resLines.AddRange(lines);
                        var connectPipeOut = new PipeDrainConnect(null, crossPoint, 0, dic);
                        var pipeLines = connectPipeOut.PipeDrainConnectByMainAxis(outDir);
                        resLines.AddRange(pipeLines);
                    }
                }
                else
                {
                    //可以直接穿过,根据穿过的线
                    crossPoint = sinkPoint.PointToLine(nearLine);
                    outDir = (crossPoint - sinkPoint).GetNormal();
                    dic.Clear();
                    dic.Add(crossPoint, 100);
                    var connectPipe = new PipeDrainConnect(room.outLine, pipePoint, 100, dic);
                    var lines = connectPipe.PipeDrainConnectByMainAxis(outDir);
                    if (null != lines && lines.Count > 0)
                    {
                        resLines.AddRange(lines);
                        resLines.Add(new Line(sinkPoint, crossPoint));
                    }
                }

            }
            return resLines;
        }
        private List<CreateBlockInfo> KitchenRoomFLs(Polyline roomPline) 
        {
            //step1 先在厨房框线内找
            var roomFLs = RoomPlineFLs(roomPline,false);
            if (null != roomFLs && roomFLs.Count > 0)
                return roomFLs;
            //step2 厨房外扩350，找FL且在卫生间内部的FL
            roomPline = roomPline.Buffer(350.0)[0] as Polyline;
            roomFLs = RoomPlineFLs(roomPline, true);
            if (null != roomFLs && roomFLs.Count > 0)
                return roomFLs;
            //step3 厨房外扩350，找FL
            roomFLs = RoomPlineFLs(roomPline, false);
            return roomFLs;
        }
        private List<CreateBlockInfo> RoomPlineFLs(Polyline roomPline,bool flInToilet) 
        {
            var roomFLs = new List<CreateBlockInfo>();
            if (_floorFLPipes == null || _floorFLPipes.Count < 1)
                return roomFLs;
            foreach (var fl in _floorFLPipes) 
            {
                bool isAdd = roomPline.Contains(fl.createPoint);
                if (!isAdd)
                    continue;
                isAdd = flInToilet ? ((_toiletRooms != null && _toiletRooms.Count > 0) ? _toiletRooms.Any(c => c.outLine.Contains(fl.createPoint)) : false) : true;
                if (!isAdd)
                    continue;
                roomFLs.Add(fl);
            }
            return roomFLs;
        }
        Line NearRoomLine(Polyline roomPLine, Point3d targetPoint, Vector3d targetLineDirection, bool pointInLine)
        {
            List<Line> roomLines = DrainSysAGCommon.PolyLineToLines(roomPLine);
            Line nearLine = null;
            var nearDis = double.MaxValue;
            foreach (var line in roomLines)
            {
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                var dot = lineDir.DotProduct(targetLineDirection);
                if (Math.Abs(dot) < 0.5)
                    continue;
                var prjPoint = targetPoint.PointToLine(line);
                if (!pointInLine || (pointInLine && prjPoint.PointInLineSegment(line)))
                {
                    var dis = prjPoint.DistanceTo(targetPoint);
                    if (dis < nearDis)
                    {
                        nearDis = dis;
                        nearLine = line;
                    }
                }
            }
            return nearLine;
        }
        Line NearRoomLine(Polyline roomPLine, Point3d pipeCenterPoint)
        {
            List<Line> roomLines = DrainSysAGCommon.PolyLineToLines(roomPLine);
            Line nearLine = null;
            var nearDis = double.MaxValue;
            foreach (var line in roomLines)
            {
                var prjPoint = pipeCenterPoint.PointToLine(line);
                if (prjPoint.PointInLineSegment(line))
                {
                    var dis = prjPoint.DistanceTo(pipeCenterPoint);
                    if (dis < nearDis)
                    {
                        nearDis = dis;
                        nearLine = line;
                    }
                }
            }
            return nearLine;
        }
        Line NearRoomLine(Polyline roomPLine, Point3d pipeCenterPoint, List<Point3d> connectPoints)
        {
            List<Line> roomLines = DrainSysAGCommon.PolyLineToLines(roomPLine);
            List<Line> canConenctLines = new List<Line>();
            foreach (var point in connectPoints)
            {
                var line = new Line(point, pipeCenterPoint);
                var dir = (pipeCenterPoint - point).GetNormal();
                foreach (var checkLine in roomLines)
                {
                    var checkDir = (checkLine.EndPoint - checkLine.StartPoint).GetNormal();
                    int inter = PointVectorUtil.LineIntersectionLine(point, dir, checkLine.StartPoint, checkDir, out Point3d interPoint);
                    if (inter != 1)
                        continue;
                    if (interPoint.PointInLineSegment(line) && interPoint.PointInLineSegment(checkLine))
                        canConenctLines.Add(checkLine);
                }
            }
            Line nearLine = null;
            var nearDis = double.MaxValue;
            foreach (var line in canConenctLines)
            {
                var prjPoint = pipeCenterPoint.PointToLine(line);
                if (prjPoint.PointInLineSegment(line))
                {
                    var dis = prjPoint.DistanceTo(pipeCenterPoint);
                    if (dis < nearDis)
                    {
                        nearDis = dis;
                        nearLine = line;
                    }
                }
            }
            return nearLine;
        }
    }
}
