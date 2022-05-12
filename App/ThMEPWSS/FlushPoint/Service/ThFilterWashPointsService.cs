using System.Linq;
using ThMEPWSS.FlushPoint.Data;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThFilterWashPointsService
    {
        #region ---------- input ----------        
        public List<Entity> Rooms { get; set; }
        public double NearbyDistance { get; set; }
        public List<Entity> CollectingWells { get; set; }
        public List<Entity> DrainageDitches { get; set; }
        #endregion
        #region ---------- output ----------
        public WashPtLayoutInfo LayoutInfo { get; set; }
        #endregion
        public ThFilterWashPointsService()
        {
            Rooms = new List<Entity>();
            LayoutInfo = new WashPtLayoutInfo();            
            CollectingWells = new List<Entity>();
            DrainageDitches = new List<Entity>();
        }
        public void Filter(List<Point3d> washPoints)
        {
            var drainageDitchService = new ThDrainageDitchNearbyService(
                DrainageDitches, Rooms, NearbyDistance);
            var collectWellService = new ThCollectingWellNearbyService(
                CollectingWells.Cast<Polyline>().ToList(), Rooms, NearbyDistance);
            LayoutInfo.NearbyPoints = washPoints.Where(o => drainageDitchService.Find(o) || collectWellService.Find(o)).ToList();
            LayoutInfo.FarawayPoints = washPoints.Where(o => !LayoutInfo.NearbyPoints.Contains(o)).ToList();
        }
    }
}
