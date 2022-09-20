using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHWindow : ThTCHElement, ICloneable
    {
        private double Angle { get; }
        [ProtoMember(21)]
        public uint WindowType { get; set; }

        private ThTCHWindow() { }
        /// <summary>
        /// 窗的几何信息构造
        /// </summary>
        /// <param name="centerPoint">底部中心点</param>
        /// <param name="length">窗的宽度</param>
        /// <param name="height">窗的高度</param>
        /// <param name="width">窗的厚度</param>
        /// <param name="angle">窗的角度</param>
        public ThTCHWindow(Point3d centerPoint, double length, double height, double width, double angle, uint windowType)
        {
            ExtrudedDirection = Vector3d.ZAxis;
            Origin = centerPoint;
            XVector = Vector3d.XAxis.RotateBy(angle, Vector3d.ZAxis);
            Width = width;
            Height = height;
            Length = length;
            Angle = angle;
            WindowType = windowType;
        }
        public object Clone()
        {
            if (this == null)
                return null;
            var window = new ThTCHWindow(this.Origin, this.Length, this.Height, this.Width, this.Angle, this.WindowType);
            window.XVector = this.XVector;
            window.Uuid = this.Uuid;
            if (this.Outline != null)
            {
                window.Outline = this.Outline.Clone() as Entity;
            }
            return window;
        }
    }
}
