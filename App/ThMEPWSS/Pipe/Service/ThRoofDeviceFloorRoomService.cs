using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofDeviceFloorRoomService : IDisposable
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
        public static List<ThWRoofDeviceFloorRoom> Build(List<ThIfcSpace> spaces, List<ThIfcGravityWaterBucket> gravityWaterBuckets, List<ThIfcSideEntryWaterBucket> sideEntryWaterBuckets, List<ThIfcRoofRainPipe> roofRainPipes, List<ThIfcSpace> baseCircles)
        {
            using (var roofDeviceFloorContainerService = new ThRoofDeviceFloorRoomService(spaces, gravityWaterBuckets, sideEntryWaterBuckets, roofRainPipes, baseCircles))
            {
                roofDeviceFloorContainerService.Build();
                return roofDeviceFloorContainerService.Rooms;
            }
        }
        public void Dispose()
        {
        }
        private void Build()
        {
            //找主体空间 空间框线包含“顶层设备空间”
            var roofDeviceFloorSpaces = GetRoofDeviceFloorSpaces();
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
                BaseCircles = ThRoofFloorBaseCircleService.Find(roofDeviceFloorSpace, BaseCircles),
            };
        }
        private List<ThIfcSpace> GetRoofDeviceFloorSpaces()
        {
            return Spaces.Where(m => m.Tags.Where(n => n.Contains("RFS")).Any()).ToList();
        }
    }
}
