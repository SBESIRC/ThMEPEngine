using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWKitchenRoom : ThWRoom
    {
        /// <summary>
        /// 台盆
        /// </summary>
        public List<ThWBasin> BasinTools { get; set; }
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
        public List<ThWRainPipe> RainPipes { get; set; }
        /// <summary>
        /// 相邻屋顶雨水管
        /// </summary>
        public List<ThWRoofRainPipe> RoofRainPipes { get; set; }
        /// <summary>
        /// 相邻冷凝管
        /// </summary>
        public List<ThWCondensePipe> CondensePipes { get; set; }
        public List<ThWFloorDrain> FloorDrains { get; set; }
        public ThWKitchenRoom()
        {
            Pypes = new List<ThIfcSpace>();
            BasinTools = new List<ThWBasin>();
            DrainageWells = new List<ThIfcSpace>();
            RainPipes = new List<ThWRainPipe>();
            RoofRainPipes = new List<ThWRoofRainPipe>();
            CondensePipes = new List<ThWCondensePipe>();
            FloorDrains = new List<ThWFloorDrain>();
        }
    }
}
