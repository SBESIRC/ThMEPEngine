using System.Linq;
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
        public ThFilterWashPointsService()
        {
            LayoutInfo = new WashPtLayoutInfo();
            DrainFacilityExtractor = new ThDrainFacilityExtractor();
        }
        public void Filter(List<Point3d> washPoints)
        {
            var drainageDitchService = new ThDrainageDitchNearbyService(DrainFacilityExtractor.DrainageDitches);
            var collectWellService = new ThCollectingWellNearbyService(
                DrainFacilityExtractor.CollectingWells.Cast<Polyline>().ToList());
            LayoutInfo.NearbyPoints = washPoints.Where(o => drainageDitchService.Find(o) || collectWellService.Find(o)).ToList();
            LayoutInfo.FarawayPoints = washPoints.Where(o => !LayoutInfo.NearbyPoints.Contains(o)).ToList();
        }
    }
}
