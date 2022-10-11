using System.Collections.Generic;

namespace ThMEPTCH.Model
{
    public class ThTCHBuilding : ThTCHElement
    {
        public string BuildingName { get; set; }
        public List<ThTCHBuildingStorey> Storeys { get; set; }
        public ThTCHBuilding() 
        {
            Storeys = new List<ThTCHBuildingStorey>();
        }
    }
}
