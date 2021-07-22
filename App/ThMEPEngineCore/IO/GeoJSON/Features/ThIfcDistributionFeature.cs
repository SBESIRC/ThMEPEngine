using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThIfcDistributionFeature
    {
        public static Feature Construct(ThIfcDistributionElement distribution)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = distribution.Outline,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, distribution.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Distribution.ToString()},
                    {ThExtractorPropertyNameManager.NamePropertyName, distribution.Name},
                    {ThExtractorPropertyNameManager.SpecPropertyName, distribution.Spec},
                    {ThExtractorPropertyNameManager.UseagePropertyName, distribution.Useage},                    
                }
            };
            return feature.ToFeature();
        }
    }
}
