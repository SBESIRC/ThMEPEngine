using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPWSS.DrainageSystemAG.DataEngine;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Engine;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    /// <summary>
    /// 屋面排布计算逻辑
    /// </summary>
    class RoofLayout
    {
        List<RoofPointInfo> _roofWaterBuckets = new List<RoofPointInfo>();
        List<FloorFramed> _roofFloors;
        List<FloorFramed> _maxRoofFloors;
        List<FloorFramed> _minRoofFloors;
        ThRoomDataEngine _roomEngine;
        public RoofLayout(List<FloorFramed> roofFloors, BlockReferenceDataEngine equipmentData,ThRoomDataEngine roomEngine) 
        {
            _roomEngine = roomEngine;
            _roofFloors = new List<FloorFramed>();
            _maxRoofFloors = new List<FloorFramed>();
            _minRoofFloors = new List<FloorFramed>();
            if (null == roofFloors || roofFloors.Count < 1)
                return;
            foreach (var floor in roofFloors)
            {
                if (!floor.floorType.Contains("屋面"))
                    continue;
                _roofFloors.Add(floor);
                bool isMaxRoof = floor.floorType.Contains("大");
                if (isMaxRoof)
                    _maxRoofFloors.Add(floor);
                else
                    _minRoofFloors.Add(floor);
                var plEquipment = equipmentData.GetPolylineEquipmentBlocks(floor.outPolyline);
                if (plEquipment == null || plEquipment.Count < 1)
                    continue;
               
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
                                _roofWaterBuckets.Add(new RoofPointInfo(floor, item.enumEquipmentType, center));
                                break;

                            }
                            else if (entity is Arc)
                            {
                                var arc = entity as Arc;
                                var center = new Point3d(arc.Center.X, arc.Center.Y, 0);
                                _roofWaterBuckets.Add(new RoofPointInfo(floor, item.enumEquipmentType, center));
                                break;
                            }
                        }
                    }
                }
            }
        }
        public bool HaveMaxRoof() 
        {
            return _maxRoofFloors != null && _maxRoofFloors.Count > 0;
        }
        public List<FloorFramed> AllMaxRoofFloor() 
        {
            var res = new List<FloorFramed>();
            if (null != _maxRoofFloors && _maxRoofFloors.Count > 0)
                _maxRoofFloors.ForEach(c => res.Add(c));
            return res;
        }
        public List<FloorFramed> AllMinRoofFloor()
        {
            var res = new List<FloorFramed>();
            if (null != _minRoofFloors && _minRoofFloors.Count > 0)
                _minRoofFloors.ForEach(c => res.Add(c));
            return res;
        }
        public List<CreateBlockInfo> RoofLayoutResult(FloorFramed livingFloor, List<CreateBlockInfo> copyBlocks) 
        {
            //小屋面数据只到大屋面
            var retRes = new List<CreateBlockInfo>();
            var maxRoofConvert = MaxRoofPipeConvert();
            //if (maxRoofConvert.Count > 0)
            //    retRes.AddRange(maxRoofConvert);
            var minToMaxRoofs = MinRoofToMaxRoof();
            if (minToMaxRoofs.Count > 0)
                retRes.AddRange(minToMaxRoofs);
            //var copyToLiving = CopyY1LToLivingFloor(livingFloor, retRes);
            //if (copyToLiving.Count > 0)
            //    retRes.AddRange(copyToLiving);
            var copyMaxRoofPipeToLiving = CopyY1LToLivingFloor(livingFloor, maxRoofConvert);
            if (copyMaxRoofPipeToLiving.Count > 0)
                retRes.AddRange(copyMaxRoofPipeToLiving);

            //将住人屋面的部分立管复制到屋面
            var copyAddBlocks = CopyPipeToMaxRoof(copyBlocks, livingFloor.datumPoint);
            if (copyAddBlocks.Count > 0)
                retRes.AddRange(copyAddBlocks);
            var maxToMinRoofs=  MaxRoofToMinRoof(copyAddBlocks);
            if (maxToMinRoofs.Count > 0)
                retRes.AddRange(maxToMinRoofs);
            return retRes;
        }
        List<CreateBlockInfo> MaxRoofPipeConvert() 
        {
            //找到所有大屋面的雨水斗的排水点。将所有的雨水排水点位根据基点平移到住人顶层，在平移后的点位生成屋面雨水立管并添加编号标注（Y1L）。
            string blockName = SetServicesModel.Instance.drawingScale == EnumDrawingScale.DrawingScale1_150 ? ThWSSCommon.Layout_PositionRiser150BlockName : ThWSSCommon.Layout_PositionRiserBlockName;
            var retRes = new List<CreateBlockInfo>();
            if (_roofWaterBuckets == null || _roofWaterBuckets.Count < 1)
                return retRes;
            foreach (var item in _roofWaterBuckets) 
            {
                if(!item.roofType.Contains("大"))
                    continue;
                var newPoint = item.centerPoint;
                var block = new CreateBlockInfo(item.roofUid, blockName, ThWSSCommon.Layout_FloorDrainBlockRainLayerName, newPoint,EnumEquipmentType.riser);
                block.tag = "Y1L";
                string dnNum = "";
                if (item.equipmentType == EnumEquipmentType.gravityRainBucket)
                    dnNum = SetServicesModel.Instance.maxRoofGravityRainBucketRiserPipeDiameter.ToString();
                else
                    dnNum = SetServicesModel.Instance.maxRoofSideDrainRiserPipeDiameter.ToString();
                block.dymBlockAttr.Add("可见性1", dnNum);
                retRes.Add(block);
            }
            return retRes;
        }
        List<CreateBlockInfo> MinRoofToMaxRoof() 
        {
            var retRes = new List<CreateBlockInfo>();
            if (_maxRoofFloors == null || _maxRoofFloors.Count < 1)
                return retRes;
            //找到小屋面上所有的雨水斗和雨水斗的排水点。根据基点将雨水斗排水点在大屋面的位置生成雨水立管然后编号（Y1L）。
            string blockName = SetServicesModel.Instance.drawingScale == EnumDrawingScale.DrawingScale1_150 ? ThWSSCommon.Layout_PositionRiser150BlockName : ThWSSCommon.Layout_PositionRiserBlockName;
            foreach (var maxRoof in _maxRoofFloors)
            {
                foreach (var pipe in _roofWaterBuckets) 
                {
                    if (pipe.roofType.Contains("大"))
                        continue;
                    var newPoint = maxRoof.datumPoint + pipe.moveVector;
                    var block = new CreateBlockInfo(maxRoof.floorUid, blockName, ThWSSCommon.Layout_FloorDrainBlockRainLayerName, newPoint, EnumEquipmentType.riser);
                    block.tag = "Y1L";
                    string dnNum = "";
                    if (pipe.equipmentType == EnumEquipmentType.gravityRainBucket)
                        dnNum = SetServicesModel.Instance.minRoofGravityRainBucketRiserPipeDiameter.ToString();
                    else
                        dnNum = SetServicesModel.Instance.minRoofSideDrainRiserPipeDiameter.ToString();
                    block.dymBlockAttr.Add("可见性1", dnNum);
                    retRes.Add(block);
                }
            }
            return retRes;
        }
        List<CreateBlockInfo> CopyY1LToLivingFloor(FloorFramed livingFloor, List<CreateBlockInfo> copyBlocks) 
        {
            var retRes = new List<CreateBlockInfo>();
            foreach (var maxRoof in _maxRoofFloors) 
            {
                var targetBlocks = copyBlocks.Where(c => c.floorId.Equals(maxRoof.floorUid)).ToList();
                if (targetBlocks == null || targetBlocks.Count < 1)
                    continue;
                foreach (var cBlock in targetBlocks) 
                {
                    retRes.Add(DrainSysAGCommon.CopyOneBlock(cBlock, maxRoof.datumPoint, livingFloor.datumPoint, livingFloor.floorUid));
                }
            }
            return retRes;
        }
        List<CreateBlockInfo> CopyPipeToMaxRoof(List<CreateBlockInfo> copyBlocks,Point3d oldBasePoint) 
        {
            //有大屋面时，找到住人顶层的所有污废立管（PL）和废水立管（FL）。将所有的立管和编号标注根据基点复制到大屋面。
            var retRes = new List<CreateBlockInfo>();
            if (_maxRoofFloors == null || _maxRoofFloors.Count < 1 || copyBlocks ==null || copyBlocks.Count<1)
                return retRes;
            foreach (var roof in _maxRoofFloors) 
            {
                //复制数据到大屋面
                foreach (var cBlock in copyBlocks) 
                {
                    retRes.Add(DrainSysAGCommon.CopyOneBlock(cBlock,oldBasePoint,roof.datumPoint,roof.floorUid));
                }
            }
            return retRes;
        }
        List<CreateBlockInfo> MaxRoofToMinRoof(List<CreateBlockInfo> maxRoofCopy) 
        {
            var retRes = new List<CreateBlockInfo>();
            if (null == maxRoofCopy || maxRoofCopy.Count < 1 || _roomEngine == null)
                return retRes;
            if (_maxRoofFloors == null || _maxRoofFloors.Count<1 || _minRoofFloors ==null || _minRoofFloors.Count<1)
                return retRes;
            //找到大屋面上所有的污废立管（PL）和废水立管（FL）。根据基点在小屋面找相同位置的点位。若点位在任何一个空间框线内则将这根立管和编号复制到小屋面。
            foreach (var mRoof in _maxRoofFloors) 
            {
                //获取屋面的空间
                var thisAllRooms = _roomEngine.GetAllRooms(mRoof.blockOutPointCollection);
                if (null == thisAllRooms || thisAllRooms.Count < 1)
                    continue;
                foreach (var cBlock in maxRoofCopy)
                {
                    if (!cBlock.floorId.Equals(mRoof.floorUid))
                        continue;
                    if (!cBlock.tag.ToUpper().Equals("PL") && !cBlock.tag.ToUpper().Equals("FL"))
                        continue;
                    bool inRoom = false;
                    foreach (var room in thisAllRooms) 
                    {
                        if (inRoom)
                            break;
                        inRoom = room.Boundary.ToNTSPolygon().ToDbPolylines().FirstOrDefault().Contains(cBlock.createPoint);
                    }
                    if (inRoom) 
                    {
                        foreach (var minRoof in _minRoofFloors) 
                        {
                            retRes.Add(DrainSysAGCommon.CopyOneBlock(cBlock, mRoof.datumPoint, minRoof.datumPoint, minRoof.floorUid));
                        }
                    }
                }
            }
            return retRes;
        }

    }
    class RoofPointInfo 
    {
        public ObjectId roofId { get; }
        public string roofUid { get; }
        public string roofType  { get; }
        public Point3d centerPoint { get; }
        public EnumEquipmentType equipmentType { get; }
        public Point3d basePoint { get; }
        public Vector3d moveVector { get; }
        public RoofPointInfo(FloorFramed floor, EnumEquipmentType type,Point3d point) 
        {
            this.roofId = floor.blockId;
            this.roofUid = floor.floorUid;
            this.roofType = floor.floorType;
            this.basePoint = floor.datumPoint;
            this.equipmentType = type;
            this.centerPoint = point;
            this.moveVector = point - basePoint;
        }
    }
}
