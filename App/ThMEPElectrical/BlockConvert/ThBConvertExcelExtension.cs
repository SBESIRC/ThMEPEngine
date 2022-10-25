using System.Collections.Generic;

namespace ThMEPElectrical.BlockConvert
{
    public static class ThBConvertExcelExtension
    {
        /// <summary>
        /// 获取Excel中的块转换映射表
        /// </summary>
        /// <param name="bConvertConfigUrl"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static List<ThBConvertRule> Rules(this string bConvertConfigUrl, ConvertMode mode)
        {
            var engine = new ThBConvertRuleEngine();
            return engine.Acquire(bConvertConfigUrl, mode);
        }

        public static List<ThBConvertRule> TCHRules(this string tchConvertConfigUrl, ConvertMode mode)
        {
            var engine = new ThBConvertRuleEngine();
            return engine.TCHAcquire(tchConvertConfigUrl, mode);
        }
    }
}
