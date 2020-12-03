﻿using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Service
{
    public class ThRoofFloorSideEntryWaterBucketService
    {
        private ThIfcSpace Space { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        private List<ThIfcSideEntryWaterBucket> Buckets { get; set; }
        private ThRoofFloorSideEntryWaterBucketService(
            ThIfcSpace space,
            List<ThIfcSideEntryWaterBucket> buckets)
        {
            Space = space;
            Buckets = buckets;
            var objs = new DBObjectCollection();
            Buckets.Select(o => o.Outline).ForEach(o => objs.Add(o));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }

        public static List<ThIfcSideEntryWaterBucket> Find(
            ThIfcSpace space,
            List<ThIfcSideEntryWaterBucket> buckets)
        {
            var service = new ThRoofFloorSideEntryWaterBucketService(space, buckets);
            return service.Find();
        }

        public List<ThIfcSideEntryWaterBucket> Find()
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
