using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System;
using System.Collections.Generic;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    [ProtoInclude(100, typeof(ThTCHProject))]
    [ProtoInclude(101, typeof(ThTCHSite))]
    [ProtoInclude(102, typeof(ThTCHBuilding))]
    [ProtoInclude(103, typeof(ThTCHBuildingStorey))]
    [ProtoInclude(104, typeof(ThTCHWall))]
    [ProtoInclude(105, typeof(ThTCHDoor))]
    [ProtoInclude(106, typeof(ThTCHSlab))]
    [ProtoInclude(107, typeof(ThTCHWindow))]
    [ProtoInclude(108, typeof(ThTCHRailing))]
    [ProtoInclude(109, typeof(ThTCHOpening))]
    [ProtoInclude(110, typeof(ThTCHColumn))]
    [ProtoInclude(111, typeof(ThTCHBeam))]
    public class ThTCHElement
    {
        /*这里预留20个序列数据，外部序列数字从21开始*/
        [ProtoMember(1)]
        public string Name { get; set; }
        public string Usage { get; set; }
        [ProtoMember(2)]
        public string Uuid { get; set; }
        #region 几何信息
        [ProtoMember(3)]
        public Entity Outline { get; set; }
        [ProtoMember(4)]
        public Point3d Origin { get; set; }
        //X轴方向和宽度方向一致
        [ProtoMember(5)]
        public Vector3d XVector { get; set; }
        /// <summary>
        /// 宽度(厚度)（Y轴方向长度）
        /// </summary>
        [ProtoMember(6)]
        public double Width { get; set; }
        /// <summary>
        /// 长度(X轴方向)
        /// </summary>
        [ProtoMember(7)]
        public double Length { get; set; }
        /// <summary>
        /// 拉伸方向
        /// </summary>
        [ProtoMember(8)]
        public Vector3d ExtrudedDirection { get; set; }
        /// <summary>
        /// 拉伸方向长度
        /// </summary>
        [ProtoMember(9)]
        public double Height { get; set; }
        /// <summary>
        /// 拉伸方向偏移值
        /// </summary>
        [ProtoMember(10)]
        public double ZOffSet { get; set; }
        /// <summary>
        /// 材质
        /// </summary>
        [ProtoMember(11)]
        public string EnumMaterial { get; set; }

        #endregion
        #region 传object数据有问题，后面需要处理
        [ProtoMember(19)]
        public Dictionary<string, string> Properties { get; set; }
        #endregion
        public ThTCHElement()
        {
            Uuid = Guid.NewGuid().ToString();
            Properties = new Dictionary<string, string>();
        }
    }
}
