﻿using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ThMEPEngineCore.CAD
{
    public class ThPropertySet
    {
        public string Section { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public ThPropertySet()
        {
            Properties = new Dictionary<string, string>();
        }
        public static ThPropertySet CreateWithHyperlink(string hyperlink)
        {
            var propertySet = new ThPropertySet();

            // 首先获取第一个分割符“：”
            int index = hyperlink.IndexOf('：');
            propertySet.Section = hyperlink.Substring(0, index);

            // 按分割符“__”分割属性
            var properties = Regex.Split(hyperlink.Substring(index + 1, hyperlink.Length - index - 1), "__");
            foreach(var property in properties)
            {
                var keyValue = Regex.Split(property, "：");
                if (ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTIES.Contains(keyValue[0]))
                {
                    propertySet.Properties.Add(keyValue[0], keyValue[1]);
                }
            }

            // 返回属性集
            return propertySet;
        }
    }
}
