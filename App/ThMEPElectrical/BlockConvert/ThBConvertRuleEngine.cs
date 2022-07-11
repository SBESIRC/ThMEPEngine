﻿using System;
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
                var source = new ThBlockConvertBlock()
                {
                    Attributes = new Dictionary<string, object>(),
                };

                // 源块名
                var column = 0;
                source.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] = StringFilter(table.Rows[row][column].ToString());

                // 过滤空行
                if (string.IsNullOrEmpty(source.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] as string))
                {
                    continue;
                }

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

                // 位置
                column++;
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_POSITION] = 
                    string.Equals(StringFilter(table.Rows[row][column].ToString()), "1");

                // 负载编号
                column++;
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_LOAD_ID] =
                    string.Equals(StringFilter(table.Rows[row][column].ToString()), "1");

                // 负载电量
                column++;
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_LOAD_POWER] =
                    string.Equals(StringFilter(table.Rows[row][column].ToString()), "1");

                // 负载用途
                column++;
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_LOAD_DESCRIPTION] =
                    string.Equals(StringFilter(table.Rows[row][column].ToString()), "1");

                // 主备关系
                column++;
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_RELATIONSHIP] =
                    string.Equals(StringFilter(table.Rows[row][column].ToString()), "1");

                // 电源类别
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_POWER_SUPPLY] =
                    string.Equals(StringFilter(table.Rows[row][column].ToString()), "1");

                // 创建映射规则
                rules.Add(new ThBConvertRule()
                {
                    Mode = mode,
                    Transformation = new Tuple<ThBlockConvertBlock, ThBlockConvertBlock>(source, target),
                });
            }
            return rules;
        }

        private string StringFilter(string str)
        {
            return str.Replace(" ", "").Replace("\n", ""); ;
        }
    }
}
