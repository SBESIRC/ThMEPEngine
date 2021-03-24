using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThKitchenRoomService 
    {
        public List<ThWKitchenRoom> KitchenContainers { get; set; }
        private List<ThIfcRoom> Spaces { get; set; }
        private List<ThWBasin> Basintools { get; set; }
        private List<ThWRainPipe> RainPipes { get; set; }
        private List<ThWRoofRainPipe> RoofRainPipes { get; set; }
        private List<ThWCondensePipe> CondensePipes { get; set; }
        private List<ThWFloorDrain> FloorDrains { get; set; }
        private ThCADCoreNTSSpatialIndex SpaceSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex BasintoolSpatialIndex { get; set; }

        private ThKitchenRoomService(            
            List<ThIfcRoom> spaces,
            List<ThWBasin> basintools,
            List<ThWRainPipe> rainPipes,
            List<ThWRoofRainPipe> roofRainPipes,
             List<ThWCondensePipe> condensePipes,
             List<ThWFloorDrain> floorDrains)
        {
            Spaces = spaces;
            Basintools = basintools;
            RainPipes = rainPipes;
            RoofRainPipes = roofRainPipes;
            CondensePipes = condensePipes;
            FloorDrains = floorDrains;
            KitchenContainers = new List<ThWKitchenRoom>();
            BuildSpatialIndex();
        }
        public static List<ThWKitchenRoom> Build(List<ThIfcRoom> spaces, List<ThWBasin> basintools, List<ThWRainPipe> rainPipes, List<ThWRoofRainPipe> roofRainPipes, List<ThWCondensePipe> condensePipes, List<ThWFloorDrain> floorDrains)
        {
            var kitchenContainerService = new ThKitchenRoomService(spaces, basintools, rainPipes, roofRainPipes, condensePipes, floorDrains);           
            kitchenContainerService.Build();
            return kitchenContainerService.KitchenContainers;
     
        }
      
        private void Build()
        {
            //找主体空间 空间框线包含“卫生间”
            var kitchenSpaces = GetKitchenSpaces();
            kitchenSpaces.ForEach(o =>
            {
                KitchenContainers.Add(CreateKitchenContainer(o));
            });
        }
        private ThWKitchenRoom CreateKitchenContainer(ThIfcRoom kitchenSpace)
        {
            ThWKitchenRoom thKitchenContainer = new ThWKitchenRoom();
            thKitchenContainer.Boundary = kitchenSpace.Boundary;
            var kitchenDrainwellService = ThKitchenDrainwellService.Find(Spaces, kitchenSpace, SpaceSpatialIndex);
            thKitchenContainer.DrainageWells = kitchenDrainwellService.Drainwells;
            thKitchenContainer.Pypes = kitchenDrainwellService.Pypes;
            var kitchenBasintoolService = ThKitchenBasintoolService.Find(Basintools, kitchenSpace, BasintoolSpatialIndex);
            thKitchenContainer.BasinTools = kitchenBasintoolService.Basintools;
            thKitchenContainer.RainPipes = FindRainPipes(RainPipes, kitchenSpace);
            thKitchenContainer.RoofRainPipes = FindRoofRainPipes(RoofRainPipes, kitchenSpace);
            thKitchenContainer.CondensePipes= FindCondensePipes(CondensePipes, kitchenSpace);
            thKitchenContainer.FloorDrains=GetFloorDrain(FloorDrains, kitchenSpace);
            return thKitchenContainer;
        }
        private static List<ThWFloorDrain> GetFloorDrain(List<ThWFloorDrain> FloorDrains, ThIfcRoom kitchenSpace)
        {
            var floorDrainList = new List<ThWFloorDrain>();
            foreach (var FloorDrain in FloorDrains)
            {
                BlockReference block = FloorDrain.Outline as BlockReference;
                Polyline boundary = kitchenSpace.Boundary as Polyline;
                if (block.Position.DistanceTo(boundary.GetCenter()) < ThWPipeCommon.MAX_TOILET_TO_FLOORDRAIN_DISTANCE2)
                {
                    floorDrainList.Add(FloorDrain);
                }
            }
            return floorDrainList;
        }
        private static List<ThWCondensePipe> FindCondensePipes(List<ThWCondensePipe> pipes, ThIfcRoom space)
        {
            var condensePipes = new List<ThWCondensePipe>();
            foreach (var pipe in pipes)
            {
                Polyline s = pipe.Outline as Polyline;
                if (s.GetCenter().DistanceTo(space.Boundary.GetCenter()) < ThWPipeCommon.MAX_TOILET_TO_CONDENSEPIPE_DISTANCE)
                {
                    condensePipes.Add(pipe);
                }

            }
            return condensePipes;
        }
        private List<ThIfcRoom> GetKitchenSpaces()
        {
            return Spaces.Where(m => m.Tags.Where(n => n.Contains("厨房")).Any()).ToList();
        }
        private void BuildSpatialIndex()
        {
            DBObjectCollection spaceObjs = new DBObjectCollection();
            Spaces.ForEach(o => spaceObjs.Add(o.Boundary));
            SpaceSpatialIndex = new ThCADCoreNTSSpatialIndex(spaceObjs);     
        }
        private static List<ThWRainPipe> FindRainPipes(List<ThWRainPipe> pipes, ThIfcRoom space)
        {
            var rainPipes = new List<ThWRainPipe>();
            foreach (var pipe in pipes)
            {
                Polyline s = pipe.Outline as Polyline;
                if (s.GetCenter().DistanceTo(space.Boundary.GetCenter()) < ThWPipeCommon.MAX_KITCHEN_TO_RAINPIPE_DISTANCE)
                {
                    rainPipes.Add(pipe);
                }

            }
            return rainPipes;
        }
        private static List<ThWRoofRainPipe> FindRoofRainPipes(List<ThWRoofRainPipe> pipes, ThIfcRoom space)
        {
            var roofRainPipes = new List<ThWRoofRainPipe>();
            foreach (var pipe in pipes)
            {
                Polyline s = pipe.Outline as Polyline;
                if (s.GetCenter().DistanceTo(space.Boundary.GetCenter()) < ThWPipeCommon.MAX_KITCHEN_TO_RAINPIPE_DISTANCE)
                {
                    roofRainPipes.Add(pipe);
                }

            }
            return roofRainPipes;
        }
    }
}
