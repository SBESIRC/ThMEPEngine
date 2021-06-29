using System;
using Linq2Acad;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertRuleEngine
    {
        /// <summary>
        /// 从图纸中获取转换规则
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public List<ThBConvertRule> Acquire(Database database, ConvertMode mode)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var rules = new List<ThBConvertRule>();
                if ((mode & ConvertMode.STRONGCURRENT) == ConvertMode.STRONGCURRENT)
                {
                    var table = acadDatabase.ModelSpace
                        .OfType<Table>()
                        .First(o => o.Cells[0, 0].Text() == ThBConvertCommon.BLOCK_MAP_RULES_TABLE_TITLE_STRONG);
                    rules.AddRange(Rules(table, ConvertMode.STRONGCURRENT));
                }
                if ((mode & ConvertMode.WEAKCURRENT) == ConvertMode.WEAKCURRENT)
                {
                    var table = acadDatabase.ModelSpace
                        .OfType<Table>()
                        .First(o => o.Cells[0, 0].Text() == ThBConvertCommon.BLOCK_MAP_RULES_TABLE_TITLE_WEAK);
                    rules.AddRange(Rules(table, ConvertMode.WEAKCURRENT));
                }
                return rules;
            }
        }

        /// <summary>
        /// 获取表中的块转换映射表
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public List<ThBConvertRule> Rules(Table table, ConvertMode mode)
        {
            var rules = new List<ThBConvertRule>();
            for (int row = 2; row < table.Rows.Count; row++)
            {
                var source = new ThBlockConvertBlock()
                {
                    Attributes = new Dictionary<string, object>(),
                };

                // 源块名
                int column = 0;
                source.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] = table.Cells[row, column].Text();

                // 源块图示
                column++;

                // 可见性
                column++;
                source.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY] = table.Cells[row, column].Text();

                var target = new ThBlockConvertBlock()
                {
                    Attributes = new Dictionary<string, object>(),
                };

                // 目标块名
                column++;
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] = table.Cells[row, column].Text();

                // 目标块图示
                column++;

                // 是否炸开
                column++;
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_EXPLODE] = table.Cells[row, column].Text();

                // 目标图层
                column++;
                target.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_LAYER] = table.Cells[row, column].Text();

                // 创建映射规则
                rules.Add(new ThBConvertRule()
                {
                    Mode = mode,
                    Transformation = new Tuple<ThBlockConvertBlock, ThBlockConvertBlock>(source, target),
                });
            }
            return rules;
        }
    }
}
