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
    }
}
