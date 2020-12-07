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
        public List<ThIfcFloorDrain> FloorDrains { get; set; }
        /// <summary>
        /// 雨水立管
        /// </summary>
        public List<ThIfcRainPipe> RainPipes { get; set; }
        /// <summary>
        /// 洗衣机
        /// </summary>
        public List<ThIfcWashMachine> Washmachines { get; set; }
        /// <summary>
        /// 阳台台盆
        /// </summary>
        public List<ThIfcBasin> BasinTools { get; set; }
        public ThWBalconyRoom()
        {
            Balcony = null;
            FloorDrains = new List<ThIfcFloorDrain>();
            RainPipes = new List<ThIfcRainPipe>();
            Washmachines = new List<ThIfcWashMachine>();
            BasinTools = new List<ThIfcBasin>();
        }
    }
}
