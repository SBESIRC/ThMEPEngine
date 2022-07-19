using ProtoBuf;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHProject : ThIfcProject
    {
        [ProtoMember(1)]
        public string ProjectName { get; set; }
        [ProtoMember(2)]
        public ThTCHSite Site { get; set; }
    }
}
