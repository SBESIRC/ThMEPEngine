using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofDeviceFloorGravityWaterBucketService
    {
        private ThIfcSpace Space { get; set; }
        private List<ThIfcGravityWaterBucket> Buckets { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThRoofDeviceFloorGravityWaterBucketService(
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
            var service = new ThRoofDeviceFloorGravityWaterBucketService(space, buckets);
            return service.Find();
        }
        private List<ThIfcGravityWaterBucket> Find()
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
