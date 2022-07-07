using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    /// <summary>
    /// 地漏转换
    /// </summary>
    class FloorDrainConvert
    {
        static double _balconyWMachineFloorDrainDistance = 1000;//阳台洗衣机找地漏范围
        public static string FloorDrainBlockDynName = "普通地漏";
        public static string FloorDrainWMachineBlockDynName = "带洗衣机插口地漏";
        public static List<CreateBlockInfo> FloorDrainConvertToBlock(string floorId,List<EquipmentBlockSpace> floorDrainBlcoks,List<EquipmentBlockSpace> balconyWMachine, List<EquipmentBlockSpace> wastewaterRisers, List<EquipmentBlockSpace> rainRisers) 
        {
            var createBlocks = new List<CreateBlockInfo>();
            if (floorDrainBlcoks == null || floorDrainBlcoks.Count < 1)
                return createBlocks;
            var balconyDrain = new List<EquipmentBlockSpace>();
            var balconyWastewaterRisers = wastewaterRisers.Where(e => e.enumRoomType.Equals(EnumRoomType.Balcony)).ToList();
            var balconyRainRisers = rainRisers.Where(e => e.enumRoomType.Equals(EnumRoomType.Balcony)).ToList();
            foreach (var item in floorDrainBlcoks) 
            {
                if (item.enumEquipmentType != EnumEquipmentType.floorDrain)
                    continue;
                switch (item.enumRoomType) 
                {
                    case EnumRoomType.Kitchen:
                    case EnumRoomType.Toilet:
                        //厨房,卫生间地漏 图层：W - DRAI - FLDR，图块：地漏平面，可见性：普通地漏
                        var tDrain = new CreateBlockInfo(floorId,ThWSSCommon.Layout_FloorDrainBlockName, ThWSSCommon.Layout_FloorDrainBlockWastLayerName, item.blockPosition,item.enumEquipmentType,item.uid);
                        tDrain.dymBlockAttr.Add("可见性", FloorDrainBlockDynName);
                        tDrain.spaceId = item.roomSpaceId;
                        createBlocks.Add(tDrain);
                        break;
                    case EnumRoomType.Corridor:
                        //连廊地漏转换
                        //图层：雨水/冷凝水地漏 W-RAIN-EQPM，图块：地漏平面，可见性：普通地漏
                        var cDrain = new CreateBlockInfo(floorId,ThWSSCommon.Layout_FloorDrainBlockName, ThWSSCommon.Layout_FloorDrainBlockRainLayerName, item.blockPosition, item.enumEquipmentType, item.uid);
                        cDrain.dymBlockAttr.Add("可见性", FloorDrainBlockDynName);
                        cDrain.spaceId = item.roomSpaceId;
                        createBlocks.Add(cDrain);
                        break;
                    case EnumRoomType.Balcony:
                        //阳台地漏要考虑是否是洗衣机地漏
                        balconyDrain.Add(item);
                        break;
                    case EnumRoomType.EquipmentPlatform:
                        //设备平台上的地漏是雨水/冷凝水地漏
                        //设备平台上的地漏有可能不生成，要进一步根据是否可以找到立管
                        var eqDrain = new CreateBlockInfo(floorId,ThWSSCommon.Layout_FloorDrainBlockName, ThWSSCommon.Layout_FloorDrainBlockRainLayerName, item.blockPosition, item.enumEquipmentType, item.uid);
                        eqDrain.spaceId = "";
                        eqDrain.dymBlockAttr.Add("可见性", FloorDrainBlockDynName);
                        createBlocks.Add(eqDrain);
                        break;
                }
            }
            if (null == balconyDrain || balconyDrain.Count < 1)
                return createBlocks;
            List<string> hisIds = new List<string>();
            if (null != balconyWMachine && balconyWMachine.Count > 0) 
            {
                //有洗衣机，判断是雨水地漏还是洗衣机地漏
                foreach (var item in balconyWMachine)
                {
                    if (item.enumRoomType != EnumRoomType.Balcony)
                        continue;
                    //获取该阳台的地漏，获取范围内的最近的地漏标为废水地漏
                    List<EquipmentBlockSpace> thisRoomDrains = balconyDrain.Where(c => c.roomSpaceId.Equals(item.roomSpaceId)).ToList();
                    if (thisRoomDrains.Count < 1)
                        continue;
                    var first = thisRoomDrains.OrderBy(c => c.blockPosition.DistanceTo(item.blockPosition)).FirstOrDefault();
                    if (first.blockPosition.DistanceTo(item.blockPosition) < _balconyWMachineFloorDrainDistance)
                    {
                        var block = new CreateBlockInfo(floorId,ThWSSCommon.Layout_FloorDrainBlockName,ThWSSCommon.Layout_FloorDrainBlockWastLayerName, first.blockPosition, first.enumEquipmentType, first.uid);
                        block.dymBlockAttr.Add("可见性", FloorDrainWMachineBlockDynName);
                        block.spaceId = first.roomSpaceId;
                        createBlocks.Add(block);
                        hisIds.Add(first.uid);
                    }
                }
            }
            //20220706Added:
            //Case1: 1个废水立管+1个地漏
            //Case2: 1个雨水立管 + 1个废水立管 + 两个地漏
            //杨工说暂时不上
            //var roomIds = balconyDrain.Select(e => e.roomSpaceId).Distinct();
            //foreach (var id in roomIds)
            //{
            //    var _wasteRisers = wastewaterRisers.Where(e => e.roomSpaceId.Equals(id));
            //    var _balconyRisers = balconyRainRisers.Where(e => e.roomSpaceId.Equals(id));
            //    var _floorDrains = balconyDrain.Where(e => !hisIds.Contains(e.uid)).Where(e => e.roomSpaceId.Equals(id));
            //    if (_wasteRisers.Count() == 1 && _floorDrains.Count() == 1 && _balconyRisers.Count() == 0)
            //    {
            //        var item = _floorDrains.First();
            //        var block = new CreateBlockInfo(floorId, ThWSSCommon.Layout_FloorDrainBlockName, ThWSSCommon.Layout_FloorDrainBlockWastLayerName, item.blockPosition, item.enumEquipmentType, item.uid);
            //        block.dymBlockAttr.Add("可见性", FloorDrainBlockDynName);
            //        block.spaceId = item.roomSpaceId;
            //        createBlocks.Add(block);
            //        hisIds.Add(item.uid);
            //    }
            //    else if (_wasteRisers.Count() == 1 && _floorDrains.Count() == 2 && _balconyRisers.Count() == 1)
            //    {
            //        var first = _floorDrains.OrderBy(e => e.blockPosition.DistanceTo(_wasteRisers.First().blockPosition)).First();
            //        var block = new CreateBlockInfo(floorId, ThWSSCommon.Layout_FloorDrainBlockName, ThWSSCommon.Layout_FloorDrainBlockWastLayerName, first.blockPosition, first.enumEquipmentType, first.uid);
            //        block.dymBlockAttr.Add("可见性", FloorDrainWMachineBlockDynName);
            //        block.spaceId = first.roomSpaceId;
            //        createBlocks.Add(block);
            //        hisIds.Add(first.uid);
            //    }
            //}
            foreach (var item in balconyDrain)
            {
                if (hisIds.Any(c => c.Equals(item.uid)))
                    continue;
                var block = new CreateBlockInfo(floorId,ThWSSCommon.Layout_FloorDrainBlockName, ThWSSCommon.Layout_FloorDrainBlockRainLayerName, item.blockPosition, item.enumEquipmentType, item.uid);
                block.dymBlockAttr.Add("可见性", FloorDrainBlockDynName);
                block.spaceId = item.roomSpaceId;
                createBlocks.Add(block);
            }
            return createBlocks;
        }
    }
}
