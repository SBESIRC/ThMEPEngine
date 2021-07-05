﻿using Autodesk.AutoCAD.Colors;
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
        double _balconyMopPoolRadius = 50;//阳台拖把池排水点半径
        double _floorDrainBalconyPipeNearDistance = 600;// 地漏找阳台立管范围
        double _floorDrainEqumPipeNearDistance = 2500;//地漏找设备平台立管范围
        double _pipeCasingLength = 100;//套管长度
        double _balconyRoomFindNearRoomDistance = 600;//阳台外扩找靠近房间距离
        double _balconyAddPipeFindNearWallDistance = 100;//阳台添加立管时获取添加点外扩找墙距离
        double _balconyAddPipeCenterNearWallDistance = 100;//阳台添加立管中心距墙距离
        double _balconyPipeMaxRadius = 150;//阳台添加立管最大半径
        double _balconyAddPipeMinWallSpace = 200;//阳台添加立管墙最短要求

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

        public List<CreateBasicElement> createBasicElements = new List<CreateBasicElement>();
        public List<CreateBlockInfo> createBlockInfos = new List<CreateBlockInfo>();

        private string _floorId;
        public BalconyCorridorEquPlatform(string floorId,List<RoomModel> balconyRooms, List<RoomModel> corridorRooms, List<RoomModel> otherRooms, List<EquipmentBlockSpace> balcCoorEqus, List<Polyline> allWalls, List<Polyline> allColumns)
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
                    if (item.enumEquipmentType == EnumEquipmentType.balconyRiser || item.enumEquipmentType == EnumEquipmentType.condensateRiser)
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
            if (null != allWalls && allWalls.Count>0) 
            {
                allWalls.ForEach(c => { if (c != null) { _wallPolylines.Add(c); } });
            }
            if (null != allColumns && allColumns.Count > 0) 
            {
                allColumns.ForEach(c => { if (c != null) { _columnPolylines.Add(c); } });
            }
        }
        public void LayoutConnect(List<CreateBlockInfo> balconyDrainCoverter) 
        {
            if (null != balconyDrainCoverter && balconyDrainCoverter.Count > 0) 
            {
                foreach (var item in balconyDrainCoverter) 
                {
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
            //阳台设备、立管连线
            BalconyCorridorConnect(balcCorridRooms);
            //根据连接设备判断连线图层
            PipeRelationToLayoutLine();
            //立管转换、需要根据立管类型
            RaiseConvert();
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
                var centerPoint = item.blockCenterPoint;
                var circle = new Circle(centerPoint, Vector3d.ZAxis, _balconyMopPoolRadius);
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
                BalconyCorridorSpaceConnect(room,roomLeftBlocks, centerPoint,lineDir);
                BalconyCorridorSpaceConnect(room,roomRightBlocks, centerPoint, lineDir);
            }
        }

        void BalconyCorridorSpaceConnect(RoomModel balcCorrRoom,List<EquipmentBlockSpace> connectSpaceEqum,Point3d centerPoint,Vector3d lineDir) 
        {
            if (null == connectSpaceEqum || connectSpaceEqum.Count < 1)
                return;
            //step1 阳台内部处理   将和阳台立管同一部分的所有地漏和拖把池排水都接入此立管
            var pipe = connectSpaceEqum.Where(c => c.enumEquipmentType == EnumEquipmentType.balconyRiser).FirstOrDefault();
            var otherBlocks = connectSpaceEqum.Where(c => c.enumEquipmentType != EnumEquipmentType.balconyRiser).ToList();
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
                pipeRelation.connectBlockIds.AddRange(otherBlocks.Select(c=>c.uid).ToList());
                if (otherBlocks.Count == 1)
                {
                    //只有一个点位，直接连接立管
                    Line addLine = new Line(otherBlocks.FirstOrDefault().blockCenterPoint, pipe.blockCenterPoint);
                    pipeRelation.pipeEquipmentConnectLines.Add(addLine);
                }
                else 
                {
                    //以立管为中心，做lineDir为X轴，垂直方向为Y轴，将其它的点位根据 距离轴
                    var connectPipe = new PipeDrainConnect(pipe.blockCenterPoint, otherBlocks.Select(c => c.blockCenterPoint).ToList());
                    var lines = connectPipe.PipeDrainConnectByMainAxis(lineDir);
                    if (null != lines && lines.Count > 0)
                        pipeRelation.pipeEquipmentConnectLines.AddRange(lines);
                }
                return;
            }
            //阳台连管侧没有立管
            if (BalconyCorridorSpaceConnectPlanB(balcCorrRoom,connectSpaceEqum,centerPoint,lineDir))
                return;
            //阳台侧没有立管，在一定范围内也没有设备平台立管
            BalconyCorridorSpaceCoonectPlanC(balcCorrRoom,connectSpaceEqum, centerPoint, lineDir);
        }
        bool BalconyCorridorSpaceConnectPlanB(RoomModel balcCorrRoom, List<EquipmentBlockSpace> connectSpaceEqum, Point3d roomCenterPoint, Vector3d lineDir) 
        {
            //step2 阳台内没有找到立管，找设备平台的立管进行连接  中心为起点在2500范围内找设备平台的阳台立管，若存在多个则取最近的一个。
            //连管需沿着阳台框线的方向走，尽量横平竖直（较复杂 看图详解）。
            //lineDir 为短轴方向
            var geo = connectSpaceEqum.FirstOrDefault().equmBlockReference.GeometricExtents;
            for (int i =1;i<connectSpaceEqum.Count;i++) 
            {
                geo.AddExtents(connectSpaceEqum[i].equmBlockReference.GeometricExtents);
            }
            var centerPoint = geo.CenterPoint();
            var targetPipes = new List<EquipmentBlockSpace>();
            foreach (var pipe in _riserPipe) 
            {
                if (pipe.enumRoomType == EnumRoomType.Other || pipe.enumRoomType == EnumRoomType.EquipmentPlatform)
                { 
                    if(pipe.enumEquipmentType == EnumEquipmentType.balconyRiser)
                        targetPipes.Add(pipe);
                }
            }
            if (targetPipes == null || targetPipes.Count < 1)
                return false;
            var nearPipe = targetPipes.OrderBy(c => c.blockCenterPoint.DistanceTo(centerPoint)).FirstOrDefault();
            if (nearPipe.blockCenterPoint.DistanceTo(centerPoint) >= _floorDrainEqumPipeNearDistance)
                return false;

            var pipeRelation = _pipeConnectRelations.Where(c => c.pipeBlockUid.Equals(nearPipe.uid)).FirstOrDefault();
            if (pipeRelation == null)
            {
                _pipeConnectRelations.Add(new PipeConnectRelation(nearPipe.uid, nearPipe.blockCenterPoint));
                pipeRelation = _pipeConnectRelations.Where(c => c.pipeBlockUid.Equals(nearPipe.uid)).FirstOrDefault();
            }
            pipeRelation.connectBlockIds.AddRange(connectSpaceEqum.Select(c=>c.uid).ToList());

            //房间轮廓取obb
            var obbPline = balcCorrRoom.outLine; //balcCorrRoom.GetRoomOBBPolyline();
            var nearLine = NearRoomLine(obbPline, nearPipe.blockCenterPoint);
            var points = connectSpaceEqum.Select(c => c.blockCenterPoint).ToList();
            Point3d crossPoint = new Point3d();
            Vector3d outDir = new Vector3d();
            if (nearLine == null)
            {
                //不能直接穿过，根据点位信息，进一步计算弯折信息
                obbPline = balcCorrRoom.GetRoomOBBPolyline();
                var orderPts = PointVectorUtil.PointsOrderByDirection(points, lineDir, nearPipe.blockCenterPoint);
                var nearPoint = orderPts.OrderBy(c => c.Value).FirstOrDefault().Key;
                var roomNearLine = NearRoomLine(obbPline, nearPoint, lineDir);
                crossPoint = nearPoint.PointToLine(roomNearLine);
                outDir = (crossPoint - nearPoint).GetNormal();
                var connectPipeInner = new PipeDrainConnect(crossPoint, points);
                var lines = connectPipeInner.PipeDrainConnectByMainAxis(outDir);
                if (null != lines && lines.Count > 0)
                {
                    pipeRelation.pipeEquipmentConnectLines.AddRange(lines);
                    var connectPipeOut = new PipeDrainConnect(crossPoint, new List<Point3d> { nearPipe.blockCenterPoint });
                    var pipeLines = connectPipeOut.PipeDrainConnectByMainAxis(outDir);
                    if (null != lines && lines.Count > 0)
                        pipeRelation.pipeEquipmentConnectLines.AddRange(lines);
                }
            }
            else 
            {
                //可以直接穿过,根据穿过的线
                crossPoint = nearPipe.blockCenterPoint.PointToLine(nearLine);
                outDir = (nearPipe.blockCenterPoint -crossPoint).GetNormal();

                var connectPipe = new PipeDrainConnect(crossPoint,points);
                var lines = connectPipe.PipeDrainConnectByMainAxis(outDir);
                //var lines = CenterConnect(crossPoint, lineDir, points,true);
                if (null != lines && lines.Count > 0)
                {
                    pipeRelation.pipeEquipmentConnectLines.AddRange(lines);
                    var nLine = new Line(nearPipe.blockCenterPoint, crossPoint);
                    pipeRelation.pipeEquipmentConnectLines.Add(nLine);
                }
            }
            //在横管穿越阳台和设备平台的位置设置穿墙套管。 图层：W-BUSH  图块：套管-AI    可见性：普通套管
            var addBlock = new CreateBlockInfo(_floorId, ThWSSCommon.Layout_PipeCasingBlockName, ThWSSCommon.Layout_PipeCasingLayerName, crossPoint, EnumEquipmentType.other);
            var angle = (-Vector3d.YAxis).GetAngleTo(outDir, Vector3d.ZAxis);
            addBlock.rotateAngle = angle%(Math.PI *2);
            addBlock.dymBlockAttr.Add("可见性", "普通套管");
            addBlock.dymBlockAttr.Add("距离", _pipeCasingLength);
            createBlockInfos.Add(addBlock);
            return true;
        }
        bool BalconyCorridorSpaceCoonectPlanC(RoomModel balcCorrRoom,List<EquipmentBlockSpace> connectSpaceEqum,Point3d roomCenterPoint, Vector3d roomXAxis) 
        {
            //step3 既没有内部，也没有设备平台的立管
            //优先在超户型内部的方向的就近墙角设置立管。不能挡门或窗，只能放在墙厚大于200的墙角。管心距离墙的两边100。如果墙角的墙厚不满足要求，则朝阳台外层的就近墙角。
            var yAxis = Vector3d.ZAxis.CrossProduct(roomXAxis);
            var points = connectSpaceEqum.Select(c => c.blockCenterPoint).ToList();
            var orderByDir = PointVectorUtil.PointsOrderByDirection(points, yAxis,false);
            var nearPoint = orderByDir.FirstOrDefault();
            //这里认为阳台墙是直墙，没有考虑斜墙，非正交的墙
            //根据原始轮廓，找到相应的角落，判断是否可以放下立管
            List<Line> roomLines = DrainSysAGCommon.PolyLineToLines(balcCorrRoom.outLine);
            Dictionary<Line,Point3d> targetLines = new Dictionary<Line, Point3d>();
            foreach (var line in roomLines) 
            {
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                var interRes = PointVectorUtil.LineIntersectionLine(nearPoint, roomXAxis, line.StartPoint, lineDir, out Point3d interPoint);
                if (interRes != 1)
                    continue;
                if (!interPoint.PointInLineSegment(line))
                    continue;
                targetLines.Add(line, interPoint);
            }
            //获取阳台旁边的相交的面积最大的房间（客餐厅，或卧室）
            RoomModel nearRoom = null;
            double maxArea = double.MinValue;
            var extendOutLine = balcCorrRoom.outLine.Buffer(_balconyRoomFindNearRoomDistance).ToNTSMultiPolygon();
            foreach (var room in _otherRooms) 
            {
                if (!extendOutLine.Intersects(room.outLine.ToNTSGeometry()))
                    continue;
                if (maxArea < room.outLine.Area)
                {
                    nearRoom = room;
                    maxArea = room.outLine.Area;
                }
            }
            var orderPoints = PointVectorUtil.PointsOrderByDirection(targetLines.Select(c => c.Value).ToList(), roomXAxis,false, nearPoint);
            if (orderPoints.Count < 1)
                return false;
            var firstPoint = orderPoints.FirstOrDefault();
            var lastPoint = orderPoints.LastOrDefault();
            if (null != nearRoom)
            {
                var firstNearPoint = nearRoom.outLine.GetClosestPointTo(firstPoint, false);
                var lastNearPoint = nearRoom.outLine.GetClosestPointTo(lastPoint, false);
                if (firstNearPoint.DistanceTo(firstPoint) > lastPoint.DistanceTo(lastNearPoint))
                {
                    roomXAxis = roomXAxis.Negate();
                    var point = new Point3d(firstPoint.X, firstPoint.Y, 0);
                    firstPoint = lastPoint;
                    lastPoint = point;
                }
            }
            Point3d centerPoint = new Point3d();
            var firstDis = firstPoint.DistanceTo(nearPoint);
            var lastDis = lastPoint.DistanceTo(nearPoint);
            //两侧都不能放置,随便放置一侧
            if (firstDis > _balconyPipeMaxRadius*2 && lastDis > _balconyPipeMaxRadius * 2)
            {
                if (CanAddPipe(firstPoint, roomXAxis, yAxis, out centerPoint)) {}
                else if (CanAddPipe(lastPoint, roomXAxis.Negate(), yAxis, out centerPoint)) {}
                else 
                {
                    if (firstDis < lastDis)
                        centerPoint = firstPoint + roomXAxis.MultiplyBy(_balconyAddPipeCenterNearWallDistance);
                    else 
                        centerPoint = lastPoint - roomXAxis.MultiplyBy(_balconyAddPipeCenterNearWallDistance);
                }
            }
            else if (firstDis > _balconyPipeMaxRadius*1.5)
            {
                centerPoint = firstPoint + roomXAxis.MultiplyBy(_balconyAddPipeCenterNearWallDistance);
            }
            else
            {
                centerPoint = lastPoint - roomXAxis.MultiplyBy(_balconyAddPipeCenterNearWallDistance);
            }
            //根据布置点，在进行连接
            var addPipeRelation = new PipeConnectRelation(Guid.NewGuid().ToString(), centerPoint);
            addPipeRelation.connectBlockIds.AddRange(connectSpaceEqum.Select(c => c.uid).ToList());
            if (connectSpaceEqum.Count == 1)
            {
                //只有一个点位，直接连接立管
                Line addLine = new Line(connectSpaceEqum.FirstOrDefault().blockCenterPoint, centerPoint);
                addPipeRelation.pipeEquipmentConnectLines.Add(addLine);
            }
            else 
            {
                var connectPipeInner = new PipeDrainConnect(centerPoint, points);
                //var lines = CenterConnect(centerPoint, roomXAxis, points);
                var lines = connectPipeInner.PipeDrainConnectByMainAxis(roomXAxis);
                if (null != lines && lines.Count > 0)
                    addPipeRelation.pipeEquipmentConnectLines.AddRange(lines);
            }
            _pipeConnectRelations.Add(addPipeRelation);
            return false;
        }
        bool CanAddPipe(Point3d point,Vector3d xAxis,Vector3d yAxis,out Point3d centerPoint) 
        {
            centerPoint = new Point3d();
            var lineSp = point - yAxis.MultiplyBy(_balconyPipeMaxRadius);
            var lineEp = point + yAxis.MultiplyBy(_balconyPipeMaxRadius);
            var line = new Line(lineSp, lineEp);
            var pline = line.Buffer(_balconyAddPipeFindNearWallDistance);
            //获取相交墙
            List<Polyline> walls = new List<Polyline>();
            foreach (var wall in _wallPolylines) 
            {
                if (pline.Intersects(wall))
                    walls.Add(wall);
            }
            if (null == walls || walls.Count < 1)
                return false;
            var spColsePoint = walls.OrderBy(c => c.GetClosestPointTo(lineSp, false)).FirstOrDefault();
            var epColsePoint = walls.OrderBy(c => c.GetClosestPointTo(lineEp, false)).FirstOrDefault();
            if (spColsePoint.Distance(epColsePoint) < _balconyAddPipeMinWallSpace)
                return false;
            centerPoint = point + xAxis.MultiplyBy(_balconyAddPipeCenterNearWallDistance);
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
        Line NearRoomLine(Polyline roomPLine, Point3d targetPoint,Vector3d targetLineDirection)
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
                if (prjPoint.PointInLineSegment(line))
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
                item.isWasteWaterPipe = IsWasteWaterPipe(item.connectBlockIds);
                string layerName = item.isWasteWaterPipe ? ThWSSCommon.Layout_PipeWastDrainConnectLayerName : ThWSSCommon.Layout_PipeRainDrainConnectLayerName;
                foreach (var line in item.pipeEquipmentConnectLines) 
                {
                    createBasicElements.Add(new CreateBasicElement(_floorId,line, layerName,item.pipeBlockUid,"DLLG_LJX"));
                }
            }
        }
        bool IsWasteWaterPipe(List<string> connectIds) 
        {
            if (connectIds == null || connectIds.Count < 1)
                return false;
            bool isWasteWater = false;
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

        void RaiseConvert() 
        {
            //1：50和1:100  带定位立管    1:150 带定位立管150   可见性就是管径来自于UI
            string blockName = SetServicesModel.Instance.drawingScale == EnumDrawingScale.DrawingScale1_150 ? ThWSSCommon.Layout_PositionRiser150BlockName : ThWSSCommon.Layout_PositionRiserBlockName;
            string flDim = SetServicesModel.Instance.balconyWasteWaterRiserPipeDiameter.ToString();
            string rainDim = SetServicesModel.Instance.balconyRiserPipeDiameter.ToString();
            string corrDim = SetServicesModel.Instance.condensingRiserPipeDiameter.ToString();
            foreach (var item in _pipeConnectRelations)
            {
                var pipeBlock = _riserPipe.Where(c => c.uid.Equals(item.pipeBlockUid)).FirstOrDefault();
                var createPoint = item.pipeCenterPoint;
                if (pipeBlock != null && pipeBlock.enumEquipmentType == EnumEquipmentType.condensateRiser)
                {
                    //冷凝立管转换
                    var block = new CreateBlockInfo(_floorId, blockName, ThWSSCommon.Layout_FloorDrainBlockRainLayerName, createPoint, EnumEquipmentType.condensateRiser);
                    block.tag = "NL";
                    block.dymBlockAttr.Add("可见性1", corrDim);
                    createBlockInfos.Add(block);
                }
                else if (item.isWasteWaterPipe)
                {
                    //废水立管
                    var block = new CreateBlockInfo(_floorId,blockName, ThWSSCommon.Layout_WastWaterPipeLayerName, createPoint,EnumEquipmentType.balconyRiser);
                    block.tag = "FL";
                    block.dymBlockAttr.Add("可见性1", flDim);
                    createBlockInfos.Add(block);
                }
                else 
                {
                    //雨水立管
                    var block = new CreateBlockInfo(_floorId,blockName, ThWSSCommon.Layout_FloorDrainBlockRainLayerName, createPoint, EnumEquipmentType.balconyRiser);
                    block.tag = "Y2L";
                    block.dymBlockAttr.Add("可见性1", rainDim);
                    createBlockInfos.Add(block);
                }
            }
            //冷凝立管转换
            foreach (var pipe in _riserPipe) 
            {
                if (pipe.enumEquipmentType != EnumEquipmentType.condensateRiser)
                    continue;
                if (_pipeConnectRelations.Any(c => c.pipeBlockUid.Equals(pipe.uid)))
                    continue;
                var block = new CreateBlockInfo(_floorId,blockName, ThWSSCommon.Layout_FloorDrainBlockRainLayerName, pipe.blockCenterPoint, EnumEquipmentType.condensateRiser);
                block.tag = "NL";
                block.dymBlockAttr.Add("可见性1", corrDim);
                createBlockInfos.Add(block);
            }
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
