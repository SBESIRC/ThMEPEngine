using System;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofDeviceFloorGravityWaterBucketService
    {
        private ThIfcSpace Space { get; set; }
        private List<ThWGravityWaterBucket> Buckets { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private ThRoofDeviceFloorGravityWaterBucketService(
            ThIfcSpace space,
            List<ThWGravityWaterBucket> buckets)
        {
            Space = space;
            Buckets = buckets;
            var objs = new DBObjectCollection();
            Buckets.ForEach(o => objs.Add(o.Outline));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public static List<ThWGravityWaterBucket> Find(
          ThIfcSpace space,
          List<ThWGravityWaterBucket> buckets)
        {
            var service = new ThRoofDeviceFloorGravityWaterBucketService(space, buckets);
            return service.Find();
        }
        private List<ThWGravityWaterBucket> Find()
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
