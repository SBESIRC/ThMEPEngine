using System;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;
using ThMEPEngineCore.Service;

namespace ThMEPWSS.Pipe.Service
{
    public class ThDevicePlatformRoomService
    {
        public List<ThWDevicePlatformRoom> DevicePlatformRoom { get; set; }
        private List<ThIfcRoom> Spaces { get; set; }
        private List<ThWFloorDrain> FloorDrains { get; set; }
        private List<ThWRainPipe> RainPipes { get; set; }
        private List<ThWRoofRainPipe> RoofRainPipes { get; set; }
        private List<ThWCondensePipe> CondensePipes { get; set; }
        private ThCADCoreNTSSpatialIndex SpaceSpatialIndex { get; set; }
        private ThDevicePlatformRoomService(
         List<ThIfcRoom> spaces,
         List<ThWFloorDrain> floorDrains,
         List<ThWRainPipe> rainPipes,
         List<ThWCondensePipe> condensePipes,
         List<ThWRoofRainPipe> roofRainPipes
        )
        {
            Spaces = spaces;
            FloorDrains = floorDrains;
            RainPipes = rainPipes;
            CondensePipes = condensePipes;
            RoofRainPipes = roofRainPipes;
            BuildSpatialIndex();
        }
        public static List<ThWDevicePlatformRoom> Build(
            List<ThIfcRoom> spaces,
            List<ThWFloorDrain> floorDrains,
            List<ThWRainPipe> rainPipes,
            List<ThWCondensePipe> condensePipes,
            List<ThWRoofRainPipe> roofRainPipes)
        {
            var service = new ThDevicePlatformRoomService(spaces, floorDrains, rainPipes, condensePipes, roofRainPipes);
            service.Build();
            return service.DevicePlatformRoom;
        }
        private void Build()
        {
            DevicePlatformRoom = new List<ThWDevicePlatformRoom>();
            DevicePlatformSpaces().ForEach(o => DevicePlatformRoom.AddRange(CreateDevicePlatformRooms(o.Item2)));
        }
        private List<ThWDevicePlatformRoom> CreateDevicePlatformRooms(List<ThIfcRoom> devicePlatformSpaces)
        {
            var thDevicePlatformRoomlist = new List<ThWDevicePlatformRoom>();

            foreach (var devicePlatformSpace in devicePlatformSpaces)
            {
                ThWDevicePlatformRoom thDevicePlatformRoom = new ThWDevicePlatformRoom();
                thDevicePlatformRoom.Boundary = devicePlatformSpace.Boundary;
                var DevicePlatformFloordrainService = ThDevicePlatformFloorDrainService.Find(FloorDrains, devicePlatformSpace);
                thDevicePlatformRoom.FloorDrains = DevicePlatformFloordrainService.FloorDrains;
                var DevicePlatformRainPipeService = ThDevicePlatformRainPipeService.Find(RainPipes, devicePlatformSpace);
                thDevicePlatformRoom.RainPipes = DevicePlatformRainPipeService.RainPipes;
                var DevicePlatformCondensePipeService = ThDevicePlatformCondensePipeService.Find(CondensePipes, devicePlatformSpace);
                thDevicePlatformRoom.CondensePipes = DevicePlatformCondensePipeService.CondensePipes;
                var DevicePlatformRoofRainPipeService = ThDevicePlatformRoofRainPipeService.Find(RoofRainPipes, devicePlatformSpace);
                thDevicePlatformRoom.RoofRainPipes = DevicePlatformRoofRainPipeService.RoofRainPipes;
                thDevicePlatformRoomlist.Add(thDevicePlatformRoom);
            }
            return thDevicePlatformRoomlist;
        }
        private List<Tuple<ThIfcRoom, List<ThIfcRoom>>> DevicePlatformSpaces()
        {
            var PlatformSpaces = new List<Tuple<ThIfcRoom, List<ThIfcRoom>>>();
            var BalconySpaces = Spaces.Where(m => m.Tags.Where(n => n.Contains("阳台")).Any()).ToList();

            var spacePredicateService = new ThRoomSpatialPredicateService(Spaces);

            foreach (var BalconySpace in BalconySpaces)
            {
                var bufferObjs = ThCADCoreNTSOperation.Buffer(BalconySpace.Boundary as Polyline, ThWPipeCommon.BALCONY_BUFFER_DISTANCE);
                if (bufferObjs.Count > 0)
                {
                    var crossObjs = SpaceSpatialIndex.SelectCrossingPolygon(bufferObjs[0] as Polyline);
                    //获取偏移后，能框选到的空间
                    var crossSpaces = Spaces.Where(o => crossObjs.Contains(o.Boundary));
                    // 找到不包含子空间的，且不含有Tag名称的空间
                    var balconies = crossSpaces.Where(m => spacePredicateService.Contains(m).Count == 0 && m.Tags.Count == 0);
                    var outerSpaces = balconies.Where(o => !BalconySpace.Boundary.ToNTSPolygon().Contains(o.Boundary.ToNTSPolygon().Buffer(-5.0)));
                    var relatedbalconies = outerSpaces.Where(o => (GetSpaceArea(o) > ThWPipeCommon.MIN_DEVICEPLATFORM_AREA && GetSpaceArea(o) < ThWPipeCommon.MAX_DEVICEPLATFORM_AREA)).ToList();
                    PlatformSpaces.Add(Tuple.Create(BalconySpace, relatedbalconies));
                }
            }
            return PlatformSpaces;
        }
        private double GetSpaceArea(ThIfcRoom thIfcSpace)
        {
            return thIfcSpace.Boundary.Area / (1000 * 1000);
        }
        private void BuildSpatialIndex()
        {
            DBObjectCollection spaceObjs = new DBObjectCollection();
            Spaces.ForEach(o => spaceObjs.Add(o.Boundary));
            SpaceSpatialIndex = new ThCADCoreNTSSpatialIndex(spaceObjs);
        }
    }
}
