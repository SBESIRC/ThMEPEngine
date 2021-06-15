using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    /// <summary>
    /// 厨房、卫生间排布计算逻辑
    /// </summary>
    class RoomKitchenToiletLayout
    {
        public List<CreateBlockInfo> createBlocks = new List<CreateBlockInfo>();
        private List<string> _hispipingShaftRoomIds = new List<string>();
        private List<RoomModel> _toiletRooms = new List<RoomModel>();
        private List<RoomModel> _kitchenRooms = new List<RoomModel>();
        private List<TubeWellsRoomModel> _tubeWellsRooms = new List<TubeWellsRoomModel>();
        private List<RoomPipeRoomRelation> _roomPipeRoomRelations = new List<RoomPipeRoomRelation>();
        private AcadDatabase _acdb;
        /// <summary>
        /// 卫生间、厨房业务逻辑构造方法
        /// </summary>
        /// <param name="toiletRooms">卫生间房间</param>
        /// <param name="kitchenRooms">厨房房间</param>
        /// <param name="tubeWellsRooms">管道井房间信息</param>
        public RoomKitchenToiletLayout(AcadDatabase database,List<RoomModel> toiletRooms,List<RoomModel> kitchenRooms, List<TubeWellsRoomModel> tubeWellsRooms) 
        {
            _acdb = database;
            if (null != toiletRooms && toiletRooms.Count>0)
                foreach (var item in toiletRooms) 
                {
                    _toiletRooms.Add(item);
                }
            if (null != kitchenRooms && kitchenRooms.Count > 0)
                foreach (var item in kitchenRooms)
                {
                    _kitchenRooms.Add(item);
                }
            if (null != tubeWellsRooms && tubeWellsRooms.Count > 0)
                foreach (var item in tubeWellsRooms)
                {
                    _tubeWellsRooms.Add(item);
                }
            //先构造信息，再计算距离
            InitRoomRelation(_toiletRooms);
            InitRoomRelation(_kitchenRooms);
            InitRoomCanLayoutRoomSpace();
        }
        public void KitchenLayout(List<EquipmentBlockSpace> equipmentBlcoks, List<RoomModel> flueRooms) 
        {
            RoomLayout(_kitchenRooms, _toiletRooms, false);
            
            //厨房台盆管道检查
            KitchenBasinPipeCheck(equipmentBlcoks, flueRooms);
        }
        public void ToiletLayout() 
        {
            RoomLayout(_toiletRooms, _kitchenRooms, true);
        }

        private void RoomLayout(List<RoomModel> layoutRooms, List<RoomModel> otherRooms,bool isToilet) 
        {
            foreach (var room in layoutRooms)
            {
                List<TubeWellsRoomModel> tempRooms = new List<TubeWellsRoomModel>();
                foreach (var item in _tubeWellsRooms)
                {
                    if (item == null)
                        continue;
                    if (null != item.innerRoomIds && item.innerRoomIds.Any(c => c.Equals(room.thIFCRoom.Uuid)))
                        tempRooms.Add(item);
                    if (null != item.intersectRoomIds && item.intersectRoomIds.Any(c => c.Equals(room.thIFCRoom.Uuid)))
                        tempRooms.Add(item);
                }
                if (tempRooms == null || tempRooms.Count < 1)
                    //该房间没有找到任何管道井，不进行布置
                    continue;
                var roomModel = LayoutRoomType(room, otherRooms, out int type, out string tId);
                if (null == roomModel)
                    continue;
                if (_hispipingShaftRoomIds.Any(c => c.Equals(roomModel.pipeRoomModel.thIFCRoom.Uuid)))
                    //管道井已经排布过，不进行后续的排布
                    continue;
                _hispipingShaftRoomIds.Add(roomModel.pipeRoomModel.thIFCRoom.Uuid);
                if (type == 1)
                {
                    //独占使用
                    if(isToilet)
                        PipingShaftOnlyToilet(roomModel, room);
                    else
                        PipingShaftOnlyKitchen(roomModel, room);
                }
                else if (type == 2)
                {
                    //卫生间厨房共用
                    var kitchenRoom = room;
                    var toiletRoom = otherRooms.Where(c => c.thIFCRoom.Uuid.Equals(tId)).FirstOrDefault();
                    if (isToilet) 
                    {
                        kitchenRoom = otherRooms.Where(c => c.thIFCRoom.Uuid.Equals(tId)).FirstOrDefault();
                        toiletRoom = room;
                    }
                    PipingShaftKitchenToilet(roomModel, kitchenRoom,toiletRoom);
                }
            }
        }
        private PipeRoomSpace LayoutRoomType(RoomModel room, List<RoomModel> targetRooms,out int type,out string shareRoomId) 
        {
            //这里没有考虑厨房和厨房共用，一个卫生间两侧都有厨房管道井的例子
            type = 1;//1 独占使用，2厨房、卫生间共用
            shareRoomId = "";
            var roomRelation = _roomPipeRoomRelations.Where(c => c.roomModel.thIFCRoom.Uuid.Equals(room.thIFCRoom.Uuid)).FirstOrDefault();
            PipeRoomSpace pipeRoomSpace = null;
            if (!string.IsNullOrEmpty(roomRelation.layoutRoomSpaceId)) 
            {
                //已经确定用哪一个空间了
                pipeRoomSpace = roomRelation.canLayoutRoomSpace.Where(c => c.pipeRoomModel.thIFCRoom.Uuid.Equals(roomRelation.layoutRoomSpaceId)).FirstOrDefault();
            }
            else if (roomRelation.canLayoutRoomSpace.Count == 1)
            {
                //只有一个可以排布的空间
                pipeRoomSpace = roomRelation.canLayoutRoomSpace.FirstOrDefault();
            }
            else 
            {
                //有多个可以排布的空间，判断是否有其它可以共用的有可以共用的优先共用的
                string shareId = "";
                foreach (var relation in _roomPipeRoomRelations)
                {
                    
                    if (relation ==null || relation.roomModel ==null || relation.canLayoutRoomSpace.Count<1 || relation.roomModel.thIFCRoom.Uuid.Equals(room.thIFCRoom.Uuid))
                        continue;
                    foreach (var item in relation.canLayoutRoomSpace) 
                    {
                        if (!string.IsNullOrEmpty(shareId))
                            break;
                        if (relation.canLayoutRoomSpace.Any(c => c.pipeRoomModel.thIFCRoom.Uuid == item.pipeRoomModel.thIFCRoom.Uuid)) 
                        {
                            shareId = item.pipeRoomModel.thIFCRoom.Uuid;
                        }
                    }
                    if (!string.IsNullOrEmpty(shareId))
                    {
                        relation.layoutRoomSpaceId = shareId;
                        break;
                    }
                }
                if (string.IsNullOrEmpty(shareId))
                {
                    //没有和其它有共同空间的，
                    pipeRoomSpace = roomRelation.canLayoutRoomSpace.FirstOrDefault();
                }
                else 
                {
                    pipeRoomSpace = roomRelation.canLayoutRoomSpace.Where(c => c.pipeRoomModel.thIFCRoom.Uuid.Equals(roomRelation.layoutRoomSpaceId)).FirstOrDefault();
                }
            }
            if (null != pipeRoomSpace) 
            {
                foreach (var relation in _roomPipeRoomRelations)
                {
                    if (relation == null || relation.roomModel == null || relation.canLayoutRoomSpace.Count < 1 || relation.roomModel.thIFCRoom.Uuid.Equals(room.thIFCRoom.Uuid))
                        continue;
                    var isShare= relation.layoutRoomSpaceId == pipeRoomSpace.pipeRoomModel.thIFCRoom.Uuid;
                    if (isShare) 
                    {
                        type = 2;
                        shareRoomId = relation.roomModel.thIFCRoom.Uuid;
                        break;
                    }
                }
            }
            return pipeRoomSpace;
        }
        /// <summary>
        /// 厨房独占的管道井布置立管
        /// </summary>
        /// <param name="roomModel"></param>
        /// <param name="room"></param>
        private void PipingShaftOnlyKitchen(PipeRoomSpace roomModel, RoomModel room) 
        {
            //厨房独用
            //一根废水立管（FL）
            //这里立管占位置都是矩形的
            //图纸比例1:50 1:100    带定位立管 定位点在中心处
            //图纸比例1:150         带定位立管150  定位点在左下角
            List<DynBlockWidthLength> blockWidth = new List<DynBlockWidthLength>();
            blockWidth.Add(new DynBlockWidthLength("带定位立管", "DN100","FL"));
            PipingToBlock(roomModel, blockWidth);

        }
        /// <summary>
        /// 卫生间独占的管道井布置立管
        /// </summary>
        /// <param name="roomModel"></param>
        /// <param name="room"></param>
        private void PipingShaftOnlyToilet(PipeRoomSpace roomModel, RoomModel room)
        {
            //卫生间独用
            //管井内必然有一根通气立管（TL）和一根污废立管（PL）。若UI上勾选了“沉箱”，则再增加一根沉箱立管（DL）。
            //Case1无沉箱 TL-PL Case2有沉箱 DL-TL-PL
            List<DynBlockWidthLength> blockWidth = new List<DynBlockWidthLength>();
            if (SetServicesModel.Instance.toiletIsCaisson) 
            {
                blockWidth.Add(new DynBlockWidthLength("带定位立管", SetServicesModel.Instance.wasteSewageVentilationRiserPipeDiameter.ToString(),"DL"));
            }
            blockWidth.Add(new DynBlockWidthLength("带定位立管", SetServicesModel.Instance.wasteSewageVentilationRiserPipeDiameter.ToString(), "TL"));
            blockWidth.Add(new DynBlockWidthLength("带定位立管", SetServicesModel.Instance.wasteSewageWaterRiserPipeDiameter.ToString(), "PL"));

            PipingToBlock(roomModel, blockWidth);
        }

        private void PipingToBlock(PipeRoomSpace roomModel,List<DynBlockWidthLength> dynBlocks) 
        {
            //这里立管占位置都是矩形的
            //图纸比例1:50 1:100    带定位立管 定位点在中心处
            //图纸比例1:150         带定位立管150  定位点在中心处
            var calcWidths = DrainSysAGCommon.GetDynBlockMaxWidth(_acdb, dynBlocks);
            var width = calcWidths.Max(c => c.width);
            var createPoint = roomModel.startPoint + roomModel.shortAxis.MultiplyBy(width / 2);
            foreach (var item in dynBlocks)
            {
                createPoint = createPoint + roomModel.longAxis.MultiplyBy(item.width / 2);
                var block = new CreateBlockInfo(item.blockName, "W-DRAI-EQPM", createPoint);
                block.spaceId = roomModel.pipeRoomModel.thIFCRoom.Uuid;
                block.tag = item.tag;
                block.dymBlockAttr.Add("可见性1", item.dynName);
                createPoint = createPoint + roomModel.longAxis.MultiplyBy(item.width / 2);
                createBlocks.Add(block);
            }
        }
        /// <summary>
        /// 卫生间厨房共用的管道井布置立管
        /// </summary>
        /// <param name="pipeRoomModel"></param>
        /// <param name="kitchenRoomModel"></param>
        /// <param name="toiletRoomModel"></param>
        private void PipingShaftKitchenToilet(PipeRoomSpace pipeRoomModel, RoomModel kitchenRoomModel, RoomModel toiletRoomModel)
        {
            //厨房卫生间共用
            //管井内必然有一根通气立管（TL）和一根污废立管（PL）。若UI上勾选了“沉箱”，则再增加一根沉箱立管（DL）。一根废水立管（FL）
            //若管井仅短边靠近厨房，顺序应为FL-TL-PL或FL-DL-TL-PL
            //若管井长边靠近厨房，则顺序为TL-PL-FL或DL-TL-PL-FL
            var startPoint = GetPipingShaftStartPoint(kitchenRoomModel, pipeRoomModel.pipeRoomModel, out Vector3d shortAxis, out Vector3d longAxis, out bool longNearKitch,true);
            List<DynBlockWidthLength> blockWidth = new List<DynBlockWidthLength>();
            if (longNearKitch)
            {
                //则顺序为TL-PL-FL或DL-TL-PL-FL
                if(SetServicesModel.Instance.toiletIsCaisson)
                {
                    blockWidth.Add(new DynBlockWidthLength("带定位立管", SetServicesModel.Instance.wasteSewageVentilationRiserPipeDiameter.ToString(), "DL"));
                }
                blockWidth.Add(new DynBlockWidthLength("带定位立管", SetServicesModel.Instance.wasteSewageVentilationRiserPipeDiameter.ToString(), "TL"));
                blockWidth.Add(new DynBlockWidthLength("带定位立管", SetServicesModel.Instance.wasteSewageWaterRiserPipeDiameter.ToString(), "PL"));
                blockWidth.Add(new DynBlockWidthLength("带定位立管", SetServicesModel.Instance.wasteSewageWaterRiserPipeDiameter.ToString(), "FL"));
            }
            else 
            {
                //顺序应为FL-TL-PL或FL-DL-TL-PL
                blockWidth.Add(new DynBlockWidthLength("带定位立管", SetServicesModel.Instance.wasteSewageWaterRiserPipeDiameter.ToString(), "FL"));
                if (SetServicesModel.Instance.toiletIsCaisson)
                {
                    blockWidth.Add(new DynBlockWidthLength("带定位立管", SetServicesModel.Instance.wasteSewageVentilationRiserPipeDiameter.ToString(), "DL"));
                }
                blockWidth.Add(new DynBlockWidthLength("带定位立管", SetServicesModel.Instance.wasteSewageVentilationRiserPipeDiameter.ToString(), "TL"));
                blockWidth.Add(new DynBlockWidthLength("带定位立管", SetServicesModel.Instance.wasteSewageWaterRiserPipeDiameter.ToString(), "PL"));
            }
            var pipeRoom = new PipeRoomSpace(pipeRoomModel.pipeRoomModel, startPoint, shortAxis, longAxis,false);
            PipingToBlock(pipeRoom, blockWidth);
        }
        private void KitchenBasinPipeCheck(List<EquipmentBlockSpace> equipmentBlcoks, List<RoomModel> flueRooms) 
        {
            if (null == equipmentBlcoks || equipmentBlcoks.Count < 1)
                return;
            foreach (var item in equipmentBlcoks) 
            {
                if (item == null || item.enumEquipmentType != EnumEquipmentType.kitchenBasin || item.enumRoomType != EnumRoomType.Kitchen)
                    continue;
            }
            foreach (var room in _roomPipeRoomRelations) 
            {
                if (room.roomModel.roomTypeName != EnumRoomType.Kitchen || string.IsNullOrEmpty(room.layoutRoomSpaceId))
                    continue;
                var roomBasins = equipmentBlcoks.Where(c => c.enumEquipmentType == EnumEquipmentType.kitchenBasin && c.roomSpaceId.Equals(room.roomModel.thIFCRoom.Uuid)).ToList();
                if (roomBasins == null || roomBasins.Count() < 1)
                    continue;
                PipeRoomSpace pipeRoomModel = room.canLayoutRoomSpace.Where(c => c.pipeRoomModel.thIFCRoom.Uuid.Equals(room.layoutRoomSpaceId)).FirstOrDefault();
                var pipeRoomPLine = pipeRoomModel.pipeRoomModel.outLine.ToNTSGeometry();
                var roomLines = DrainSysAGCommon.PolyLineToLines(room.roomModel.outLine);
                foreach (var basin in roomBasins)
                {
                    var point = basin.blockPosition;
                    point = new Point3d(point.X, point.Y, 0);
                    var angle = basin.equmBlockReference.Rotation;
                    var yDir = Vector3d.YAxis;
                    yDir = yDir.RotateBy(angle, Vector3d.ZAxis);
                    point = point - yDir.MultiplyBy(50);
                    var xDir = yDir.CrossProduct(Vector3d.ZAxis);
                    //厨房的宽度不会太宽，这里就不通过直线判断
                    Line line = new Line(point - xDir.MultiplyBy(10000), point + xDir.MultiplyBy(10000));
                    if (line.ToNTSGeometry().Crosses(pipeRoomPLine))
                        continue;
                    //没有相应的管道井，进一步判断是否可以生成管道
                    Line nearLine = null;
                    double nearDis = double.MaxValue;
                    foreach (var li in roomLines) 
                    {
                        var nearPoint = li.GetClosestPointTo(point, false);
                        var dis = nearPoint.DistanceTo(point);
                        if (dis < nearDis) 
                        {
                            nearDis = dis;
                            nearLine = li;
                        }
                    }
                    if (null == nearLine)
                        continue;
                    //分别判断线的端点处是否可以放置地漏
                    if(!AddFloorDrainBlock(nearLine, roomLines,room.roomModel.thIFCRoom.Uuid, flueRooms))
                        AddFloorDrainBlock(new Line(nearLine.EndPoint,nearLine.EndPoint), roomLines, room.roomModel.thIFCRoom.Uuid, flueRooms);
                }
            }
        }
        private bool AddFloorDrainBlock(Line line,List<Line> roomLines,string roomId, List<RoomModel> flueRooms) 
        {
            bool isBreak = false;
            var sp = line.StartPoint;
            var ep = line.EndPoint;
            if (null != flueRooms && flueRooms.Count > 0) 
            {
                foreach (var room in flueRooms) 
                {
                    if (isBreak)
                        continue;
                    var closePoint = room.outLine.GetClosestPointTo(sp, false);
                    isBreak = closePoint.DistanceTo(sp) < 200;
                }
            }
            if (isBreak)
                return false;
            var dir = (ep - sp).GetNormal();
            List<Line> spLines = roomLines.Where(c => c.StartPoint.DistanceTo(sp) < 1 || c.EndPoint.DistanceTo(sp) < 1).ToList();
            //获取到应该是两根线
            var line2 = spLines.Where(c => c.StartPoint.DistanceTo(ep) > 5 && c.EndPoint.DistanceTo(ep) > 5).FirstOrDefault();
            if (line2.StartPoint.DistanceTo(sp) > 1)
                line2 = new Line(line2.EndPoint, line2.StartPoint);
            var dir2 = (line2.EndPoint - line2.StartPoint).GetNormal();
            if (line2.Length < 200)
                return false;
            //两个线夹角大不进行放置
            /*
             ---------+
               墙     |  内部
                      |
                    墙|
             */
            if (dir.CrossProduct(dir).Z < 0)
                return false;
            //判断附近是否有烟道井
            List<DynBlockWidthLength> dynBlocks = new List<DynBlockWidthLength>();
            dynBlocks.Add(new DynBlockWidthLength("带定位立管", "DN100", "FL"));
            var calcWidths = DrainSysAGCommon.GetDynBlockMaxWidth(_acdb, dynBlocks);
            var width = calcWidths.Max(c => c.width);
            var createPoint = sp + dir.MultiplyBy(width / 2);
            foreach (var item in dynBlocks)
            {
                createPoint = createPoint + dir2.MultiplyBy(item.width / 2);
                var block = new CreateBlockInfo(item.blockName, "W-DRAI-EQPM", createPoint);
                block.spaceId = roomId;
                block.tag = item.tag;
                block.dymBlockAttr.Add("可见性1", item.dynName);
                createPoint = createPoint + dir2.MultiplyBy(item.width / 2);
                createBlocks.Add(block);
            }
            return false;
        }
        /// <summary>
        /// 初始化房间的和管道井的关系及排布点距离等信息
        /// </summary>
        /// <param name="targetRooms"></param>
        private void InitRoomRelation(List<RoomModel> targetRooms) 
        {
            foreach (var room in targetRooms)
            {
                var roomPline = room.outLine.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
                var roomPipeRoom = new RoomPipeRoomRelation(room);
                foreach (var item in _tubeWellsRooms)
                {
                    if (item == null)
                        continue;
                    PipeRoomSpace pipeRoom = null;
                    if (null != item.innerRoomIds && item.innerRoomIds.Any(c => c.Equals(room.thIFCRoom.Uuid)))
                    {
                        var point = GetPipingShaftStartPoint(room, item.roomModel, out Vector3d shortAxis, out Vector3d longAxis,out bool longNearKitch);
                        pipeRoom = new PipeRoomSpace(item.roomModel, point, shortAxis, longAxis, true);
                    }
                    if (null != item.intersectRoomIds && item.intersectRoomIds.Any(c => c.Equals(room.thIFCRoom.Uuid)))
                    {
                        var point = GetPipingShaftStartPoint(room, item.roomModel, out Vector3d shortAxis, out Vector3d longAxis, out bool longNearKitch);
                        pipeRoom = new PipeRoomSpace(item.roomModel, point, shortAxis, longAxis, false);
                    }
                    if (null == pipeRoom)
                        continue;
                    pipeRoom.minDisToRoom = roomPline.Distance(pipeRoom.startPoint);
                    roomPipeRoom.pipeRoomSpaces.Add(pipeRoom);
                }
                _roomPipeRoomRelations.Add(roomPipeRoom);
            }
        }
        /// <summary>
        /// 根据房间的可能管道井中找可以布置的管道井
        /// </summary>
        private void InitRoomCanLayoutRoomSpace() 
        {
            foreach (var item in _roomPipeRoomRelations) 
            {
                if (item == null || item.pipeRoomSpaces == null || item.pipeRoomSpaces.Count<1)
                    continue;
                var innerRooms = item.pipeRoomSpaces.Where(c => c.inRoom).ToList();
                if (innerRooms != null && innerRooms.Count > 0) 
                {
                    item.canLayoutRoomSpace.AddRange(innerRooms);
                    continue;
                }
                //外部找距离近的
                var minDis = item.pipeRoomSpaces.Min(c => c.minDisToRoom);
                var minDisRooms = item.pipeRoomSpaces.Where(c => Math.Abs(c.minDisToRoom - minDis) < 1).ToList();
                item.canLayoutRoomSpace.AddRange(minDisRooms);
            }
        }
        /// <summary>
        /// 独占房间获取排布点
        /// </summary>
        /// <param name="room">厨房或卫生间</param>
        /// <param name="tubeWells">管道井房间信息</param>
        /// <returns></returns>
        private Point3d GetPipingShaftStartPoint(RoomModel room, RoomModel tubeWell,out Vector3d shortAxis, out Vector3d longAxis, out bool longNearKitch,bool checkNear=false)
        {
            //管井是个矩形。先找到矩形的较短的两侧，然后在两侧中找到距离墙（延长线）较远的边（靠外布置）。这条边认为是布置立管的起点，按顺序往另一条短边依次布置立管。
            //优先排布在长边一侧，靠近房间,靠近房间的线基本和房间线平行，这里不用考虑太多斜边问题
            //房间的形状基本不会太过不规整，这里就不考虑斜边多，怪异形状
            var gmtry = room.outLine.ToNTSGeometry().EnvelopeInternal;
            var centerPoint = new Point3d((gmtry.MinX + gmtry.MaxX) / 2, (gmtry.MinY + gmtry.MaxY) / 2, 0);

            var pipeRoomPline = tubeWell.outLine.ToNTSPolygon().ToDbPolylines().FirstOrDefault();
            var pipeRoomLines = DrainSysAGCommon.PolyLineToLines(pipeRoomPline);
            var objs = new DBObjectCollection();
            pipeRoomLines.ForEach(x => objs.Add(x));
            pipeRoomLines = ThMEPLineExtension.LineSimplifier(objs, 50, 2.0, 2.0, Math.PI / 180.0).Cast<Line>().ToList();
            var pipeRoomPoints = new List<Point3d>();
            foreach (var line in pipeRoomLines) 
            {
                var sp = line.StartPoint;
                var ep = line.EndPoint;
                if (!pipeRoomPoints.Any(c => c.DistanceTo(sp) < 5))
                {
                    pipeRoomPoints.Add(sp);
                }
                if (!pipeRoomPoints.Any(c => c.DistanceTo(ep) < 5))
                {
                    pipeRoomPoints.Add(ep);
                }
            }
            //获取离房间几何中心最近的点作为起点，在根据线计算方向
            var startPoint = pipeRoomPoints.OrderBy(c => c.DistanceTo(centerPoint)).FirstOrDefault();
            var pointLines = pipeRoomLines.Where(c => c.StartPoint.DistanceTo(startPoint) < 5 || c.EndPoint.DistanceTo(startPoint) < 5).ToList();
            var maxLengthLine = pipeRoomLines.OrderByDescending(c => c.Length).FirstOrDefault();
            var dir = (maxLengthLine.EndPoint - maxLengthLine.StartPoint).GetNormal();
            //依据该点可以找到两个线，根据这两根线计算X轴，Y轴，X短轴方向，Y为长轴方向
            shortAxis = new Vector3d();
            longAxis = new Vector3d();
            longNearKitch = false;
            foreach (var line in pointLines) 
            {
                var liDir = (line.EndPoint - line.StartPoint).GetNormal();
                var dot = liDir.DotProduct(dir);
                if (Math.Abs(dot) > 0.5)
                {
                    //线和整体的长轴方向一致
                    if (line.StartPoint.DistanceTo(startPoint) < 5)
                    {
                        longAxis = liDir;
                    }
                    else 
                    {
                        longAxis = liDir.Negate();
                    }
                    //长线，判断是否靠近厨房，或在厨房内
                    if (checkNear) 
                    {
                        if (room.outLine.Contains(startPoint))
                        {
                            //点包含在厨房中
                            longNearKitch = true;
                        }
                        else
                        {
                            //点在厨房外，进一步判断是否长边距离厨房近
                            longNearKitch = line.Buffer(100).ToNTSGeometry().Crosses(room.outLine.ToNTSGeometry());
                        }
                    }
                }
                else 
                {
                    //线和短轴方向一致
                    if (line.StartPoint.DistanceTo(startPoint) < 5)
                    {
                        shortAxis = liDir;
                    }
                    else
                    {
                        shortAxis = liDir.Negate();
                    }
                }
            }
            return startPoint;
        }
    }
    
    class RoomPipeRoomRelation 
    {
        public RoomModel roomModel { get; }
        public string layoutRoomSpaceId { get; set; }
        public List<PipeRoomSpace> canLayoutRoomSpace { get; }
        public List<PipeRoomSpace> pipeRoomSpaces { get; }
        public RoomPipeRoomRelation(RoomModel room) 
        {
            this.roomModel = room;
            this.layoutRoomSpaceId = "";
            this.canLayoutRoomSpace = new List<PipeRoomSpace>();
            this.pipeRoomSpaces = new List<PipeRoomSpace>();
        }
    }
    class PipeRoomSpace 
    {
        public RoomModel pipeRoomModel { get; }
        public double minDisToRoom { get; set; }
        public Point3d startPoint { get; }
        public Vector3d shortAxis { get; }
        public Vector3d longAxis { get; }
        public bool inRoom { get; }
        public PipeRoomSpace(RoomModel pipeRoom,Point3d startLayoutPoint,Vector3d shortAxis,Vector3d longAxis,bool inRoom) 
        {
            this.pipeRoomModel = pipeRoom;
            this.startPoint = startLayoutPoint;
            this.shortAxis = shortAxis;
            this.longAxis = longAxis;
            this.inRoom = inRoom;
        }
    }
}
