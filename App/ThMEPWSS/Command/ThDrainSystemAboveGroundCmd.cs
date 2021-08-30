using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPWSS.DrainageSystemAG;
using ThMEPWSS.DrainageSystemAG.Bussiness;
using ThMEPWSS.DrainageSystemAG.DataEngine;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.DrainageSystemAG.Services;
using ThMEPWSS.Engine;
using ThMEPWSS.Model;
using ThMEPWSS.ViewModel;

namespace ThMEPWSS.Command
{
    /// <summary>
    /// 地上排水系统
    /// </summary>
    public class ThDrainSystemAboveGroundCmd : IAcadCommand, IDisposable
    {
        public string errorMsg = "";
        ThWallColumnsEngine _wallColumnsEngine = null;
        ThRoomDataEngine _roomEngine = null;
        DoorWindowEngine _doorWindowEngine = null;
        BasicElementEngine _basicElementEngine;
        BlockReferenceDataEngine _blockReferenceData;

        List<FloorFramed> floorFrameds = new List<FloorFramed>();
        List<FloorFramed> roofFloors = new List<FloorFramed>();
        FloorFramed livingHighestFloor = null;

        List<Polyline> _allWalls;
        List<Polyline> _allColumns;

        List<CreateBlockInfo> createBlockInfos = new List<CreateBlockInfo>();
        List<CreateBasicElement> createBasicElems = new List<CreateBasicElement>();
        List<CreateDBTextElement> createTextElems = new List<CreateDBTextElement>();
        List<EquipmentBlcokModel> _floorBlockEqums = new List<EquipmentBlcokModel>();
        List<EquipmentBlockSpace> _classifyResult = new List<EquipmentBlockSpace>();
        Dictionary<string, List<string>> _configLayerNames =new Dictionary<string, List<string>>();

        double _obstacleAxisAngle = 5;
        List<EnumEquipmentType> _obstacleBlockTypes = new List<EnumEquipmentType>
        {
            EnumEquipmentType.equipment,
            EnumEquipmentType.buildingElevation,
            EnumEquipmentType.airConditioningOutMachine,
            EnumEquipmentType.door,
            EnumEquipmentType.stairs,
        };
        public ThDrainSystemAboveGroundCmd(List<FloorFramed> selectFloors, DrainageSystemAGViewmodel viewmodel,Dictionary<string,List<string>> layerNames) 
        {
            _configLayerNames.Clear();
            if (null != selectFloors && selectFloors.Count > 0)
                selectFloors.ForEach(c => { if (c != null) floorFrameds.Add(c); });
            if (null != viewmodel) 
            {
                SetServicesModel.Instance.drawingScale = (EnumDrawingScale)viewmodel.ScaleSelectItem.Value;
                var intSVPipeDiam = (int)viewmodel.WSVPipeDiameterSelectItem.Value;
                if (intSVPipeDiam < 0)
                {
                    SetServicesModel.Instance.haveSewageVentilation = false;
                }
                else 
                {
                    SetServicesModel.Instance.haveSewageVentilation = true;
                    SetServicesModel.Instance.wasteSewageVentilationRiserPipeDiameter = (EnumPipeDiameter)viewmodel.WSVPipeDiameterSelectItem.Value;
                }
                SetServicesModel.Instance.caissonRiserPipeDiameter = (EnumPipeDiameter)viewmodel.CaissonRiseSelectItem.Value;
                SetServicesModel.Instance.wasteSewageWaterRiserPipeDiameter = (EnumPipeDiameter)viewmodel.WSWPipeDiameterSelectItem.Value;
                SetServicesModel.Instance.toiletIsCaisson = viewmodel.ToiletIsCaisson;
                
                SetServicesModel.Instance.balconyRiserPipeDiameter = (EnumPipeDiameter)viewmodel.BPipeDiameterSelectItem.Value;
                SetServicesModel.Instance.balconyWasteWaterRiserPipeDiameter = (EnumPipeDiameter)viewmodel.BWWPipeDiameterSelectItem.Value;
                SetServicesModel.Instance.condensingRiserPipeDiameter = (EnumPipeDiameter)viewmodel.CPipeDiameterSelectItem.Value;

                SetServicesModel.Instance.roofRainRiserPipeDiameter = (EnumPipeDiameter)viewmodel.RPipeDiameterSelectItem.Value;
                SetServicesModel.Instance.maxRoofGravityRainBucketRiserPipeDiameter = (EnumPipeDiameter)viewmodel.MRGPipeDiameterSelectItem.Value;
                SetServicesModel.Instance.maxRoofSideDrainRiserPipeDiameter = (EnumPipeDiameter)viewmodel.MRSPipeDiameterSelectItem.Value;
                SetServicesModel.Instance.minRoofGravityRainBucketRiserPipeDiameter = (EnumPipeDiameter)viewmodel.MIRGPipeDiameterSelectItem.Value;
                SetServicesModel.Instance.minRoofSideDrainRiserPipeDiameter = (EnumPipeDiameter)viewmodel.MIRSPipeDiameterSelectItem.Value;
            }
            if (null != layerNames && layerNames.Count > 0) 
            {
                foreach (var keyValue in layerNames) 
                {
                    if (string.IsNullOrEmpty(keyValue.Key) || keyValue.Value == null || keyValue.Value.Count < 1)
                        continue;
                    var tempListNames = new List<string>();
                    foreach (var str in keyValue.Value) 
                    {
                        if (string.IsNullOrEmpty(str) || tempListNames.Any(c=>c.Equals(str)))
                            continue;
                        tempListNames.Add(str);
                    }
                    if (tempListNames.Count < 1)
                        continue;
                    _configLayerNames.Add(keyValue.Key,tempListNames);
                }
            }
        }
        public void Dispose(){}

