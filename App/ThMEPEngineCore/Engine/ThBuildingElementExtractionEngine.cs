using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThRawIfcBuildingElementData
    {
        public object Data { get; set; }
        public Entity Geometry { get; set; }
    }

    public abstract class ThBuildingElementExtractionEngine
    {
        public List<ThRawIfcBuildingElementData> Results { get; protected set; }

        public ThBuildingElementExtractionEngine()
        {
            Results = new List<ThRawIfcBuildingElementData>();
        }

        public void Extract(Database database, Polyline frame)
        {
            Extract(database);
            if (Results.Count > 0)
            {
                var geometries = Results.Select(o => o.Geometry).ToCollection();
                var spatialIndex = new ThCADCoreNTSSpatialIndex(geometries);
                var selections = spatialIndex.SelectCrossingPolygon(frame);
                Results.RemoveAll(o => !selections.Contains(o.Geometry));
            }
        }

        public abstract void Extract(Database database);
    }
}
