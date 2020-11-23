using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;
using System.Collections.Generic;


namespace ThMEPWSS.Pipe.Model
{
    public class ThWBalconyRoom : ThWRoom
    { /// <summary>
      /// 生活阳台
      /// </summary>
        public ThIfcSpace Balcony { get; set; }
        //地漏
        public List<ThIfcFloorDrain> FloorDrains { get; set; }
        //排水立管
        //public List<ThIfcSpace> DrainWells { get; set; }
        /// <summary>
        /// 雨水立管
        /// </summary>
        public List<ThIfcRainPipe> RainPipes { get; set; }
        //洗衣机
        public List<ThIfcWashMachine> Washmachines { get; set; }
        public ThWBalconyRoom()
        {
            Balcony = null;
            FloorDrains = new List<ThIfcFloorDrain>();
            //DrainWells = new List<ThIfcSpace>();
            RainPipes = new List<ThIfcRainPipe>();
            Washmachines = new List<ThIfcWashMachine>();
        }

    }
}
