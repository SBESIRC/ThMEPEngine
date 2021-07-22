using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON.Features
{
    public class ThIfcStairFeature
    {
        public static Feature Construct(ThIfcStair stair)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = stair.Outline,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, stair.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Site.ToString()},
                    {ThExtractorPropertyNameManager.NamePropertyName, stair.Name},
                }
            };
            return feature.ToFeature();
        }
    }
}
