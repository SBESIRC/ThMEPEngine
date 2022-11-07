using System;
using System.Linq;
using System.Collections.Generic;

using ThCADExtension;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertManager : IDisposable
    {
        /// <summary>
        /// 块转换的映射规则
        /// </summary>
        public List<ThBConvertRule> Rules { get; set; }

        /// <summary>
        /// 块转换的映射规则
        /// </summary>
        public List<ThBConvertRule> TCHRules { get; set; }

        /// <summary>
        /// 从Excel中读取数据创建对象
        /// </summary>
        /// <param name="bConvertConfigUrl"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static ThBConvertManager CreateManager(string bConvertConfigUrl, ConvertMode mode)
        {
            return new ThBConvertManager()
            {
                Rules = bConvertConfigUrl.Rules(mode),
            };
        }

        /// <summary>
        /// 从TCHExcel中读取数据创建对象
        /// </summary>
        /// <param name="tchConvertConfigUrl"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static ThBConvertManager CreateTCHManager(string tchConvertConfigUrl, ConvertMode mode)
        {
            return new ThBConvertManager()
            {
                TCHRules = tchConvertConfigUrl.TCHRules(mode),
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

        public ThBlockConvertBlock TCHTransformRule(string tch)
        {
            var rule = TCHRules.First(o =>
                (string)o.Transformation.Item1.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_TCH_MODEL] == tch);
            return rule?.Transformation.Item2;
        }
    }
}
