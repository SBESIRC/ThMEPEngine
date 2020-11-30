using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.BlockConvert
{
    public static class ThBConvertRuleExtension
    {
        public static bool Explodable(this ThBConvertRule rule)
        {
            return Convert.ToBoolean(rule.Transformation.Item2.Attributes[ThBConvertCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_EXPLODE]);
        }
    }
}
