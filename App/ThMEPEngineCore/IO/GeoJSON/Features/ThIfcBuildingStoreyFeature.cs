using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON.Features
{
    public class ThIfcBuildingStoreyFeature
    {
        public static Feature Construct(ThIfcBuildingStorey storey)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = storey.Boundary,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, storey.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.BuildingStorey.ToString()},
                    {ThExtractorPropertyNameManager.NamePropertyName, storey.Name},
                }
            };
            return feature.ToFeature();
        }
    }
}
