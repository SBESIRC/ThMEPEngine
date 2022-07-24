using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ThMEPWSS.DrainageSystemAG.DataEngineVisitor;
using ThMEPWSS.Model;

namespace ThMEPWSS.DrainageSystemAG.Models
{
    /// <summary>
    /// 排水设备块对应的信息
    /// </summary>
    public class EquipmentBlcokVisitorModel
    {
        /// <summary>
        /// 块名称包含或=字符串的
        /// 1包含，2等于,3 startWith,4 Endwith;
        /// </summary>
        public Dictionary<string, int> blockNames { get; }
        public EnumEquipmentType enumEquipmentType { get; }
        public BlockReferenceDataEnginVisitor equipmentDataVisitor { get; }
        public bool isLayerName { get; }
        public EquipmentBlcokVisitorModel(EnumEquipmentType type, Dictionary<string, int> bNames = null,bool islayerName=false,bool isModelSpace=false)
        {
            this.blockNames = new Dictionary<string, int>();
            this.enumEquipmentType = type;
            this.isLayerName = isLayerName;
            this.equipmentDataVisitor = new BlockReferenceDataEnginVisitor(bNames,islayerName, isModelSpace)
            {
                LayerFilter = new HashSet<string>(CurveXrefLayers()),
            };

            if (null != bNames && bNames.Count > 0)
            {
                foreach (var name in bNames)
                {
                    if (string.IsNullOrEmpty(name.Key))
                        continue;
                    this.blockNames.Add(name.Key, name.Value);
                }
            }
        }
        private  List<string> CurveXrefLayers()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                //return acadDatabase.Layers
                //    .Where(o => IsVisibleLayer(o))
                //    .Select(o => ThMEPXRefService.OriginalFromXref(o.Name))
                //    .ToList();
                return acadDatabase.Layers
                    .Where(o => IsVisibleLayer(o))
                    .Select(o => o.Name)
                    .ToList();
            }
        }

        private bool IsVisibleLayer(LayerTableRecord layerTableRecord)
        {
            return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
        }
    }
    public class EquipmentBlcokModel
    {
        public EnumEquipmentType enumEquipmentType { get; }
        public List<BlockReference> blockReferences { get; set; }
        public EquipmentBlcokModel(EnumEquipmentType type, List<BlockReference> blockReferences = null)
        {
            this.enumEquipmentType = type;
            this.blockReferences = new List<BlockReference>();
            if (null != blockReferences && blockReferences.Count > 0)
            {
                foreach (var item in blockReferences)
                    this.blockReferences.Add(item);
            }
        }
    }
    class EquipmentBlockSpace
    {
        public EnumEquipmentType enumEquipmentType { get; }
        public Point3d blockPosition { get; }
        public Point3d blockCenterPoint { get; set; }
        public BlockReference equmBlockReference { get; }
        public EnumRoomType enumRoomType { get; set; }
        public string roomSpaceId { get; set; }
        public string uid { get; }
        public EquipmentBlockSpace(BlockReference block,EnumEquipmentType equipmentType,string spaceId ="") 
        {
            this.equmBlockReference = block;
            this.enumEquipmentType = equipmentType;
            //var centerPoint = block.GeometricExtents.ToNTSPolygon().EnvelopeInternal.Centre.ToAcGePoint3d();
            var centerPoint = DrainSysAGCommon.GetBlockGeometricCenter(block);
            this.blockPosition = centerPoint; //new Point3d(block.Position.X, block.Position.Y, 0);
            this.blockCenterPoint = centerPoint;
            this.roomSpaceId = spaceId;
            this.enumRoomType = EnumRoomType.Other;
            this.uid = Guid.NewGuid().ToString();
        }
    }

    public enum EnumEquipmentType 
    {
        /// <summary>
        /// 其它
        /// </summary>
        [Description("其它")]
        other =-999,
        /// <summary>
        /// 立管
        /// </summary>
        [Description("立管")]
        riser = -998,
        /// <summary>
        /// 设备
        /// </summary>
        [Description("设备")]
        equipment = -997,
        /// <summary>
        /// 建筑标高
        /// </summary>
        [Description("建筑标高")]
        buildingElevation = -996,
        /// <summary>
        /// 空调外机
        /// </summary>
        [Description("空调外机")]
        airConditioningOutMachine =-995,
        /// <summary>
        /// 门
        /// </summary>
        [Description("门")]
        door =-994,
        /// <summary>
        /// 楼梯
        /// </summary>
        [Description("楼梯")]
        stairs = -993,
        /// <summary>
        /// 地漏
        /// </summary>
        [Description("地漏")]
        floorDrain = 0,
        /// <summary>
        /// 重力雨水斗
        /// </summary>
        [Description("重力雨水斗")]
        gravityRainBucket = 2,
        /// <summary>
        /// 侧入式雨水斗
        /// </summary>
        [Description("侧入式雨水斗")]
        sideRainBucket =3,
        /// <summary>
        /// 冷凝水立管
        /// </summary>
        [Description("冷凝水立管")]
        condensateRiser = 4,
        /// <summary>
        /// 阳台立管
        /// </summary>
        [Description("阳台立管")]
        balconyRiser=5,
        /// <summary>
        /// 屋面雨水立管
        /// </summary>
        [Description("屋面雨水立管")]
        roofRainRiser =6,
        /// <summary>
        /// 洗衣机
        /// </summary>
        [Description("洗衣机")]
        washingMachine =7,
        /// <summary>
        /// 厨房台盆
        /// </summary>
        [Description("厨房台盆")]
        kitchenBasin =8,
        /// <summary>
        /// 拖把池
        /// </summary>
        [Description("拖把池")]
        mopPool =9,
        /// <summary>
        /// 通气立管
        /// </summary>
        [Description("通气立管")]
        ventRiser = 10,
        /// <summary>
        /// 废水立管
        /// </summary>
        [Description("废水立管")]
        wastewaterRiser =11,
        /// <summary>
        /// 沉箱立管
        /// </summary>
        [Description("沉箱立管")]
        caissonRiser =12,
        /// <summary>
        /// 污废合流立管
        /// </summary>
        [Description("污废合流立管")]
        sewageWasteRiser =13,
        /// <summary>
        /// 马桶
        /// </summary>
        [Description("马桶")]
        toilet =14,
        /// <summary>
        /// 单盆洗手台
        /// </summary>
        [Description("单盆洗手台")]
        singleBasinWashingTable = 15,
        /// <summary>
        /// 双盆洗手台
        /// </summary>
        [Description("双盆洗手台")]
        doubleBasinWashingTable = 16,
        /// <summary>
        /// 污水立管
        /// </summary>
        [Description("污水立管")]
        sewageWaterRiser = 17,
        /// <summary>
        /// 水管井
        /// </summary>
        [Description("水管井")]
        waterTubeWell=100,
        /// <summary>
        /// 烟道井
        /// </summary>
        [Description("烟道井")]
        flueWell = 101,
    }
}
