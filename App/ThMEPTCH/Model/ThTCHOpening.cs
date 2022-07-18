﻿using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHOpening : ThIfcOpeningElement
    {
        [ProtoMember(1)]
        public Point3d CenterPoint { get; set; }
        [ProtoMember(2)]
        public double Width { get; set; }
        [ProtoMember(3)]
        public double Height { get; set; }
        [ProtoMember(4)]
        public double Thickness { get; set; }
        [ProtoMember(5)]
        public Vector3d ExtrudedDirection { get; }
        //X轴方向和宽度方向一致
        [ProtoMember(6)]
        public Vector3d XVector { get; set; }

        private ThTCHOpening()
        {

        }
        public ThTCHOpening(Point3d centerPoint, double width, double height, double thickness, double angle)
        {
            ExtrudedDirection = Vector3d.ZAxis;
            CenterPoint = centerPoint;
            XVector = Vector3d.XAxis.RotateBy(angle, Vector3d.ZAxis);
            Width = width;
            Height = height;
            Thickness = thickness;
        }
    }
}
