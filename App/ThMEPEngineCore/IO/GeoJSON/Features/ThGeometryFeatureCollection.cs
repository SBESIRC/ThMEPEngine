using System.Collections.Generic;
using NetTopologySuite.Features;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThGeometryFeatureCollection
    {
        public static FeatureCollection Construct(List<ThGeometry> geos)
        {
            var features = new FeatureCollection();
            geos.ForEach(o => features.Add(ThGeometryFeature.Construct(o)));
            return features;
        }
    }
}
