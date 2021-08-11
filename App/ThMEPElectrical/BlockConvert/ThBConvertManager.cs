using System;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertManager : IDisposable
    {
        /// <summary>
        /// 块转换的映射规则
        /// </summary>
        public List<ThBConvertRule> Rules { get; set; }

        /// <summary>
        /// 从数据库中读取数据创建对象
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static ThBConvertManager CreateManager(Database database, ConvertMode mode)
        {
            return new ThBConvertManager()
            {
                Rules = database.Rules(mode),
            };
        }

        public void Dispose()
        {
            //
        }

        /// <summary>
        /// 根据源块引用，获取转换后的块信息
        /// </summary>
        /// <param name="blkRef"></param>
        /// <returns></returns>
        public ThBlockConvertBlock TransformRule(string block)
        {
            var rule = Rules.First(o =>
                (string)o.Transformation.Item1.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] == block);
            return rule?.Transformation.Item2;
        }

        /// <summary>
        /// 根据块名和可见性，或者转换后的块信息
        /// </summary>
        /// <param name="block"></param>
        /// <param name="visibility"></param>
        /// <returns></returns>
        public ThBlockConvertBlock TransformRule(string block, string visibility)
        {
            var rule = Rules.First(o =>
                (string)o.Transformation.Item1.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_NAME] == block &&
                 ThStringTools.CompareWithChinesePunctuation((string)o.Transformation.Item1.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY], visibility));
            return rule?.Transformation.Item2;
        }
    }
}
