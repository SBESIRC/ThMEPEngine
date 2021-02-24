using System;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThBalconyFloorDrainService
    {
        public List<ThWFloorDrain> FloorDrains { get; private set; }
        private List<ThWFloorDrain> FloorDrainList { get; set; }
        private ThIfcSpace BalconySpace { get; set; }
        private ThCADCoreNTSSpatialIndex FloorDrainSpatialIndex { get; set; }
        private ThBalconyFloorDrainService(
           List<ThWFloorDrain> floordrainList,
           ThIfcSpace balconySpace,
           ThCADCoreNTSSpatialIndex floordrainSpatialIndex)
        {
            BalconySpace = balconySpace;
            FloorDrainList = floordrainList;
            FloorDrainSpatialIndex = floordrainSpatialIndex;
            if (FloorDrainSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                FloorDrainList.ForEach(o => dbObjs.Add(o.Outline));
                FloorDrainSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
        }
        public static ThBalconyFloorDrainService Find(
          List<ThWFloorDrain> floordrains,
          ThIfcSpace balconySpace,
          ThCADCoreNTSSpatialIndex floordrainSpatialIndex = null)
        {
            var instance = new ThBalconyFloorDrainService(floordrains, balconySpace, floordrainSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            var balconyBoundary = BalconySpace.Boundary as Polyline;
            var crossObjs = FloorDrainSpatialIndex.SelectCrossingPolygon(balconyBoundary);
            var crossFloordrains = FloorDrainList.Where(o => crossObjs.Contains(o.Outline));
            FloorDrains = crossFloordrains.Where(o =>
            {
                var block = o.Outline as BlockReference;
                var bufferObjs = block.GeometricExtents.ToNTSPolygon().Buffer(-10.0).ToDbCollection();
                return balconyBoundary.Contains(bufferObjs[0] as Curve);
            }).ToList();
        }
    }
}
