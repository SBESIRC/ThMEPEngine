using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHWindow : ThIfcWindow
    {
        [ProtoMember(1)]
        public Point3d CenterPoint { get; set; }
        [ProtoMember(2)]
        public double Width { get; set; }
        [ProtoMember(3)]
        public double Thickness { get; set; }
        [ProtoMember(4)]
        public Vector3d XVector { get; set; }
        //X轴方向和宽度方向一致
        [ProtoMember(5)]
        public Vector3d ExtrudedDirection { get; }
        [ProtoMember(6)]
        public string OpenDirection { get; set; }

        private ThTCHWindow()
        {

        }
        public ThTCHWindow(Point3d centerPoint, double width, double height, double thickness, double angle)
        {
            OpenDirection = "(0,1)";
            ExtrudedDirection = Vector3d.ZAxis;
            CenterPoint = centerPoint;
            XVector = Vector3d.XAxis.RotateBy(angle, Vector3d.ZAxis);
            Width = width;
            Height = height;
            Thickness = thickness;
        }
    }
}
