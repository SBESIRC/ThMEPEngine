using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThShearWallFeatureCollection
    {
        public static FeatureCollection Construct(List<ThIfcWall> walls)
        {
            var features = new FeatureCollection();
            walls.ForEach(o => features.Add(ThShearWallFeature.Construct(o)));
            return features;
        }
    }
}
