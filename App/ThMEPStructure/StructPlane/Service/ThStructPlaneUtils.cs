using System.Collections.Generic;
using ThMEPEngineCore.IO.SVG;

namespace ThMEPStructure.StructPlane.Service
{
    internal static class ThStructPlaneUtils
    {
        public static string GetFillColor(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.FillColorPropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }
        public static string GetLineType(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.LineTypePropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }
        public static string GetCategory(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.CategoryPropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }
        public static double GetFloorBottomElevation(this Dictionary<string, string> properties)
        {
            if(properties.ContainsKey(ThSvgPropertyNameManager.FloorBottomElevationPropertyName))
            {
                var value = properties[ThSvgPropertyNameManager.FloorBottomElevationPropertyName];
                double dValue = 0.0;
                if(double.TryParse(value,out dValue))
                {
                    return dValue;
                }                
            }
            return 0.0;
        }
        public static double GetFloorElevation(this Dictionary<string, string> properties)
        {
            if (properties.ContainsKey(ThSvgPropertyNameManager.FloorElevationPropertyName))
            {
                var value = properties[ThSvgPropertyNameManager.FloorElevationPropertyName];
                double dValue = 0.0;
                if (double.TryParse(value, out dValue))
                {
                    return dValue;
                }
            }
            return 0.0;
        }
        public static string GetSpec(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.SpecPropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }
        public static string GetElevation(this Dictionary<string, object> properties)
        {
            var value = properties.GetPropertyValue(ThSvgPropertyNameManager.ElevationPropertyName);
            if (value == null)
            {
                return "";
            }
            else
            {
                return (string)value;
            }
        }
        private static object GetPropertyValue(this Dictionary<string, object> properties, string key)
        {
            foreach (var item in properties)
            {
                if (item.Key.ToUpper() == key.ToUpper())
                {
                    return item.Value;
                }
            }
            return null;
        }
    }
}
