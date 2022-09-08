using Autodesk.AutoCAD.Geometry;
using ProtoBuf;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHVerticalPipe
    {
        [ProtoMember(1)]
        public Point3d PipeBottomPoint;
        [ProtoMember(2)]
        public Point3d PipeTopPoint;
        [ProtoMember(3)]
        public string PipeSystem;
        [ProtoMember(4)]
        public string PipeMaterial;
        [ProtoMember(5)]
        public string DnType;
        [ProtoMember(6)]
        public string ShortCode;
        [ProtoMember(7)]
        public double PipeDN;
        [ProtoMember(8)]
        public string FloorNum;
        [ProtoMember(9)]
        public string PipeNum;
        [ProtoMember(10)]
        public Point3d TurnPoint;
        [ProtoMember(11)]
        public Vector3d TextDirection;
        [ProtoMember(12)]
        public string PipeDimText;
        [ProtoMember(13)]
        public string DimTypeText;
        [ProtoMember(14)]
        public double TextHeight;
        [ProtoMember(15)]
        public string TextStyle;
        [ProtoMember(16)]
        public int DimType;
        [ProtoMember(17)]
        public double DocScale;
        [ProtoMember(18)]
        public double DimRadius;
        [ProtoMember(19)]
        public double Spacing;
        [ProtoMember(20)]
        public int FloorType;
        [ProtoMember(21)]
        public double LayoutRotation;
    }
}
