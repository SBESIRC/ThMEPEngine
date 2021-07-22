using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThBeamFeature
    {
        public static Feature Construct(ThIfcBeam beam)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = beam.Outline,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, beam.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.NamePropertyName, beam.Name},
                    {ThExtractorPropertyNameManager.SpecPropertyName, beam.Spec},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Beam.ToString()},
                    {ThExtractorPropertyNameManager.BottomDistanceToFloorPropertyName, beam.DistanceToFloor},
                    {ThExtractorPropertyNameManager.WidthPropertyName, beam.Width},
                    {ThExtractorPropertyNameManager.HeightPropertyName, beam.Height},
                }
            };            
            return feature.ToFeature();
        }
    }
}
