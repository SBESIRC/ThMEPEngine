using System;

namespace ThMEPElectrical.BlockConvert
{
    public static class ThBConvertRuleExtension
    {
        public static bool Explodable(this ThBConvertRule rule)
        {
            return Convert.ToBoolean(Convert.ToInt32(rule.Transformation.Item2.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_EXPLODE]));
        }

        public static Tuple<string, string> InsertMode(this ThBConvertRule rule)
        {
            return Tuple.Create(Convert.ToString(rule.Transformation.Item2.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_INSERT_MODE]),
                Convert.ToString(rule.Transformation.Item2.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_GEOMETRY_LAYER]));
        }
    }
}
