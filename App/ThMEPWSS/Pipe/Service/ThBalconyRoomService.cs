using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using GeometryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;
using ThMEPWSS.Pipe.Model;


namespace ThMEPWSS.Pipe.Service
{
   public class ThBalconyRoomService : IDisposable
    {
        public List<ThWBalconyRoom> BalconyRooms { get; set; }
        private List<ThIfcSpace> Spaces { get; set; }
        private List<ThIfcFloorDrain> FloorDrains { get; set; }

        private List<ThIfcWashMachine> Washmachines { get; set; }
        private List<ThIfcRainPipe> RainPipes { get; set; }
        private ThCADCoreNTSSpatialIndex SpaceSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex FloorDrainSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex WashmachineSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex RainPipeSpatialIndex { get; set; }
        private ThBalconyRoomService(
            List<ThIfcSpace> spaces,
            List<ThIfcWashMachine> washmachines,
            List<ThIfcFloorDrain> floorDrains,
            List<ThIfcRainPipe> rainPipes)
        {
            Spaces = spaces;        
            FloorDrains = floorDrains;
            BalconyRooms = new List<ThWBalconyRoom>();
            Washmachines = washmachines;
            RainPipes = rainPipes;
            BuildSpatialIndex();
        }
        public static ThBalconyRoomService Build(List<ThIfcSpace> spaces, List<ThIfcWashMachine> washmachines,List<ThIfcFloorDrain> floorDrains, List<ThIfcRainPipe> rainPipes)
        {
            using (var balconyRoomService = new ThBalconyRoomService(spaces, washmachines,floorDrains, rainPipes))
            {
                balconyRoomService.Build();
                return balconyRoomService;
            }
        }
        public void Dispose()
        {
        }
        private void Build()
        {
            //找主体空间 空间框线包含“生活阳台”
            var balconySpaces = BalconySpaces();
            balconySpaces.ForEach(o =>
            {
                BalconyRooms.Add(CreateBalconyRooms(o));
            });
        }
        private ThWBalconyRoom CreateBalconyRooms(ThIfcSpace balconySpace)
        {
            ThWBalconyRoom thBalconyRoom = new ThWBalconyRoom();
            thBalconyRoom.Balcony = balconySpace;
      
            var BalconyWashmachineService = ThBalconyWashMachineService.Find(Washmachines, balconySpace, WashmachineSpatialIndex);
            thBalconyRoom.Washmachines = BalconyWashmachineService.Washmachines;

            var BalconyFloordrainService = ThBalconyFloorDrainService.Find(FloorDrains, balconySpace, FloorDrainSpatialIndex);
            thBalconyRoom.FloorDrains = BalconyFloordrainService.FloorDrains;

            var BalconyRainPipeService = ThBalconyRainPipeService.Find(RainPipes, balconySpace, RainPipeSpatialIndex);
            thBalconyRoom.RainPipes = BalconyRainPipeService.RainPipe;

            return thBalconyRoom;
        }
        private List<ThIfcSpace> BalconySpaces()
        {
            return Spaces.Where(m => m.Tags.Where(n => n.Contains("生活阳台")).Any()).ToList();

        }
        private void BuildSpatialIndex()
        {
            DBObjectCollection spaceObjs = new DBObjectCollection();
            Spaces.ForEach(o => spaceObjs.Add(o.Boundary));
            SpaceSpatialIndex = new ThCADCoreNTSSpatialIndex(spaceObjs);

            DBObjectCollection washmachineObjs = new DBObjectCollection();
            Washmachines.ForEach(o => washmachineObjs.Add(o.Outline));
            WashmachineSpatialIndex = new ThCADCoreNTSSpatialIndex(washmachineObjs);

            DBObjectCollection floordrainObjs = new DBObjectCollection();
            FloorDrains.ForEach(o => floordrainObjs.Add(o.Outline));
            FloorDrainSpatialIndex = new ThCADCoreNTSSpatialIndex(floordrainObjs);

            DBObjectCollection rainpipeObjs = new DBObjectCollection();
            RainPipes.ForEach(o => rainpipeObjs.Add(o.Outline));
            RainPipeSpatialIndex = new ThCADCoreNTSSpatialIndex(rainpipeObjs);
        }
    }
}
