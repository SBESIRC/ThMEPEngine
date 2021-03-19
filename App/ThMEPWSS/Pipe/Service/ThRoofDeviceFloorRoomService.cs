using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofDeviceFloorRoomService
    {
        private List<ThIfcSpace> Spaces { get; set; }
        private List<ThWRoofRainPipe> RoofRainPipes { get; set; }
        private List<ThWGravityWaterBucket> GravityWaterBuckets { get; set; }
        private List<ThWSideEntryWaterBucket> SideEntryWaterBuckets { get; set; }
        public List<ThWRoofTopFloorRoom> Rooms { get; private set; }
        private List<ThIfcSpace> BaseCircles { get; set; }

        private ThRoofDeviceFloorRoomService(
            List<ThIfcSpace> spaces,
            List<ThWGravityWaterBucket> gravityWaterBuckets,
            List<ThWSideEntryWaterBucket> sideEntryWaterBuckets,
            List<ThWRoofRainPipe> roofRainPipes,
            List<ThIfcSpace> baseCircles)
        {
            Spaces = spaces;
            RoofRainPipes = roofRainPipes;
            GravityWaterBuckets = gravityWaterBuckets;
            SideEntryWaterBuckets = sideEntryWaterBuckets;
            BaseCircles = baseCircles;
            Rooms = new List<ThWRoofTopFloorRoom>();
        }
        public static List<ThWRoofTopFloorRoom> Build(
            List<ThIfcSpace> spaces, 
            List<ThWGravityWaterBucket> gravityWaterBuckets, 
            List<ThWSideEntryWaterBucket> sideEntryWaterBuckets, 
            List<ThWRoofRainPipe> roofRainPipes, List<ThIfcSpace> baseCircles)
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
        private ThWRoofTopFloorRoom CreateRoofDeviceFloorContainer(ThIfcSpace roofDeviceFloorSpace)
        {
            return new ThWRoofTopFloorRoom()
            {
                Boundary = roofDeviceFloorSpace.Boundary,
                RoofRainPipes = ThRoofDeviceFloorRoofRainPipeService.Find(roofDeviceFloorSpace, RoofRainPipes),
                GravityWaterBuckets = ThRoofDeviceFloorGravityWaterBucketService.Find(roofDeviceFloorSpace, GravityWaterBuckets),
                SideEntryWaterBuckets = ThRoofDeviceFloorSideEntryWaterBucketService.Find(roofDeviceFloorSpace, SideEntryWaterBuckets),
                BaseCircles =BaseCircles,
            };
        }
       
    }
}
