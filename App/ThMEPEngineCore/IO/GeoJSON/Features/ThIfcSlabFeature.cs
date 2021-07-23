using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON.Features
{
    public class ThIfcSlabFeature
    {
        public static Feature Construct(ThIfcSlab site)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = site.Outline,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, site.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Site.ToString()},
                    {ThExtractorPropertyNameManager.NamePropertyName, site.Name},
                    {ThExtractorPropertyNameManager.ThicknessPropertyName, site.Thickness},
                }
            };
            return feature.ToFeature();
        }
    }
}
