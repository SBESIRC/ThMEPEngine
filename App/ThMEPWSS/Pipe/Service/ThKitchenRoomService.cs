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
    public class ThKitchenRoomService : IDisposable
    {
        public List<ThWKitchenRoom> KitchenContainers { get; set; }
        private List<ThIfcSpace> Spaces { get; set; }
        private List<ThIfcBasin> Basintools { get; set; }
        private ThCADCoreNTSSpatialIndex SpaceSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex BasintoolSpatialIndex { get; set; }

        private ThKitchenRoomService(            
            List<ThIfcSpace> spaces,
            List<ThIfcBasin> basintools)
        {
            Spaces = spaces;
            Basintools = basintools;
            KitchenContainers = new List<ThWKitchenRoom>();
            BuildSpatialIndex();
        }
        public static List<ThWKitchenRoom> Build(List<ThIfcSpace> spaces, List<ThIfcBasin> basintools)
        {
            using (var kitchenContainerService = new ThKitchenRoomService(spaces, basintools))
            {
                kitchenContainerService.Build();
                return kitchenContainerService.KitchenContainers;
            }
        }
        public void Dispose()
        {
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

            DBObjectCollection basintoolObjs = new DBObjectCollection();
            Basintools.ForEach(o => basintoolObjs.Add(o.Outline));
            BasintoolSpatialIndex = new ThCADCoreNTSSpatialIndex(basintoolObjs);
        }
    }
}
