using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofFloorGravityWaterBucketService
    {
        private ThIfcSpace Space { get; set; }
        private List<ThIfcGravityWaterBucket> Buckets { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThRoofFloorGravityWaterBucketService(
            ThIfcSpace space,
            List<ThIfcGravityWaterBucket> buckets)
        {
            Space = space;
            Buckets = buckets;
            var objs = new DBObjectCollection();
            Buckets.ForEach(o => objs.Add(o.Outline));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public static List<ThIfcGravityWaterBucket> Find(
          ThIfcSpace space,
          List<ThIfcGravityWaterBucket> buckets)
        {
            var service = new ThRoofFloorGravityWaterBucketService(space, buckets);
            return service.Find();
        }
        private List<ThIfcGravityWaterBucket> Find()
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
