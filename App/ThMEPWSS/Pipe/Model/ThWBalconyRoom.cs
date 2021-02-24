using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;
using System.Collections.Generic;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWBalconyRoom : ThWRoom
    { 
        /// <summary>
        /// 生活阳台
        /// </summary>
        public ThIfcSpace Balcony { get; set; }
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
        public List<ThIfcWashMachine> Washmachines { get; set; }
        /// <summary>
        /// 阳台台盆
        /// </summary>
        public List<ThWBasin> BasinTools { get; set; }
        public ThWBalconyRoom()
        {
            Balcony = null;
            FloorDrains = new List<ThWFloorDrain>();
            RainPipes = new List<ThWRainPipe>();
            Washmachines = new List<ThIfcWashMachine>();
            BasinTools = new List<ThWBasin>();
        }
    }
}
