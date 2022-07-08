using System;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Runtime;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;

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
        WEAKCURRENT = 1,
        /// <summary>
        /// 强电设备
        /// </summary>
        STRONGCURRENT = 2,
        /// <summary>
        /// 全部
        /// </summary>
        ALL = WEAKCURRENT | STRONGCURRENT,
    }

    /// <summary>
    /// 转换专业
    /// </summary>
    public enum ConvertCategory
    {
        /// <summary>
        /// 给排水
        /// </summary>
        WSS = 1,

        /// <summary>
        /// 暖通
        /// </summary>
        HVAC = 2,

        /// <summary>
        /// 所有
        /// </summary>
        ALL = WSS | HVAC,
    }

    public static class ThBConvertUtils
    {
        /// <summary>
        /// 负载编号：“设备符号&-&楼层-编号”
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static string LoadSN(ThBlockReferenceData blockReference)
        {
            string name = blockReference.StringValue(ThBConvertCommon.PROPERTY_EQUIPMENT_SYMBOL);
            if (string.IsNullOrEmpty(name))
            {
                name = blockReference.StringValue(ThBConvertCommon.PROPERTY_FAN_TYPE);
            }
            return string.Format("{0}-{1}", name, blockReference.StringValue(ThBConvertCommon.PROPERTY_STOREY_AND_NUMBER));
        }

        /// <summary>
        /// 从天华MEP风机模型中提取电量
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static string LoadPowerFromTHModel(ThBlockReferenceData blockReference)
        {
            // "电量"属性值中“电量：”后的字符 , 例: 5.5kW 或 5.5/2.2kW
            var value = blockReference.StringValue(ThBConvertCommon.PROPERTY_POWER_QUANTITY);
            Match match = Regex.Match(value, @"^(电量[:：])(.+)$");
            if (match.Success)
            {
                return match.Groups[2].Value;
            }
            Match match2 = Regex.Match(value, @"^(.+)$");
            if (match2.Success)
            {
                return match2.Groups[1].Value + "kW";
            }
            return string.Empty;
        }

        /// <summary>
        /// 电量
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static string LoadPower(ThBlockReferenceData blockReference)
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
                // 2.如无，当”电量”的值大于等于0.55时采用默认值380，小于0.55时采用220
                voltage = (quantity >= 0.55) ? 380 : 220;
            }

            // 电压等级默认为380V
            // 当电压等级为默认值时，相应字段可以省略
            if (Math.Abs(voltage - 380.0) < 0.0000001)
            {
                return string.Format("{0}kW", quantity.ToString(), voltage.ToString());
            }
            else
            {
                return string.Format("{0}kW {1}V", quantity.ToString(), voltage.ToString());
            }
        }

        /// <summary>
        /// 负载用途
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static string LoadUsage(ThBlockReferenceData blockReference)
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
                name = ThMEPXRefService.OriginalFromXref(blockReference.EffectiveName);
                Match match = Regex.Match(name, @"^.+([\(（].+[\)）]).*$");
                if (match.Success)
                {
                    name = name.Replace(match.Groups[1].Value as string, "");
                }
            }

            // 定频
            // 1.优先从属性"定频", "变频", "双速"，"变频、双速或定频"中提取
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
                frequency = blockReference.StringValue(ThBConvertCommon.PROPERTY_ALL_FREQUENCY);
            }
            if (string.IsNullOrEmpty(frequency))
            {
                frequency = ThBConvertCommon.PROPERTY_FIXED_FREQUENCY;
            }

            // 设备工作频率默认为定频
            // 当工作频率为默认值时，相应字段可以省略
            if (frequency == ThBConvertCommon.PROPERTY_FIXED_FREQUENCY)
            {
                return string.Format("{0}", name, frequency);
            }
            else
            {
                return string.Format("{0}({1})", name, frequency);
            }
        }

        /// <summary>
        /// 是否为消防电源
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static bool IsFirePower(ThBlockReferenceData blockReference)
        {
            string name = blockReference.StringValue(ThBConvertCommon.PROPERTY_FIRE_POWER_SUPPLY);
            if (string.IsNullOrEmpty(name))
            {
                name = blockReference.StringValue(ThBConvertCommon.PROPERTY_FIRE_POWER_SUPPLY2);
            }
            return name == ThBConvertCommon.PROPERTY_VALUE_FIRE_POWER;
        }

        /// <summary>
        /// 获取属性（字符串）
        /// </summary>
        /// <param name="blockReference"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string StringValue(this ThBlockReferenceData blockReference, string name)
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

        public static void InsertRevcloud(Database database, Polyline obb, short colorIndex, string linetype, double scale)
        {
            using (var db = AcadDatabase.Use(database))
            {
                // 创建云线
                var layerId = db.Database.CreateAILayer("AI-圈注", colorIndex);
                ObjectId revcloud = ObjectId.Null;
                var buffer = obb.Buffer(300.0 * scale);
                var objId = db.ModelSpace.Add(buffer[0] as Entity);
                void handler(object s, ObjectEventArgs e)
                {
                    if (e.DBObject is Polyline polyline)
                    {
                        revcloud = e.DBObject.ObjectId;
                    }
                }
                db.Database.ObjectAppended += handler;
#if ACAD_ABOVE_2014
                Active.Editor.Command("_.REVCLOUD", "_arc", 300, 300, "_Object", objId, "_No");
#else
                    ResultBuffer args = new ResultBuffer(
                       new TypedValue((int)LispDataType.Text, "_.REVCLOUD"),
                       new TypedValue((int)LispDataType.Text, "_ARC"),
                       new TypedValue((int)LispDataType.Text, "300"),
                       new TypedValue((int)LispDataType.Text, "300"),
                       new TypedValue((int)LispDataType.Text, "_Object"),
                       new TypedValue((int)LispDataType.ObjectId, objId),
                       new TypedValue((int)LispDataType.Text, "_No"));
                    Active.Editor.AcedCmd(args);
#endif
                db.Database.ObjectAppended -= handler;

                // 设置运行属性
                var revcloudObj = db.Element<Entity>(revcloud, true);
                revcloudObj.LayerId = layerId;
                revcloudObj.Linetype = linetype;
                revcloudObj.ColorIndex = (int)ColorIndex.BYLAYER;
            }
        }
    }
}
