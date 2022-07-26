using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using System;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHDoor : ThTCHElement, ICloneable
    {
        private ThTCHDoor(){ }
        private double Angle { get; set; }
        /// <summary>
        /// 矩形门的构造
        /// </summary>
        /// <param name="centerPoint">轮廓底部中心点</param>
        /// <param name="length">长度（门宽）</param>
        /// <param name="height">高度（门高）</param>
        /// <param name="width">宽度（门厚）</param>
        /// <param name="angle">旋转角度（计算X轴方向）</param>
        public ThTCHDoor(Point3d centerPoint,double length,double height,double width,double angle) 
        {
            ExtrudedDirection = Vector3d.ZAxis;
            Origin = centerPoint;
            XVector = Vector3d.XAxis.RotateBy(angle, Vector3d.ZAxis);
            Length = length;
            Height = height;
            Width = width;
            Angle = angle;
        }
        public object Clone()
        {
            if (this == null)
                return null;
            var door = new ThTCHDoor(this.Origin, this.Length, this.Height, this.Width, this.Angle);
            door.XVector = this.XVector;
            door.Uuid = this.Uuid;
            if (this.Outline != null) 
            {
                door.Outline = this.Outline.Clone() as Entity;
            }
            return door;
        }
    }
}
