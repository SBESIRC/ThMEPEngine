using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Hydrant.Data
{
    public class ThHydrantDoorOpeningExtractor : ThDoorOpeningExtractor
    {
        private const double EnlargeTolerance = 5.0;
        public void FilterOuterDoors(List<Entity> rooms)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(rooms.ToCollection());
            Doors = Doors.Where(o =>
            {
                var neighbors = spatialIndex.SelectCrossingPolygon(Buffer(o));
                return neighbors.Count > 1;                
            }).ToList();
        }
        private Polyline Buffer(Polyline door)
        {
            var objs = door.Buffer(EnlargeTolerance);
            return objs.Cast<Polyline>().OrderByDescending(o => o.Area).First();
        }
    }
}
