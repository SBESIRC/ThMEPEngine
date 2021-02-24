using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Model;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Service
{
    public class ThBalconyRoomService
    {
        public List<ThWBalconyRoom> BalconyRooms { get; private set; }
        private List<ThIfcSpace> Spaces { get; set; }
        private List<ThWFloorDrain> FloorDrains { get; set; }
        private List<ThIfcWashMachine> Washmachines { get; set; }
        private List<ThWRainPipe> RainPipes { get; set; }
        private List<ThWBasin> Basintools { get; set; }
        private ThBalconyRoomService(
            List<ThIfcSpace> spaces,
            List<ThIfcWashMachine> washmachines,
            List<ThWFloorDrain> floorDrains,
            List<ThWRainPipe> rainPipes,
            List<ThWBasin> basintools)
        {
            Spaces = spaces;
            RainPipes = rainPipes;
            Basintools = basintools;
            FloorDrains = floorDrains;
            Washmachines = washmachines;
        }
        public static List<ThWBalconyRoom> Build(
            List<ThIfcSpace> spaces,
            List<ThIfcWashMachine> washmachines,
            List<ThWFloorDrain> floorDrains,
            List<ThWRainPipe> rainPipes,
            List<ThWBasin> basintools)
        {
            var service = new ThBalconyRoomService(spaces, washmachines, floorDrains, rainPipes, basintools);
            service.Build();
            return service.BalconyRooms;
        }
        private void Build()
        {
            BalconyRooms = new List<ThWBalconyRoom>();
            BalconySpaces().ForEach(o => BalconyRooms.Add(CreateBalconyRooms(o)));
        }
        private ThWBalconyRoom CreateBalconyRooms(ThIfcSpace balconySpace)
        {
            ThWBalconyRoom thBalconyRoom = new ThWBalconyRoom();
            thBalconyRoom.Balcony = balconySpace;

            var BalconyWashmachineService = ThBalconyWashMachineService.Find(Washmachines, balconySpace);
            thBalconyRoom.Washmachines = BalconyWashmachineService.Washmachines;

            var BalconyFloordrainService = ThBalconyFloorDrainService.Find(FloorDrains, balconySpace);
            thBalconyRoom.FloorDrains = BalconyFloordrainService.FloorDrains;

            var BalconyRainPipeService = ThBalconyRainPipeService.Find(RainPipes, balconySpace);
            thBalconyRoom.RainPipes = BalconyRainPipeService.RainPipes;
            var BalconyBasintoolsService = ThBalconyBasintoolService.Find(Basintools, balconySpace);
            thBalconyRoom.BasinTools = BalconyBasintoolsService.Basintools;

            return thBalconyRoom;
        }
        private List<ThIfcSpace> BalconySpaces()
        {
            return Spaces.Where(m => m.Tags.Where(n => n.Contains("阳台")).Any()).ToList();
        }
    }
}
