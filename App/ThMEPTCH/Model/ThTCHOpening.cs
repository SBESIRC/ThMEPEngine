using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPTCH.Model
{
    public class ThTCHOpening : ThTCHElement, ICloneable
    {
        /// <summary>
        /// 是否忽略尺寸标注
        /// </summary>
        public bool ShowDimension { get; set; }

        /// <summary>
        /// 是否遮挡元素
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// 底高
        /// </summary>
        public double BottomElevation { get; set; }

        /// <summary>
        /// 编号前缀
        /// </summary>
        public string NumberPrefix { get; set; }

        /// <summary>
        /// 编号后缀
        /// </summary>
        public string NumberPostfix { get; set; }

        /// <summary>
        /// 立面显示
        /// </summary>
        public bool ElevationDisplay { get; set; }

        private double Angle { get; }

        public ThTCHOpening()
        {
        }

        /// <summary>
        /// 洞口的信息
        /// </summary>
        /// <param name="centerPoint">底部矩形轮廓的中心点</param>
        /// <param name="width">长度</param>
        /// <param name="height">高度</param>
        /// <param name="width">宽度(厚度)</param>
        /// <param name="angle">角度（计算X轴方向的）</param>
        public ThTCHOpening(Point3d centerPoint, double length, double height, double width, double angle)
        {
            ExtrudedDirection = Vector3d.ZAxis;
            Origin = centerPoint;
            XVector = Vector3d.XAxis.RotateBy(angle, Vector3d.ZAxis);
            Width = width;
            Height = height;
            Length = length;
            Angle = angle;
        }

        public object Clone()
        {
            if (this == null)
                return null;
            var opening = new ThTCHOpening(this.Origin, this.Length, this.Height, this.Width, this.Angle);
            opening.XVector = this.XVector;
            opening.Uuid = this.Uuid;
            if (this.Outline != null)
            {
                opening.Outline = this.Outline.Clone() as Entity;
            }
            return opening;
        }
    }
}
