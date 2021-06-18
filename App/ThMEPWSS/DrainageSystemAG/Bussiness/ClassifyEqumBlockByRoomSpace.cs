using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    class ClassifyEqumBlockByRoomSpace
    {
        private double outBalconyDistanceEqumPipe = 2500;
        private List<RoomModel> _roomModels = new List<RoomModel>();
        private List<EquipmentBlcokModel> _targetEquipmentBlock = new List<EquipmentBlcokModel>();
        public ClassifyEqumBlockByRoomSpace(List<RoomModel> roomModels, List<EquipmentBlcokModel> targetEquipmentBlock) 
        {
            if (null != roomModels && roomModels.Count > 0)
                roomModels.ForEach(c => { if (c != null) { _roomModels.Add(c); } });
            if (null != targetEquipmentBlock && targetEquipmentBlock.Count > 0)
                targetEquipmentBlock.ForEach(c => { if (c != null) { _targetEquipmentBlock.Add(c); } });
        }
        public List<EquipmentBlockSpace> GetClassifyEquipments() 
        {
            var retModels = new List<EquipmentBlockSpace>();
            foreach (var item in _targetEquipmentBlock) 
            {
                if (item == null || item.blockReferences == null || item.blockReferences.Count < 1)
                    continue;
                var classify= new List<EquipmentBlockSpace>();
                switch (item.enumEquipmentType) 
                {
                    case EnumEquipmentType.balconyRiser:
                    case EnumEquipmentType.condensateRiser:
                    case EnumEquipmentType.roofRainRiser:
                        //雨水管分类
                        classify = GetRiserClassifies(item.blockReferences, item.enumEquipmentType);
                        break;
                    case EnumEquipmentType.floorDrain:
                        //地漏进行分类
                        classify = GetFloorDrainClassifies(item.blockReferences, item.enumEquipmentType);
                        break;
                    case EnumEquipmentType.kitchenBasin://厨房台盆
                    case EnumEquipmentType.washingMachine://洗衣机进行分类
                        //这里只是区分所属房间，就通用的获取归属房间信息
                        classify = GetBlockRoomClassifies(item.blockReferences, item.enumEquipmentType);
                        break;
                }
                if (null != classify && classify.Count > 0)
                    retModels.AddRange(classify);
            }
            return retModels;
        }

        List<EquipmentBlockSpace> GetRiserClassifies(List<BlockReference> riserBlocks, EnumEquipmentType enumEquipment)
        {
            //获取阳台的立管，在阳台空间的范围内，在外参中找屋面雨水立管、阳台立管和冷凝立管。
            var riserClassify = new List<EquipmentBlockSpace>();
            if (null == riserBlocks || riserBlocks.Count < 1)
                return riserClassify;
            foreach (var block in riserBlocks)
            {
                riserClassify.Add(new EquipmentBlockSpace(block, enumEquipment));
            }
            //阳台立管,连廊立管
            foreach (var room in _roomModels)
            {
                if (room.roomTypeName != EnumRoomType.Balcony && room.roomTypeName != EnumRoomType.Corridor)
                    continue;
                var roomPLine = room.outLine;
                foreach (var block in riserClassify)
                {
                    if (block.enumRoomType != EnumRoomType.Other)
                        continue;
                    if (roomPLine.Contains(block.blockPosition))
                    {
                        block.enumRoomType = room.roomTypeName;
                        block.roomSpaceId = room.thIFCRoom.Uuid;
                    }
                }
            }
            //阳台外扩后判断是否是设备立管
            foreach (var room in _roomModels)
            {
                if (room.roomTypeName != EnumRoomType.Corridor)
                    continue;
                var roomOutPLine = room.outLine.BufferPL(outBalconyDistanceEqumPipe).Cast<Polyline>().FirstOrDefault();
                foreach (var riserBlock in riserClassify)
                {
                    if (riserBlock.enumRoomType != EnumRoomType.Other)
                        continue;
                    if (roomOutPLine.Contains(riserBlock.blockPosition))
                    {
                        riserBlock.enumRoomType = EnumRoomType.equipmentPlatform;
                    }
                }
            }
            return riserClassify;
        }

        List<EquipmentBlockSpace> GetFloorDrainClassifies(List<BlockReference> floorDrains, EnumEquipmentType enumEquipment) 
        {
            var drainsClassify = new List<EquipmentBlockSpace>();
            if (null == floorDrains || floorDrains.Count < 1)
                return drainsClassify;
            foreach (var block in floorDrains)
            {
                drainsClassify.Add(new EquipmentBlockSpace(block, enumEquipment));
            }
            foreach (var item in drainsClassify)
            {
                if (item == null || item.enumRoomType != EnumRoomType.Other)
                    continue;
                foreach (var room in _roomModels)
                {
                    if (null == room || (room.roomTypeName != EnumRoomType.Toilet && room.roomTypeName != EnumRoomType.Corridor && room.roomTypeName != EnumRoomType.Balcony))
                        continue;
                    if (room.outLine.Contains(item.blockPosition))
                    {
                        item.enumRoomType = room.roomTypeName;
                        item.roomSpaceId = room.thIFCRoom.Uuid;
                        break;
                    }
                }
                if (item.enumRoomType != EnumRoomType.Other)
                    continue;
                //没有归属空间，认为是设备平台的地漏
                item.enumRoomType = EnumRoomType.equipmentPlatform;
                item.roomSpaceId = "";
            }
            return drainsClassify;
        }

        List<EquipmentBlockSpace> GetBlockRoomClassifies(List<BlockReference> washMachines, EnumEquipmentType enumEquipment) 
        {
            var retModels = new List<EquipmentBlockSpace>();
            if (null == washMachines || washMachines.Count < 1)
                return retModels;
            washMachines.ForEach(c => { if (c != null) { retModels.Add(new EquipmentBlockSpace(c, enumEquipment)); } });
            foreach (var room in _roomModels)
            {
                foreach (var item in retModels)
                {
                    if (item.enumRoomType != EnumRoomType.Other)
                        continue;
                    if (room.outLine.Contains(item.blockPosition))
                    {
                        item.enumRoomType = room.roomTypeName;
                        item.roomSpaceId = room.thIFCRoom.Uuid;
                    }
                }
            }
            return retModels;
        }
    }
}
