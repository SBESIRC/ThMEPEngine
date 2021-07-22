using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON.Features
{
    public class ThIfcRailingFeature
    {
        public static Feature Construct(ThIfcRailing railing)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = railing.Outline,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, railing.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.NamePropertyName, railing.Name},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Railing.ToString()},
                }
            };
            return feature.ToFeature();
        }
    }
}
