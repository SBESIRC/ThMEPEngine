using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThToiletRoomService
    {
        public List<ThWToiletRoom> ToiletContainers { get; set; }
        private List<ThIfcRoom> Spaces { get; set; }
        private List<ThWClosestool> Closestools { get; set; }
        private List<ThWFloorDrain> FloorDrains { get; set; }
        private List<ThWCondensePipe> CondensePipes { get; set; }
        private List<ThWRoofRainPipe> RoofRainPipes { get; set; }
        private ThCADCoreNTSSpatialIndex SpaceSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex ClosestoolSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex FloorDrainSpatialIndex { get; set; }

        private ThToiletRoomService(
            List<ThIfcRoom> spaces,
            List<ThWClosestool> closestools,
            List<ThWFloorDrain> floorDrains,
            List<ThWCondensePipe> condensePipes,
            List<ThWRoofRainPipe> roofRainPipes)
        {
            Spaces = spaces;
            Closestools = closestools;
            FloorDrains = floorDrains;
            CondensePipes = condensePipes;
            RoofRainPipes = roofRainPipes;
            ToiletContainers = new List<ThWToiletRoom>();
            BuildSpatialIndex();
        }
        public static List<ThWToiletRoom> Build(List<ThIfcRoom> spaces,
            List<ThWClosestool> closestools,
            List<ThWFloorDrain> floorDrains,
            List<ThWCondensePipe> condensePipes,
            List<ThWRoofRainPipe> roofRainPipes)
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
        private ThWToiletRoom CreateToiletContainer(ThIfcRoom toiletSpace)
        {
            ThWToiletRoom thToiletContainer = new ThWToiletRoom();
            thToiletContainer.Boundary = toiletSpace.Boundary;
            var toiletDrainwellService = ThToiletDrainwellService.Find(Spaces, toiletSpace, SpaceSpatialIndex);
            thToiletContainer.DrainageWells = toiletDrainwellService.Drainwells;

            var toiletClosestoolService = ThToiletClosestoolService.Find(Closestools, toiletSpace, ClosestoolSpatialIndex);
            thToiletContainer.Closestools = toiletClosestoolService.Closestools;

            var toiletFloordrainService = ThToiletFloorDrainService.Find(FloorDrains, toiletSpace, FloorDrainSpatialIndex);
            thToiletContainer.FloorDrains = toiletFloordrainService.FloorDrains;
            if(!(toiletFloordrainService.FloorDrains.Count>0))//加一层过滤，后期可合并到里层
            {
                foreach(var FloorDrain in FloorDrains)
                {
                    BlockReference block = FloorDrain.Outline as BlockReference;
                   Polyline boundary= toiletSpace.Boundary as Polyline;
                    if (block.Position.DistanceTo(boundary.GetCenter())< ThWPipeCommon.MAX_TOILET_TO_FLOORDRAIN_DISTANCE1)
                    {
                        thToiletContainer.FloorDrains.Add(FloorDrain);
                        break;
                    }
                }
            }
            else
            {
                foreach (var FloorDrain in FloorDrains)
                {
                    BlockReference block = FloorDrain.Outline as BlockReference;
                    Polyline boundary = toiletSpace.Boundary as Polyline;
                    if (block.Position.DistanceTo(boundary.GetCenter()) < ThWPipeCommon.MAX_TOILET_TO_FLOORDRAIN_DISTANCE2)
                    {
                        thToiletContainer.FloorDrains.Add(FloorDrain);
                    }
                }
            }
            thToiletContainer.CondensePipes = FindCondensePipes(CondensePipes, toiletSpace);
            thToiletContainer.RoofRainPipes = FindRoofRainPipes(RoofRainPipes, toiletSpace);
            return thToiletContainer;
        }
        private List<ThIfcRoom> ToiletSpaces()
        {
            return Spaces.Where(m => m.Tags.Where(n => n.Contains("卫生间")).Any()).ToList();
        }
        private void BuildSpatialIndex()
        {
            DBObjectCollection spaceObjs = new DBObjectCollection();
            Spaces.ForEach(o => spaceObjs.Add(o.Boundary));
            SpaceSpatialIndex = new ThCADCoreNTSSpatialIndex(spaceObjs);
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
        private static List<ThWRoofRainPipe> FindRoofRainPipes(List<ThWRoofRainPipe> pipes, ThIfcRoom space)
        {
            var roofRainPipes = new List<ThWRoofRainPipe>();
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
