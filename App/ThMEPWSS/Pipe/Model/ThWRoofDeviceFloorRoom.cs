using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWRoofDeviceFloorRoom : ThWRoom
    {
        /// <summary>
        /// 屋顶设备层空间
        /// </summary>
        public ThIfcSpace RoofDeviceFloor { get; set; }
        /// <summary>
        /// 重力水斗
        /// </summary>
        public List<ThIfcGravityWaterBucket> GravityWaterBuckets { get; set; }
        /// <summary>
        /// 侧入式水斗
        /// </summary>
        public List<ThIfcSideEntryWaterBucket> SideEntryWaterBuckets { get; set; }

        public List<ThIfcRoofRainPipe> RoofRainPipes { get; set; }
        public ThWRoofDeviceFloorRoom()
        {
            RoofDeviceFloor = null;
            GravityWaterBuckets = new List<ThIfcGravityWaterBucket>();
            SideEntryWaterBuckets = new List<ThIfcSideEntryWaterBucket>();
            RoofRainPipes = new List<ThIfcRoofRainPipe>();
        }
    }
}