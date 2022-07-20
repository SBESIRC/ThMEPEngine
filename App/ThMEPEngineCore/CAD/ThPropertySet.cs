using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ThMEPEngineCore.CAD
{
    public class ThPropertySet
    {
        public string Section { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public ThPropertySet()
        {
            Section = string.Empty;
            Properties = new Dictionary<string, string>();
        }
        public static ThPropertySet CreateWithHyperlink(string hyperlink)
        {
            var propertySet = new ThPropertySet();

            // 首先获取第一个分割符“：”
            int index = hyperlink.IndexOf('：');
            if (index == -1)
            {
                return propertySet;
            }
            propertySet.Section = hyperlink.Substring(0, index);

            // 按分割符“__”分割属性
            var properties = Regex.Split(hyperlink.Substring(index + 1, hyperlink.Length - index - 1), "__");
            foreach(var property in properties)
            {
                var keyValue = Regex.Split(property, "：");
                if(keyValue.Length<2)
                {
                    continue;
                }
                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim();
                if (ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTIES.Contains(key))
                {
                    propertySet.Properties.Add(key, value);
                }
            }

            // 返回属性集
            return propertySet;
        }

        public static ThPropertySet CreateWithHyperlink2(string hyperlink)
        {
            var propertySet = new ThPropertySet();

            // 按分割符“__”分割属性
            var properties = Regex.Split(hyperlink, "__");
            foreach (var property in properties)
            {
                var keyValue = Regex.Split(property, "：");
                if (keyValue.Length < 2)
                {
                    continue;
                }
                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim();
                if (ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTIES.Contains(key))
                {
                    propertySet.Properties.Add(key, value);
                }
            }

            // 返回属性集
            return propertySet;
        }

        /// <summary>
        /// 是否为建筑墙
        /// </summary>
        public bool IsArchWall
        {
            get
            {
                if (Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY)
                    && Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_LAYER))
                {
                    return Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY] == ThMEPEngineCoreCommon.BUILDELEMENT_CATEGORY_WALL
                        && Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_LAYER] == ThMEPEngineCoreCommon.BUILDELEMENT_LAYER_WALL;
                }
                return false;
            }
        }

        /// <summary>
        /// 是否为幕墙
        /// </summary>
        public bool IsCurtainWall
        {
            get
            {
                if (Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY)
                    && Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_LAYER))
                {
                    return Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY] == ThMEPEngineCoreCommon.BUILDELEMENT_CATEGORY_WALL
                        && Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_LAYER] == ThMEPEngineCoreCommon.BUILDELEMENT_LAYER_CURTAIN_WALL;
                }
                return false;
            }
        }

        /// <summary>
        /// 是否为结构填充（柱或者剪力墙）
        /// </summary>
        public bool IsSHatch
        {
            get
            {
                if (Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY)
                    && Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_HATCH_LAYER))
                {
                    return Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY] == ThMEPEngineCoreCommon.BUILDELEMENT_CATEGORY_S_COLUMN
                    && Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_HATCH_LAYER] == ThMEPEngineCoreCommon.BUILDELEMENT_LAYER_STRU_HACH;
                }
                return false;
            }
        }

        /// <summary>
        /// 是否为结构梁
        /// </summary>
        public bool IsSBeam
        {
            get
            {
                if (Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY)
                    && Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_LAYER))
                {
                    return Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY] == ThMEPEngineCoreCommon.BUILDELEMENT_CATEGORY_S_COLUMN
                    && Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_LAYER] == ThMEPEngineCoreCommon.BUILDELEMENT_LAYER_WALL;
                }
                return false;
            }
        }
        /// <summary>
        /// 是否为门
        /// </summary>
        public bool IsDoor
        {
            get
            {
                if (Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY))
                {
                    return Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY] == ThMEPEngineCoreCommon.BUILDELEMENT_CATEGORY_DOOR;
                }
                return false;
            }
        }
        /// <summary>
        /// 是否为窗户
        /// </summary>
        public bool IsWindow
        {
            get
            {
                if (Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY)
                    && Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_LAYER))
                {
                    return Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY] == ThMEPEngineCoreCommon.BUILDELEMENT_CATEGORY_WINDOW;
                        //&& Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_LAYER] == ThMEPEngineCoreCommon.BUILDELEMENT_LAYER_WINDOW
                }
                return false;
            }
        }

        /// <summary>
        /// 是否为楼板
        /// </summary>
        public bool IsSlab
        {
            get
            {
                if (Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY)
                    && Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_LAYER))
                {
                    return Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY] == ThMEPEngineCoreCommon.BUILDELEMENT_CATEGORY_SLAB;
                    // && Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_LAYER] == ThMEPEngineCoreCommon.BUILDELEMENT_LAYER_FLOOR
                }
                return false;
            }
        }

        /// <summary>
        /// 是否为栏杆
        /// </summary>
        public bool IsRailing
        {
            get
            {
                if (Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY)
                    && Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_LAYER))
                {
                    return Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY] == ThMEPEngineCoreCommon.BUILDELEMENT_CATEGORY_RAILING
                        && Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_LAYER] == ThMEPEngineCoreCommon.BUILDELEMENT_LAYER_RAILING;
                }
                return false;
            }
        }

        /// <summary>
        /// 是否为线脚
        /// </summary>
        public bool IsCornice
        {
            get
            {
                if (Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY)
                    && Properties.ContainsKey(ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_LAYER))
                {
                    return Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_CATEGORY] == ThMEPEngineCoreCommon.BUILDELEMENT_CATEGORY_LINEFOOT;
                        //&& Properties[ThMEPEngineCoreCommon.BUILDELEMENT_PROPERTY_LAYER] == ThMEPEngineCoreCommon.BUILDELEMENT_LAYER_FLOOR;
                }
                return false;
            }
        }
    }
}
