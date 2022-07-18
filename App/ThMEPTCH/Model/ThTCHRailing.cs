using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    /// <summary>
    /// 栏杆
    /// </summary>
    [ProtoContract]
    public class ThTCHRailing : ThIfcRailing
    {
        [ProtoMember(1)]
        public double Depth { get; set; }
        [ProtoMember(2)]
        public double Thickness { get; set; }
        [ProtoMember(3)]
        public Vector3d ExtrudedDirection { get; set; }
    }
}
