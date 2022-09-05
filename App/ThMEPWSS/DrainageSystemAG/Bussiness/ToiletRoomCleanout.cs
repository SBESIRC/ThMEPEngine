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
    class ToiletRoomCleanout
    {
        private double _roomExtendFindPipeDis = 200;
        private double _pipeCheckOtherPipeDis = 250;
        private double _pipeCheckRoomLineDis = 500;
        private List<CreateBlockInfo> _createPipeBlocks;
        private List<RoomModel> _toiletRooms;
        private List<EquipmentBlockSpace> _toilets;
        private string _floorUid;
        public ToiletRoomCleanout(string floorId, List<RoomModel> toiletRooms,List<CreateBlockInfo> createPipeBlocks) 
        {
            _createPipeBlocks = new List<CreateBlockInfo>();
            _toiletRooms = new List<RoomModel>();
            _toilets = new List<EquipmentBlockSpace>();
            _floorUid = floorId;
            if (null != toiletRooms && toiletRooms.Count > 0) 
            {
                toiletRooms.ForEach(c => _toiletRooms.Add(c));
            }
            if (null != createPipeBlocks && createPipeBlocks.Count > 0) 
            {
                createPipeBlocks.ForEach(c => _createPipeBlocks.Add(c));
            }
        }
        public List<CreateBlockInfo> GetCreateCleanout(List<EquipmentBlockSpace> toilets) 
        {
            _toilets.Clear();
            if (null != toilets && toilets.Count > 0) 
            {
                foreach (var item in toilets)
                    _toilets.Add(item);
            }
            var retBlocks = new List<CreateBlockInfo>();
            if (null == _toiletRooms || _toiletRooms.Count<1 || _createPipeBlocks ==null || _createPipeBlocks.Count<1)
                return retBlocks;
            var hisIds = new List<string>();
            foreach (var room in _toiletRooms) 
            {
                //在内部找PL
                var roomOutLine = room.outLine;
                var innerPipes = GetRoomPLPipe(roomOutLine);
                if (null == innerPipes || innerPipes.Count < 1)
                {
                    var roomOutPLine = room.outLine.BufferPL(_roomExtendFindPipeDis).Cast<Polyline>().FirstOrDefault();
                    innerPipes = GetRoomPLPipe(roomOutPLine);
                }
                if (null == innerPipes || innerPipes.Count < 1)
                    //房间没有找到PL立管,不进行后续布置
                    continue;
                var lines = DrainSysAGCommon.PolyLineToLines(room.outLine);
                var maxLengthLine = lines.OrderByDescending(c => c.Length).First();
                var xAxis = (maxLengthLine.EndPoint - maxLengthLine.StartPoint).GetNormal();
                var yAxis = Vector3d.ZAxis.CrossProduct(xAxis);
                var baseDirs = new List<Vector3d>() { xAxis,xAxis.Negate(),yAxis,yAxis.Negate()};
                var thisRoomToilets = toilets.Where(c => c.roomSpaceId.Equals(room.thIFCRoom.Uuid)).ToList();
                foreach (var pipe in innerPipes) 
                {
                    var canLayoutDir = GetLayoutDir(pipe.createPoint, baseDirs, roomOutLine);
                    if (null == canLayoutDir || canLayoutDir.Count < 1)
                        continue;
                    if (canLayoutDir.Count > 1) 
                    {
                        //进一步判断可布置方向
                        if (thisRoomToilets.Count > 0)
                        {
                            //有马桶，根据马桶判断用那个方向
                            var toiletPoint = thisRoomToilets.First().blockCenterPoint;
                            canLayoutDir = GetLayoutDirByToiles(pipe.createPoint, canLayoutDir, toiletPoint);
                            if (canLayoutDir.Count > 1)
                                canLayoutDir = GetLayoutDirByMinAngleToiles(pipe.createPoint, canLayoutDir, toiletPoint);
                        }
                        else 
                        {
                            //没有马桶根据房间框线判断该用那个方向
                            canLayoutDir = GetLayoutDirByMaxDisRoomLine(pipe.createPoint, canLayoutDir, lines);
                        }
                    }
                    if (null == canLayoutDir || canLayoutDir.Count < 1)
                        continue;
                    var dir = canLayoutDir.First();
                    var angle = Vector3d.YAxis.GetAngleTo(dir, Vector3d.ZAxis);
                    var createPoint = pipe.createPoint + dir.MultiplyBy(DrainSysAGCommon.GetBlockCircleRadius(pipe, "可见性1"));
                    var block = new CreateBlockInfo(_floorUid, ThWSSCommon.Layout_CleanoutBlockName, ThWSSCommon.Layout_FloorDrainBlockWastLayerName, createPoint, EnumEquipmentType.other);
                    block.spaceId = room.thIFCRoom.Uuid;
                    block.rotateAngle = angle;
                    block.scaleNum = 1;
                    retBlocks.Add(block);
                }
            }
            return retBlocks;
        }
        List<Vector3d> GetLayoutDir(Point3d pipePoint,List<Vector3d> checkLayoutDirs, Polyline roomPLine) 
        {
            var canLayoutDirs = new List<Vector3d>();
            foreach (var item in checkLayoutDirs) 
            {
                if (!DirCheckByOtherPipe(pipePoint, item))
                    continue;
                if (!DirCheckByRoomPolyLine(pipePoint, item, roomPLine))
                    continue;
                canLayoutDirs.Add(item);
            }
            return canLayoutDirs;
        }
        bool DirCheckByOtherPipe(Point3d plPoint,Vector3d checkDir) 
        {
            foreach (var item in _createPipeBlocks) 
            {
                var dis = item.createPoint.DistanceTo(plPoint);
                if (dis > _pipeCheckOtherPipeDis || dis<10)
                    continue;
                var dir = (item.createPoint - plPoint).GetNormal();
                var dot = dir.DotProduct(checkDir);
                if (dot > 0.5)
                    return false;
            }
            return true;
        }
        bool DirCheckByRoomPolyLine(Point3d plPoint, Vector3d checkDir,Polyline roomPLine) 
        {
            var checkPoint = plPoint + checkDir.MultiplyBy(_pipeCheckRoomLineDis);
            return roomPLine.Contains(checkPoint);
        }
        List<Vector3d> GetLayoutDirByToiles(Point3d plPoint, List<Vector3d> checkDirs,Point3d totilePoint) 
        {
            var canLayoutDirs = new List<Vector3d>();
            foreach (var dir in checkDirs) 
            {
                var checkDir = (totilePoint - plPoint).GetNormal();
                var dot = checkDir.DotProduct(dir);
                if (dot < 0)
                    continue;
                canLayoutDirs.Add(dir);
            }
            return canLayoutDirs;
        }
        List<Vector3d> GetLayoutDirByMinAngleToiles(Point3d plPoint, List<Vector3d> checkDirs, Point3d totilePoint)
        {
            var canLayoutDirs = new List<Vector3d>();
            var minAngleDir = new Vector3d();
            var minAngle = double.MaxValue;
            var checkDir = (totilePoint - plPoint);
            foreach (var dir in checkDirs)
            {
                var angle = dir.GetAngleTo(checkDir, Vector3d.ZAxis);
                angle %= (Math.PI * 2);
                if (angle > Math.PI)
                    angle = Math.PI * 2 - angle;
                if (minAngle > angle)
                {
                    minAngleDir = dir;
                    minAngle = angle;
                }
            }
            if (minAngle< 10)
                canLayoutDirs.Add(minAngleDir);
            return canLayoutDirs;
        }
        List<Vector3d> GetLayoutDirByMaxDisRoomLine(Point3d plPoint, List<Vector3d> checkDirs, List<Line> roomLines)
        {
            var canLayoutDirs = new List<Vector3d>();
            Vector3d maxSpaceDir = new Vector3d();
            var maxDis = double.MinValue;
            foreach (var dir in checkDirs)
            {
                foreach (var line in roomLines) 
                {
                    var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                    var dirDot = dir.DotProduct(lineDir);
                    //接近平行的线进行后续的运算
                    if (Math.Abs(dirDot) > 0.98)
                        continue;
                    int interRes = PointVectorUtil.LineIntersectionLine(plPoint, dir, line.StartPoint, lineDir, out Point3d intersectionPoint);
                    if (interRes != 1)
                        continue;
                    if (!intersectionPoint.PointInLineSegment(line))
                        continue;
                    var dis = intersectionPoint.DistanceTo(plPoint);
                    if (maxDis < dis) 
                    {
                        maxDis = dis;
                        maxSpaceDir = dir;
                    }
                }
            }
            if (maxDis > -1)
                canLayoutDirs.Add(maxSpaceDir);
            return canLayoutDirs;
        }
        List<CreateBlockInfo> GetRoomPLPipe(Polyline roomOutLine) 
        {
            var retBlocks = new List<CreateBlockInfo>();
            foreach (var item in _createPipeBlocks) 
            {
                if (string.IsNullOrEmpty(item.tag) || !item.tag.Contains("PL"))
                    continue;
                if(roomOutLine.Contains(item.createPoint))
                    retBlocks.Add(item);
            }
            return retBlocks;
        }
    }
}
