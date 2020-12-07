using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;
using ThMEPWSS.Pipe.Model;
using Autodesk.AutoCAD.DatabaseServices;
using AcHelper;


namespace ThMEPWSS.Pipe.Service
{
   public  class ThDevicePlatformRoomService : IDisposable
    {
        public List<ThWDevicePlatformRoom> DevicePlatformRoom { get; set; }
        private List<ThIfcSpace> Spaces { get; set; }   
        private List<ThIfcFloorDrain> FloorDrains { get; set; }
        private List<ThIfcRainPipe> RainPipes { get; set; }
        private List<ThIfcRoofRainPipe> RoofRainPipes { get; set; }
        private List<ThIfcCondensePipe> CondensePipes { get; set; }
        private ThCADCoreNTSSpatialIndex SpaceSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex FloorDrainSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex RainPipeSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex CondensePipeSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex RoofRainPipeSpatialIndex { get; set; }

        public double ClosedDistance { get; set; } = 500.0;
        private ThDevicePlatformRoomService(
         List<ThIfcSpace> spaces,
         List<ThIfcFloorDrain> floorDrains,
         List<ThIfcRainPipe> rainPipes,
         List<ThIfcCondensePipe> condensePipes,
         List<ThIfcRoofRainPipe> roofRainPipes
        )
        {  
             Spaces = spaces;
            FloorDrains = floorDrains;
            RainPipes = rainPipes;
            CondensePipes = condensePipes;
            RoofRainPipes = roofRainPipes;
            DevicePlatformRoom = new List<ThWDevicePlatformRoom>();
            BuildSpatialIndex();
        }
        public static List<ThWDevicePlatformRoom> Build(List<ThIfcSpace> spaces, List<ThIfcFloorDrain> floorDrains, List<ThIfcRainPipe> rainPipes, List<ThIfcCondensePipe> condensePipes, List<ThIfcRoofRainPipe> roofRainPipes)
        {
            using (var devicePlatformRoomService = new ThDevicePlatformRoomService(spaces, floorDrains, rainPipes, condensePipes, roofRainPipes))
            {
                devicePlatformRoomService.Build();
                return devicePlatformRoomService.DevicePlatformRoom;
            }
        }
        public void Dispose()
        {
        }
        private void Build()
        {
            //找主体空间 空间框线包含“设备平台”
            var devicePlatformSpaces = DevicePlatformSpaces();
            devicePlatformSpaces.ForEach(o =>
            {
                 DevicePlatformRoom.AddRange(CreateDevicePlatformRooms(o.Item2));
            });
        }
        private List<ThWDevicePlatformRoom> CreateDevicePlatformRooms(List<ThIfcSpace> devicePlatformSpaces)
        {
            var thDevicePlatformRoomlist = new List<ThWDevicePlatformRoom>();
           
            foreach (var devicePlatformSpace in devicePlatformSpaces)
            {
                ThWDevicePlatformRoom thDevicePlatformRoom = new ThWDevicePlatformRoom();
                thDevicePlatformRoom.DevicePlatforms.Add(devicePlatformSpace);
                var DevicePlatformFloordrainService = ThDevicePlatformFloorDrainService.Find(FloorDrains, devicePlatformSpace, FloorDrainSpatialIndex);
                thDevicePlatformRoom.FloorDrains = DevicePlatformFloordrainService.FloorDrains;
                var DevicePlatformRainPipeService = ThDevicePlatformRainPipeService.Find(RainPipes, devicePlatformSpace, RainPipeSpatialIndex);
                thDevicePlatformRoom.RainPipes = DevicePlatformRainPipeService.RainPipe;
                var DevicePlatformCondensePipeService = ThDevicePlatformCondensePipeService.Find(CondensePipes, devicePlatformSpace, CondensePipeSpatialIndex);
                thDevicePlatformRoom.CondensePipes = DevicePlatformCondensePipeService.CondensePipe;
                var DevicePlatformRoofRainPipeService = ThDevicePlatformRoofRainPipeService.Find(RoofRainPipes, devicePlatformSpace, RoofRainPipeSpatialIndex);
                thDevicePlatformRoom.RoofRainPipes = DevicePlatformRoofRainPipeService.RoofRainPipe;
                thDevicePlatformRoomlist.Add(thDevicePlatformRoom);
            }
            return thDevicePlatformRoomlist;
        }
        private List<Tuple<ThIfcSpace,List<ThIfcSpace>>> DevicePlatformSpaces()
        {
            var PlatformSpaces = new List<Tuple<ThIfcSpace, List<ThIfcSpace>>>();
            var BalconySpaces= Spaces.Where(m => m.Tags.Where(n => n.Contains("生活阳台")).Any()).ToList();
            foreach (var BalconySpace in BalconySpaces)
            {
                var bufferObjs = ThCADCoreNTSOperation.Buffer(BalconySpace.Boundary as Polyline, ClosedDistance);
                if (bufferObjs.Count > 0)
                {
                    var crossObjs = SpaceSpatialIndex.SelectCrossingPolygon(bufferObjs[0] as Polyline);
                    //获取偏移后，能框选到的空间
                    var crossSpaces = Spaces.Where(o => crossObjs.Contains(o.Boundary));
                    // 找到不包含子空间的，且不含有Tag名称的空间
                    var balconies = crossSpaces.Where(m => m.SubSpaces.Count==0 && m.Tags.Count==0);
                    var outerSpaces = balconies.Where(o=> !BalconySpace.Boundary.ToNTSPolygon().Contains(o.Boundary.ToNTSPolygon().Buffer(-5.0)));
                    var relatedbalconies = outerSpaces.Where(o => (GetSpaceArea(o) > 0.4 && GetSpaceArea(o) < 1)).ToList();
                    PlatformSpaces.Add(Tuple.Create(BalconySpace, relatedbalconies));
                }
            }
            return PlatformSpaces;
        }
        private double GetSpaceArea(ThIfcSpace thIfcSpace)
        {
            return thIfcSpace.Boundary.Area / (1000 * 1000);
        }
        private void BuildSpatialIndex()
        {
            DBObjectCollection spaceObjs = new DBObjectCollection();
            Spaces.ForEach(o => spaceObjs.Add(o.Boundary));
            SpaceSpatialIndex = new ThCADCoreNTSSpatialIndex(spaceObjs);


            DBObjectCollection floordrainObjs = new DBObjectCollection();
            FloorDrains.ForEach(o => floordrainObjs.Add(o.Outline));
            FloorDrainSpatialIndex = new ThCADCoreNTSSpatialIndex(floordrainObjs);
            DBObjectCollection rainPipeObjs = new DBObjectCollection();
            RainPipes.ForEach(o => rainPipeObjs.Add(o.Outline));
            RainPipeSpatialIndex = new ThCADCoreNTSSpatialIndex(rainPipeObjs);
            DBObjectCollection condensePipeObjs = new DBObjectCollection();
            CondensePipes.ForEach(o => condensePipeObjs.Add(o.Outline));
            CondensePipeSpatialIndex = new ThCADCoreNTSSpatialIndex(condensePipeObjs);
            DBObjectCollection roofRainPipeObjs = new DBObjectCollection();
            RoofRainPipes.ForEach(o => roofRainPipeObjs.Add(o.Outline));
            RoofRainPipeSpatialIndex = new ThCADCoreNTSSpatialIndex(roofRainPipeObjs);
        }
    }
}
