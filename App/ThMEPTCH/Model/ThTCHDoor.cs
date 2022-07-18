using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    public class ThTCHDoor : ThIfcDoor, ICloneable
    {
        public Point3d CenterPoint { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public Vector3d ExtrudedDirection { get; }
        //X轴方向和宽度方向一致
        public Vector3d XVector { get; set; }
        public string OpenDirection { get; set; }
        private double Angle { get; set; }
        public ThTCHDoor(Point3d centerPoint,double width,double height,double thickness,double angle) 
        {
            OpenDirection = "(0,1)";
            ExtrudedDirection = Vector3d.ZAxis;
            CenterPoint = centerPoint;
            XVector = Vector3d.XAxis.RotateBy(angle, Vector3d.ZAxis);
            Width = width;
            Height = height;
            Thickness = thickness;
            Angle = angle;
        }

        public object Clone()
        {
            if (this == null)
                return null;
            var door = new ThTCHDoor(this.CenterPoint, this.Width, this.Height, this.Thickness, this.Angle);
            door.OpenDirection = this.OpenDirection;
            door.XVector = this.XVector;
            if (this.Outline != null) 
            {
                door.Outline = this.Outline.Clone() as Entity;
            }
            return door;
        }
    }
}
