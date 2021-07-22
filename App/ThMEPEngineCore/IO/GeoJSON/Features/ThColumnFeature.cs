using ThMEPEngineCore.Model;
using NetTopologySuite.Features;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThColumnFeature
    {
        public static Feature Construct(ThIfcColumn column)
        {
            var feature = new ThGeoJSONFeature()
            {
                Geometry = column.Outline as Curve,
                Attributes = new Dictionary<string, object>
                {
                    {ThExtractorPropertyNameManager.IdPropertyName, column.Uuid},
                    {ThExtractorPropertyNameManager.ParentIdPropertyName, ""},
                    {ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Column.ToString()},
                    {ThExtractorPropertyNameManager.NamePropertyName, column.Name},
                    {ThExtractorPropertyNameManager.SpecPropertyName, column.Spec},                    
                    {ThExtractorPropertyNameManager.IsolatePropertyName, false},
                }
            };
            return feature.ToFeature();
        }
    }
}
