using System;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofDeviceFloorSideEntryWaterBucketService
    {
        private ThIfcSpace Space { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private List<ThWSideEntryWaterBucket> Buckets { get; set; }
        private ThRoofDeviceFloorSideEntryWaterBucketService(
            ThIfcSpace space,
            List<ThWSideEntryWaterBucket> buckets)
        {
            Space = space;
            Buckets = buckets;
            var objs = new DBObjectCollection();
            Buckets.Select(o => o.Outline).ForEach(o => objs.Add(o));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }

        public static List<ThWSideEntryWaterBucket> Find(
            ThIfcSpace space,
            List<ThWSideEntryWaterBucket> buckets)
        {
            var service = new ThRoofDeviceFloorSideEntryWaterBucketService(space, buckets);
            return service.Find();
        }

        public List<ThWSideEntryWaterBucket> Find()
        {
            var roofDeviceFloorBoundary = Space.Boundary as Polyline;
            var crossObjs = SpatialIndex.SelectCrossingPolygon(roofDeviceFloorBoundary);
            return Buckets.Where(o => crossObjs.Contains(o.Outline)).Where(o =>
            {
                var block = o.Outline as BlockReference;
                var bufferObjs = block.GeometricExtents.ToNTSPolygon().Buffer(-10.0).ToDbCollection();
                return roofDeviceFloorBoundary.Contains(bufferObjs[0] as Curve);
            }).ToList();
        }
    }
}