        public void Execute()
        {
            errorMsg = "";
            if (null == floorFrameds || floorFrameds.Count < 1 || Active.Document == null)
                return;
            Active.Document.LockDocument();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                //所有的楼层框 必须有顶层，没有时不进行后续的生成
                //var allFrames = FramedReadUtil.ReadAllFloorFramed();
                _roomEngine = new ThRoomDataEngine();
                var tempRooms = _roomEngine.GetAllRooms(new Point3dCollection());
                if (!CheckData(floorFrameds))
                {
                    if (!string.IsNullOrEmpty(errorMsg)) 
                        Active.Database.GetEditor().WriteMessage(errorMsg);
                    return;
                }
                InitData(acdb.Database);
                var allRooms = _roomEngine.GetAllRooms(livingHighestFloor.blockOutPointCollection);
                
                var tubeBlocks = new List<BlockReference>();
                var flueBlocks = new List<BlockReference>();
                foreach (var item in _floorBlockEqums)
                {
                    if (item.enumEquipmentType == EnumEquipmentType.waterTubeWell)
                        tubeBlocks.AddRange(item.blockReferences);
                    else if (item.enumEquipmentType == EnumEquipmentType.flueWell)
                        flueBlocks.AddRange(item.blockReferences);
                }
                var tubeFlueRooms = _roomEngine.TubeFlueWellToRoom(tubeBlocks, flueBlocks);
                var rooms = _roomEngine.GetRoomModelRooms(allRooms, tubeFlueRooms);
                rooms = rooms.Where(c => c.outLine.Area > 100).ToList();
                //对设备数据进行分类
                var classifyEqumBlock = new ClassifyEqumBlockByRoomSpace(rooms, _floorBlockEqums);
                _classifyResult = classifyEqumBlock.GetClassifyEquipments();

                //对房间进行分类处理
                var tRooms = _roomEngine.GetTubeWellRooms(rooms);
                var tubeRooms = DrainSysAGCommon.GetTubeWellRoomRelation(rooms.Where(c => c.roomTypeName == Model.EnumRoomType.Toilet || c.roomTypeName == Model.EnumRoomType.Kitchen).ToList(), tRooms);
                var kitchenRooms = rooms.Where(c => c.roomTypeName == EnumRoomType.Kitchen).ToList();
                var toiletRooms = rooms.Where(c => c.roomTypeName == EnumRoomType.Toilet).ToList();

                //标准层、非标层的房间布置逻辑
                //地漏转换
                var blocks = FloorDrainConvert.FloorDrainConvertToBlock(livingHighestFloor.floorUid,
                    _classifyResult.Where(c => c.enumEquipmentType == EnumEquipmentType.floorDrain).ToList(),
                    _classifyResult.Where(c => c.enumEquipmentType == EnumEquipmentType.washingMachine).ToList());
                if (null != blocks && blocks.Count > 0)
                    createBlockInfos.AddRange(blocks);

                //厨房卫生间逻辑
                //var roomKitchenToiletLayout = new RoomKitchenToiletLayout(livingHighestFloor.floorUid, toiletRooms, kitchenRooms, tubeRooms);
                //roomKitchenToiletLayout.ToiletLayout();
                //roomKitchenToiletLayout.KitchenLayout(_classifyResult.Where(c => c.enumRoomType == EnumRoomType.Kitchen).ToList(), 
                //    rooms.Where(c => c.roomTypeName == EnumRoomType.FlueWell).ToList());
                //if (null != roomKitchenToiletLayout.createBlocks && roomKitchenToiletLayout.createBlocks.Count > 0)
                //    createBlockInfos.AddRange(roomKitchenToiletLayout.createBlocks);

                var converterTypes = new List<EnumEquipmentType>
                {
                    EnumEquipmentType.sewageWasteRiser,
                    EnumEquipmentType.ventRiser,
                    EnumEquipmentType.wastewaterRiser,
                    EnumEquipmentType.caissonRiser,
                    EnumEquipmentType.ventRiser,
                    EnumEquipmentType.balconyRiser,
                    EnumEquipmentType.condensateRiser,
                };
                var pipeConverter = RaisePipeConvert.ConvetPipeToBlock(livingHighestFloor.floorUid, _classifyResult.Where(c => converterTypes.Any(x => x == c.enumEquipmentType)).ToList());
                if (null != pipeConverter && pipeConverter.Count > 0) 
                {
                    createBlockInfos.AddRange(pipeConverter);
                }

                //阳台逻辑
                var balconyRooms = rooms.Where(c => c.roomTypeName == EnumRoomType.Balcony).ToList();
                var corridorRooms = rooms.Where(c => c.roomTypeName == EnumRoomType.Corridor).ToList();
                var equpBlockRoomTypes = new List<EnumRoomType>
                {
                    EnumRoomType.Other,
                    EnumRoomType.Corridor,
                    EnumRoomType.Balcony,
                    EnumRoomType.EquipmentPlatform
                };
                var equpBlocks = _classifyResult.Where(c => equpBlockRoomTypes.Any(x=>x == c.enumRoomType)).ToList();
                var otherRooms = new List<RoomModel>();
                foreach (var room in rooms)
                {
                    if (room == null || room.roomTypeName == EnumRoomType.Balcony || room.roomTypeName == EnumRoomType.Corridor)
                        continue;
                    otherRooms.Add(room);
                }
                var balconyCorridorEqu = new BalconyCorridorEquPlatform(livingHighestFloor.floorUid, balconyRooms, corridorRooms, otherRooms, equpBlocks, _allWalls, _allColumns);
                balconyCorridorEqu.LayoutConnect(createBlockInfos,out List<string> changeY1ToFLIds,out List<string> changeDrainToFDrainIds);
                if ((null != changeY1ToFLIds && changeY1ToFLIds.Count > 0) || (null != changeDrainToFDrainIds && changeDrainToFDrainIds.Count > 0)) 
                {
                    foreach (var item in createBlockInfos)
                    {
                        if (item.equipmentType != EnumEquipmentType.balconyRiser && item.equipmentType != EnumEquipmentType.floorDrain)
                            continue;
                        if (item.equipmentType == EnumEquipmentType.floorDrain && null != changeDrainToFDrainIds && changeDrainToFDrainIds.Any(c => c == item.belongBlockId))
                        {
                            item.dymBlockAttr.Clear();
                            item.dymBlockAttr.Add("可见性", FloorDrainConvert.FloorDrainWMachineBlockDynName);
                        }
                        else if (item.equipmentType == EnumEquipmentType.balconyRiser && null != changeY1ToFLIds && changeY1ToFLIds.Any(c => c == item.belongBlockId)) 
                        {
                            //item.equipmentType = EnumEquipmentType.wastewaterRiser;
                            item.tag = "FL";
                        }
                    }
                }
                if (balconyCorridorEqu.createBasicElements != null && balconyCorridorEqu.createBasicElements.Count > 0)
                    createBasicElems.AddRange(balconyCorridorEqu.createBasicElements);
                if (balconyCorridorEqu.createBlockInfos != null && balconyCorridorEqu.createBlockInfos.Count > 0)
                    createBlockInfos.AddRange(balconyCorridorEqu.createBlockInfos);

                var midY = LivingFloorMidY(rooms, createBlockInfos.Where(c => c.floorId.Equals(livingHighestFloor.floorUid)).ToList());
                RoofPipeLabelLayout(midY, _classifyResult.Where(c=>c.enumEquipmentType == EnumEquipmentType.roofRainRiser).ToList());
                
                var createBlocks = CreateBlockService.CreateBlocks(acdb.Database, createBlockInfos);
                var createElems = CreateBlockService.CreateBasicElement(acdb.Database, createBasicElems);
                var createTexts = CreateBlockService.CreateTextElement(acdb.Database, createTextElems);
                //根据生成的数据，将同一图纸中的进行创建block
                foreach (var floor in floorFrameds) 
                {
                    var thisFloorBlocks = createBlocks.Where(c => c.floorUid.Equals(floor.floorUid)).ToList();
                    var thisFloorElems = createElems.Where(c => c.floorUid.Equals(floor.floorUid)).ToList();
                    var thisFloorTexts = createTexts.Where(c => c.floorUid.Equals(floor.floorUid)).ToList();

                    var createEntitys = new List<Entity>();
                    thisFloorBlocks.ForEach(c => createEntitys.Add(acdb.Element<Entity>(c.objectId)));
                    thisFloorElems.ForEach(c => createEntitys.Add(acdb.Element<Entity>(c.objectId)));
                    thisFloorTexts.ForEach(c => createEntitys.Add(acdb.Element<Entity>(c.objectId)));
                    if (createEntitys.Count < 1)
                        continue;
                    string blockName = string.Format("{0}", DrainSysAGCommon.BLOCKNAMEPREFIX);
                    int i = 0;
                    while (acdb.Database.BlockTable().Has(blockName)) 
                    {
                        i += 1;
                        blockName = string.Format("{0}-{1}", DrainSysAGCommon.BLOCKNAMEPREFIX, i);
                    }
                    var record = ThBlockTools.AddBlockTableRecordDBEntity(acdb.Database, blockName,floor.datumPoint, createEntitys.Select(c=>c.ObjectId).ToArray(),true);
                    var blockRecord = acdb.Blocks.Element(record);
                    acdb.ModelSpace.ObjectId.InsertBlockReference("0", blockRecord.Name, floor.datumPoint, new Scale3d(),0.0);
                }
            }
        }

