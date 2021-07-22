using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThRoomFeature
    {
        public static Feature Construct(ThIfcRoom room)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = room.Boundary,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, room.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Room.ToString()},
                    {ThExtractorPropertyNameManager.NamePropertyName, room.Name},
                    {ThExtractorPropertyNameManager.UseagePropertyName, room.Useage},
                }
            };
            return feature.ToFeature();
        }
    }
}
