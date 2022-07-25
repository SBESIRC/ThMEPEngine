using ProtoBuf;
using System.Collections.Generic;

namespace ThMEPTCH.Model
{
    [ProtoContract]
    public class ThTCHBuilding : ThTCHElement
    {
        [ProtoMember(21)]
        public string BuildingName { get; set; }
        [ProtoMember(22)]
        public List<ThTCHBuildingStorey> Storeys { get; set; }
        public ThTCHBuilding() 
        {
            Storeys = new List<ThTCHBuildingStorey>();
        }
       
    }
}
