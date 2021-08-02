using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThDoorFeature
    {
        public static Feature Construct(ThIfcDoor door)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = door.Outline,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, door.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Door.ToString()},
                    {ThExtractorPropertyNameManager.NamePropertyName, door.Name},
                    {ThExtractorPropertyNameManager.SpecPropertyName, door.Spec},
                    {ThExtractorPropertyNameManager.SwitchPropertyName, door.Switch},
                    {ThExtractorPropertyNameManager.UseagePropertyName, door.Useage},
                    {ThExtractorPropertyNameManager.AnglePropertyName, door.OpenAngle},
                    {ThExtractorPropertyNameManager.HeightPropertyName, door.Height},
                }
            };
            return feature.ToFeature();
        }
    }
}
