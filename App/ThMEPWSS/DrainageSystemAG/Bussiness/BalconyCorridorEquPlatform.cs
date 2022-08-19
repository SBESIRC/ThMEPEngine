using Autodesk.AutoCAD.Colors;
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
    /// <summary>
    /// 阳台、连廊、设备平台
    /// </summary>
    class BalconyCorridorEquPlatform
    {
        double _balconyMopPoolRadius = 25;//阳台拖把池排水点半径
        double _floorDrainBalconyPipeNearDistance = 600;// 地漏找阳台立管范围
        double _floorDrainEqumPipeNearDistance = 2500;//地漏找设备平台立管范围
        double _pipeCasingLength = 100;//套管长度
        double _pipeCenterCheckDistance = 10;//线调整，点在圆心的判断范围
        double _mopPoolAdjustRange = 150;//拖把根据立管，地漏池调整中心点范围

        List<RoomModel> _balconyRooms = new List<RoomModel>();
        List<RoomModel> _corridorRooms = new List<RoomModel>();
        List<RoomModel> _otherRooms = new List<RoomModel>();
        List<EquipmentBlockSpace> _riserPipe = new List<EquipmentBlockSpace>();
        List<EquipmentBlockSpace> _drains = new List<EquipmentBlockSpace>();
        List<EquipmentBlockSpace> _mopPools = new List<EquipmentBlockSpace>();
        List<CreateBlockInfo> _balconyDrains = new List<CreateBlockInfo>();
        List<PipeConnectRelation> _pipeConnectRelations = new List<PipeConnectRelation>();
        List<Polyline> _wallPolylines = new List<Polyline>();
        List<Polyline> _columnPolylines = new List<Polyline>();
        List<Polyline> _beamPolylines = new List<Polyline>();
        List<CreateBlockInfo> _pipeConverters = new List<CreateBlockInfo>();
        List<CreateBlockInfo> _allDrains = new List<CreateBlockInfo>();
        List<Circle> _pipeDrainCircles = new List<Circle>();

        public List<CreateBasicElement> createBasicElements = new List<CreateBasicElement>();
        public List<CreateBlockInfo> createBlockInfos = new List<CreateBlockInfo>();
        public List<CreateDBTextElement> createDBTextElements = new List<CreateDBTextElement>();
        private List<string> ChangedRoomTypeuid = new List<string>();

        private string _floorId;
        public BalconyCorridorEquPlatform(string floorId,List<RoomModel> balconyRooms, List<RoomModel> corridorRooms, List<RoomModel> otherRooms, List<EquipmentBlockSpace> balcCoorEqus, StruParameters parameters)
        {
            this._floorId = floorId;
            if (null != balconyRooms && balconyRooms.Count > 0)
            {
                balconyRooms.ForEach(c => { if (c != null) { _balconyRooms.Add(c); } });
            }
            if (null != corridorRooms && corridorRooms.Count > 0)
            {
                corridorRooms.ForEach(c => { if (c != null) { _corridorRooms.Add(c); } });
            }
            if (null != balcCoorEqus && balcCoorEqus.Count > 0)
            {
                foreach (var item in balcCoorEqus)
                {
                    if (item.enumEquipmentType == EnumEquipmentType.balconyRiser || item.enumEquipmentType == EnumEquipmentType.wastewaterRiser ||  item.enumEquipmentType == EnumEquipmentType.condensateRiser)
                    {
                        _riserPipe.Add(item);
                    }
                    else if (item.enumEquipmentType == EnumEquipmentType.floorDrain)
                    {
                        _drains.Add(item);
                    }
                    else if (item.enumEquipmentType == EnumEquipmentType.mopPool)
                    {
                        _mopPools.Add(item);
                    }
                }
            }
            if (null != otherRooms && otherRooms.Count > 0) 
            {
                foreach (var room in otherRooms) 
                {
                    if (room == null || room.roomTypeName == EnumRoomType.Balcony || room.roomTypeName == EnumRoomType.Corridor || room.roomTypeName == EnumRoomType.EquipmentPlatform)
                        continue;
                    if (room.outLine.Area < 2 * 1000000)
                        continue;
                    _otherRooms.Add(room);
                }
            }
            if (null != parameters) 
            {
                if (null != parameters.Walls && parameters.Walls.Count > 0)
                {
                    parameters.Walls.ForEach(c => { if (c != null) { _wallPolylines.Add(c); } });
                }
                if (null != parameters.Columns && parameters.Columns.Count > 0)
                {
                    parameters.Columns.ForEach(c => { if (c != null) { _columnPolylines.Add(c); } });
                }
                if (null != parameters.Beams && parameters.Beams.Count > 0) 
                {
                    parameters.Beams.ForEach(c => { if (c != null) { _beamPolylines.Add(c); } });
                }
            }
        }

        public void LayoutConnect(List<CreateBlockInfo> balconyDrainCoverter,out List<string> changeToFLY1Ids,out List<string> changeToFDrainIds) 
        {
            changeToFLY1Ids = new List<string>();
            changeToFDrainIds = new List<string>();
            _balconyDrains.Clear();
            _pipeConverters.Clear();
            _allDrains.Clear();
            if (null != balconyDrainCoverter && balconyDrainCoverter.Count > 0)
            {
                List<string> pipeTags = new List<string> { "FL", "Y2", "NL", "WL" };
                foreach (var item in balconyDrainCoverter)
                {
                    if (!string.IsNullOrEmpty(item.tag) && pipeTags.Any(c => item.tag.Contains(c)))
                        _pipeConverters.Add(item);
                    if (item.equipmentType == EnumEquipmentType.floorDrain)
                        _allDrains.Add(item);
                    if (string.IsNullOrEmpty(item.spaceId) || !_balconyRooms.Any(c => c.thIFCRoom.Uuid.Equals(item.spaceId)))
                        continue;
                    if (item.equipmentType != EnumEquipmentType.floorDrain)
                        continue;
                    _balconyDrains.Add(item);
                }
            }
            //阳台拖把池转换
            BalconyMopPool();
            //设备平台内地漏转换
            EqumPlatformConnect();
            var balcCorridRooms = new List<RoomModel>();
            if (null != _balconyRooms && _balconyRooms.Count > 0)
                balcCorridRooms.AddRange(_balconyRooms);
            if (null != _corridorRooms && _corridorRooms.Count > 0)
                balcCorridRooms.AddRange(_corridorRooms);

            _pipeDrainCircles = GetCheckCircles();
            //阳台设备、立管连线
            BalconyCorridorConnect(balcCorridRooms);
            //阳台附近连线的转管，该标志为阳台立管
            foreach (var blockinfo in balconyDrainCoverter)
            {
                if (ChangedRoomTypeuid.Contains(blockinfo.belongBlockId))
                    blockinfo.tag = "FyL";
            }
            //根据连接设备判断连线图层
            PipeRelationToLayoutLine();
            foreach (var item in _pipeConnectRelations)
            {
                if (item.isWasteWaterPipe) 
                {
                    var pipeRaise = _riserPipe.Where(c => c.uid == item.pipeBlockUid).First();
                    if (pipeRaise.enumEquipmentType != EnumEquipmentType.wastewaterRiser)
                        changeToFLY1Ids.Add(item.pipeBlockUid);
                    if (null == _balconyDrains || _balconyDrains.Count < 1)
                        continue;
                    foreach (var connectId in item.connectBlockIds) 
                    {
                        if (null != _mopPools && _mopPools.Count > 0 && _mopPools.Any(c => c.uid.Equals(connectId)))
                            continue;
                        if (_balconyDrains.Any(c => c.belongBlockId.Equals(connectId) &&!(c.layerName.Equals(ThWSSCommon.Layout_FloorDrainBlockWastLayerName))))
                        {
                            changeToFDrainIds.Add(connectId);
                        }
                    }
                }
            }
        }
        void BalconyMopPool() 
        {
            //阳台拖把池 在拖把池图块的中心生成一个排水点位。 图层：W - DRAI - EQPM 图元：半径50的圆 位置：拖把池的obb的中心
            if (null == _mopPools || _mopPools.Count < 1)
                return;
            foreach (var item in _mopPools) 
            {
                if (item == null || item.enumRoomType != EnumRoomType.Balcony)
                    continue;
                //获取所属阳台的房间，获取X轴，Y轴，并获取该阳台中的立管，地漏对拖把池进行位置修正
                var room = _balconyRooms.Where(c => c.thIFCRoom.Uuid == item.roomSpaceId).FirstOrDefault();
                var centerPoint = item.blockCenterPoint;
                var createPoint = centerPoint;
                if (null != room) 
                {
                    var points = new List<Point3d>();
                    foreach (var pipe in _balconyDrains) 
                    {
                        if (room.outLine.Contains(pipe.createPoint))
                            points.Add(pipe.createPoint);
                    }
                    if (points.Count > 0) 
                    {
                        //获取该空间内的地漏，立管，X轴Y轴
                        var xPoints = new List<Point3d>();
                        var yPoints = new List<Point3d>();
                        var xAxis = RoomXAxis(room);
                        var yAxis = Vector3d.ZAxis.CrossProduct(xAxis);
                        foreach (var point in points) 
                        {
                            if (point.PointInLine(createPoint, xAxis, _mopPoolAdjustRange))
                                xPoints.Add(point);
                            if (point.PointInLine(createPoint, yAxis, _mopPoolAdjustRange))
                                yPoints.Add(point);
                        }
                        if (xPoints.Count > 0)
                        {
                            var first = xPoints.OrderBy(c => c.DistanceTo(createPoint)).First();
                            createPoint = createPoint.PointToLine(first, xAxis);
                        }
                        if (yPoints.Count > 0) 
                        {
                            var first = yPoints.OrderBy(c => c.DistanceTo(createPoint)).First();
                            createPoint = createPoint.PointToLine(first, yAxis);
                        }
                    }
                }
                item.blockCenterPoint = createPoint;
                var circle = new Circle(createPoint, Vector3d.ZAxis, _balconyMopPoolRadius);
                createBasicElements.Add(new CreateBasicElement(_floorId,circle, ThWSSCommon.Layout_WastWaterPipeLayerName, item.uid,"YTTBC_PS"));
            }
        }
        void EqumPlatformConnect() 
        {
            //设备平台内的地漏连线
            foreach (var item in _drains) 
            {
                if (item == null || item.enumEquipmentType != EnumEquipmentType.floorDrain)
                    continue;
                if (item.enumRoomType != EnumRoomType.EquipmentPlatform && item.enumRoomType != EnumRoomType.Other)
                    continue;
                EquipmentBlockSpace lnPipe = null;
                EquipmentBlockSpace ytPipe = null;
                double lnNearDis = double.MaxValue;
                double ytNearDis = double.MaxValue;
                foreach (var pipe in _riserPipe) 
                {
                    if (pipe == null)
                        continue;
                    var dis = pipe.blockCenterPoint.DistanceTo(item.blockCenterPoint);
                    if (pipe.enumEquipmentType == EnumEquipmentType.condensateRiser)
                    {
                        if (dis < lnNearDis)
                        {
                            lnPipe = pipe;
                            lnNearDis = dis;
                        }
                    }
                    else if (pipe.enumEquipmentType == EnumEquipmentType.balconyRiser) 
                    {
                        if (dis < ytNearDis)
                        {
                            ytPipe = pipe;
                            ytNearDis = dis;
                        }
                    }
                }
                if (lnNearDis <= _floorDrainBalconyPipeNearDistance)
                {
                    //若地漏的600范围内存在冷凝立管，则直接连接地漏和直线距离最近的冷凝立管的圆心。
                    Line addLine = new Line(item.blockCenterPoint, lnPipe.blockCenterPoint);
                    var pipeRelation = _pipeConnectRelations.Where(c => c.pipeBlockUid.Equals(lnPipe.uid)).FirstOrDefault();
                    if (pipeRelation == null)
                    {
                        _pipeConnectRelations.Add(new PipeConnectRelation(lnPipe.uid, lnPipe.blockCenterPoint));
                        pipeRelation = _pipeConnectRelations.Where(c => c.pipeBlockUid.Equals(lnPipe.uid)).FirstOrDefault();
                    }
                    pipeRelation.pipeEquipmentConnectLines.Add(addLine);
                    pipeRelation.connectBlockIds.Add(item.uid);
                }
                else if (ytNearDis <= _floorDrainBalconyPipeNearDistance)
                {
                    //若地漏的600范围内不存在冷凝立管，则在600范围内找阳台立管。若能找到，则直接连接地漏和直线距离最近的阳台立管的圆心。
                    Line addLine = new Line(item.blockCenterPoint, ytPipe.blockCenterPoint);
                    var pipeRelation = _pipeConnectRelations.Where(c => c.pipeBlockUid.Equals(ytPipe.uid)).FirstOrDefault();
                    if (pipeRelation == null)
                    {
                        _pipeConnectRelations.Add(new PipeConnectRelation(ytPipe.uid, ytPipe.blockCenterPoint));
                        pipeRelation = _pipeConnectRelations.Where(c => c.pipeBlockUid.Equals(ytPipe.uid)).FirstOrDefault();
                    }
                    pipeRelation.pipeEquipmentConnectLines.Add(addLine);
                    pipeRelation.connectBlockIds.Add(item.uid);
                }
                else 
                {
                    //若地漏的600范围内不存在任何冷凝立管或阳台立管，则此地漏不做连管。用0图层全局宽度为50的红色矩形圈出提醒。矩形的尺寸比地漏图元的bbox大50 %。
                    var ntsPolyg = item.equmBlockReference.GeometricExtents.ToNTSPolygon();
                    double buffer = ntsPolyg.EnvelopeInternal.Width / 2;
                    var pline = ntsPolyg.ToDbPolylines().FirstOrDefault().Buffer(buffer).ToNTSMultiPolygon().ToDbPolylines().FirstOrDefault();
                    var color = Color.FromRgb(255, 0, 0);
                    createBasicElements.Add(new CreateBasicElement(_floorId,pline, "0", item.uid,"DLLG_LJX",color));
                }
            }
        }
        void BalconyCorridorConnect(List<RoomModel> balcCorridRooms) 
        {
            if (null == balcCorridRooms || balcCorridRooms.Count < 1)
                return;
            //雨水管/冷凝水管 W-RAIN-PIPE  废水 W-DRAI-WAST-PIPE
            //地漏和拖把池排水,将阳台沿着长度方向分为等分的两部分；
            var baclconyEqum = new List<EquipmentBlockSpace>();
            baclconyEqum.AddRange(_riserPipe);
            baclconyEqum.AddRange(_drains);
            baclconyEqum.AddRange(_mopPools);
            foreach (var room in balcCorridRooms)
            {
                var centerPoint = room.GetRoomCenterPoint();
                var lines = DrainSysAGCommon.PolyLineToLines(room.outLine);
                Line nearLine = null;
                var nearDis = double.MaxValue;
                foreach (var line in lines)
                {
                    var neraPoint = line.GetClosestPointTo(centerPoint, false);
                    var dis = neraPoint.DistanceTo(centerPoint);
                    if (dis < nearDis)
                    {
                        nearLine = line;
                        nearDis = dis;
                    }
                }
                if (nearLine == null)
                    continue;
                var closePoint = nearLine.GetClosestPointTo(centerPoint, false);
                var lineDir = (centerPoint - closePoint).GetNormal();
                
                //获取该阳台的地漏，拖把池,立管，区分左右两侧
                var roomLeftBlocks = new List<EquipmentBlockSpace>();
                var roomRightBlocks = new List<EquipmentBlockSpace>();
                foreach (var equipment in baclconyEqum) 
                {
                    if (!equipment.roomSpaceId.Equals(room.thIFCRoom.Uuid))
                        continue;
                    var dir = (equipment.blockPosition - centerPoint).GetNormal();
                    if (dir.CrossProduct(lineDir).Z < 0)
                    {
                        //左侧
                        roomLeftBlocks.Add(equipment);
                    }
                    else 
                    {
                        //右侧
                        roomRightBlocks.Add(equipment);
                    }
                }
                NewBalconyCorridorSpaceConnect(room, roomLeftBlocks, centerPoint, lineDir);
                NewBalconyCorridorSpaceConnect(room, roomRightBlocks, centerPoint, lineDir);
            }
        }

        Vector3d RoomXAxis(RoomModel room) 
        {
            var centerPoint = room.GetRoomCenterPoint();
            var lines = DrainSysAGCommon.PolyLineToLines(room.outLine);
            Line nearLine = null;
            var nearDis = double.MaxValue;
            foreach (var line in lines)
            {
                var neraPoint = line.GetClosestPointTo(centerPoint, false);
                var dis = neraPoint.DistanceTo(centerPoint);
                if (dis < nearDis)
                {
                    nearLine = line;
                    nearDis = dis;
                }
            }
            var closePoint = nearLine.GetClosestPointTo(centerPoint, false);
            var lineDir = (centerPoint - closePoint).GetNormal();
            return lineDir;
        }
        void NewBalconyCorridorSpaceConnect(RoomModel balcCorrRoom, List<EquipmentBlockSpace> connectSpaceEqum, Point3d centerPoint, Vector3d lineDir) 
        {
            //获取本阳台内的阳台立管、废水立管
            var pipe = connectSpaceEqum.Where(c => c.enumEquipmentType == EnumEquipmentType.balconyRiser 
            || (c.enumEquipmentType == EnumEquipmentType.wastewaterRiser && c.enumRoomType == EnumRoomType.Balcony)).FirstOrDefault();
            var otherBlocks = connectSpaceEqum.Where(c => c.enumEquipmentType != EnumEquipmentType.balconyRiser && c.enumEquipmentType != EnumEquipmentType.wastewaterRiser).ToList();
            if (otherBlocks.Count < 1)
                return;
            if (pipe != null)
            {
                //阳台连管侧有立管，将其它设备连接到立管上
                var pipeRelation = _pipeConnectRelations.Where(c => c.pipeBlockUid.Equals(pipe.uid)).FirstOrDefault();
                if (pipeRelation == null)
                {
                    _pipeConnectRelations.Add(new PipeConnectRelation(pipe.uid, pipe.blockCenterPoint));
                    pipeRelation = _pipeConnectRelations.Where(c => c.pipeBlockUid.Equals(pipe.uid)).FirstOrDefault();
                }

                otherBlocks = otherBlocks.OrderBy(c => c.blockCenterPoint.DistanceTo(pipe.blockCenterPoint)).ToList();
                pipeRelation.connectBlockIds.AddRange(otherBlocks.Select(c => c.uid).ToList());
                if (otherBlocks.Count == 1)
                {
                    //只有一个点位，直接连接立管
                    Line addLine = new Line(otherBlocks.FirstOrDefault().blockCenterPoint, pipe.blockCenterPoint);
                    pipeRelation.pipeEquipmentConnectLines.Add(addLine);
                }
                else
                {
                    //以立管为中心，做lineDir为X轴，垂直方向为Y轴，将其它的点位根据 距离轴
                    var connectPipe = new PipeDrainConnect(balcCorrRoom.outLine,pipe.blockCenterPoint,0, ConnectNoBayDistance(otherBlocks.Select(c => c.blockCenterPoint).ToList()));
                    var lines = connectPipe.PipeDrainConnectByMainAxis(lineDir);
                    if (null != lines && lines.Count > 0)
                        pipeRelation.pipeEquipmentConnectLines.AddRange(lines);
                }
                return;
            }
            //阳台上没有可以连接的，找其他的进行连接
            NewBalconyCorridorSpaceConnectPlanB(balcCorrRoom, connectSpaceEqum, centerPoint, lineDir);
        }
        bool NewBalconyCorridorSpaceConnectPlanB(RoomModel balcCorrRoom, List<EquipmentBlockSpace> connectSpaceEqum, Point3d roomCenterPoint, Vector3d lineDir)
        {
            //step2 阳台内没有找到立管，找设备平台的立管进行连接  中心为起点在2500范围内找设备平台的阳台立管，若存在多个则取最近的一个。
            //连管需沿着阳台框线的方向走，尽量横平竖直（较复杂 看图详解）。
            //lineDir 为短轴方向
            var geo = connectSpaceEqum.FirstOrDefault().equmBlockReference.GeometricExtents;
            for (int i = 1; i < connectSpaceEqum.Count; i++)
            {
                geo.AddExtents(connectSpaceEqum[i].equmBlockReference.GeometricExtents);
            }
            var centerPoint = geo.CenterPoint();
            var targetPipes = new List<EquipmentBlockSpace>();
            foreach (var pipe in _riserPipe)
            {
                if (pipe.enumRoomType == EnumRoomType.Other || pipe.enumRoomType == EnumRoomType.EquipmentPlatform)
                {
                    if (pipe.enumEquipmentType == EnumEquipmentType.balconyRiser || pipe.enumEquipmentType == EnumEquipmentType.wastewaterRiser)
                        targetPipes.Add(pipe);
                }
            }
            if (targetPipes == null || targetPipes.Count < 1)
                return false;
            var nearPipe = targetPipes.OrderBy(c => c.blockCenterPoint.DistanceTo(centerPoint)).FirstOrDefault();
            if (nearPipe.blockCenterPoint.DistanceTo(centerPoint) >= _floorDrainEqumPipeNearDistance)
                return false;
            ChangedRoomTypeuid.Add(nearPipe.uid);
            var pipeRelation = _pipeConnectRelations.Where(c => c.pipeBlockUid.Equals(nearPipe.uid)).FirstOrDefault();
            if (pipeRelation == null)
            {
                _pipeConnectRelations.Add(new PipeConnectRelation(nearPipe.uid, nearPipe.blockCenterPoint));
                pipeRelation = _pipeConnectRelations.Where(c => c.pipeBlockUid.Equals(nearPipe.uid)).FirstOrDefault();
            }
            pipeRelation.connectBlockIds.AddRange(connectSpaceEqum.Select(c => c.uid).ToList());

            var tempPoints = connectSpaceEqum.Select(c => c.blockCenterPoint).ToList();
            while (tempPoints.Count > 0) 
            {
                var points = new List<Point3d>();
                tempPoints.ForEach(c => points.Add(c));
                //房间轮廓取obb
                var obbPline = balcCorrRoom.GetRoomOBBPolyline();
                var pipePoint = nearPipe.blockCenterPoint;
                Line nearLine = null;
                if (obbPline.Contains(pipePoint))
                    nearLine = NearRoomLine(balcCorrRoom.outLine, pipePoint, points);
                else
                {
                    var nearLine1 = NearRoomLine(obbPline, pipePoint);
                    var nearPoint1 = pipePoint.PointToLine(nearLine1);
                    var nearLine2 = NearRoomLine(balcCorrRoom.outLine, pipePoint);
                    var nearPoint2 = pipePoint.PointToLine(nearLine2);
                    if (PointVectorUtil.PointInLineSegment(nearPoint2, new Line(pipePoint, nearPoint1), 10, 10))
                        nearLine = NearRoomLine(balcCorrRoom.outLine, pipePoint, points);
                }

                Point3d crossPoint = new Point3d();
                Vector3d outDir = new Vector3d();
                double crossLength = _pipeCasingLength;
                bool haveBeam = false;
                if (nearLine == null)
                {
                    //不能直接穿过，根据点位信息，进一步计算弯折信息
                    var roomNearLine = NearRoomLine(balcCorrRoom.outLine, pipePoint, lineDir, false);
                    var tempPoint = pipePoint.PointToLine(roomNearLine);
                    roomNearLine = NearRoomLine(balcCorrRoom.outLine, tempPoint, points);
                    var orderPts = PointVectorUtil.PointsOrderByDirection(points, lineDir, tempPoint);
                    var nearPoint = orderPts.First().Key;
                    crossPoint = nearPoint.PointToLine(roomNearLine);
                    points.Clear();
                    outDir = (crossPoint - nearPoint).GetNormal();
                    crossLength = CalcCrossWidth(crossPoint, outDir,out haveBeam);
                    foreach (var point in tempPoints) 
                    {
                        var prjPoint = point.PointToLine(crossPoint, outDir);
                        if (!balcCorrRoom.outLine.Contains(prjPoint))
                            continue;
                        var checkDir = (prjPoint - crossPoint).GetNormal();
                        if (checkDir.DotProduct(outDir) > 0)
                            continue;
                        points.Add(point);
                    }
                    if (points.Count < 1)
                        break;
                    var connectPipeInner = new PipeDrainConnect(balcCorrRoom.outLine, crossPoint, 0, ConnectNoBayDistance(points));
                    var lines = connectPipeInner.PipeDrainConnectByMainAxis(outDir);
                    if (null != lines && lines.Count > 0)
                    {
                        pipeRelation.pipeEquipmentConnectLines.AddRange(lines);
                        var connectPipeOut = new PipeDrainConnect(null, crossPoint, crossLength, ConnectNoBayDistance(new List<Point3d> { nearPipe.blockCenterPoint }));
                        var pipeLines = connectPipeOut.PipeDrainConnectByMainAxis(outDir);
                        if (null != pipeLines && pipeLines.Count > 0)
                            pipeRelation.pipeEquipmentConnectLines.AddRange(pipeLines);
                    }
                }
                else
                {
                    //可以直接穿过,根据穿过的线
                    crossPoint = nearPipe.blockCenterPoint.PointToLine(nearLine);
                    outDir = (nearPipe.blockCenterPoint - crossPoint).GetNormal();
                    points.Clear();
                    crossLength = CalcCrossWidth(crossPoint, outDir,out haveBeam);
                    foreach (var point in tempPoints)
                    {
                        var prjPoint = point.PointToLine(crossPoint, outDir);
                        if (!balcCorrRoom.outLine.Contains(prjPoint))
                            continue;
                        var checkDir = (prjPoint - crossPoint).GetNormal();
                        if (checkDir.DotProduct(outDir) > 0)
                            continue;
                        points.Add(point);
                    }
                    if (points.Count < 1)
                        break;
                    var connectPipe = new PipeDrainConnect(balcCorrRoom.outLine, crossPoint, 0, ConnectNoBayDistance(points));
                    var lines = connectPipe.PipeDrainConnectByMainAxis(outDir);
                    if (null != lines && lines.Count > 0)
                    {
                        pipeRelation.pipeEquipmentConnectLines.AddRange(lines);
                        var nLine = new Line(nearPipe.blockCenterPoint, crossPoint);
                        pipeRelation.pipeEquipmentConnectLines.Add(nLine);
                    }
                }
                if (haveBeam) 
                {
                    //在横管穿越阳台和设备平台的位置设置穿墙套管。 图层：W-BUSH  图块：套管-AI    可见性：普通套管
                    var addBlock = new CreateBlockInfo(_floorId, ThWSSCommon.Layout_PipeCasingBlockName, ThWSSCommon.Layout_PipeCasingLayerName, crossPoint, EnumEquipmentType.other);
                    var angle = (-Vector3d.YAxis).GetAngleTo(outDir, Vector3d.ZAxis);
                    addBlock.rotateAngle = angle % (Math.PI * 2);
                    addBlock.dymBlockAttr.Add("可见性", "普通套管");
                    addBlock.dymBlockAttr.Add("距离", crossLength);
                    createBlockInfos.Add(addBlock);

                    //套管处增加标注
                    var lineDri = Vector3d.YAxis.Negate();
                    var lineSp = crossPoint + outDir.MultiplyBy(crossLength / 2);
                    var lineEp = lineSp + lineDri.MultiplyBy(500);
                    var upText = DrainSysAGCommon.CreateDBText("DN100", lineEp, ThWSSCommon.Layout_PipeCasingTextLayerName, ThWSSCommon.Layout_TextStyle);
                    var btText = DrainSysAGCommon.CreateDBText("h1-0.30", lineEp, ThWSSCommon.Layout_PipeCasingTextLayerName, ThWSSCommon.Layout_TextStyle);
                    var upMaxPoint = upText.GeometricExtents.MaxPoint;
                    var upMinPoint = upText.GeometricExtents.MinPoint;
                    var btMaxPoint = btText.GeometricExtents.MaxPoint;
                    var btMinPoint = btText.GeometricExtents.MinPoint;
                    var upWidth = upMaxPoint.X - upMinPoint.X;
                    var upHeight = upMaxPoint.Y - upMinPoint.Y;
                    var btWidth = btMaxPoint.X - btMinPoint.X;
                    var btHeight = btMaxPoint.Y - btMinPoint.Y;
                    var maxWidth = Math.Max(upWidth, btWidth);
                    var leftDir = Vector3d.XAxis;

                    createBasicElements.Add(new CreateBasicElement(_floorId, new Line(lineSp, lineEp), ThWSSCommon.Layout_PipeCasingTextLayerName, "", ""));
                    createBasicElements.Add(new CreateBasicElement(_floorId, new Line(lineEp, lineEp + leftDir.MultiplyBy(maxWidth + 100)), ThWSSCommon.Layout_PipeCasingTextLayerName, "", ""));
                    var upTextPt = lineEp + Vector3d.XAxis.MultiplyBy(10) + lineDri.MultiplyBy(10);
                    upText.Position = upTextPt;
                    var btTextPt = lineEp + Vector3d.XAxis.MultiplyBy(10) + lineDri.MultiplyBy(btHeight + 30);
                    btText.Position = btTextPt;
                    createDBTextElements.Add(new CreateDBTextElement(_floorId, upTextPt, upText, "", ThWSSCommon.Layout_PipeCasingTextLayerName, ThWSSCommon.Layout_TextStyle));
                    createDBTextElements.Add(new CreateDBTextElement(_floorId, btTextPt, btText, "", ThWSSCommon.Layout_PipeCasingTextLayerName, ThWSSCommon.Layout_TextStyle));
                }
                tempPoints = tempPoints.Where(c => !points.Any(x => x.DistanceTo(c) < 5)).ToList();
            }
            
            return true;
        }
        Line NearRoomLine(Polyline roomPLine,Point3d pipeCenterPoint) 
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
        Line NearRoomLine(Polyline roomPLine, Point3d targetPoint, Vector3d targetLineDirection,bool pointInLine)
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
        Line NearRoomLine(Polyline roomPLine, Point3d pipeCenterPoint,List<Point3d> connectPoints)
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
        void PipeRelationToLayoutLine() 
        {
            if (null == _pipeConnectRelations || _pipeConnectRelations.Count < 1)
                return;
            foreach (var item in _pipeConnectRelations) 
            {
                if (item.connectBlockIds == null || item.connectBlockIds.Count < 1 || item.pipeEquipmentConnectLines == null || item.pipeEquipmentConnectLines.Count<1)
                    continue;
                //判断是否有废水地漏，雨水管/冷凝水管 W-RAIN-PIPE 废水 W-DRAI-WAST-PIPE
                //洗衣机地漏-废水地漏-拖把池 ----->废水
                item.isWasteWaterPipe = IsWasteWaterPipe(item.pipeBlockUid,item.connectBlockIds);
                string layerName = item.isWasteWaterPipe ? ThWSSCommon.Layout_PipeWastDrainConnectLayerName : ThWSSCommon.Layout_PipeRainDrainConnectLayerName;
                foreach (var line in item.pipeEquipmentConnectLines) 
                {
                    var moveLine = MoveLineToCircleOut(line, _pipeDrainCircles);
                    if (null == moveLine)
                        continue;
                    createBasicElements.Add(new CreateBasicElement(_floorId, moveLine, layerName,item.pipeBlockUid,"DLLG_LJX"));
                }
            }
        }
        Line MoveLineToCircleOut(Line line, List<Circle> checkCircles)
        {
            var sp = line.StartPoint;
            var ep = line.EndPoint;
            var dir = (ep - sp).GetNormal();
            foreach (var circle in checkCircles)
            {
                var spOffSet = 0.0;
                var epOffSet = 0.0;
                if (sp.DistanceTo(circle.Center) < _pipeCenterCheckDistance)
                    spOffSet = circle.Radius;
                else if (ep.DistanceTo(circle.Center) < _pipeCenterCheckDistance)
                    epOffSet = circle.Radius;
                if (spOffSet + epOffSet > (sp.DistanceTo(ep)))
                    return null;
                sp = sp + dir.MultiplyBy(spOffSet);
                ep = ep - dir.MultiplyBy(epOffSet);
            }
            return new Line(sp, ep);
        }
        bool IsWasteWaterPipe(string pipeId,List<string> connectIds) 
        {
            if (connectIds == null || connectIds.Count < 1)
                return false;
            bool isWasteWater = _riserPipe.Where(c=>c.uid == pipeId).First().enumEquipmentType == EnumEquipmentType.wastewaterRiser;
            foreach (var id in connectIds) 
            {
                if (isWasteWater)
                    break;
                //先判断是否是拖把池
                if (null != _mopPools && _mopPools.Count > 0 && _mopPools.Any(c => c.uid.Equals(id))) 
                {
                    isWasteWater = true;
                    break;
                }
                //在判断是否有废水地漏
                if (null != _balconyDrains && _balconyDrains.Count > 0) 
                {
                    if (_balconyDrains.Any(c => c.belongBlockId.Equals(id) && 
                    (c.layerName.Equals(ThWSSCommon.Layout_FloorDrainBlockWastLayerName) )))
                    {
                        isWasteWater = true;
                        break;
                    }
                }
            }
            return isWasteWater;
        }
        List<Circle> GetCheckCircles()
        {
            var checkCircles = new List<Circle>();
            foreach (var pipe in _pipeConverters)
            {
                var radius = DrainSysAGCommon.GetBlockCircleRadius(pipe, "可见性1");
                var circle = new Circle(pipe.createPoint, Vector3d.ZAxis, radius);
                checkCircles.Add(circle);
            }
            foreach (var drain in _allDrains)
            {
                var radius = DrainSysAGCommon.GetBlockCircleRadius(drain, "可见性");
                var circle = new Circle(drain.createPoint, Vector3d.ZAxis, radius);
                checkCircles.Add(circle);
            }
            foreach (var basicElement in createBasicElements)
            {
                if (basicElement.baseCurce is Circle circle1)
                {
                    var circle = new Circle(circle1.Center, circle1.Normal, circle1.Radius);
                    checkCircles.Add(circle);
                }
            }
            return checkCircles;
        }
        Dictionary<Point3d, double> ConnectNoBayDistance(List<Point3d> points) 
        {
            var dicDis = new Dictionary<Point3d, double>();
            foreach (var item in points) 
            {
                double dis = 0.0;
                var circle = _pipeDrainCircles.Where(c => c.Center.DistanceTo(item) < 1).FirstOrDefault();
                if (circle != null)
                    dis = circle.Radius+100;
                dicDis.Add(item, dis);
            }
            return dicDis;
        }

        double CalcCrossWidth(Point3d point,Vector3d outDir,out bool haveBeam) 
        {
            haveBeam = false;
            double crosLength = _pipeCasingLength;
            if (null == _beamPolylines || _beamPolylines.Count < 1)
                return crosLength;
            var beamDis = new Dictionary<Polyline, double>();
            foreach (var item in _beamPolylines) 
            {
                beamDis.Add(item, item.Distance(point));
            }
            var nearPLine = beamDis.OrderBy(c => c.Value).First().Key;
            var nearDis = nearPLine.Distance(point);
            if (nearDis < 5) 
            {
                haveBeam = true;
                Line line = new Line(point - outDir.MultiplyBy(500), point + outDir.MultiplyBy(500));
                var innerLine = nearPLine.Trim(line).OfType<Curve>().ToList().FirstOrDefault();
                if (null != innerLine)
                    crosLength = innerLine.GetLength();
            }
            return crosLength;
        }
    }
    class PipeConnectRelation 
    {
        public string pipeBlockUid { get; }
        public Point3d pipeCenterPoint { get; }
        public List<string> connectBlockIds { get; }
        public List<Line> pipeEquipmentConnectLines { get; }
        public bool isWasteWaterPipe { get; set; }
        public PipeConnectRelation(string pipeUid,Point3d pipePoint) 
        {
            this.pipeBlockUid = pipeUid;
            this.pipeCenterPoint = pipePoint;
            this.connectBlockIds = new List<string>();
            this.pipeEquipmentConnectLines = new List<Line>();
            this.isWasteWaterPipe = false;
        }
    }
}
