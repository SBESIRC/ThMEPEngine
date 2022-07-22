using ProtoBuf;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHProject : ThTCHElement
    {
        [ProtoMember(11)]
        public string ProjectName { get; set; }
        [ProtoMember(12)]
        public ThTCHSite Site { get; set; }
    }
}
