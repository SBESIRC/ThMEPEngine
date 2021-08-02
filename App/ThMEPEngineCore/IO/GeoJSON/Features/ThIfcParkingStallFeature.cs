using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON.Features
{
    public class ThIfcParkingStallFeature
    {
        public static Feature Construct(ThIfcParkingStall parkingStall)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = parkingStall.Boundary,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, parkingStall.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.NamePropertyName, parkingStall.Name},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.ParkingStall.ToString()},
                }
            };
            return feature.ToFeature();
        }
    }
}
