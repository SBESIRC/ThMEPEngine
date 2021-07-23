using NetTopologySuite.Features;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThGeometryFeature
    {
        public static Feature Construct(ThGeometry geometry)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = geometry.Boundary,
                Attributes = geometry.Properties,
            };
            return feature.ToFeature();
        }
    }
}
