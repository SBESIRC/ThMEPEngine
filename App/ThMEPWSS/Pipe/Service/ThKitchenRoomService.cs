using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;
using ThMEPWSS.Pipe.Model;
using Dreambuild.AutoCAD;

namespace ThMEPWSS.Pipe.Service
{
    public class ThKitchenRoomService 
    {
        public List<ThWKitchenRoom> KitchenContainers { get; set; }
        private List<ThIfcSpace> Spaces { get; set; }
        private List<ThIfcBasin> Basintools { get; set; }
        private List<ThIfcRainPipe> RainPipes { get; set; }
        private List<ThIfcRoofRainPipe> RoofRainPipes { get; set; }
        private ThCADCoreNTSSpatialIndex SpaceSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex BasintoolSpatialIndex { get; set; }

        private ThKitchenRoomService(            
            List<ThIfcSpace> spaces,
            List<ThIfcBasin> basintools,
            List<ThIfcRainPipe> rainPipes,
            List<ThIfcRoofRainPipe> roofRainPipes)
        {
            Spaces = spaces;
            Basintools = basintools;
            RainPipes = rainPipes;
            RoofRainPipes = roofRainPipes;
            KitchenContainers = new List<ThWKitchenRoom>();
            BuildSpatialIndex();
        }
        public static List<ThWKitchenRoom> Build(List<ThIfcSpace> spaces, List<ThIfcBasin> basintools, List<ThIfcRainPipe> rainPipes, List<ThIfcRoofRainPipe> roofRainPipes)
        {
            var kitchenContainerService = new ThKitchenRoomService(spaces, basintools, rainPipes, roofRainPipes);           
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
        private ThWKitchenRoom CreateKitchenContainer(ThIfcSpace kitchenSpace)
        {
            ThWKitchenRoom thKitchenContainer = new ThWKitchenRoom();
            thKitchenContainer.Kitchen = kitchenSpace;
            var kitchenDrainwellService = ThKitchenDrainwellService.Find(Spaces, kitchenSpace, SpaceSpatialIndex);
            thKitchenContainer.DrainageWells = kitchenDrainwellService.Drainwells;
            thKitchenContainer.Pypes = kitchenDrainwellService.Pypes;
            var kitchenBasintoolService = ThKitchenBasintoolService.Find(Basintools, kitchenSpace, BasintoolSpatialIndex);
            thKitchenContainer.BasinTools = kitchenBasintoolService.Basintools;
            thKitchenContainer.RainPipes = FindRainPipes(RainPipes, kitchenSpace);
            thKitchenContainer.RoofRainPipes = FindRoofRainPipes(RoofRainPipes, kitchenSpace);
            return thKitchenContainer;
        }
        private List<ThIfcSpace> GetKitchenSpaces()
        {
            return Spaces.Where(m => m.Tags.Where(n => n.Contains("厨房")).Any()).ToList();
        }
        private void BuildSpatialIndex()
        {
            DBObjectCollection spaceObjs = new DBObjectCollection();
            Spaces.ForEach(o => spaceObjs.Add(o.Boundary));
            SpaceSpatialIndex = new ThCADCoreNTSSpatialIndex(spaceObjs);     
        }
        private static List<ThIfcRainPipe> FindRainPipes(List<ThIfcRainPipe> pipes, ThIfcSpace space)
        {
            var rainPipes = new List<ThIfcRainPipe>();
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
        private static List<ThIfcRoofRainPipe> FindRoofRainPipes(List<ThIfcRoofRainPipe> pipes, ThIfcSpace space)
        {
            var roofRainPipes = new List<ThIfcRoofRainPipe>();
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
