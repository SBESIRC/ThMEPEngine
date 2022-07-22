using ProtoBuf;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHSite : ThTCHElement
    {
        [ProtoMember(11)]
        public ThTCHBuilding Building { get; set; }
    }
}
