using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace TianHua.FanSelection.Function
{
    public class ThFanSelectionUtils
    {
        /// <summary>
        /// 属性“设备符号”值
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="installSpace"></param>
        /// <returns></returns>
        public static string Symbol(string scenario, string installSpace)
        {
            if (string.IsNullOrEmpty(installSpace) || installSpace == "未指定子项")
            {
                return ScenarioPrefix(scenario);
            }
            else
            {
                return string.Format("{0}-{1}", ScenarioPrefix(scenario), installSpace);
            }
        }

        /// <summary>
        /// 属性“楼层-编号”值
        /// </summary>
        public static string StoreyNumber(string storey, string number)
        {
            return string.Format("{0}-{1}", storey, number);
        }

        /// <summary>
        /// 属性"安装方式"值
        /// </summary>
        /// <param name="mount"></param>
        /// <returns></returns>
        public static string Mount(string mount)
        {
            return string.Format("安装方式：{0}", mount);
        }

        /// <summary>
        /// 属性“风量”值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string AirVolume(string description)
        {
            return string.Format("风量：{0} cmh", description);
        }

        /// <summary>
        /// 属性“全压”值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string WindResis(string description)
        {
            return string.Format("全压：{0} Pa", description);
        }

        /// <summary>
        /// 属性“电量”值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string MotorPower(string description)
        {
            return string.Format("电量：{0} kW", description);
        }

        /// <summary>
        /// 属性“变频”值
        /// </summary>
        /// <param name="control"></param>
        /// <param name="fre"></param>
        /// <returns></returns>
        public static string FixedFrequency(string control, bool fre)
        {
            if (fre)
            {
                if (control == ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_SINGLE_SPEED)
                {
                    return ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_VARIABLE_FREQUENCY;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                if (control == ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_SINGLE_SPEED)
                {
                    return ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_FIXED_FREQUENCY;
                }
                else
                {
                    return ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_DOUBLE_SPEED;
                }
            }
        }

        /// <summary>
        /// 属性“消防电源”值
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string FirePower(string type)
        {
            if (type == "消防")
            {
                return ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_FIRE_POWER;
            }
            else
            {
                return ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_NON_FIRE_POWER;
            }
        }

        /// <summary>
        /// 设备符号前缀
        /// </summary>
        /// <param name="scenario"></param>
        /// <returns></returns>
        public static string ScenarioPrefix(string scenario)
        {
            return PubVar.g_ListFanPrefixDict.Where(o => o.FanUse == scenario).First().Prefix;
        }

        /// <summary>
        /// 匹配模型名称和可见性名称
        /// </summary>
        /// <param name="model"></param>
        /// <param name="visibility"></param>
        /// <returns></returns>
        public static bool MatchModelName(string model, string visibility)
        {
            // 规则1：去掉模型名称最后面的字母
            return visibility == model.Substring(0, model.Length - 1);
        }

        /// <summary>
        /// 规格及型号（轴流风机）
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="modelName"></param>
        /// <returns></returns>
        public static string AXIALModelName(string model, string mount)
        {
            // 寻找最后一个"-"
            string modelName = string.Empty;
            int index = model.LastIndexOf('-');
            if (index == -1)
            {
                modelName = model;
            }
            else
            {
                // "数字+字母"的形式
                string suffix = model.Substring(index + 1, model.Length - index -1);
                Match match = Regex.Match(suffix, @"^([0-9]*(?:\.[0-9]*)?)([A-Z]+)$");
                if (match.Success)
                {
                    modelName = model.Substring(0, index + 1) + match.Groups[1].Value;
                }
                else
                {
                    modelName = model;
                }
            }
            if (mount == ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_HOIST)
            {
                return string.Format("{0}（{1}）", modelName, ThFanSelectionCommon.AXIAL_MODEL_NAME_SUFFIX);
            }
            else
            {
                return modelName;
            }
        }

        /// <summary>
        /// 规格及型号（离心风机）
        /// </summary>
        /// <param name="style"></param>
        /// <param name="airflow"></param>
        /// <param name="mount"></param>
        /// <returns></returns>
        public static string HTFCModelName(string style, string airflow, string modelNumber)
        {
            return string.Format("{0} {1}#{2}", style.Substring(0, 2), modelNumber, airflow);
        }

        /// <summary>
        /// 是否为离心风机
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public static bool IsHTFCModelStyle(string style)
        {
            return style.Contains(ThFanSelectionCommon.HTFC_TYPE_NAME);
        }

        /// <summary>
        /// 是否为后倾离心风机
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        public static bool IsHTFCBackwardModelStyle(string style)
        {
            return IsHTFCModelStyle(style) && 
                style.Contains(ThFanSelectionCommon.HTFC_BACKWARD_NAME);
        }

        /// <summary>
        /// 获取离心风机图块名
        /// </summary>
        /// <param name="style"></param>
        /// <param name="airflow"></param>
        /// <param name="mount"></param>
        /// <returns></returns>
        public static string HTFCBlockName(string style, string airflow, string mount)
        {
            switch (style)
            {
                case "前倾离心(电机外置)":
                case "后倾离心(电机外置)":
                    {
                        switch (airflow)
                        {
                            case "直进直出":
                                {
                                    switch (mount)
                                    {
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_FLOOR:
                                            return "离心风机(电机外置、直进直出、有基础)";
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_HOIST:
                                            return "离心风机(电机外置、直进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case "侧进直出":
                                {
                                    switch (mount)
                                    {
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_FLOOR:
                                            return "离心风机(电机外置、侧进直出、有基础)";
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_HOIST:
                                            return "离心风机(电机外置、侧进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case "上进直出":
                                {
                                    switch (mount)
                                    {
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_FLOOR:
                                            return "离心风机(电机外置、上进直出、有基础)";
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_HOIST:
                                            return "离心风机(电机外置、上进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case "直进上出":
                                {
                                    switch (mount)
                                    {
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_FLOOR:
                                            return "离心风机(电机外置、直进上出、有基础)";
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_HOIST:
                                            return "离心风机(电机外置、直进上出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case "直进下出":
                                {
                                    switch (mount)
                                    {
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_FLOOR:
                                            return "离心风机(电机外置、直进下出、有基础)";
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_HOIST:
                                            return "离心风机(电机外置、直进下出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case "下进直出":
                                {
                                    switch (mount)
                                    {
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_FLOOR:
                                            return "离心风机(电机外置、下进直出、有基础)";
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_HOIST:
                                            return "离心风机(电机外置、下进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            default:
                                throw new NotSupportedException();
                        }
                    }
                case "前倾离心(电机内置)":
                case "后倾离心(电机内置)":
                    {
                        switch (airflow)
                        {
                            case "直进直出":
                                {
                                    switch (mount)
                                    {
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_FLOOR:
                                            return "离心风机(电机内置、直进直出、有基础)";
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_HOIST:
                                            return "离心风机(电机内置、直进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case "侧进直出":
                                {
                                    switch (mount)
                                    {
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_FLOOR:
                                            return "离心风机(电机内置、侧进直出、有基础)";
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_HOIST:
                                            return "离心风机(电机内置、侧进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case "上进直出":
                                {
                                    switch (mount)
                                    {
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_FLOOR:
                                            return "离心风机(电机内置、上进直出、有基础)";
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_HOIST:
                                            return "离心风机(电机内置、上进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case "直进上出":
                                {
                                    switch (mount)
                                    {
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_FLOOR:
                                            return "离心风机(电机内置、直进上出、有基础)";
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_HOIST:
                                            return "离心风机(电机内置、直进上出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case "直进下出":
                                {
                                    switch (mount)
                                    {
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_FLOOR:
                                            return "离心风机(电机内置、直进下出、有基础)";
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_HOIST:
                                            return "离心风机(电机内置、直进下出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            case "下进直出":
                                {
                                    switch (mount)
                                    {
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_FLOOR:
                                            return "离心风机(电机内置、下进直出、有基础)";
                                        case ThFanSelectionCommon.BLOCK_ATTRIBUTE_VALUE_MOUNT_HOIST:
                                            return "离心风机(电机内置、下进直出、无基础)";
                                        default:
                                            throw new NotSupportedException();
                                    }
                                }
                            default:
                                throw new NotSupportedException();
                        }
                    }
                default:
                    throw new NotSupportedException();
            }
        }
    }
}