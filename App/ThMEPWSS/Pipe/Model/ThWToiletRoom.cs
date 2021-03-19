using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWToiletRoom : ThIfcRoom
    {
        /// <summary>
        /// 排水管井
        /// </summary>
        public List<ThIfcSpace> DrainageWells { get; set; }
        /// <summary>
        /// 坐便器
        /// </summary>
        public List<ThWClosestool> Closestools { get; set; }
        /// <summary>
        /// 地漏
        /// </summary>
        public List<ThWFloorDrain> FloorDrains { get; set; }
        /// <summary>
        /// 冷凝管
        /// </summary>
        public List<ThWCondensePipe> CondensePipes { get; set; }
        public List<ThWRoofRainPipe> RoofRainPipes { get; set; }
        public ThWToiletRoom()
        {
            Closestools = new List<ThWClosestool>();
            DrainageWells = new List<ThIfcSpace>();
            FloorDrains = new List<ThWFloorDrain>();
            CondensePipes = new List<ThWCondensePipe>();
            RoofRainPipes = new List<ThWRoofRainPipe>();
        }
    }
}
