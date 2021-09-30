using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;

namespace ThMEPWSS.Hydrant.Data
{
    public class ThHydrantDoorOpeningExtractor : ThDoorOpeningExtractor
    {
        private const double EnlargeTolerance = 5.0;
        private const double MinimumAreaTolerance = 100.0;

        public override void Extract(Database database, Point3dCollection pts)
        {
            var doors = new DBObjectCollection();
            var doorEngine = new ThDB3DoorRecognitionEngine();
            doorEngine.Recognize(database, pts);
            doorEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ForEach(o => doors.Add(o));
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database, pts);
            instance.Polys.ForEach(o=> doors.Add(o));
            if (FilterMode == FilterMode.Window)
            {
                doors = FilterWindowPolygon(pts, doors.Cast<Entity>().ToList()).ToCollection();
            }
            doors = ThCADCoreNTSGeometryFilter.GeometryEquality(doors);
            doors = doors.FilterSmallArea(MinimumAreaTolerance);
            doors = doors.Cast<Polyline>().Select(o => o.GetMinimumRectangle()).ToCollection();
            doors = doors.UnionPolygons();
            doors = doors.FilterSmallArea(MinimumAreaTolerance);
            Doors.AddRange(doors.Cast<Polyline>().Select(o => o.GetMinimumRectangle()).ToList());
        }

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
