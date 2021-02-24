using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Model
{
    /// <summary>
    /// 屋顶设备层
    /// </summary>
    public class ThWRoofDeviceFloorRoom : ThWRoom
    {
        /// <summary>
        /// 屋顶设备层空间
        /// </summary>
        public ThIfcSpace RoofDeviceFloor { get; set; }
        /// <summary>
        /// 重力水斗
        /// </summary>
        public List<ThWGravityWaterBucket> GravityWaterBuckets { get; set; }
        /// <summary>
        /// 侧入式水斗
        /// </summary>
        public List<ThWSideEntryWaterBucket> SideEntryWaterBuckets { get; set; }
        /// <summary>
        /// 屋顶雨水管
        /// </summary>
        public List<ThWRoofRainPipe> RoofRainPipes { get; set; }
        /// <summary>
        /// 基点区域
        /// </summary>
        public List<ThIfcSpace> BaseCircles { get; set; }
        public ThWRoofDeviceFloorRoom()
        {
            GravityWaterBuckets = new List<ThWGravityWaterBucket>();
            SideEntryWaterBuckets = new List<ThWSideEntryWaterBucket>();
            RoofRainPipes = new List<ThWRoofRainPipe>();
            BaseCircles= new List<ThIfcSpace>();
        }
    }
}