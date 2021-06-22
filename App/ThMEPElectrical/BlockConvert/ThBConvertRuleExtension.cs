using System;

namespace ThMEPElectrical.BlockConvert
{
    public static class ThBConvertRuleExtension
    {
        public static bool Explodable(this ThBConvertRule rule)
        {
            return Convert.ToBoolean(Convert.ToInt32(rule.Transformation.Item2.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_EXPLODE]));
        }
    }
}
