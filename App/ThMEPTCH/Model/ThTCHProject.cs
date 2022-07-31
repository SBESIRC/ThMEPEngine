using ProtoBuf;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHProject : ThTCHElement
    {
        [ProtoMember(21)]
        public string ProjectName { get; set; }
        [ProtoMember(22)]
        public ThTCHSite Site { get; set; }
    }
}