        void InitData(Database database) 
        {
            //载入数据
            ClearLoadBlockServices.LoadBlockLayerToDocument(database);
            ClearLoadBlockServices.ClearHisFloorBlock(database, floorFrameds.Select(c => c.outPolyline).ToList());

            _basicElementEngine = new BasicElementEngine();
            _blockReferenceData = new BlockReferenceDataEngine(_configLayerNames);
            try
            {
                _doorWindowEngine = new DoorWindowEngine(database);
            }
            catch (Exception ex) { }

            _allColumns = new List<Polyline>();
            _allWalls = new List<Polyline>();
            //获取相应的数据，框线内的房间，烟道井，墙，柱
            try
            {
                _wallColumnsEngine = new ThWallColumnsEngine();
                _wallColumnsEngine.GetStructureInfo(livingHighestFloor.outPolyline, out _allColumns, out _allWalls);
            }
            catch (Exception ex) { }

            //去重，在一定范围内不能有同一类的数据
            _floorBlockEqums = InitFloorData(livingHighestFloor);
        }
        void RoofPipeLabelLayout(double midY, List<EquipmentBlockSpace> livingFloorY1LBlocks) 
        {
            //屋面数据处理
            //有大屋面时，找到住人顶层的所有污废立管（PL）和废水立管（FL）。将所有的立管和编号标注根据基点复制到大屋面。
            var roofV = GetRoofFloorBlocks();
            var roofRooms = MaxRoofFloorRooms();
            var roofLayout = new RoofLayout(roofFloors, roofV, roofRooms);
            var copyToRoofBlocks = new List<CreateBlockInfo>();
            if (roofLayout.HaveMaxRoof())
            {
                foreach (var item in createBlockInfos)
                {
                    if (string.IsNullOrEmpty(item.tag) || (!item.tag.ToUpper().Equals("PL") && !item.tag.ToUpper().Equals("FL")))
                        continue;
                    copyToRoofBlocks.Add(item);
                }
            }
            var addBlocks = roofLayout.RoofLayoutResult(livingHighestFloor, copyToRoofBlocks);
            if (null != addBlocks && addBlocks.Count > 0)
                createBlockInfos.AddRange(addBlocks);

            //Y1L转换
            var addY1Ls = roofLayout.RoofY1LConverter(livingHighestFloor, livingFloorY1LBlocks, out List<CreateBasicElement> addLines, 1500);
            if (null != addY1Ls && addY1Ls.Count > 0)
                createBlockInfos.AddRange(addY1Ls);
            if (null != addLines && addLines.Count > 0)
                createBasicElems.AddRange(addLines);

            LivingFloorLabelLayout(midY);

            //将数据复制到其它楼层表数据
            var copyToOtherFloor = new CopyToOtherFloor(livingHighestFloor,
                createBlockInfos.Where(c => c.floorId.Equals(livingHighestFloor.floorUid)).ToList(),
                createBasicElems.Where(c => c.floorId.Equals(livingHighestFloor.floorUid)).ToList(),
                createTextElems.Where(c => c.floorUid.Equals(livingHighestFloor.floorUid)).ToList());

            //将标注复制到屋,先将数复制到大屋面，再根据大屋面数据到小屋面
            var maxRoofFloors = roofLayout.AllMaxRoofFloor();
           
            if (maxRoofFloors.Count > 0)
            {
                //如果没有大屋面，这里就不需要后续的处理
                foreach (var item in maxRoofFloors)
                {
                    var maxRoofPipes = createBlockInfos.Where(c => c.floorId.Equals(item.floorUid) && (string.IsNullOrEmpty(c.tag) || !c.tag.Contains("Y1"))).ToList();
                    if (null == maxRoofPipes || maxRoofPipes.Count < 1)
                        continue;
                    var copyLines = copyToOtherFloor.CopyFloorLabelTextToMaxRoof(item, maxRoofPipes, out List<CreateDBTextElement> copyTexts);
                    if (copyLines != null && copyLines.Count > 0)
                        createBasicElems.AddRange(copyLines);
                    if (copyTexts != null && copyTexts.Count > 0)
                        createTextElems.AddRange(copyTexts);
                }
                foreach (var minRoof in roofLayout.AllMinRoofFloor())
                {
                    var minRoofBlock = createBlockInfos.Where(c => c.floorId.Equals(minRoof.floorUid)).ToList();
                    if (minRoofBlock == null || minRoofBlock.Count < 1)
                        continue;
                    foreach (var maxRoof in maxRoofFloors)
                    {
                        var maxRoofPipes = createBlockInfos.Where(c => c.floorId.Equals(maxRoof.floorUid)).ToList();
                        if (null == maxRoofPipes || maxRoofPipes.Count < 1)
                            continue;
                        var maxRoofLines = createBasicElems.Where(c => c.floorId.Equals(maxRoof.floorUid)).ToList();
                        if (null == maxRoofLines || maxRoofLines.Count < 1)
                            continue;
                        var maxRoofTexts = createTextElems.Where(c => c.floorUid.Equals(maxRoof.floorUid)).ToList();
                        if (null == maxRoofTexts || maxRoofTexts.Count < 1)
                            continue;
                        var copyLines = copyToOtherFloor.CopyFloorLabelTextToMinRoof(minRoof, minRoofBlock, maxRoof, maxRoofPipes, maxRoofLines, maxRoofTexts, out List<CreateDBTextElement> copyTexts);
                        if (copyLines != null && copyLines.Count > 0)
                            createBasicElems.AddRange(copyLines);
                        if (copyTexts != null && copyTexts.Count > 0)
                            createTextElems.AddRange(copyTexts);
                    }
                }

                //大屋面立管标注
                foreach (var item in maxRoofFloors) 
                {
                    RoofFloorLavelLayout(item,midY);
                }
            }
            //复制到其它非屋面楼层
            foreach (var item in floorFrameds)
            {
                if (item.floorType.Contains("屋面") || item.floorUid.Equals(livingHighestFloor.floorUid))
                    continue;
                var copyBlocks = copyToOtherFloor.CopyAllToFloor(item, out List<CreateBasicElement> copyElems, out List<CreateDBTextElement> copyTexts);
                if (copyBlocks != null && copyBlocks.Count > 0)
                    createBlockInfos.AddRange(copyBlocks);
                if (copyElems != null && copyElems.Count > 0)
                    createBasicElems.AddRange(copyElems);
                if (copyTexts != null && copyTexts.Count > 0)
                    createTextElems.AddRange(copyTexts);
            }
        }
        void LivingFloorLabelLayout(double midY) 
        {
            //标注处理
            var pipelineLabel = new PipeLineLabelLayout(livingHighestFloor, midY);
            pipelineLabel.InitFloorData(livingHighestFloor, createBlockInfos.Where(c => c.floorId.Equals(livingHighestFloor.floorUid)).ToList(), null);
            pipelineLabel.AddObstacleEntitys(_allWalls);
            pipelineLabel.AddObstacleEntitys(_allColumns);
            LabelLayout(pipelineLabel, livingHighestFloor, _floorBlockEqums);
        }

