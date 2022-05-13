using System;
using System.Collections.Generic;
using ThMEPEngineCore.IO.SVG;

namespace ThMEPStructure.ArchiecturePlane.Service
{
    internal static class ThArchPlaneUtils
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
