﻿using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofFloorRoomService 
    {
        private List<ThIfcSpace> BaseCircles { get; set; }
        private List<ThIfcSpace> Spaces { get; set; }
        private List<ThIfcRoofRainPipe> RoofRainPipes { get; set; }
        private List<ThIfcGravityWaterBucket> GravityWaterBuckets { get; set; }
        private List<ThIfcSideEntryWaterBucket> SideEntryWaterBuckets { get; set; }
        public List<ThWRoofFloorRoom> Rooms { get; private set; }

        private ThRoofFloorRoomService(
            List<ThIfcSpace> baseCircles,
            List<ThIfcSpace> spaces,
            List<ThIfcGravityWaterBucket> gravityWaterBuckets,
            List<ThIfcSideEntryWaterBucket> sideEntryWaterBuckets,
            List<ThIfcRoofRainPipe> roofRainPipes)
        {
            Spaces = spaces;
            RoofRainPipes = roofRainPipes;
            BaseCircles = baseCircles;
            GravityWaterBuckets = gravityWaterBuckets;
            SideEntryWaterBuckets = sideEntryWaterBuckets;
            Rooms = new List<ThWRoofFloorRoom>();
        }
        public static List<ThWRoofFloorRoom> Build(List<ThIfcSpace> spaces, List<ThIfcGravityWaterBucket> gravityWaterBuckets, List<ThIfcSideEntryWaterBucket> sideEntryWaterBuckets, List<ThIfcRoofRainPipe> roofRainPipes, List<ThIfcSpace> baseCircles)
        {
            var roofFloorContainerService = new ThRoofFloorRoomService(baseCircles, spaces, gravityWaterBuckets, sideEntryWaterBuckets, roofRainPipes);
            roofFloorContainerService.Build();
            return roofFloorContainerService.Rooms;            
        }      
        private void Build()
        {
            //找主体空间 空间框线包含“顶层设备空间”
            var roofFloorSpaces = Spaces;
            roofFloorSpaces.ForEach(o =>
            {
                Rooms.Add(CreateRoofFloorContainer(o));
            });
        }
        private ThWRoofFloorRoom CreateRoofFloorContainer(ThIfcSpace roofFloorSpace)
        {
            return new ThWRoofFloorRoom()
            {
                RoofFloor = roofFloorSpace,
                RoofRainPipes = ThRoofFloorRoofRainPipeService.Find(roofFloorSpace, RoofRainPipes),
                GravityWaterBuckets = ThRoofFloorGravityWaterBucketService.Find(roofFloorSpace, GravityWaterBuckets),
                SideEntryWaterBuckets = ThRoofFloorSideEntryWaterBucketService.Find(roofFloorSpace, SideEntryWaterBuckets),
                BaseCircles = BaseCircles,
            };
        }
    }
}