        List<EquipmentBlcokModel> InitFloorData(FloorFramed floor, double disToDist=30) 
        {
            var tempBlocks = _blockReferenceData.GetPolylineEquipmentBlocks(floor.outPolyline);
            var axisEntitys = _basicElementEngine.GetExtractorEntity(floor.outPolyline, new List<EnumElementType> { EnumElementType.ExternalLineAxis });
            var floorBlocks = DrainSysAGCommon.GetFloorBlocks(tempBlocks, axisEntitys);
            //去重，在一定范围内不能有同一类的数据
            var retBlocks = new List<EquipmentBlcokModel>();
            foreach (var item in floorBlocks)
            {
                if (null == item || item.blockReferences == null || item.blockReferences.Count < 1)
                    continue;
                var tempList = new List<BlockReference>();
                foreach (var block in item.blockReferences)
                {
                    var blockPt2d = new Point3d(block.Position.X, block.Position.Y, 0);
                    bool isAdd = true;
                    foreach (var checkBlock in tempList)
                    {
                        if (!isAdd)
                            break;
                        var checkPoint2d = new Point3d(checkBlock.Position.X, checkBlock.Position.Y, 0);
                        isAdd = checkPoint2d.DistanceTo(blockPt2d) > disToDist;
                    }
                    if (isAdd)
                        tempList.Add(block);
                }
                bool addType = true;
                foreach (var retItem in retBlocks)
                {
                    if (!addType || retItem.enumEquipmentType != item.enumEquipmentType)
                        continue;
                    addType = false;
                    foreach (var block in tempList) 
                    {
                        bool isAdd = true;
                        var blockPt2d = new Point3d(block.Position.X, block.Position.Y, 0);
                        foreach (var checkBlock in retItem.blockReferences)
                        {
                            if (!isAdd)
                                break;
                            var checkPoint2d = new Point3d(checkBlock.Position.X, checkBlock.Position.Y, 0);
                            isAdd = checkPoint2d.DistanceTo(blockPt2d) > disToDist;
                        }
                        if (isAdd)
                            retItem.blockReferences.Add(block);
                    }
                    break;
                }
                if (addType && tempList.Count > 0)
                    retBlocks.Add(new EquipmentBlcokModel(item.enumEquipmentType, tempList));
            }
            return retBlocks;
        }
        void RoofFloorLavelLayout(FloorFramed roofFloor,double midY) 
        {
            var pipelineLabel = new PipeLineLabelLayout(livingHighestFloor, midY);
            var roofTexts = createTextElems.Where(c => c.floorUid.Equals(roofFloor.floorUid) && c.dbText != null).Select(c=>c.dbText).ToList();
            var roofLayouts = createBlockInfos.Where(c => c.floorId.Equals(roofFloor.floorUid) && !string.IsNullOrEmpty(c.tag) && c.tag.Contains("Y1") ).ToList();
            pipelineLabel.InitFloorData(roofFloor, roofLayouts, null);
            var roofWalls = new List<Polyline>();
            var roofColumns = new List<Polyline>();
            try
            {
                _wallColumnsEngine.GetStructureInfo(roofFloor.outPolyline, out roofColumns, out roofWalls);
            }
            catch (Exception ex) { }
            if(null != roofTexts && roofTexts.Count > 0) 
            {
                foreach (var text in roofTexts) 
                {
                    pipelineLabel.AddObstacleEntity(text.GeometricExtents.ToRectangle());
                }
            }
            pipelineLabel.AddObstacleEntitys(roofWalls);
            pipelineLabel.AddObstacleEntitys(roofColumns);
            var roofFloorBlocks = _blockReferenceData.GetPolylineEquipmentBlocks(roofFloor.outPolyline);
            LabelLayout(pipelineLabel, roofFloor, roofFloorBlocks);
        }

