using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    public class ThTCHBuilding : ThIfcBuilding
    {
        public string BuildingName { get; set; }
        public List<ThTCHBuildingStorey> Storeys { get; set; }
        public ThTCHBuilding() 
        {
            Storeys = new List<ThTCHBuildingStorey>();
        }
       
    }
}
