using System;
using TianHua.Electrical.PDS.UI.WpfServices;

namespace TianHua.Electrical.PDS.UI.Services
{
    public static class ThPDSComponentBlockInfoService
    {
        public static bool IsRCBreaker(BlockInfo info)
        {
            return info.BlockName is "RCD";
        }

        public static bool IsBreaker(BlockInfo info)
        {
            return info.BlockName is "CircuitBreaker" or "RCD";
        }
    }
}
