using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    public class ThTCHWindow : ThIfcWindow, ICloneable
    {
        public Point3d CenterPoint { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public Vector3d XVector { get; set; }
        //X轴方向和宽度方向一致
        public Vector3d ExtrudedDirection { get; }
        public string OpenDirection { get; set; }
        private double Angle { get; set; }
        public ThTCHWindow(Point3d centerPoint, double width, double height, double thickness, double angle)
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
            var window = new ThTCHWindow(this.CenterPoint, this.Width, this.Height, this.Thickness, this.Angle);
            window.XVector = this.XVector;
            if (this.Outline != null)
            {
                window.Outline = this.Outline.Clone() as Entity;
            }
            return window;
        }
    }
}
