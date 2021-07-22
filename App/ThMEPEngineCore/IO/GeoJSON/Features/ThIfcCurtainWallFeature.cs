using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON.Features
{
    public class ThIfcCurtainWallFeature
    {
        public static Feature Construct(ThIfcCurtainWall wall)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = wall.Outline,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, wall.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.CurtainWall.ToString()},
                    {ThExtractorPropertyNameManager.NamePropertyName, wall.Name},
                    {ThExtractorPropertyNameManager.IsolatePropertyName, false},
                }
            };
            return feature.ToFeature();
        }
    }
}