        List<RoofPointInfo> GetRoofFloorBlocks() 
        {
            var retData = new List<RoofPointInfo>();
            foreach (var floor in roofFloors)
            {
                if (!floor.floorType.Contains("屋面"))
                    continue;
                var plEquipment = InitFloorData(floor); //equipmentData.GetPolylineEquipmentBlocks(floor.outPolyline);
                if (plEquipment == null || plEquipment.Count < 1)
                    continue;
                var tempRooWaterBuckets = new List<RoofPointInfo>();
                foreach (var item in plEquipment)
                {
                    if (null == item || (item.enumEquipmentType != EnumEquipmentType.gravityRainBucket && item.enumEquipmentType != EnumEquipmentType.sideRainBucket))
                        continue;
                    if (null == item.blockReferences || item.blockReferences.Count < 1)
                        continue;
                    foreach (var block in item.blockReferences)
                    {
                        var mcs2wcs = block.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                        var circles = DrainSysAGCommon.GetBlockInnerElement<Circle>(block, mcs2wcs);
                        if (circles == null || circles.Count < 1)
                        {
                            circles = DrainSysAGCommon.GetBlockInnerElement<Arc>(block, mcs2wcs);
                        }
                        if (null == circles || circles.Count < 1)
                            continue;
                        foreach (var entity in circles)
                        {
                            if (entity is Circle)
                            {
                                var cir = entity as Circle;
                                var center = new Point3d(cir.Center.X, cir.Center.Y, 0);
                                tempRooWaterBuckets.Add(new RoofPointInfo(floor, item.enumEquipmentType, center));
                                break;

                            }
                            else if (entity is Arc)
                            {
                                var arc = entity as Arc;
                                var center = new Point3d(arc.Center.X, arc.Center.Y, 0);
                                var testPoint = center.TransformBy(mcs2wcs);
                                tempRooWaterBuckets.Add(new RoofPointInfo(floor, item.enumEquipmentType, center));
                                break;
                            }
                        }
                    }
                }

                //有定位点偏移的，进一步过滤
                foreach (var item in tempRooWaterBuckets)
                {
                    if (!roofFloors.Any(c => c.outPolyline.Contains(item.centerPoint)))
                        continue;
                    retData.Add(item);
                }
            }
            return retData;
        }
        Dictionary<string, List<ThIfcRoom>> MaxRoofFloorRooms() 
        {
            var retDic = new Dictionary<string, List<ThIfcRoom>>();
            foreach (var item in roofFloors) 
            {
                if (!item.floorName.Contains("大屋面"))
                    continue;
                var rooms = _roomEngine.GetAllRooms(item.blockOutPointCollection);
                if (null == rooms || rooms.Count < 1)
                    continue;
                retDic.Add(item.floorUid,rooms);
            }
            return retDic;
        }
        void LabelLayout(PipeLineLabelLayout pipelineLabel, FloorFramed labelFloor, List<EquipmentBlcokModel> floorBlocks)
        {
            var floorDoor = new List<Polyline>();
            var floorWindow = new List<Polyline>();
            try
            {
                floorDoor = _doorWindowEngine.AllDoors(labelFloor.outPolyline);
                floorWindow = _doorWindowEngine.AllWindows(labelFloor.outPolyline);
            }
            catch (Exception ex) { }
            pipelineLabel.AddObstacleEntitys(floorDoor);
            pipelineLabel.AddObstacleEntitys(floorWindow);
            
            //获取躲避块
            foreach (var item in floorBlocks)
            {
                if (item.blockReferences.Count < 1)
                    continue;
                if (!_obstacleBlockTypes.Any(c=>c == item.enumEquipmentType))
                    continue;
                foreach (var block in item.blockReferences)
                    pipelineLabel.AddObstacleEntity(block);
            }
            var addEntitys = new List<Entity>();
            var baseEntitys = _basicElementEngine.GetAllTypeEntity(labelFloor.outPolyline);
            foreach (var item in baseEntitys)
            {
                if (item.Value == null || item.Value.Count < 1)
                    continue;
                if (item.Key != EnumElementType.ExternalLineAxis) 
                {
                    addEntitys.AddRange(item.Value);
                    continue;
                }
                //轴网线过滤,轴线和文字方向平行的不要，这里文字方向为X轴方向，这里只避让X方向上的轴网
                foreach (var axis in item.Value)
                {
                    var isLine = axis is Line;
                    if (!isLine)
                    {
                        addEntitys.Add(axis);
                        continue;
                    }
                    var line = axis as Line;
                    var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                    var angle = lineDir.GetAngleTo(Vector3d.XAxis, Vector3d.ZAxis);
                    angle %= Math.PI;
                    var minAngle = Math.PI * _obstacleAxisAngle / 180;
                    var maxAngle = Math.PI - minAngle;
                    if (Math.Abs(angle) > minAngle && Math.Abs(angle) < maxAngle)
                        continue;
                    addEntitys.Add(axis);
                }
            }
            pipelineLabel.AddObstacleEntitys(addEntitys);
            var dbText = pipelineLabel.SpliteFloorSpace(out List<CreateBasicElement> lineAdds);
            if (null != dbText && dbText.Count > 0)
                createTextElems.AddRange(dbText);
            if (null != lineAdds && lineAdds.Count > 0)
                createBasicElems.AddRange(lineAdds);
        }

