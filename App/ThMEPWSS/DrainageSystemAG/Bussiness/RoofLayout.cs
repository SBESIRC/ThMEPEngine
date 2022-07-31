using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    /// <summary>
    /// 屋面排布计算逻辑
    /// </summary>
    class RoofLayout
    {
        List<RoofPointInfo> _roofWaterBuckets = new List<RoofPointInfo>();
        Dictionary<string, List<ThIfcRoom>> _roofFloorRooms = new Dictionary<string, List<ThIfcRoom>>();
        List<FloorFramed> _roofFloors;
        List<FloorFramed> _maxRoofFloors;
        List<FloorFramed> _minRoofFloors;
        double _convertPipeDimLineLength = 2000;
        string _convertPipeDimText = "接雨水斗";
        public RoofLayout(List<FloorFramed> roofFloors, List<RoofPointInfo> roofWaterBuckets, Dictionary<string, List<ThIfcRoom>> roofFloorRooms) 
        {
            _roofFloors = new List<FloorFramed>();
            _maxRoofFloors = new List<FloorFramed>();
            _minRoofFloors = new List<FloorFramed>();
            _roofFloorRooms = new Dictionary<string, List<ThIfcRoom>>();
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
            }
            if (null != roofWaterBuckets && roofWaterBuckets.Count > 0)
            {
                foreach (var item in roofWaterBuckets)
                    _roofWaterBuckets.Add(item);
            }
            if (null != roofFloorRooms && roofFloorRooms.Count > 0)
            {
                foreach (var keyValue in roofFloorRooms) 
                {
                    if (null == keyValue.Key || null == keyValue.Value || keyValue.Value.Count < 1)
                        continue;
                    _roofFloorRooms.Add(keyValue.Key,keyValue.Value);
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
            var minToMaxRoofs = MinRoofToMaxRoof();
            if (minToMaxRoofs.Count > 0)
                retRes.AddRange(minToMaxRoofs);

            //将住人屋面的部分立管复制到屋面
            var copyAddBlocks = CopyPipeToMaxRoof(copyBlocks, livingFloor.datumPoint);
            if (copyAddBlocks.Count > 0)
                retRes.AddRange(copyAddBlocks);
            var maxToMinRoofs=  MaxRoofToMinRoof(copyAddBlocks);
            if (maxToMinRoofs.Count > 0)
                retRes.AddRange(maxToMinRoofs);
            return retRes;
        }
        public List<CreateBlockInfo> RoofY1LGravityConverter(FloorFramed livingFloor, List<CreateBlockInfo> floorY1LBlcoks, out List<CreateBasicElement> addLines,out List<CreateDBTextElement> addText ,double findY1LDis) 
        {
            //获取改楼层的Y1L
            addLines = new List<CreateBasicElement>();
            var retRes = new List<CreateBlockInfo>();
            addText = new List<CreateDBTextElement>();
            if (null == floorY1LBlcoks || floorY1LBlcoks.Count < 1)
                return retRes;
            var maxRoofConvert = MaxRoofPipeConvert();
            var copyMaxRoofPipeToLiving = CopyY1LToLivingFloor(livingFloor, maxRoofConvert);
            foreach (var item in copyMaxRoofPipeToLiving) 
            {
                var roofPipe = maxRoofConvert.Where(c => c.uid.Equals(item.copyId)).FirstOrDefault();
                var roofPoint = GetRoofPointInfo(roofPipe.createPoint);
                if (roofPoint == null || roofPoint.equipmentType != EnumEquipmentType.gravityRainBucket)
                    continue;
                var liveFloorY1L = floorY1LBlcoks.Where(c => c.createPoint.DistanceTo(item.createPoint) < findY1LDis).FirstOrDefault();
                if (null == liveFloorY1L)
                    continue;
                if (item.createPoint.DistanceTo(liveFloorY1L.createPoint) < 100)
                    continue;
                item.tag = DrainSysAGCommon.NOTCOPYTAG;
                retRes.Add(item);
                var dimDir = (Vector3d.XAxis - Vector3d.YAxis).GetNormal();
                var dimAddLines = PipeAddDim(livingFloor.floorUid, item.createPoint, dimDir, _convertPipeDimLineLength, _convertPipeDimText, out List<CreateDBTextElement> dimAddTexts);
                if (dimAddLines != null)
                    addLines.AddRange(dimAddLines);
                if (null != dimAddTexts)
                    addText.AddRange(dimAddTexts);
                //位置偏差大需要连线
                var lineDir = (liveFloorY1L.createPoint - item.createPoint).GetNormal();
                var lineSp = item.createPoint + lineDir.MultiplyBy(DrainSysAGCommon.GetBlockCircleRadius(liveFloorY1L, "可见性1"));
                var lineEp = liveFloorY1L.createPoint;
                if (lineSp.DistanceTo(lineEp) < 10)
                    continue;
                if (lineSp.DistanceTo(lineEp) > item.createPoint.DistanceTo(liveFloorY1L.createPoint))
                    continue;
                addLines.Add(new CreateBasicElement(item.floorId, new Line(lineSp, lineEp), ThWSSCommon.Layout_PipeRainDrainConnectLayerName, "", DrainSysAGCommon.NOTCOPYTAG));
            }
            return retRes;
        }
        public List<CreateBlockInfo> RoofY1LSideConverter(FloorFramed livingFloor, List<CreateBlockInfo> floorY1LBlcoks, out List<CreateBasicElement> addLines, out List<CreateDBTextElement> addText, double findY1LDis)
        {
            addLines = new List<CreateBasicElement>();
            var retRes = new List<CreateBlockInfo>();
            addText = new List<CreateDBTextElement>();
            if (null == floorY1LBlcoks || floorY1LBlcoks.Count < 1)
                return retRes;
            var maxRoofConvert = MaxRoofPipeConvert();
            foreach (var roof in _maxRoofFloors) 
            {
                var copyY1ToRoof = CopyY1LToRoof(roof,livingFloor, floorY1LBlcoks);
                if (copyY1ToRoof == null || copyY1ToRoof.Count < 1)
                    continue;
                foreach (var item in copyY1ToRoof)
                {
                    var maxY1L = maxRoofConvert.Where(c => c.createPoint.DistanceTo(item.createPoint) < findY1LDis).FirstOrDefault();
                    if (null == maxY1L)
                        continue;
                    var roofPoint = GetRoofPointInfo(maxY1L.createPoint);
                    if (roofPoint == null || roofPoint.equipmentType != EnumEquipmentType.sideRainBucket)
                        continue;
                    if (item.createPoint.DistanceTo(maxY1L.createPoint) < 100)
                        continue;
                    retRes.Add(item);
                    var dimDir = (Vector3d.XAxis - Vector3d.YAxis).GetNormal();
                    var dimAddLines = PipeAddDim(livingFloor.floorUid, maxY1L.createPoint, dimDir, _convertPipeDimLineLength, _convertPipeDimText, out List<CreateDBTextElement> dimAddTexts);
                    if (dimAddLines != null)
                        addLines.AddRange(dimAddLines);
                    if (null != dimAddTexts)
                        addText.AddRange(dimAddTexts);
                    //位置偏差大需要连线
                    var lineDir = (maxY1L.createPoint - item.createPoint).GetNormal();
                    var lineSp = item.createPoint + lineDir.MultiplyBy(DrainSysAGCommon.GetBlockCircleRadius(item, "可见性1"));
                    var lineEp = maxY1L.createPoint;
                    if (lineSp.DistanceTo(lineEp) < 10)
                        continue;
                    if (lineSp.DistanceTo(lineEp) > item.createPoint.DistanceTo(maxY1L.createPoint))
                        continue;
                    addLines.Add(new CreateBasicElement(maxY1L.floorId, new Line(lineSp, lineEp), ThWSSCommon.Layout_PipeRainDrainConnectLayerName, "", DrainSysAGCommon.NOTCOPYTAG));
                }
            }
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
        List<CreateBlockInfo> CopyY1LToRoof(FloorFramed maxRoofFloor, FloorFramed livingFloor, List<CreateBlockInfo> copyBlocks)
        {
            var retRes = new List<CreateBlockInfo>();
            if (copyBlocks == null || copyBlocks.Count < 1)
                return retRes;
            var targetBlocks = copyBlocks.Where(c => c.floorId.Equals(livingFloor.floorUid)).ToList();
            foreach (var cBlock in targetBlocks)
            {
                retRes.Add(DrainSysAGCommon.CopyOneBlock(cBlock, livingFloor.datumPoint, maxRoofFloor.datumPoint, maxRoofFloor.floorUid));
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
            if (null == maxRoofCopy || maxRoofCopy.Count < 1)
                return retRes;
            if (_maxRoofFloors == null || _maxRoofFloors.Count<1 || _minRoofFloors ==null || _minRoofFloors.Count<1)
                return retRes;
            //找到大屋面上所有的污废立管（PL）和废水立管（FL）。根据基点在小屋面找相同位置的点位。若点位在任何一个空间框线内则将这根立管和编号复制到小屋面。
            foreach (var mRoof in _maxRoofFloors) 
            {
                //获取屋面的空间
                var thisAllRooms = _roofFloorRooms.Where(c=>c.Key.Equals(mRoof.floorUid)).FirstOrDefault().Value;
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
                        inRoom = room.Boundary.ToNTSPolygonalGeometry().ToDbPolylines().FirstOrDefault().Contains(cBlock.createPoint);
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

        List<CreateBasicElement> PipeAddDim(string floorId,Point3d startPt,Vector3d fisrtLineDir,double outLength,string dimText,out List<CreateDBTextElement> addTexts) 
        {
            var addLines = new List<CreateBasicElement>();
            addTexts = new List<CreateDBTextElement>();
            //转管时生成的立管加入标注
            var lineSp = startPt;
            var lineEp = lineSp + fisrtLineDir.MultiplyBy(outLength);
            var upText = DrainSysAGCommon.CreateDBText(dimText, lineEp, ThWSSCommon.Layout_PipeRainTextLayerName, ThWSSCommon.Layout_TextStyle);
            var upMaxPoint = upText.GeometricExtents.MaxPoint;
            var upMinPoint = upText.GeometricExtents.MinPoint;
            var upWidth = upMaxPoint.X - upMinPoint.X;
            var upHeight = upMaxPoint.Y - upMinPoint.Y;
            var leftDir = Vector3d.XAxis;

            addLines.Add(new CreateBasicElement(floorId, new Line(lineSp, lineEp), ThWSSCommon.Layout_PipeRainTextLayerName, "", DrainSysAGCommon.NOTCOPYTAG));
            addLines.Add(new CreateBasicElement(floorId, new Line(lineEp, lineEp + leftDir.MultiplyBy(upWidth + 100)), ThWSSCommon.Layout_PipeRainTextLayerName, "", DrainSysAGCommon.NOTCOPYTAG));
            var upTextPt = lineEp + Vector3d.XAxis.MultiplyBy(10) + Vector3d.YAxis.MultiplyBy(10);
            upText.Position = upTextPt;

            addTexts.Add(new CreateDBTextElement(floorId, upTextPt, upText, "", ThWSSCommon.Layout_PipeCasingTextLayerName, ThWSSCommon.Layout_TextStyle,"", DrainSysAGCommon.NOTCOPYTAG));
            return addLines;
        }
        RoofPointInfo GetRoofPointInfo(Point3d centerPoint) 
        {
            if (null == _roofWaterBuckets || _roofWaterBuckets.Count < 1)
                return null;
            foreach (var item in _roofWaterBuckets) 
            {
                if (item.centerPoint.DistanceTo(centerPoint) < 5)
                    return item;
            }
            return null;
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
