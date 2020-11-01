using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.FanSelection
{
    public enum EnumScenario
    {
        消防排烟,
        消防补风,
        消防加压送风,
        厨房排油烟,
        厨房排油烟补风,
        平时送风,
        平时排风,
        消防排烟兼平时排风,
        消防补风兼平时送风,
        事故排风,
        事故补风,
        平时送风兼事故补风,
        平时排风兼事故排风

    }
}