        double LivingFloorMidY(List<RoomModel> livingFloorRooms,List<CreateBlockInfo> livingFloorBlocks) 
        {
            var listBlockPoints = livingFloorBlocks.Select(c => c.createPoint).ToList();
            foreach (var room in livingFloorRooms)
            {
                for (int i = 0; i < room.outLine.NumberOfVertices; i++)
                {
                    var pt = room.outLine.GetPoint3dAt(i);
                    listBlockPoints.Add(pt);
                }
            }
            var maxY = listBlockPoints.Max(c => c.Y);
            var minY = listBlockPoints.Min(c => c.Y);
            return (maxY + minY) / 2;
        }
        bool CheckData(List<FloorFramed> allFloorFramed) 
        {
            if (floorFrameds == null|| floorFrameds.Count<1 || null == allFloorFramed || allFloorFramed.Count < 1)
                return false;
            //必须有顶层住人屋面
            if (_roomEngine.GetAllRooms(new Point3dCollection()).Count < 1) 
            {
                errorMsg = "项目中没有任何房间，无法进行后续操作";
                return false;
            }
            var upFloorFramed = allFloorFramed.OrderByDescending(c => c.endFloorOrder).ToList();
            foreach (var floor in upFloorFramed) 
            {
                if (floor == null || floor.floorType.Contains("屋面"))
                    continue;
                var allRooms = _roomEngine.GetAllRooms(floor.blockOutPointCollection);
                var rooms = _roomEngine.GetRoomModelRooms(allRooms, null);
                if (rooms.Any(c => c.roomTypeName == EnumRoomType.Kitchen))
                {
                    livingHighestFloor = floor;
                    break;
                }
            }
            roofFloors.AddRange(floorFrameds.Where(c => c.floorType.Contains("屋面")).ToList());
            if (null == livingHighestFloor)
            {
                //该楼层中没有住人楼层，没有在楼层中找到厨房
                errorMsg = "没有找到住人顶层，无法进行后续操作";
            }
            else 
            {
                livingHighestFloor = floorFrameds.Where(c => c.blockId == livingHighestFloor.blockId).FirstOrDefault();
                if (null != livingHighestFloor)
                {
                    //选择了顶层住人屋面
                    return true;
                }
                else 
                {
                    //没有选择顶层住人屋面不进行后续的操作
                    errorMsg = "检查到有住人顶层，但没有选择，无法进行后续操作，请选择住人顶层后再进行后续操作";
                }
            }
            return false;
        }
    }
}