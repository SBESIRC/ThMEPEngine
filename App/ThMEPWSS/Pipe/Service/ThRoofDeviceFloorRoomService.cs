using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofDeviceFloorRoomService
    {
        private List<ThIfcSpace> Spaces { get; set; }
        private List<ThIfcRoofRainPipe> RoofRainPipes { get; set; }
        private List<ThIfcGravityWaterBucket> GravityWaterBuckets { get; set; }
        private List<ThIfcSideEntryWaterBucket> SideEntryWaterBuckets { get; set; }
        public List<ThWRoofDeviceFloorRoom> Rooms { get; private set; }
        private List<ThIfcSpace> BaseCircles { get; set; }

        private ThRoofDeviceFloorRoomService(
            List<ThIfcSpace> spaces,
            List<ThIfcGravityWaterBucket> gravityWaterBuckets,
            List<ThIfcSideEntryWaterBucket> sideEntryWaterBuckets,
            List<ThIfcRoofRainPipe> roofRainPipes,
            List<ThIfcSpace> baseCircles)
        {
            Spaces = spaces;
            RoofRainPipes = roofRainPipes;
            GravityWaterBuckets = gravityWaterBuckets;
            SideEntryWaterBuckets = sideEntryWaterBuckets;
            BaseCircles = baseCircles;
            Rooms = new List<ThWRoofDeviceFloorRoom>();
        }
        public static List<ThWRoofDeviceFloorRoom> Build(
            List<ThIfcSpace> spaces, 
            List<ThIfcGravityWaterBucket> gravityWaterBuckets, 
            List<ThIfcSideEntryWaterBucket> sideEntryWaterBuckets, 
            List<ThIfcRoofRainPipe> roofRainPipes, List<ThIfcSpace> baseCircles)
        {
            var roofDeviceFloorContainerService = new ThRoofDeviceFloorRoomService(
                spaces, gravityWaterBuckets, sideEntryWaterBuckets, roofRainPipes, baseCircles);          
            roofDeviceFloorContainerService.Build();
            return roofDeviceFloorContainerService.Rooms;          
        }   
        private void Build()
        {
            //找主体空间 空间框线包含“顶层设备空间”
            var roofDeviceFloorSpaces = Spaces;
            roofDeviceFloorSpaces.ForEach(o =>
            {
                Rooms.Add(CreateRoofDeviceFloorContainer(o));
            });
        }
        private ThWRoofDeviceFloorRoom CreateRoofDeviceFloorContainer(ThIfcSpace roofDeviceFloorSpace)
        {
            return new ThWRoofDeviceFloorRoom()
            {
                RoofDeviceFloor = roofDeviceFloorSpace,
                RoofRainPipes = ThRoofDeviceFloorRoofRainPipeService.Find(roofDeviceFloorSpace, RoofRainPipes),
                GravityWaterBuckets = ThRoofDeviceFloorGravityWaterBucketService.Find(roofDeviceFloorSpace, GravityWaterBuckets),
                SideEntryWaterBuckets = ThRoofDeviceFloorSideEntryWaterBucketService.Find(roofDeviceFloorSpace, SideEntryWaterBuckets),
                BaseCircles =BaseCircles,
            };
        }
       
    }
}
