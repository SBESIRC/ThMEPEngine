using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWKitchenRoom : ThIfcRoom
    {
        /// <summary>
        /// 台盆
        /// </summary>
        public List<ThWBasin> BasinTools { get; set; }
        /// <summary>
        /// 排油烟管
        /// </summary>
        public List<ThIfcRoom> Pypes { get; set; }
        /// <summary>
        /// 排水管井
        /// </summary>
        public List<ThIfcRoom> DrainageWells { get; set; }
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
            Pypes = new List<ThIfcRoom>();
            BasinTools = new List<ThWBasin>();
            DrainageWells = new List<ThIfcRoom>();
            RainPipes = new List<ThWRainPipe>();
            RoofRainPipes = new List<ThWRoofRainPipe>();
            CondensePipes = new List<ThWCondensePipe>();
            FloorDrains = new List<ThWFloorDrain>();
        }
    }
}
