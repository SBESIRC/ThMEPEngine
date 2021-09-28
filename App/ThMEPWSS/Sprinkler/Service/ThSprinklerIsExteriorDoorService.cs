using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Service
{
    public class ThSprinklerIsExteriorDoorService
    {
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThSprinklerIsExteriorDoorService(List<Entity> rooms, List<Polyline> holes)
        {
            var objs = new DBObjectCollection();
            rooms.ForEach(p => objs.Add(p));
            holes.ForEach(p => objs.Add(p));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }

        public bool IsExteriorDoor(Polyline door)
        {
            var crossObjs = SpatialIndex.SelectCrossingPolygon(door);
            return crossObjs.Count == 1;
        }
    }
}
