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
        /// <summary>
        /// 台盆
        /// </summary>
        public List<ThIfcBasin> BasinTools { get; set; }
        /// <summary>
        /// 排油烟管
        /// </summary>
        public List<ThIfcSpace> Pypes { get; set; }
        /// <summary>
        /// 排水管井
        /// </summary>
        public List<ThIfcSpace> DrainageWells { get; set; }
        /// <summary>
        /// 相邻雨水管
        /// </summary>
        public List<ThIfcRainPipe> RainPipes { get; set; }
        /// <summary>
        /// 相邻屋顶雨水管
        /// </summary>
        public List<ThIfcRoofRainPipe> RoofRainPipes { get; set; }
        public ThWKitchenRoom()
        {
            Pypes = new List<ThIfcSpace>();
            BasinTools = new List<ThIfcBasin>();
            DrainageWells = new List<ThIfcSpace>();
            RainPipes = new List<ThIfcRainPipe>();
            RoofRainPipes = new List<ThIfcRoofRainPipe>();
        }
    }
}
