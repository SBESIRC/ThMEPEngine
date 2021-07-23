using Newtonsoft.Json;
using NetTopologySuite.IO;
using ThMEPEngineCore.Model;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThShearWallGeoJsonWriter : GeoJsonWriter
    {
        public void Write(List<ThIfcWall> walls, JsonWriter writer)
        {
            Write(ThShearWallFeatureCollection.Construct(walls), writer);
        }
    }
}
