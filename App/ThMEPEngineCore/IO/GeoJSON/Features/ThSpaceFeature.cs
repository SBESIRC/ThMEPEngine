using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThSpaceFeature
    {
        public static Feature Construct(ThIfcSpace space)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = space.Boundary,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, space.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Space.ToString()},
                    {ThExtractorPropertyNameManager.NamePropertyName, space.Name},
                    {ThExtractorPropertyNameManager.UseagePropertyName, space.Useage},
                }
            };
            return feature.ToFeature();
        }
    }
}
