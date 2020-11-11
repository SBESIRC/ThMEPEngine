using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public abstract class ThBConvertRuleEngine
    {
        /// <summary>
        /// 从图纸中获取转换规则
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public abstract List<ThBConvertRule> Acquire(Database database);

        /// <summary>
        /// 获取表中的块转换映射表
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public abstract List<ThBConvertRule> Rules(Table table);
    }
}
