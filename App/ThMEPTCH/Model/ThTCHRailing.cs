using Autodesk.AutoCAD.Geometry;
using ProtoBuf;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    /// <summary>
    /// 栏杆
    /// </summary>
    [ProtoContract]
    public class ThTCHRailing : ThTCHElement
    {
        [ProtoMember(11)]
        public double Depth { get; set; }
        [ProtoMember(12)]
        public double Thickness { get; set; }
        [ProtoMember(13)]
        public Vector3d ExtrudedDirection { get; set; }
    }
}
