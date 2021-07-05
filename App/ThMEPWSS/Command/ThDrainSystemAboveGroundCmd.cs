using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.Common;
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
        List<FloorFramed> floorFrameds = new List<FloorFramed>();
        ThRoomDataEngine roomEngine = null;
        FloorFramed livingHighestFloor = null;
        List<FloorFramed> roofFloors = new List<FloorFramed>();
        public string errorMsg = "";
        public ThDrainSystemAboveGroundCmd(List<FloorFramed> selectFloors, DrainageSystemAGViewmodel viewmodel) 
        {
            if (null != selectFloors && selectFloors.Count > 0)
                selectFloors.ForEach(c => { if (c != null) floorFrameds.Add(c); });
            if (null != viewmodel) 
            {
                SetServicesModel.Instance.drawingScale = (EnumDrawingScale)viewmodel.ScaleSelectItem.Value;
                SetServicesModel.Instance.wasteSewageVentilationRiserPipeDiameter = (EnumPipeDiameter)viewmodel.WSVPipeDiameterSelectItem.Value;
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
                var allFrames = FramedReadUtil.ReadAllFloorFramed();
                roomEngine = new ThRoomDataEngine();
                if (!CheckData(allFrames))
                {
                    if (!string.IsNullOrEmpty(errorMsg)) 
                    {
                        
                    }
                    return;
                }
                //载入数据
                ClearLoadBlockServices.LoadBlockLayerToDocument(acdb.Database);
                ClearLoadBlockServices.ClearHisFloorBlock(acdb.Database, floorFrameds.Select(c => c.outPolyline).ToList());

                //return;
                var baseEngine = new BasicElementEngine();
                var equipmentData = new BlockReferenceDataEngine();
                //
                var allDoors = new List<Polyline>();
                var allWindows = new List<Polyline>();
                try
                {
                    allDoors = OtherDataEngine.AllDoors(livingHighestFloor.outPolyline);
                    allWindows = OtherDataEngine.AllWindows(livingHighestFloor.outPolyline);
                }
                catch (Exception ex)
                {

                }
                var spaceColumns = new List<Polyline>();
                var spaceWalls = new List<Polyline>();

                //获取相应的数据，框线内的房间，烟道井，墙，柱
                try
                {
                    var wallColumnsEngine = new ThWallColumnsEngine();
                    wallColumnsEngine.GetStructureInfo(livingHighestFloor.outPolyline, out spaceColumns, out spaceWalls);
                }
                catch (Exception ex)
                { }

                var createBlockInfos = new List<CreateBlockInfo>();
                var createBasicElems = new List<CreateBasicElement>();
                var createTextElems = new List<CreateDBTextElement>();
                var allRooms = roomEngine.GetAllRooms(livingHighestFloor.blockOutPointCollection);
                var floorEqums = equipmentData.GetPolylineEquipmentBlocks(livingHighestFloor.outPolyline);
                var tubeBlocks = new List<BlockReference>();
                var flueBlocks = new List<BlockReference>();
                foreach (var item in floorEqums)
                {
                    if (item.enumEquipmentType == EnumEquipmentType.waterTubeWell)
                        tubeBlocks.AddRange(item.blockReferences);
                    else if (item.enumEquipmentType == EnumEquipmentType.flueWell)
                        flueBlocks.AddRange(item.blockReferences);
                }
                var tubeFlueRooms = roomEngine.TubeFlueWellToRoom(null, flueBlocks);
                var rooms = roomEngine.GetRoomModelRooms(allRooms, tubeFlueRooms);
                rooms = rooms.Where(c => c.outLine.Area > 100).ToList();
                //对设备数据进行分类
                var classifyEqumBlock = new ClassifyEqumBlockByRoomSpace(rooms, floorEqums);
                var classifyResult = classifyEqumBlock.GetClassifyEquipments();

                //对房间进行分类处理
                var tRooms = roomEngine.GetTubeWellRooms(rooms);
                var tubeRooms = DrainSysAGCommon.GetTubeWellRoomRelation(rooms.Where(c => c.roomTypeName == Model.EnumRoomType.Toilet || c.roomTypeName == Model.EnumRoomType.Kitchen).ToList(), tRooms);
                var kitchenRooms = rooms.Where(c => c.roomTypeName == EnumRoomType.Kitchen).ToList();
                var toiletRooms = rooms.Where(c => c.roomTypeName == EnumRoomType.Toilet).ToList();

                //标准层、非标层的房间布置逻辑
                //地漏转换
                var blocks = FloorDrainConvert.FloorDrainConvertToBlock(livingHighestFloor.floorUid,classifyResult.Where(c => c.enumEquipmentType == EnumEquipmentType.floorDrain).ToList(),
                    classifyResult.Where(c => c.enumEquipmentType == EnumEquipmentType.washingMachine).ToList());
                if (null != blocks && blocks.Count > 0)
                    createBlockInfos.AddRange(blocks);

                //厨房卫生间逻辑
                var roomKitchenToiletLayout = new RoomKitchenToiletLayout(livingHighestFloor.floorUid, acdb, toiletRooms, kitchenRooms, tubeRooms);
                roomKitchenToiletLayout.ToiletLayout();
                roomKitchenToiletLayout.KitchenLayout(
                    classifyResult.Where(c=>c.enumRoomType== EnumRoomType.Kitchen).ToList(),rooms.Where(c=>c.roomTypeName == EnumRoomType.FlueWell).ToList());
                if (null != roomKitchenToiletLayout.createBlocks && roomKitchenToiletLayout.createBlocks.Count > 0)
                    createBlockInfos.AddRange(roomKitchenToiletLayout.createBlocks);

                //阳台逻辑
                var balconyRooms = rooms.Where(c => c.roomTypeName == EnumRoomType.Balcony).ToList();
                var corridorRooms = rooms.Where(c => c.roomTypeName == EnumRoomType.Corridor).ToList();
                var equpBlocks = classifyResult.Where(c => c.enumRoomType == Model.EnumRoomType.Other || c.enumRoomType == Model.EnumRoomType.Corridor || c.enumRoomType == Model.EnumRoomType.Balcony || c.enumRoomType == Model.EnumRoomType.equipmentPlatform).ToList();
                var otherRooms = new List<RoomModel>();
                foreach (var room in rooms)
                {
                    if (room == null || room.roomTypeName == EnumRoomType.Balcony || room.roomTypeName == EnumRoomType.Corridor)
                        continue;
                    otherRooms.Add(room);
                }
                var balconyCorridorEqu = new BalconyCorridorEquPlatform(livingHighestFloor.floorUid, balconyRooms, corridorRooms, otherRooms, equpBlocks, spaceWalls, spaceColumns);
                balconyCorridorEqu.LayoutConnect(createBlockInfos);
                if (balconyCorridorEqu.createBasicElements != null && balconyCorridorEqu.createBasicElements.Count > 0)
                    createBasicElems.AddRange(balconyCorridorEqu.createBasicElements);
                if (balconyCorridorEqu.createBlockInfos != null && balconyCorridorEqu.createBlockInfos.Count > 0)
                    createBlockInfos.AddRange(balconyCorridorEqu.createBlockInfos);

                //屋面数据处理
                //有大屋面时，找到住人顶层的所有污废立管（PL）和废水立管（FL）。将所有的立管和编号标注根据基点复制到大屋面。
                var roofLayout = new RoofLayout(roofFloors,equipmentData,roomEngine);
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
                var addBlocks = roofLayout.RoofLayoutResult(livingHighestFloor,copyToRoofBlocks);
                if (null != addBlocks && addBlocks.Count > 0)
                    createBlockInfos.AddRange(addBlocks);

                //标注处理
                var pipelineLabel = new PipelineLabel(livingHighestFloor, createBlockInfos.Where(c => c.floorId.Equals(livingHighestFloor.floorUid)).ToList(), null);
                pipelineLabel.AddObstacleEntitys(spaceWalls);
                pipelineLabel.AddObstacleEntitys(spaceColumns);
                pipelineLabel.AddObstacleEntitys(allDoors);
                pipelineLabel.AddObstacleEntitys(allWindows);
                var baseEntitys = baseEngine.GetAllEntity(livingHighestFloor.outPolyline);
                pipelineLabel.AddObstacleEntitys(baseEntitys);
                var dbText = pipelineLabel.SpliteFloorSpace(out List<CreateBasicElement> lineAdds);
                if (null != dbText && dbText.Count > 0)
                    createTextElems.AddRange(dbText);
                if (null != lineAdds && lineAdds.Count > 0)
                    createBasicElems.AddRange(lineAdds);

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
                        var maxRoofPipes = createBlockInfos.Where(c => c.floorId.Equals(item.floorUid)).ToList();
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
                            var copyLines = copyToOtherFloor.CopyFloorLabelTextToMinRoof(minRoof, minRoofBlock,maxRoof, maxRoofPipes,maxRoofLines,maxRoofTexts, out List<CreateDBTextElement> copyTexts);
                            if (copyLines != null && copyLines.Count > 0)
                                createBasicElems.AddRange(copyLines);
                            if (copyTexts != null && copyTexts.Count > 0)
                                createTextElems.AddRange(copyTexts);
                        }
                    }
                }
                //复制到其它非屋面楼层
                foreach (var item in floorFrameds)
                {
                    if (item.floorType.Contains("屋面") || item.floorUid.Equals(livingHighestFloor.floorUid))
                        continue;
                    var copyBlocks = copyToOtherFloor.CopyAllToFloor(item, out List<CreateBasicElement> copyElems,out List<CreateDBTextElement> copyTexts);
                    if (copyBlocks != null && copyBlocks.Count > 0)
                        createBlockInfos.AddRange(copyBlocks);
                    if (copyElems != null && copyElems.Count > 0)
                        createBasicElems.AddRange(copyElems);
                    if (copyTexts != null && copyTexts.Count > 0)
                        createTextElems.AddRange(copyTexts);
                }
                
                var createBlocks = CreateBlockService.CreateBlocks(acdb.Database, createBlockInfos);
                var createElems = CreateBlockService.CreateBasicElement(acdb.Database, createBasicElems);
                var createTexts = CreateBlockService.CreateTextElement(acdb.Database, createTextElems);

                //根据生成的数据，将同一图纸中的进行创建block
                foreach (var floor in floorFrameds) 
                {
                    var thisFloorBlocks = createBlocks.Where(c => c.floorUid.Equals(floor.floorUid)).ToList();
                    var thisFloorElems = createElems.Where(c => c.floorUid.Equals(floor.floorUid)).ToList();
                    var thisFloorTexts = createTexts.Where(c => c.floorUid.Equals(floor.floorUid)).ToList();
                    //块名称 TH-A
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

        bool CheckData(List<FloorFramed> allFloorFramed) 
        {
            if (floorFrameds == null|| floorFrameds.Count<1 || null == allFloorFramed || allFloorFramed.Count < 1)
                return false;
            //必须有顶层住人屋面
            if (roomEngine.GetAllRooms(new Point3dCollection()).Count < 1) 
            {
                errorMsg = "项目中没有任何房间，无法进行后续操作";
                return false;
            }
            var upFloorFramed = allFloorFramed.OrderByDescending(c => c.endFloorOrder).ToList();
            foreach (var floor in upFloorFramed) 
            {
                if (floor == null || floor.floorType.Contains("屋面"))
                    continue;
                var allRooms = roomEngine.GetAllRooms(floor.blockOutPointCollection);
                var rooms = roomEngine.GetRoomModelRooms(allRooms, null);
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

        void SPointArrow(AcadDatabase acdb, ThMEPOriginTransformer originTransformer, Point3d sp, Point3d ep)
        {
            Line line = new Line(sp, ep);
            Vector3d dir = line.LineDirection();
            SPointArrow(acdb, originTransformer, sp, dir);
        }
        void SPointArrow(AcadDatabase acdb, ThMEPOriginTransformer originTransformer, Point3d sp, Vector3d dir)
        {
            Vector3d normal = new Vector3d(0, 0, 1);
            Point3d tempPt = sp + dir.MultiplyBy(100);
            Vector3d x = -dir.RotateBy(Math.PI / 6, normal);
            Point3d tempEp = tempPt + x.MultiplyBy(100);
            Line line1 = new Line(tempPt, tempEp);
            x = -dir.RotateBy(-Math.PI / 6, normal);
            tempEp = tempPt + x.MultiplyBy(100);
            Line line2 = new Line(tempPt, tempEp);
            if (null != originTransformer)
                originTransformer.Reset(line1);
            acdb.ModelSpace.Add(line1);
            if (null != originTransformer)
                originTransformer.Reset(line2);
            acdb.ModelSpace.Add(line2);
        }
        void PointToView(AcadDatabase acdb, ThMEPOriginTransformer originTransformer, Point3d sp)
        {
            Vector3d x = new Vector3d(1, 0, 0);
            Vector3d y = new Vector3d(0, 1, 0);
            Point3d tempPt = sp - x.MultiplyBy(100);
            Point3d tempEp = sp + x.MultiplyBy(100);
            Line line1 = new Line(tempPt, tempEp);
            if (null != originTransformer)
                originTransformer.Reset(line1);
            acdb.ModelSpace.Add(line1);
            tempPt = sp - y.MultiplyBy(100);
            tempEp = sp + y.MultiplyBy(100);
            Line line2 = new Line(tempPt, tempEp);
            if (null != originTransformer)
                originTransformer.Reset(line2);
            acdb.ModelSpace.Add(line2);

        }
    }
}