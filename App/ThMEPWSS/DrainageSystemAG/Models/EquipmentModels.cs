﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
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
        /// 1包含，2等于
        /// </summary>
        public Dictionary<string, int> blockNames { get; }
        public EnumEquipmentType enumEquipmentType { get; }
        public EquipmentDataEnginVisitor equipmentDataVisitor { get; }
        public EquipmentBlcokVisitorModel(EnumEquipmentType type, Dictionary<string, int> bNames = null)
        {
            this.blockNames = new Dictionary<string, int>();
            this.enumEquipmentType = type;
            this.equipmentDataVisitor = new EquipmentDataEnginVisitor(bNames);
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
    }
    public class EquipmentBlcokModel
    {
        public EnumEquipmentType enumEquipmentType { get; }
        public List<BlockReference> blockReferences { get; }
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
        public Point3d blockCenterPoint { get; }
        public BlockReference equmBlockReference { get; }
        public EnumRoomType enumRoomType { get; set; }
        public string roomSpaceId { get; set; }
        public string uid { get; }
        public EquipmentBlockSpace(BlockReference block,EnumEquipmentType equipmentType,string spaceId ="") 
        {
            this.equmBlockReference = block;
            this.enumEquipmentType = equipmentType;
            this.blockPosition = new Point3d(block.Position.X, block.Position.Y, 0);
            this.blockCenterPoint = block.GeometricExtents.ToNTSPolygon().EnvelopeInternal.Centre.ToAcGePoint3d();
            this.roomSpaceId = spaceId;
            this.enumRoomType = EnumRoomType.Other;
            this.uid = Guid.NewGuid().ToString();
        }
    }

    public enum EnumEquipmentType 
    {
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
    public enum EnumCreateEquipment 
    {
        /// <summary>
        /// 地漏
        /// </summary>
        [Description("地漏")]
        floorDrain = 0,
        /// <summary>
        /// 立管
        /// </summary>
        [Description("立管")]
        riser = 1,
    }
}
