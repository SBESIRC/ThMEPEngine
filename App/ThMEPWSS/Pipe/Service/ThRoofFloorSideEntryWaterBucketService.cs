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
    public class ThRoofFloorSideEntryWaterBucketService
    {
        private ThIfcRoom Space { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private List<ThWSideEntryWaterBucket> Buckets { get; set; }
        private ThRoofFloorSideEntryWaterBucketService(
            ThIfcRoom space,
            List<ThWSideEntryWaterBucket> buckets)
        {
            Space = space;
            Buckets = buckets;
            var objs = new DBObjectCollection();
            Buckets.Select(o => o.Outline).ForEach(o => objs.Add(o));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }

        public static List<ThWSideEntryWaterBucket> Find(
            ThIfcRoom space,
            List<ThWSideEntryWaterBucket> buckets)
        {
            var service = new ThRoofFloorSideEntryWaterBucketService(space, buckets);
            return service.Find();
        }

        public List<ThWSideEntryWaterBucket> Find()
        {
            var roofFloorBoundary = Space.Boundary as Polyline;
            var crossObjs = SpatialIndex.SelectCrossingPolygon(roofFloorBoundary);
            return Buckets.Where(o => crossObjs.Contains(o.Outline)).Where(o =>
            {
                var block = o.Outline as BlockReference;
                var bufferObjs = block.GeometricExtents.ToNTSPolygon().Buffer(-10.0).ToDbCollection();
                return roofFloorBoundary.Contains(bufferObjs[0] as Curve);
            }).ToList();
        }
    }
}
