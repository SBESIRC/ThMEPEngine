﻿using System.Linq;
using ThMEPWSS.FlushPoint.Data;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThFilterWashPointsService
    {
        public ThDrainFacilityExtractor DrainFacilityExtractor { get; set; }
        public WashPtLayoutInfo LayoutInfo { get; set; }
        public List<Entity> Rooms { get; set; }
        private double NearbyDistance { get; set; }
        public ThFilterWashPointsService(double nearbyDistance)
        {
            Rooms = new List<Entity>();
            LayoutInfo = new WashPtLayoutInfo();
            NearbyDistance = nearbyDistance;
            DrainFacilityExtractor = new ThDrainFacilityExtractor();
        }
        public void Filter(List<Point3d> washPoints)
        {
            var drainageDitchService = new ThDrainageDitchNearbyService(
                DrainFacilityExtractor.DrainageDitches, Rooms, NearbyDistance);
            var collectWellService = new ThCollectingWellNearbyService(
                DrainFacilityExtractor.CollectingWells.Cast<Polyline>().ToList(), Rooms, NearbyDistance);
            LayoutInfo.NearbyPoints = washPoints.Where(o => drainageDitchService.Find(o) || collectWellService.Find(o)).ToList();
            LayoutInfo.FarawayPoints = washPoints.Where(o => !LayoutInfo.NearbyPoints.Contains(o)).ToList();
        }
    }
}
