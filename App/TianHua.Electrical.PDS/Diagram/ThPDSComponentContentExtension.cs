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
            switch(breaker.ComponentType)
            {
                case ComponentType.CB:
                    {
                        if (breaker.Appendix == Project.Module.AppendixType.无)
                        {
                            return $"{breaker.Model}{breaker.FrameSpecification}-{breaker.TripUnitType}{breaker.RatedCurrent}/{breaker.PolesNum}";
                        }
                        else
                        {
                            return $"{breaker.Model}{breaker.FrameSpecification}-{breaker.TripUnitType}{breaker.RatedCurrent}/{breaker.PolesNum}/{breaker.Appendix}";
                        }
                    }
                case ComponentType.一体式RCD:
                    {
                        if (breaker.Appendix == Project.Module.AppendixType.无)
                        {
                            return $"{breaker.Model}{breaker.FrameSpecification}-{breaker.TripUnitType}{breaker.RatedCurrent}/{breaker.PolesNum}/{breaker.RCDType} {breaker.ResidualCurrent.GetDescription()}";
                        }
                        else
                        {
                            return $"{breaker.Model}{breaker.FrameSpecification}-{breaker.TripUnitType}{breaker.RatedCurrent}/{breaker.PolesNum}/{breaker.RCDType} {breaker.ResidualCurrent.GetDescription()}/{breaker.Appendix}";
                        }
                    }
                case ComponentType.组合式RCD:
                    {
                        if (breaker.Appendix == Project.Module.AppendixType.无)
                        {
                            return $"{breaker.Model}{breaker.FrameSpecification}-{breaker.TripUnitType}{breaker.RatedCurrent}/{breaker.PolesNum}/{breaker.RCDType}{breaker.ResidualCurrent.GetDescription()}";
                        }
                        else
                        {
                            return $"{breaker.Model}{breaker.FrameSpecification}-{breaker.TripUnitType}{breaker.RatedCurrent}/{breaker.PolesNum}/{breaker.Appendix} {breaker.RCDType}{breaker.ResidualCurrent.GetDescription()}";
                        }
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        public static string Content(this TransferSwitch transferSwitch)
        {
            return $"{transferSwitch.Model} {transferSwitch.RatedCurrent}A {transferSwitch.PolesNum}";
        }
    }
}
