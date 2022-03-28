using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Diagram
{
    public static class ThPDSComponentContentExtension
    {
        public static string Content(this ThermalRelay thermalRelay)
        {
            return $"{thermalRelay.Model} {thermalRelay.RatedCurrent}A";
        }

        public static string Content(this Contactor contactor)
        {
            return $"{contactor.Model} {contactor.RatedCurrent}/{contactor.PolesNum}";
        }

        public static string Content(this IsolatingSwitch isolatingSwitch)
        {
            return $"{isolatingSwitch.Model} {isolatingSwitch.RatedCurrent}/{isolatingSwitch.PolesNum}";
        }
    }
}
