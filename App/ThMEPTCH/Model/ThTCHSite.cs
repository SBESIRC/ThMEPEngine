using ProtoBuf;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHSite : ThIfcSite
    {
        [ProtoMember(1)]
        public ThTCHBuilding Building { get; set; }
    }
}
