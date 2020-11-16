using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Model
{
    public class ThToiletContainer
    {
        /// <summary>
        /// 卫生间空间
        /// </summary>
        public ThIfcSpace Toilet { get; set; }
        /// <summary>
        /// 排水管井
        /// </summary>
        public List<ThIfcSpace> DrainageWells { get; set; }
        /// <summary>
        /// 坐便器
        /// </summary>
        public List<ThIfcClosestool> Closestools { get; set; }
        /// <summary>
        /// 地漏
        /// </summary>
        public List<ThIfcFloorDrain> FloorDrains { get; set; }
        public ThToiletContainer()
        {
            Toilet = null;
            Closestools = new List<ThIfcClosestool>();
            DrainageWells = new List<ThIfcSpace>();
            FloorDrains = new List<ThIfcFloorDrain>();
        }
    }
}
