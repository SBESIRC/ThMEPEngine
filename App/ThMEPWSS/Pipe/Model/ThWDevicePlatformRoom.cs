using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;
using System.Collections.Generic;

namespace ThMEPWSS.Pipe.Model
{
    /// <summary>
    /// 设备平台
    /// </summary>
    public class ThWDevicePlatformRoom : ThWRoom
    {
        /// <summary>
        /// 设备平台区域
        /// </summary>
        public List<ThIfcSpace> DevicePlatforms { get; set; }
        /// <summary>
        /// 地漏
        /// </summary>
        public List<ThIfcFloorDrain> FloorDrains { get; set; }
        /// <summary>
        /// 冷凝水立管
        /// </summary>
        public List<ThIfcCondensePipe> CondensePipes { get; set; }
        /// <summary>
        /// 雨水立管
        /// </summary>
        public List<ThIfcRainPipe> RainPipes { get; set; }
        /// <summary>
        /// 屋顶雨水立管
        /// </summary>
        public List<ThIfcRoofRainPipe> RoofRainPipes { get; set; }
        public ThWDevicePlatformRoom()
        {
            DevicePlatforms = new List<ThIfcSpace>();
            FloorDrains = new List<ThIfcFloorDrain>();
            CondensePipes = new List<ThIfcCondensePipe>();
            RainPipes = new List<ThIfcRainPipe>();
            RoofRainPipes = new List<ThIfcRoofRainPipe>();
        }
    }
}
