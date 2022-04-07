using TianHua.Electrical.PDS.UI.WpfServices;

namespace TianHua.Electrical.PDS.UI.Services
{
    public static class ThPDSComponentBlockInfoService
    {
        public static bool IsBreaker(this BlockInfo info)
        {
            return info.BlockName is "CircuitBreaker" or "RCD";
        }

        public static bool IsContactor(this BlockInfo info)
        {
            return info.BlockName == "Contactor";
        }

        public static bool IsThermalRelay(this BlockInfo info)
        {
            return info.BlockName == "ThermalRelay";
        }

        public static bool IsCPS(this BlockInfo info)
        {
            return info.BlockName == "CPS";
        }

        public static bool IsMeter(this BlockInfo info)
        {
            return info.BlockName == "Meter";
        }

        public static bool IsMotor(this BlockInfo info)
        {
            return info.BlockName == "Motor";
        }

        public static bool IsIsolator(this BlockInfo info)
        {
            return info.BlockName == "Isolator";
        }

        public static bool IsATSE(this BlockInfo info)
        {
            return info.BlockName == "ATSE";
        }

        public static bool IsMTSE(this BlockInfo info)
        {
            return info.BlockName == "TSE";
        }

        public static bool IsOUVP(this BlockInfo info)
        {
            return info.BlockName == "过欠电压保护器";
        }
    }
}
