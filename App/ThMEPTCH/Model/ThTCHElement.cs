using System;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.Model
{
    public class ThTCHElement
    {
        public string Name { get; set; }
        public string Usage { get; set; }
        public string Uuid { get; set; }
        #region 几何信息
        public Entity Outline { get; set; }
        public Point3d Origin { get; set; }
        //X轴方向和宽度方向一致
        public Vector3d XVector { get; set; }
        /// <summary>
        /// 宽度(厚度)（Y轴方向长度）
        /// </summary>
        public double Width { get; set; }
        /// <summary>
        /// 长度(X轴方向)
        /// </summary>
        public double Length { get; set; }
        /// <summary>
        /// 拉伸方向
        /// </summary>
        public Vector3d ExtrudedDirection { get; set; }
        /// <summary>
        /// 拉伸方向长度
        /// </summary>
        public double Height { get; set; }
        /// <summary>
        /// 拉伸方向偏移值
        /// </summary>
        public double ZOffSet { get; set; }
        /// <summary>
        /// 材质
        /// </summary>
        public string EnumMaterial { get; set; }

        #endregion
        #region 传object数据有问题，后面需要处理
        public Dictionary<string, string> Properties { get; set; }
        #endregion
        public ThTCHElement()
        {
            Uuid = Guid.NewGuid().ToString();
            Properties = new Dictionary<string, string>();
        }
    }
}
