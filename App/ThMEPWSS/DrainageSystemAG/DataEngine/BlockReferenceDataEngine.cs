using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using ThMEPWSS.DrainageSystemAG.Models;

namespace ThMEPWSS.DrainageSystemAG.DataEngine
{
    class BlockReferenceDataEngine
    {
        List<EquipmentBlcokVisitorModel> equipmentBlcokVisitors { get; }
        List<EquipmentBlcokVisitorModel> equipmentBlcokVisitorsModelSpace { get; }
        Dictionary<string, List<string>> _layerNameConfig;
        /// <summary>
        /// 获取本图纸中所有的设备信息
        /// </summary>
        public BlockReferenceDataEngine(Dictionary<string,List<string>> configLayers) 
        {
            _layerNameConfig = new Dictionary<string, List<string>>();
            equipmentBlcokVisitors = new List<EquipmentBlcokVisitorModel>();
            equipmentBlcokVisitorsModelSpace = new List<EquipmentBlcokVisitorModel>();

            if (null != configLayers && configLayers.Count > 0)
            {
                foreach (var keyValue in configLayers)
                {
                    if (string.IsNullOrEmpty(keyValue.Key) || keyValue.Value == null || keyValue.Value.Count < 1)
                        continue;
                    var tempListNames = new List<string>();
                    foreach (var str in keyValue.Value)
                    {
                        if (string.IsNullOrEmpty(str) || tempListNames.Any(c => c.Equals(str)))
                            continue;
                        tempListNames.Add(str);
                    }
                    if (tempListNames.Count < 1)
                        continue;
                    _layerNameConfig.Add(keyValue.Key, tempListNames);
                }
            }

            InitBlockNames();
            InitModelSapaceBlock();
            
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                if (null != this.equipmentBlcokVisitorsModelSpace && this.equipmentBlcokVisitorsModelSpace.Count > 0)
                {
                    ThDistributionElementExtractor thDistributionMS = new ThDistributionElementExtractor();
                    foreach (var item in this.equipmentBlcokVisitorsModelSpace)
                    {
                        thDistributionMS.Accept(item.equipmentDataVisitor);
                    }
                    thDistributionMS.Extract(acdb.Database);

                    var blocks = acdb.ModelSpace.OfType<BlockReference>().ToList();   
                    foreach (var block in blocks)
                    {
                        if (block == null || block.BlockTableRecord == null || !block.BlockTableRecord.IsValid)
                            continue;
                        bool isEx = MatchBlockToEquipmentVistor(block, Matrix3d.Identity);
                        if (!isEx)
                            continue;
                        var mcs2wcs = block.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                        var blockTableRecord = acdb.Blocks.Element(block.BlockTableRecord);
                        var objs = new ObjectIdCollection();
                        foreach (var objId in blockTableRecord)
                        {
                            var dbObj = acdb.Element<Entity>(objId);
                            if (dbObj.Visible)
                            {
                                objs.Add(objId);
                            }
                        }
                        foreach (ObjectId objId in objs)
                        {
                            var dbObj = acdb.Element<Entity>(objId);
                            if (dbObj is BlockReference blockObj)
                            {
                                if (blockObj.BlockTableRecord.IsNull)
                                {
                                    continue;
                                }
                                MatchBlockToEquipmentVistor(blockObj, mcs2wcs);
                            }
                        }
                    }
                }
                if (null != this.equipmentBlcokVisitors && this.equipmentBlcokVisitors.Count > 0)
                {
                    ThDistributionElementExtractor thDistribution = new ThDistributionElementExtractor();
                    foreach (var item in this.equipmentBlcokVisitors)
                    {
                        thDistribution.Accept(item.equipmentDataVisitor);
                    }
                    thDistribution.Extract(acdb.Database);
                }
            }
        }
        private bool MatchBlockToEquipmentVistor(BlockReference block,Matrix3d matrix)
        {
            var isEx = true;
            foreach (var visitor in equipmentBlcokVisitorsModelSpace)
            {
                var elems = new List<ThRawIfcDistributionElementData>();
                if (visitor.equipmentDataVisitor.IsBuildElementBlockReference(block))
                {
                    if (visitor.equipmentDataVisitor.CheckLayerValid(block) && visitor.equipmentDataVisitor.IsDistributionElement(block))
                    {
                        visitor.equipmentDataVisitor.DoExtract(elems, block, matrix);
                    }
                }
                if (null != elems && elems.Count > 0)
                {
                    visitor.equipmentDataVisitor.Results.AddRange(elems);
                    isEx = false;
                }
            }
            return isEx;
        }
        /// 获取框线内的设备块
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<EquipmentBlcokModel> GetPolylineEquipmentBlocks(Polyline polyline, double disToDist = 30) 
        {
            var equipments = new List<EquipmentBlcokModel>();
            var tempModel = GetModelSpaceEquipmentBlocks(polyline);
            if (null != tempModel && tempModel.Count > 0)
                equipments.AddRange(tempModel);
            var tempExts = GetExtractEquipmentBlocks(polyline);
            if (null != tempExts && tempExts.Count > 0)
                equipments.AddRange(tempExts);

            return Distinct(equipments, disToDist);
        }
        private List<EquipmentBlcokModel> Distinct(List<EquipmentBlcokModel> targetBlocks,double disToDist) 
        {
            //去重，在一定范围内不能有同一类的数据
            var retBlocks = new List<EquipmentBlcokModel>();
            foreach (var item in targetBlocks)
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
        public List<EquipmentBlcokModel> GetModelSpaceEquipmentBlocks(Polyline polyline) 
        {
            var equipments = new List<EquipmentBlcokModel>();
            if (null == polyline || this.equipmentBlcokVisitors == null || this.equipmentBlcokVisitors.Count < 1)
                return equipments;
            foreach (var item in this.equipmentBlcokVisitors)
            {
                if (item.equipmentDataVisitor == null || item.equipmentDataVisitor.Results == null || item.equipmentDataVisitor.Results.Count < 1)
                    continue;
                var blcokModel = new EquipmentBlcokModel(item.enumEquipmentType);
                foreach (var obj in item.equipmentDataVisitor.Results)
                {
                    if (null == obj)
                        continue;
                    BlockReference block = obj.Geometry as BlockReference;

                    if (null == block)
                        continue;
                    if (!block.Bounds.HasValue)
                        continue;
                    var centerPoint = DrainSysAGCommon.GetBlockGeometricCenter(block);
                    if (polyline.Contains(centerPoint))
                    {
                        blcokModel.blockReferences.Add(block);
                    }
                }
                if (blcokModel.blockReferences.Count > 0)
                    equipments.Add(blcokModel);
            }
            return equipments;
        }
        public List<EquipmentBlcokModel> GetExtractEquipmentBlocks(Polyline polyline) 
        {
            var equipments = new List<EquipmentBlcokModel>();
            if (null == polyline || this.equipmentBlcokVisitors == null || this.equipmentBlcokVisitors.Count < 1)
                return equipments;
            foreach (var item in this.equipmentBlcokVisitorsModelSpace)
            {
                if (item.equipmentDataVisitor == null || item.equipmentDataVisitor.Results == null || item.equipmentDataVisitor.Results.Count < 1)
                    continue;
                var blcokModel = new EquipmentBlcokModel(item.enumEquipmentType);
                foreach (var obj in item.equipmentDataVisitor.Results)
                {
                    if (null == obj)
                        continue;
                    BlockReference block = obj.Geometry as BlockReference;

                    if (null == block)
                        continue;
                    var centerPoint = block.GeometricExtents.CenterPoint();
                    var pointP = new Point3d(centerPoint.X, centerPoint.Y, 0);
                    if (polyline.Contains(pointP))
                    {
                        blcokModel.blockReferences.Add(block);
                    }
                }
                if (blcokModel.blockReferences.Count > 0)
                    equipments.Add(blcokModel);
            }
            return equipments;
        }
        private void InitBlockNames() 
        {
            //拖把池 - 块名称过滤
            Dictionary<string, int> mopPoolNames = new Dictionary<string, int>();
            mopPoolNames.Add(ThWSSCommon.MopPoolBlockName, 2);
            GetVisitorDictionary(EnumEquipmentType.mopPool, ref mopPoolNames);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.mopPool, mopPoolNames));

