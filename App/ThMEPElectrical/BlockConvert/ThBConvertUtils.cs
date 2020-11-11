using System;
using System.Text.RegularExpressions;

namespace ThMEPElectrical.BlockConvert
{
    /// <summary>
    /// 转换模式
    /// </summary>
    public enum ConvertMode
    {
        /// <summary>
        /// 弱电设备
        /// </summary>
        WEAKCURRENT = 0,
        /// <summary>
        /// 强电设备
        /// </summary>
        STRONGCURRENT = 1,
    }

    public static class ThBConvertUtils
    {
        /// <summary>
        /// 负载编号：“设备符号&-&楼层-编号”
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static string LoadSN(ThBConvertBlockReference blockReference)
        {
            string name = blockReference.StringValue(ThBConvertCommon.PROPERTY_EQUIPMENT_SYMBOL);
            if (string.IsNullOrEmpty(name))
            {
                name = blockReference.StringValue(ThBConvertCommon.PROPERTY_FAN_TYPE);
            }
            return string.Format("{0}-{1}", name, blockReference.StringValue(ThBConvertCommon.PROPERTY_STOREY_AND_NUMBER));
        }

        /// <summary>
        /// 电量
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static string LoadPower(ThBConvertBlockReference blockReference)
        {
            // 电量
            // 1.优先从属性"电量"提取（仅数字），属性内值可能为：数字+字母
            var value = blockReference.StringValue(ThBConvertCommon.PROPERTY_POWER_QUANTITY);
            if (!double.TryParse(value, out double quantity))
            {
                // 2.如无，则从块可见性中文字"电量：*"中提取数字
                var texts = blockReference.VisibleTexts();
                foreach (var text in texts)
                {
                    Match match = Regex.Match(text, @"^(电量[:：])([+-]?[0-9]+(?:\.[0-9]*)?)([kK][wW])$");
                    if (match.Success)
                    {
                        quantity = double.Parse(match.Groups[2].Value);
                    }
                }
            }

            // 电压
            // 1.优先从属性"电压"提取（仅数字），属性内值可能为：数字 + 字母
            value = blockReference.StringValue(ThBConvertCommon.PROPERTY_POWER_VOLTAGE);
            if (!double.TryParse(value, out double voltage))
            {
                // 2.如无，则采用默认值380
                voltage = ThBConvertCommon.default_voltage;
            }

            return string.Format("{0}kW {1}V", quantity.ToString(), voltage.ToString());
        }

        /// <summary>
        /// 负载用途
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static string LoadUsage(ThBConvertBlockReference blockReference)
        {
            // 设备名称
            // 1.优先从属性"设备名称", "风机功能"提取
            string name = blockReference.StringValue(ThBConvertCommon.PROPERTY_EQUIPMENT_NAME);
            if (string.IsNullOrEmpty(name))
            {
                name = blockReference.StringValue(ThBConvertCommon.PROPERTY_FAN_USAGE);
            }
            if (string.IsNullOrEmpty(name))
            {
                // 2.如无，则采用块名，需要删除括号内值（括号不分中英文）
                name = blockReference.EffectiveName;
                Match match = Regex.Match(name, @"^.+([\(（].+[\)）]).*$");
                if (match.Success)
                {
                    name = name.Replace(match.Groups[1].Value as string, "");
                }
            }

            // 定频
            // 1.优先从属性"定频", "变频", "双速"中提取
            // 2.如无，则采用默认值定频
            string frequency = blockReference.StringValue(ThBConvertCommon.PROPERTY_FIXED_FREQUENCY);
            if (string.IsNullOrEmpty(frequency))
            {
                frequency = blockReference.StringValue(ThBConvertCommon.PROPERTY_VARIABLE_FREQUENCY);
            }
            if (string.IsNullOrEmpty(frequency))
            {
                frequency = blockReference.StringValue(ThBConvertCommon.PROPERTY_DUAL_FREQUENCY);
            }
            if (string.IsNullOrEmpty(frequency))
            {
                frequency = ThBConvertCommon.PROPERTY_FIXED_FREQUENCY;
            }

            return string.Format("{0}({1})", name, frequency);
        }

        /// <summary>
        /// 获取属性（字符串）
        /// </summary>
        /// <param name="blockReference"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string StringValue(this ThBConvertBlockReference blockReference, string name)
        {
            try
            {
                return blockReference.Attributes[name] as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取转换规则的值（字符串）
        /// </summary>
        /// <param name="block"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string StringValue(this ThBlockConvertBlock block, string name)
        {
            try
            {
                return block.Attributes[name] as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 是否为消防电源
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static bool IsFirePowerSupply(this ThBConvertBlockReference blockReference)
        {
            return blockReference.StringValue(ThBConvertCommon.PROPERTY_FIRE_POWER_SUPPLY) == "消防电源";
        }
        
        /// <summary>
        /// 块的转换比例
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public static double Scale(this ThBlockConvertBlock block)
        {
            try
            {
                return Convert.ToDouble(block.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_SCALE]);
            }
            catch
            {
                return 1.0;
            }
        }
    }
}
