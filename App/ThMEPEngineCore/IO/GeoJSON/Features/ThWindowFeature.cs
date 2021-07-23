using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThWindowFeature
    {
        public static Feature Construct(ThIfcWindow window)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = window.Outline,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, window.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.NamePropertyName, window.Name},
                    {ThExtractorPropertyNameManager.SpecPropertyName, window.Spec},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Window.ToString()},
                    {ThExtractorPropertyNameManager.HeightPropertyName, window.Height},
                }
            };            
            return feature.ToFeature();
        }
    }
}
