using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThOpeningFeature
    {
        public static Feature Construct(ThIfcOpening opening)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = opening.Outline,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, opening.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Opening.ToString()},
                    {ThExtractorPropertyNameManager.NamePropertyName, opening.Name},
                    {ThExtractorPropertyNameManager.SpecPropertyName, opening.Spec},
                    {ThExtractorPropertyNameManager.UseagePropertyName, opening.Useage},                    
                    {ThExtractorPropertyNameManager.WidthPropertyName, opening.Width},
                    {ThExtractorPropertyNameManager.HeightPropertyName, opening.Height},
                }
            };
            return feature.ToFeature();
        }
    }
}
