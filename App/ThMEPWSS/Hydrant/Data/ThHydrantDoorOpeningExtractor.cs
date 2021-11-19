using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPWSS.OutsideFrameRecognition;

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

        public void FilterOuterDoors(List<Entity> rooms,List<Polyline> outsideFrames)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(rooms.ToCollection());
            var outsideFrameSpatialIndex = new ThCADCoreNTSSpatialIndex(outsideFrames.ToCollection());
            Doors = Doors.Where(o =>
            {
                var neighbors = spatialIndex.SelectCrossingPolygon(Buffer(o));
                if (neighbors.Count > 1)
                {
                    return true;
                }
                else
                {
                    if (outsideFrameSpatialIndex.SelectCrossingPolygon(o).Count>0)
                    {
                        return true;
                    }
                    foreach (var frame in outsideFrames)
                    {
                        if (frame.Contains(o.GetCenter()))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }).ToList();
        }

        private Polyline Buffer(Polyline door)
        {
            var objs = door.Buffer(EnlargeTolerance);
            return objs.Cast<Polyline>().OrderByDescending(o => o.Area).First();
        }
    }
}