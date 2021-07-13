using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using ThMEPWSS.DrainageSystemAG.Models;

namespace ThMEPWSS.DrainageSystemAG.DataEngine
{
    class BlockReferenceDataEngine
    {
        List<EquipmentBlcokVisitorModel> equipmentBlcokVisitors { get; }
        /// <summary>
        /// 获取本图纸中所有的设备信息
        /// </summary>
        public BlockReferenceDataEngine() 
        {
            equipmentBlcokVisitors = new List<EquipmentBlcokVisitorModel>();
            InitBlockNames();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                ThDistributionElementExtractor thDistribution = new ThDistributionElementExtractor();
                foreach (var item in this.equipmentBlcokVisitors)
                {
                    thDistribution.Accept(item.equipmentDataVisitor);
                }
                thDistribution.Extract(acdb.Database);
            }

        }
        /// 获取框线内的设备块
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<EquipmentBlcokModel> GetPolylineEquipmentBlocks(Polyline polyline) 
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
                    var centerPoint = block.GeometricExtents.CenterPoint();
                    var pointP = new Point3d(centerPoint.X, centerPoint.Y, 0);
                    if (polyline.Contains(pointP))
                    {
                        blcokModel.blockReferences.Add(block);
                    }
                }
                if(blcokModel.blockReferences.Count>0)
                    equipments.Add(blcokModel);
            }
            return equipments;
        }
        private void InitBlockNames() 
        {
            //拖把池 - 块名称过滤
            Dictionary<string, int> mopPoolNames = new Dictionary<string, int>();
            mopPoolNames.Add(ThWSSCommon.MopPoolBlockName, 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.mopPool, mopPoolNames));

            //洗衣机 - 块名称过滤
            Dictionary<string, int> washingMachineNames = new Dictionary<string, int>();
            washingMachineNames.Add(ThWSSCommon.WashingMachineBlockName, 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.washingMachine, washingMachineNames));

            //厨房台盆 - 块名称过滤
            Dictionary<string, int> kitchenBasinNames = new Dictionary<string, int>();
            kitchenBasinNames.Add(ThWSSCommon.KitchenBasinBlockName, 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.kitchenBasin, kitchenBasinNames));

            //地漏 - 块名称过滤
            Dictionary<string, int> floorDrainNames = new Dictionary<string, int>();
            //floorDrainNames.Add("地漏", 1);
            floorDrainNames.Add("W-drain", 2);
            floorDrainNames.Add("地漏平面", 2);
            floorDrainNames.Add("地漏-阳台、空调", 2);
            floorDrainNames.Add("地漏-卫", 2);
            floorDrainNames.Add(ThWSSCommon.FloorDrainBlockName_1, 2);
            floorDrainNames.Add(ThWSSCommon.FloorDrainBlockName_2, 2);
            floorDrainNames.Add(ThWSSCommon.FloorDrainBlockName_3, 2);
            floorDrainNames.Add(ThWSSCommon.FloorDrainBlockName_4, 2);
            floorDrainNames.Add(ThWSSCommon.FloorDrainBlockName_5, 2);
            floorDrainNames.Add(ThWSSCommon.FloorDrainBlockName_6, 2);
            floorDrainNames.Add(ThWSSCommon.FloorDrainBlockName_7, 2);
            floorDrainNames.Add(ThWSSCommon.FloorDrainBlockName_8, 2);
            floorDrainNames.Add(ThWSSCommon.FloorDrainBlockName_9, 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.floorDrain, floorDrainNames));

            //重力流雨水斗 - 块名称过滤
            Dictionary<string, int> gravityRainBucketNames = new Dictionary<string, int>();
            gravityRainBucketNames.Add(ThWSSCommon.GravityFlowRainBucketBlockName_Contain, 1);
            gravityRainBucketNames.Add(ThWSSCommon.GravityFlowRainBucketBlockName, 2);
            gravityRainBucketNames.Add("ffdsf", 2);
            gravityRainBucketNames.Add("W-drain-51", 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.gravityRainBucket, gravityRainBucketNames));

            //侧入式雨水斗 - 块名称过滤
            Dictionary<string, int> sideRainBucketNames = new Dictionary<string, int>();
            sideRainBucketNames.Add(ThWSSCommon.SideRainBucketBlockName_1, 2);
            sideRainBucketNames.Add(ThWSSCommon.SideRainBucketBlockName_2, 2);
            sideRainBucketNames.Add("W-drain-21", 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel( EnumEquipmentType.sideRainBucket, sideRainBucketNames));

            //阳台立管 - 块名称过滤
            Dictionary<string, int> balconyRiserNames = new Dictionary<string, int>();
            balconyRiserNames.Add(ThWSSCommon.BalconyRiserBlockName, 2);
            balconyRiserNames.Add("阳台立管", 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.balconyRiser, balconyRiserNames));

            //屋面雨水立管 - 块名称过滤
            Dictionary<string, int> roofRiserNames = new Dictionary<string, int>();
            roofRiserNames.Add(ThWSSCommon.RoofRainwaterRiserBlockName, 2);
            roofRiserNames.Add("屋面雨水", 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.roofRainRiser, roofRiserNames));

            //冷凝水立管 - 块名称过滤
            Dictionary<string, int> condensateRiserNames = new Dictionary<string, int>();
            condensateRiserNames.Add(ThWSSCommon.CondensateRiserBlockName_1, 2);
            condensateRiserNames.Add(ThWSSCommon.CondensateRiserBlockName_2, 2);
            condensateRiserNames.Add("冷凝", 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.condensateRiser, condensateRiserNames));

            //水管井留洞 - 块名称过滤
            Dictionary<string, int> waterTWellNames = new Dictionary<string, int>();
            waterTWellNames.Add(ThWSSCommon.WaterPipeWellBlockName, 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.waterTubeWell, waterTWellNames));

            //烟道留洞 - 块名称过滤
            Dictionary<string, int> chimneyNames = new Dictionary<string, int>();
            chimneyNames.Add(ThWSSCommon.FlueShaftBlockName, 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.flueWell, chimneyNames));


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

            /* 楼梯块目前有问题，暂时不获取
            //获取楼梯块
            Dictionary<string, int> stairsNames = new Dictionary<string, int>();
            stairsNames.Add("楼梯,DB", 1);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.stairs, stairsNames));
            */
        }

    }
}
