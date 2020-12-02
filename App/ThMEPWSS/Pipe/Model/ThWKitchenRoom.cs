using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWKitchenRoom : ThWRoom
    {
        /// <summary>
        /// 厨房空间
        /// </summary>
        public ThIfcSpace Kitchen { get; set; }
        //台盆
        public List<ThIfcBasin> BasinTools { get; set; }
        //排油烟管
        public List<ThIfcSpace> Pypes { get; set; }
        /// <summary>
        /// 排水管井
        /// </summary>
        public List<ThIfcSpace> DrainageWells { get; set; }

        public ThWKitchenRoom()
        {
            Kitchen = null;
            BasinTools = new List<ThIfcBasin>();
            Pypes = new List<ThIfcSpace>();
            DrainageWells = new List<ThIfcSpace>();
        }
    }
}
