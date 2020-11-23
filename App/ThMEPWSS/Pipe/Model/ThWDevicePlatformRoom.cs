using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;
using System.Collections.Generic;


namespace ThMEPWSS.Pipe.Model
{
    public class ThWDevicePlatformRoom : ThWRoom
    {
        /// <summary>
        /// 设备平台
        /// </summary>
        public List<ThIfcSpace> DevicePlatform { get; set; }
        //地漏
        public List<ThIfcFloorDrain> FloorDrains { get; set; }
        //冷凝水立管
        public List<ThIfcCondensePipe> CondensePipes { get; set; }
        /// <summary>
        /// 雨水立管
        /// </summary>
        public List<ThIfcRainPipe> RainPipes { get; set; }
        public ThWDevicePlatformRoom()
        {
            DevicePlatform = new List<ThIfcSpace>();
            FloorDrains = new List<ThIfcFloorDrain>();
            CondensePipes = new List<ThIfcCondensePipe>();
            RainPipes = new List<ThIfcRainPipe>();
        }
    }
}
