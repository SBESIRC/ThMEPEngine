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
    class EquipmentDataEngine
    {
        public List<EquipmentBlcokVisitorModel> equipmentBlcokVisitors { get; }
        /// <summary>
        /// 获取本图纸中所有的设备信息
        /// </summary>
        public EquipmentDataEngine() 
        {
            equipmentBlcokVisitors = new List<EquipmentBlcokVisitorModel>();
            InitBlockNames();
            ///一次遍历获取数据，防止后面在多次遍历数据
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var eqBlocks = acdb.ModelSpace.OfType<BlockReference>();
                var test = acdb.ModelSpace.OfType<Entity>();
                foreach (var item in test) 
                {
                    continue;
                    if (item.GetType().Name.StartsWith("Imp")) 
                    {
                        Type type = item.GetType();
                        var tType = item.AcadObject.GetType();
                        var block = acdb.Blocks.Where(c => c.Id == item.OwnerId);
                        var t = item.GetType().Assembly;
                        var testT= t.GetTypes().Where(c => item.GetType().IsAssignableFrom(c)).Where(c => c.IsClass);
                        string typeN = item.LayerNameTypedValue().ToString();

                        PropertyDescriptor pd = TypeDescriptor.GetProperties(item.AcadObject)["ObjectName"];
                        var dict = new Dictionary<string, object>();
                        foreach (PropertyDescriptor pi in TypeDescriptor.GetProperties(item.AcadObject))
                        {
                            var proT = pi.PropertyType;
                            if (proT == typeof(string) || proT == typeof(double) || proT == typeof(float) || proT == typeof(int) || proT == typeof(long)
                                || proT == typeof(Point2d) || proT == typeof(Point3d))
                            {
                                try
                                {
                                    var v = pi.GetValue(item.AcadObject);
                                    dict[pi.Name] = v;
                                    var str = pd.GetValue(item.AcadObject) as string;
                                }
                                catch
                                {

                                }
                                
                            }
                        }
                        
                        var mt = type.GetMethod("get_BlockName");
                        //object obj = Activator.CreateInstance(type,item.UnmanagedObject,false);
                        //var res = mt.Invoke(obj, null);
                        
                        var explodeResult = new DBObjectCollection();
                        item.Explode(explodeResult);
                    }
                    if (item.GetType().Name.StartsWith("ProxyEntity")) 
                    {
                        //其它插件生成的元素，该电脑没有安装相应的插件，无法显示
                        var explodeResult = new DBObjectCollection();
                        item.Explode(explodeResult);
                    }
                }
            }


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
                    var pointP = new Point3d(block.Position.X, block.Position.Y, 0);
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
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.gravityRainBucket, gravityRainBucketNames));

            //侧入式雨水斗 - 块名称过滤
            Dictionary<string, int> sideRainBucketNames = new Dictionary<string, int>();
            sideRainBucketNames.Add(ThWSSCommon.SideRainBucketBlockName_1, 2);
            sideRainBucketNames.Add(ThWSSCommon.SideRainBucketBlockName_2, 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel( EnumEquipmentType.sideRainBucket, sideRainBucketNames));

            //阳台立管 - 块名称过滤
            Dictionary<string, int> balconyRiserNames = new Dictionary<string, int>();
            balconyRiserNames.Add(ThWSSCommon.BalconyRiserBlockName, 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.balconyRiser, balconyRiserNames));

            //屋面雨水斗 - 块名称过滤
            Dictionary<string, int> roofRiserNames = new Dictionary<string, int>();
            roofRiserNames.Add(ThWSSCommon.RoofRainwaterRiserBlockName, 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.roofRainRiser, roofRiserNames));

            //冷凝水立管 - 块名称过滤
            Dictionary<string, int> condensateRiserNames = new Dictionary<string, int>();
            condensateRiserNames.Add(ThWSSCommon.CondensateRiserBlockName_1, 2);
            condensateRiserNames.Add(ThWSSCommon.CondensateRiserBlockName_2, 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.condensateRiser, condensateRiserNames));

            //水管井留洞 - 块名称过滤
            Dictionary<string, int> waterTWellNames = new Dictionary<string, int>();
            waterTWellNames.Add(ThWSSCommon.WaterPipeWellBlockName, 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.waterTubeWell, waterTWellNames));

            //烟道留洞 - 块名称过滤
            Dictionary<string, int> chimneyNames = new Dictionary<string, int>();
            chimneyNames.Add(ThWSSCommon.FlueShaftBlockName, 2);
            this.equipmentBlcokVisitors.Add(new EquipmentBlcokVisitorModel(EnumEquipmentType.flueWell, chimneyNames));
        }

    }
}
