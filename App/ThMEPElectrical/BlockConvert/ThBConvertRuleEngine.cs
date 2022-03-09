using System;
using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.IO.ExcelService;
using Autodesk.AutoCAD.DatabaseServices;

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
        public List<ThBConvertRule> Rules(System.Data.DataTable table, ConvertMode mode)
        {
            var rules = new List<ThBConvertRule>();
            for (int row = 0; row < table.Rows.Count; row++)
            {
                var source = new ThBlockConvertBlock()
                {
                    Attributes = new Dictionary<string, object>(),
                };

                // 源块名
                int column = 0;
                source.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] = StringFilter(table.Rows[row][column].ToString());

                // 过滤空行
                if(string.IsNullOrEmpty(source.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] as string))
                {
                    continue;
                }

                // 源块图示
                column++;

                // 可见性
                column++;
                source.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY] = StringFilter(table.Rows[row][column].ToString());

                var target = new ThBlockConvertBlock()
                {
                    Attributes = new Dictionary<string, object>(),
                };

                // 目标块名
                column++;
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] = StringFilter(table.Rows[row][column].ToString());

                // 目标块图示
                column++;

                // 是否炸开
                column++;
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_EXPLODE] = 
                    string.Equals(StringFilter(table.Rows[row][column].ToString()), "1");

                // 目标图层
                column++;
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_LAYER] = StringFilter(table.Rows[row][column].ToString());

                // 内含图块
                column++;
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_INTERNAL] = StringFilter(table.Rows[row][column].ToString());

                // 插入模式
                column++;
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_INSERT_MODE] =
                    (ThBConvertInsertMode)Enum.Parse(typeof(ThBConvertInsertMode), StringFilter(table.Rows[row][column].ToString()));

                // 外形图层
                column++;
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_GEOMETRY_LAYER] = StringFilter(table.Rows[row][column].ToString());

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
