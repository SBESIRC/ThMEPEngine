using System;
using System.Data;
using System.Collections.Generic;

using ThMEPEngineCore.IO.ExcelService;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertRuleEngine
    {
        /// <summary>
        /// 从Excel表中获取转换规则
        /// </summary>
        /// <param name="bConvertConfigUrl"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public List<ThBConvertRule> Acquire(string bConvertConfigUrl, ConvertMode mode)
        {
            var rules = new List<ThBConvertRule>();
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(bConvertConfigUrl, true);
            if ((mode & ConvertMode.STRONGCURRENT) == ConvertMode.STRONGCURRENT)
            {
                var table = dataSet.Tables[ThBConvertCommon.STRONG_CURRENT];
                rules.AddRange(Rules(table, ConvertMode.STRONGCURRENT));
            }
            if ((mode & ConvertMode.WEAKCURRENT) == ConvertMode.WEAKCURRENT)
            {
                var table = dataSet.Tables[ThBConvertCommon.WEAK_CURRENT];
                rules.AddRange(Rules(table, ConvertMode.WEAKCURRENT));
            }
            return rules;
        }

        /// <summary>
        /// 从TCHExcel表中获取转换规则
        /// </summary>
        /// <param name="tchConvertConfigUrl"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public List<ThBConvertRule> TCHAcquire(string tchConvertConfigUrl, ConvertMode mode)
        {
            var rules = new List<ThBConvertRule>();
            var excelSrevice = new ReadExcelService();
            var dataSet = excelSrevice.ReadExcelToDataSet(tchConvertConfigUrl, true);
            if ((mode & ConvertMode.STRONGCURRENT) == ConvertMode.STRONGCURRENT)
            {
                var table = dataSet.Tables[ThBConvertCommon.STRONG_CURRENT];
                rules.AddRange(TCHRules(table, ConvertMode.STRONGCURRENT));
            }
            if ((mode & ConvertMode.WEAKCURRENT) == ConvertMode.WEAKCURRENT)
            {
                var table = dataSet.Tables[ThBConvertCommon.WEAK_CURRENT];
                rules.AddRange(TCHRules(table, ConvertMode.WEAKCURRENT));
            }
            return rules;
        }

        /// <summary>
        /// 获取表中的块转换映射表
        /// </summary>
        /// <param name="table"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public List<ThBConvertRule> Rules(DataTable table, ConvertMode mode)
        {
            var rules = new List<ThBConvertRule>();
            for (int row = 0; row < table.Rows.Count; row++)
            {
                var column = 0;
                var blockName = StringFilter(table.Rows[row][column].ToString());

                // 过滤空行
                if (string.IsNullOrEmpty(blockName))
                {
                    continue;
                }

                var source = new ThBlockConvertBlock()
                {
                    Attributes = new Dictionary<string, object>(),
                };

                // 源块名
                source.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] = blockName;

                // 可见性
                column++;
                source.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY] = StringFilter(table.Rows[row][column].ToString());

                // 源块计算模式
                column++;
                source.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_POSITION_MODE] =
                    (ThBConvertInsertMode)Enum.Parse(typeof(ThBConvertInsertMode), StringFilter(table.Rows[row][column].ToString()));

                // 源块外形图层
                column++;
                source.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_GEOMETRY_LAYER] = StringFilter(table.Rows[row][column].ToString());

                var target = CreateTarget(table, column, row);

                // 创建映射规则
                rules.Add(new ThBConvertRule()
                {
                    Mode = mode,
                    Transformation = new Tuple<ThBlockConvertBlock, ThBlockConvertBlock>(source, target),
                });
            }
            return rules;
        }

        public List<ThBConvertRule> TCHRules(DataTable table, ConvertMode mode)
        {
            var rules = new List<ThBConvertRule>();
            for (var row = 0; row < table.Rows.Count; row++)
            {
                var column = 0;
                var tchType = StringFilter(table.Rows[row][column].ToString());

                // 过滤空行
                if (string.IsNullOrEmpty(tchType))
                {
                    continue;
                }

                var source = new ThBlockConvertBlock()
                {
                    Attributes = new Dictionary<string, object>(),
                };

                // 类型
                source.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_TCH_TYPE] = tchType;

                // 型号
                column++;
                source.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_TCH_MODEL] = StringFilter(table.Rows[row][column].ToString());

                var target = CreateTarget(table, column, row);

                // 创建映射规则
                rules.Add(new ThBConvertRule()
                {
                    Mode = mode,
                    Transformation = new Tuple<ThBlockConvertBlock, ThBlockConvertBlock>(source, target),
                });
            }
            return rules;
        }

        private ThBlockConvertBlock CreateTarget(DataTable table, int column, int row)
        {
            var target = new ThBlockConvertBlock()
            {
                Attributes = new Dictionary<string, object>(),
            };

            // 目标块名
            column++;
            target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] = StringFilter(table.Rows[row][column].ToString());

            // 目标图层
            column++;
            target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_LAYER] = StringFilter(table.Rows[row][column].ToString());

            // 目标块计算模式
            column++;
            target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_POSITION_MODE] =
                (ThBConvertInsertMode)Enum.Parse(typeof(ThBConvertInsertMode), StringFilter(table.Rows[row][column].ToString()));

            // 目标块外形图层
            column++;
            target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_GEOMETRY_LAYER] = StringFilter(table.Rows[row][column].ToString());

            // 是否炸开
            column++;
            target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_EXPLODE] =
                string.Equals(StringFilter(table.Rows[row][column].ToString()), "1");

            // 内含图块
            column++;
            target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_INTERNAL] = StringFilter(table.Rows[row][column].ToString());

            // 旋转矫正
            column++;
            target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_ROTATION_CORRECT] =
                string.Equals(StringFilter(table.Rows[row][column].ToString()), "1");

            // 来源专业
            column++;
            target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_CATEGORY] = StringFilter(table.Rows[row][column].ToString());

            // 设备类型
            column++;
            target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_EQUIMENT] = StringFilter(table.Rows[row][column].ToString());

            return target;
        }

        private string StringFilter(string str)
        {
            return str.Replace(" ", "").Replace("\n", ""); ;
        }
    }
}
