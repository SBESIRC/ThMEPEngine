using ThMEPEngineCore.Model;
using System.Collections.Generic;

namespace ThMEPWSS.Pipe.Model
{
    /// <summary>
    /// 设备平台
    /// </summary>
    public class ThWDevicePlatformRoom : ThWRoom
    {
        /// <summary>
        /// 地漏
        /// </summary>
        public List<ThWFloorDrain> FloorDrains { get; set; }
        /// <summary>
        /// 冷凝水立管
        /// </summary>
        public List<ThWCondensePipe> CondensePipes { get; set; }
        /// <summary>
        /// 雨水立管
        /// </summary>
        public List<ThWRainPipe> RainPipes { get; set; }
        /// <summary>
        /// 屋顶雨水立管
        /// </summary>
        public List<ThWRoofRainPipe> RoofRainPipes { get; set; }
        public ThWDevicePlatformRoom()
        {
            FloorDrains = new List<ThWFloorDrain>();
            CondensePipes = new List<ThWCondensePipe>();
            RainPipes = new List<ThWRainPipe>();
            RoofRainPipes = new List<ThWRoofRainPipe>();
        }
    }
}
