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
    public class ThToiletRoomService
    {
        public List<ThWToiletRoom> ToiletContainers { get; set; }
        private List<ThIfcSpace> Spaces { get; set; }
        private List<ThIfcClosestool> Closestools { get; set; }
        private List<ThIfcFloorDrain> FloorDrains { get; set; }
        private List<ThIfcCondensePipe> CondensePipes { get; set; }
        private List<ThIfcRoofRainPipe> RoofRainPipes { get; set; }
        private ThCADCoreNTSSpatialIndex SpaceSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex ClosestoolSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex FloorDrainSpatialIndex { get; set; }    

        private ThToiletRoomService(
            List<ThIfcSpace> spaces,
            List<ThIfcClosestool> closestools,
            List<ThIfcFloorDrain> floorDrains,
            List<ThIfcCondensePipe> condensePipes,
            List<ThIfcRoofRainPipe> roofRainPipes)
        {
            Spaces = spaces;
            Closestools = closestools;
            FloorDrains = floorDrains;
            CondensePipes = condensePipes;
            RoofRainPipes = roofRainPipes;
            ToiletContainers = new List<ThWToiletRoom>();
            BuildSpatialIndex();
        }
        public static List<ThWToiletRoom> Build(List<ThIfcSpace> spaces,
            List<ThIfcClosestool> closestools,
            List<ThIfcFloorDrain> floorDrains,
            List<ThIfcCondensePipe> condensePipes,
            List<ThIfcRoofRainPipe> roofRainPipes)
        {
            var toiletContainerService = new ThToiletRoomService(spaces, closestools, floorDrains, condensePipes, roofRainPipes);          
            toiletContainerService.Build();
            return toiletContainerService.ToiletContainers;            
        }      
        private void Build()
        {
            //找主体空间 空间框线包含“卫生间”
            var toiletSpaces = ToiletSpaces();
            toiletSpaces.ForEach(o =>
            {
                ToiletContainers.Add(CreateToiletContainer(o));
            });
        }
        private ThWToiletRoom CreateToiletContainer(ThIfcSpace toiletSpace)
        {
            ThWToiletRoom thToiletContainer = new ThWToiletRoom();
            thToiletContainer.Toilet = toiletSpace;
            var toiletDrainwellService = ThToiletDrainwellService.Find(Spaces, toiletSpace, SpaceSpatialIndex);
            thToiletContainer.DrainageWells = toiletDrainwellService.Drainwells;

            var toiletClosestoolService = ThToiletClosestoolService.Find(Closestools, toiletSpace, ClosestoolSpatialIndex);
            thToiletContainer.Closestools = toiletClosestoolService.Closestools;

            var toiletFloordrainService = ThToiletFloorDrainService.Find(FloorDrains, toiletSpace, FloorDrainSpatialIndex);
            thToiletContainer.FloorDrains = toiletFloordrainService.FloorDrains;
           
            thToiletContainer.CondensePipes = FindCondensePipes(CondensePipes, toiletSpace);
            thToiletContainer.RoofRainPipes = FindRoofRainPipes(RoofRainPipes, toiletSpace);
            return thToiletContainer;
        }
        private List<ThIfcSpace> ToiletSpaces()
        {
            return Spaces.Where(m => m.Tags.Where(n => n.Contains("卫生间")).Any()).ToList();
        }
        private void BuildSpatialIndex()
        {
            DBObjectCollection spaceObjs = new DBObjectCollection();
            Spaces.ForEach(o => spaceObjs.Add(o.Boundary));
            SpaceSpatialIndex = new ThCADCoreNTSSpatialIndex(spaceObjs);
        }
        private static List<ThIfcCondensePipe> FindCondensePipes(List<ThIfcCondensePipe> pipes, ThIfcSpace space)
        {
           var condensePipes = new List<ThIfcCondensePipe>();
           foreach(var pipe in pipes)
            {
                Polyline s = pipe.Outline as Polyline;
                if (s.GetCenter().DistanceTo(space.Boundary.GetCenter())< ThWPipeCommon.MAX_TOILET_TO_CONDENSEPIPE_DISTANCE)
                {
                    condensePipes.Add(pipe);
                }

            }
            return condensePipes;
        }
        private static List<ThIfcRoofRainPipe> FindRoofRainPipes(List<ThIfcRoofRainPipe> pipes, ThIfcSpace space)
        {
            var roofRainPipes = new List<ThIfcRoofRainPipe>(); 
            foreach (var pipe in pipes)
            {
                Polyline s = pipe.Outline as Polyline;
                if (s.GetCenter().DistanceTo(space.Boundary.GetCenter()) < ThWPipeCommon.MAX_TOILET_TO_CONDENSEPIPE_DISTANCE)
                {
                    roofRainPipes.Add(pipe);
                }

            }
            return roofRainPipes;
        }
    }
}
