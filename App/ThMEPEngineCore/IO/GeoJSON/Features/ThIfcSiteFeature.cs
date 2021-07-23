using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON.Features
{
    public class ThIfcSiteFeature
    {
        public static Feature Construct(ThIfcSite site)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = site.Boundary,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, site.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Site.ToString()},
                    {ThExtractorPropertyNameManager.NamePropertyName, site.Name},
                    {ThExtractorPropertyNameManager.UseagePropertyName, site.Useage},
                }
            };
            return feature.ToFeature();
        }
    }
}
