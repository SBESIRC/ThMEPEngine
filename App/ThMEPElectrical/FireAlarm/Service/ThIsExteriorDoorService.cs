using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.FireAlarm.Service
{
    public class ThIsExteriorDoorService
    {
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThIsExteriorDoorService(List<Entity> rooms)
        {
            var objs = new DBObjectCollection();
            rooms.ForEach(p => objs.Add(p));
            SpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
        }
        public bool IsExteriorDoor(Polyline door)
        {
            var crossObjs = SpatialIndex.SelectCrossingPolygon(door);
            return crossObjs.Count == 1;
        }
    }
}
