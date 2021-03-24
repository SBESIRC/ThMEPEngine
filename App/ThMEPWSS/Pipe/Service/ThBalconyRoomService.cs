using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;
using Dreambuild.AutoCAD;

namespace ThMEPWSS.Pipe.Service
{
    public class ThBalconyRoomService
    {
        public List<ThWBalconyRoom> BalconyRooms { get; private set; }
        private List<ThIfcRoom> Spaces { get; set; }
        private List<ThWFloorDrain> FloorDrains { get; set; }
        private List<ThWWashingMachine> Washmachines { get; set; }
        private List<ThWRainPipe> RainPipes { get; set; }
        private List<ThWBasin> Basintools { get; set; }
        private ThBalconyRoomService(
            List<ThIfcRoom> spaces,
            List<ThWWashingMachine> washmachines,
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
            List<ThIfcRoom> spaces,
            List<ThWWashingMachine> washmachines,
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
        private ThWBalconyRoom CreateBalconyRooms(ThIfcRoom balconySpace)
        {
            ThWBalconyRoom thBalconyRoom = new ThWBalconyRoom();
            thBalconyRoom.Boundary = balconySpace.Boundary;

            var BalconyWashmachineService = ThBalconyWashMachineService.Find(Washmachines, balconySpace);
            thBalconyRoom.Washmachines = BalconyWashmachineService.Washmachines;

            var BalconyFloordrainService = ThBalconyFloorDrainService.Find(FloorDrains, balconySpace);
            thBalconyRoom.FloorDrains = BalconyFloordrainService.FloorDrains;

            var BalconyRainPipeService = ThBalconyRainPipeService.Find(RainPipes, balconySpace);
            thBalconyRoom.RainPipes = BalconyRainPipeService.RainPipes;
            if (BalconyRainPipeService.RainPipes.Count == 0)
            {
                thBalconyRoom.RainPipes = FindRainPipes(RainPipes, balconySpace);
            }
            var BalconyBasintoolsService = ThBalconyBasintoolService.Find(Basintools, balconySpace);
            thBalconyRoom.BasinTools = BalconyBasintoolsService.Basintools;

            return thBalconyRoom;
        }
        private static List<ThWRainPipe> FindRainPipes(List<ThWRainPipe> pipes, ThIfcRoom space)
        {
            var rainPipes = new List<ThWRainPipe>();
            foreach (var pipe in pipes)
            {
                Polyline s = pipe.Outline as Polyline;
                if (s.GetCenter().DistanceTo(space.Boundary.GetCenter()) < ThWPipeCommon.MAX_BALCONY_TO_RAINPIPE_DISTANCE)
                {
                    rainPipes.Add(pipe);
                }

            }
            return rainPipes;
        }
        private List<ThIfcRoom> BalconySpaces()
        {
            return Spaces.Where(m => m.Tags.Where(n => n.Contains("阳台")).Any()).ToList();
        }
    }
}
