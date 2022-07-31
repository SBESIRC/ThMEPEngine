using ProtoBuf;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHSite : ThTCHElement
    {
        [ProtoMember(21)]
        public ThTCHBuilding Building { get; set; }
    }
}
