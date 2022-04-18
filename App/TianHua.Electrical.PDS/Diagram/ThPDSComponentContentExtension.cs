using System;
using ThCADExtension;
using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Component.Extension;

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

        public static string Content(this CPS cps)
        {
            return $"{cps.Model}{cps.Combination}-{cps.FrameSpecification}/{cps.CodeLevel}{cps.RatedCurrent}/{cps.PolesNum}{cps.RatedCurrent}";
        }

        public static string Content(this OUVP cps)
        {
            return "自复式过欠电压保护器";
        }

        public static string Content(this Breaker breaker)
        {
            if(breaker.ComponentType == ComponentType.CB || breaker.ComponentType == ComponentType.一体式RCD)
            {
                return $"{breaker.Model}{breaker.FrameSpecification} {breaker.TripUnitType}{breaker.RatedCurrent}/{breaker.PolesNum}{(breaker.Appendix == Project.Module.AppendixType.ST?"/ST":"")}";
            }
            else if(breaker.ComponentType == ComponentType.组合式RCD)
            {
                return $"{breaker.Model}{breaker.FrameSpecification}-{breaker.TripUnitType}{breaker.RatedCurrent}/{breaker.PolesNum}/RC {breaker.RCDType}{breaker.ResidualCurrent.GetDescription()}";
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static string Content(this TransferSwitch transferSwitch)
        {
            return $"{transferSwitch.Model} {transferSwitch.RatedCurrent}A {transferSwitch.PolesNum}";
        }
    }
}
