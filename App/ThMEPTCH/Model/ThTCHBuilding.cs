using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    public class ThTCHBuilding : ThIfcBuilding
    {
        public List<ThTCHBuildingStorey> Storeys { get; set; }
    }
}
