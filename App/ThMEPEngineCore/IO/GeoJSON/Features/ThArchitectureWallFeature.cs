using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThArchitectureWallFeature
    {
        public static Feature Construct(ThIfcWall wall)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = wall.Outline,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, wall.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.ArchitectureWall.ToString()},
                    {ThExtractorPropertyNameManager.NamePropertyName, wall.Name},
                    {ThExtractorPropertyNameManager.SpecPropertyName, wall.Spec},
                    {ThExtractorPropertyNameManager.IsolatePropertyName, false},
                }
            };
            return feature.ToFeature();
        }
    }
}
