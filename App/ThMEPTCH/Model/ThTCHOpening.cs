using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHOpening : ThTCHElement, ICloneable
    {
        private ThTCHOpening() { }
        private double Angle { get; }
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
