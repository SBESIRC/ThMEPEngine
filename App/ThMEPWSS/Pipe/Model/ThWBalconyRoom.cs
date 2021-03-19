using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Model
{
    /// <summary>
    /// 阳台
    /// </summary>
    public class ThWBalconyRoom : ThIfcRoom
    { 
        /// <summary>
        /// 地漏
        /// </summary>
        public List<ThWFloorDrain> FloorDrains { get; set; }
        /// <summary>
        /// 雨水立管
        /// </summary>
        public List<ThWRainPipe> RainPipes { get; set; }
        /// <summary>
        /// 洗衣机
        /// </summary>
        public List<ThWWashingMachine> Washmachines { get; set; }
        /// <summary>
        /// 阳台台盆
        /// </summary>
        public List<ThWBasin> BasinTools { get; set; }
        public ThWBalconyRoom()
        {
            FloorDrains = new List<ThWFloorDrain>();
            RainPipes = new List<ThWRainPipe>();
            Washmachines = new List<ThWWashingMachine>();
            BasinTools = new List<ThWBasin>();
        }
    }
}
