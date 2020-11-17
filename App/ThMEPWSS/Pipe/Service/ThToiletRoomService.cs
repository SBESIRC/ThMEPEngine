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
    public class ThToiletRoomService : IDisposable
    {
        public List<ThWToiletRoom> ToiletContainers { get; set; }
        private List<ThIfcSpace> Spaces { get; set; }
        private List<ThIfcClosestool> Closestools { get; set; }
        private List<ThIfcFloorDrain> FloorDrains { get; set; }

        private ThCADCoreNTSSpatialIndex SpaceSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex ClosestoolSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex FloorDrainSpatialIndex { get; set; }

        private ThToiletRoomService(
            List<ThIfcSpace> spaces,
            List<ThIfcClosestool> closestools,
            List<ThIfcFloorDrain> floorDrains)
        {
            Spaces = spaces;
            Closestools = closestools;
            FloorDrains = floorDrains;
            ToiletContainers = new List<ThWToiletRoom>();
            BuildSpatialIndex();
        }
        public static ThToiletRoomService Build(List<ThIfcSpace> spaces,
            List<ThIfcClosestool> closestools,
            List<ThIfcFloorDrain> floorDrains)
        {
            using (var toiletContainerService = new ThToiletRoomService(spaces, closestools, floorDrains))
            {
                toiletContainerService.Build();
                return toiletContainerService;
            }
        }
        public void Dispose()
        {
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

            DBObjectCollection closestoolObjs = new DBObjectCollection();
            Closestools.ForEach(o => closestoolObjs.Add(o.Outline));
            ClosestoolSpatialIndex = new ThCADCoreNTSSpatialIndex(closestoolObjs);

            DBObjectCollection floordrainObjs = new DBObjectCollection();
            FloorDrains.ForEach(o => floordrainObjs.Add(o.Outline));
            FloorDrainSpatialIndex = new ThCADCoreNTSSpatialIndex(floordrainObjs);
        }
    }
}
