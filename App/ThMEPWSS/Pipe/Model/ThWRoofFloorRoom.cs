﻿using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Model
{
    /// <summary>
    /// 屋顶
    /// </summary>
    public class ThWRoofFloorRoom : ThWRoom
    {
        /// <summary>
        /// 屋顶空间
        /// </summary>
        public ThIfcSpace RoofFloor { get; set; }
        /// <summary>
        /// 重力水斗
        /// </summary>
        public List<ThIfcGravityWaterBucket> GravityWaterBuckets { get; set; }
        /// <summary>
        /// 侧入式水斗
        /// </summary>
        public List<ThIfcSideEntryWaterBucket> SideEntryWaterBuckets { get; set; }
        /// <summary>
        /// 屋顶雨水管
        /// </summary>
        public List<ThWRoofRainPipe> RoofRainPipes { get; set; }
        /// <summary>
        /// 基点区域
        /// </summary>
        public List<ThIfcSpace> BaseCircles { get; set; }
        public ThWRoofFloorRoom()
        {
            GravityWaterBuckets = new List<ThIfcGravityWaterBucket>();
            SideEntryWaterBuckets = new List<ThIfcSideEntryWaterBucket>();
            RoofRainPipes = new List<ThWRoofRainPipe>();
            BaseCircles = new List<ThIfcSpace>();
        }
    }
}