            //洗衣机 - 块名称过滤
            Dictionary<string, int> washingMachineNames = new Dictionary<string, int>();
            washingMachineNames.Add(ThWSSCommon.WashingMachineBlockName, 2);
            GetVisitorDictionary(EnumEquipmentType.washingMachine, ref washingMachineNames);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.washingMachine, washingMachineNames));

            //重力流雨水斗 - 块名称过滤
            Dictionary<string, int> gravityRainBucketNames = new Dictionary<string, int>();
            gravityRainBucketNames.Add(ThWSSCommon.GravityFlowRainBucketBlockName_Contain, 1);
            gravityRainBucketNames.Add(ThWSSCommon.GravityFlowRainBucketBlockName, 2);
            gravityRainBucketNames.Add("ffdsf", 2);
            gravityRainBucketNames.Add("W-drain-51", 2);
            gravityRainBucketNames.Add("W-drain-4", 2);
            GetVisitorDictionary(EnumEquipmentType.gravityRainBucket, ref gravityRainBucketNames);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.gravityRainBucket, gravityRainBucketNames));

            //侧入式雨水斗 - 块名称过滤
            Dictionary<string, int> sideRainBucketNames = new Dictionary<string, int>();
            sideRainBucketNames.Add(ThWSSCommon.SideRainBucketBlockName_1, 2);
            sideRainBucketNames.Add(ThWSSCommon.SideRainBucketBlockName_2, 2);
            sideRainBucketNames.Add("W-drain-21", 2);
            GetVisitorDictionary(EnumEquipmentType.sideRainBucket, ref sideRainBucketNames);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel( EnumEquipmentType.sideRainBucket, sideRainBucketNames));

            //获取设备图块
            Dictionary<string, int> equipmentNames = new Dictionary<string, int>();
            equipmentNames.Add("AE-EQPM", 1);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.equipment, equipmentNames, true));

            //获取建筑标高图块
            Dictionary<string, int> buildingElevationNames = new Dictionary<string, int>();
            buildingElevationNames.Add("AD-LEVL-HIGH", 1);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.buildingElevation, buildingElevationNames, true));

            //获取空调外机图块
            Dictionary<string, int> airOutMachineNames = new Dictionary<string, int>();
            airOutMachineNames.Add("H-AC-2", 1);
            airOutMachineNames.Add("H-AC-3", 1);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.airConditioningOutMachine, airOutMachineNames, true));

            //获取门的图块
            Dictionary<string, int> doorNames = new Dictionary<string, int>();
            doorNames.Add("A-door-", 1);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.door, doorNames));

            //获取马桶
            Dictionary<string, int> toiletNames = new Dictionary<string, int>();
            toiletNames.Add("A-Toilet-5", 1);
            GetVisitorDictionary(EnumEquipmentType.toilet, ref toiletNames);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.toilet, toiletNames));

            //获取厨房台盆
            Dictionary<string, int> kitchenSinkNames = new Dictionary<string, int>();
            kitchenSinkNames.Add("A-Kitchen-4",1);
            GetVisitorDictionary(EnumEquipmentType.kitchenBasin, ref kitchenSinkNames);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.kitchenBasin, kitchenSinkNames));

            /* 楼梯块目前有问题，暂时不获取
            //获取楼梯块
            Dictionary<string, int> stairsNames = new Dictionary<string, int>();
            stairsNames.Add("楼梯,DB", 1);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.stairs, stairsNames));
            */
        }

        private void InitModelSapaceBlock() 
        {
            //立管地漏改为从本地获取

            //地漏 - 块名称过滤
            Dictionary<string, int> floorDrainNames = new Dictionary<string, int>();
            floorDrainNames.Add("地漏-AI", 2);
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.floorDrain, floorDrainNames,false,true));

            //冷凝水立管 - 块名称过滤
            Dictionary<string, int> condensateRiserNames = new Dictionary<string, int>();
            condensateRiserNames.Add("冷凝水立管-AI", 2);
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.condensateRiser, condensateRiserNames, false, true));

            //屋面雨水立管 - 块名称过滤
            var roofRiserNames = new Dictionary<string, int>();
            roofRiserNames.Add("屋面雨水立管-AI", 2);
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.roofRainRiser, roofRiserNames, false, true));

            //阳台立管 - 块名称过滤
            var balconyRiserNames = new Dictionary<string, int>();
            balconyRiserNames.Add("阳台立管-AI", 2);
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.balconyRiser, balconyRiserNames, false, true));

            //污废合流立管
            var swRaiserNames = new Dictionary<string, int>();
            swRaiserNames.Add("污废合流立管-AI",2);
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.sewageWasteRiser,swRaiserNames, false, true));

            //通气立管
            var tlRaiseNames = new Dictionary<string, int>();
            tlRaiseNames.Add("通气立管-AI",2);
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.ventRiser,tlRaiseNames, false, true));

            //沉箱立管
            var corRaiseNames = new Dictionary<string, int>();
            corRaiseNames.Add("沉箱立管-AI",2);
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.caissonRiser, corRaiseNames, false, true));

            //废水立管
            var fRaiseNames = new Dictionary<string, int>();
            fRaiseNames.Add("废水立管-AI",2);
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.wastewaterRiser,fRaiseNames, false, true));

            //污水立管
            var sewageRaiseNames = new Dictionary<string, int>();
            sewageRaiseNames.Add("污水立管-AI", 2);
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.sewageWaterRiser, sewageRaiseNames, false, true));

            //拖把池 - 块名称过滤
            var mopPoolNames = new Dictionary<string, int>();
            GetVisitorDictionary(EnumEquipmentType.mopPool, ref mopPoolNames);
            if(mopPoolNames.Count>0)
                this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.mopPool, mopPoolNames));

            //洗衣机 - 块名称过滤
            Dictionary<string, int> washingMachineNames = new Dictionary<string, int>();
            GetVisitorDictionary(EnumEquipmentType.washingMachine, ref washingMachineNames);
            if (washingMachineNames.Count > 0)
                this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.washingMachine, washingMachineNames));

            //重力流雨水斗 - 块名称过滤
            Dictionary<string, int> gravityRainBucketNames = new Dictionary<string, int>();
            GetVisitorDictionary(EnumEquipmentType.gravityRainBucket, ref gravityRainBucketNames);
            if (gravityRainBucketNames.Count > 0)
                this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.gravityRainBucket, gravityRainBucketNames));

            //侧入式雨水斗 - 块名称过滤
            Dictionary<string, int> sideRainBucketNames = new Dictionary<string, int>();
            GetVisitorDictionary(EnumEquipmentType.sideRainBucket, ref sideRainBucketNames);
            if (sideRainBucketNames.Count > 0)
                this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.sideRainBucket, sideRainBucketNames));

            //获取马桶
            Dictionary<string, int> toiletNames = new Dictionary<string, int>();
            toiletNames.Add("A-Toilet-5", 1);
            GetVisitorDictionary(EnumEquipmentType.toilet, ref toiletNames);
            this.equipmentBlcokVisitorsModelSpace.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.toilet, toiletNames));
        }

        private void GetVisitorDictionary(EnumEquipmentType type,ref Dictionary<string,int> visirorDict) 
        {
            foreach (var keyValue in _layerNameConfig)
            {
                var thisType = -1;
                switch (keyValue.Key) 
                {
                    case "侧入式雨水斗":
                        thisType = (int)EnumEquipmentType.sideRainBucket;
                        break;
                    case "重力流雨水斗":
                        thisType = (int)EnumEquipmentType.gravityRainBucket;
                        break;
                    case "拖把池":
                        thisType = (int)EnumEquipmentType.mopPool;
                        break;
                    case "洗衣机":
                        thisType = (int)EnumEquipmentType.washingMachine;
                        break;
                    case "坐便器":
                        thisType = (int)EnumEquipmentType.toilet;
                        break;
                    case "厨房洗涤盆":
                        thisType = (int)EnumEquipmentType.kitchenBasin;
                        break;
                }
                if (thisType != (int)type)
                    continue;
                foreach (var name in keyValue.Value) 
                {
                    if (visirorDict.Any(c => c.Key.ToUpper().Equals(name.ToUpper())))
                        continue;
                    visirorDict.Add(name,4);
                }
            }
        }
    }
}
