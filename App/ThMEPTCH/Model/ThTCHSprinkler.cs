using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHSprinkler : ThIfcSprinkler
    {
        [ProtoMember(1)]
        public Point3d Location { get; set; }
        [ProtoMember(2)]
        public int Type { get; set; }
        [ProtoMember(3)]
        public int LinkMode { get; set; }
        [ProtoMember(4)]
        public string System { get; set; }
        [ProtoMember(5)]
        public double Radius { get; set; }
        [ProtoMember(6)]
        public double PipeLength { get; set; }
        [ProtoMember(7)]
        public double PipeDn { get; set; }
        [ProtoMember(8)]
        public int K { get; set; }
        [ProtoMember(9)]
        public double Angle { get; set; }
        [ProtoMember(10)]
        public double SizeX { get; set; }
        [ProtoMember(11)]
        public double SizeY { get; set; }
        [ProtoMember(12)]
        public int HidePipe { get; set; }
        [ProtoMember(13)]
        public int MirrorByX { get; set; }
        [ProtoMember(14)]
        public int MirrorByY { get; set; }
        [ProtoMember(15)]
        public double DocScale { get; set; }
    }
}
