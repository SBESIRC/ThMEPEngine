using System;
using System.Collections.Generic;
using ThCADExtension;
using TianHua.Electrical.PDS.Project;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Extension
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
            var IcuLevel = cps.Icu.Substring(1, 1);
            return $"{cps.Model}{ (cps.IsNeglectCombination ? "" : cps.Combination)}{cps.FrameSpecification}{IcuLevel}-{cps.CodeLevel}{cps.RatedCurrent}/{cps.PolesNum}";
        }

        public static string Content(this OUVP cps)
        {
            return $"{cps.Model} {cps.RatedCurrent}/{cps.PolesNum}";
        }

        public static string Content(this Breaker breaker)
        {
            var IcuLevel = breaker.Icu.Substring(0, 1);
            var appendixStr = "";
            if (breaker.RCDAppendix.HasValue && true == breaker.RCDAppendix.Value)
            {
                appendixStr += "+RCD";
            }
            if (breaker.STAppendix.HasValue && true == breaker.STAppendix.Value)
            {
                appendixStr += "+ST";
            }
            if (breaker.ALAppendix.HasValue && true == breaker.ALAppendix.Value)
            {
                appendixStr += "+AL";
            }
            if (breaker.URAppendix.HasValue && true == breaker.URAppendix.Value)
            {
                appendixStr += "+UR";
            }
            if (breaker.AXAppendix.HasValue && true == breaker.AXAppendix.Value)
            {
                appendixStr += "+AX";
            }

            switch (breaker.Model)
            {
                case Project.Module.BreakerModel.MCB:
                    {
                        return $"{breaker.Model}{breaker.FrameSpecification}{IcuLevel}-{(breaker.TripUnitType == "TM" ? breaker.Characteristics : breaker.TripUnitType)}{breaker.RatedCurrent}/{breaker.PolesNum}{appendixStr}";
                    }
                case Project.Module.BreakerModel.MCCB:
                case Project.Module.BreakerModel.ACB:
                    {
                        return $"{breaker.Model}{breaker.FrameSpecification}{IcuLevel}-{breaker.TripUnitType}{breaker.RatedCurrent}/{breaker.PolesNum}{appendixStr}";
                    }
                case Project.Module.BreakerModel.RCBO:
                case Project.Module.BreakerModel.RCCB:
                    {
                        return $"{breaker.Model}{breaker.FrameSpecification}{IcuLevel}-{breaker.TripUnitType}{breaker.RatedCurrent}/{breaker.PolesNum}  {breaker.RCDType}{breaker.ResidualCurrent.GetDescription()}{appendixStr}";
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        public static string Content(this Meter meter)
        {
            if (meter.PolesNum == "1P")
            {
                return meter.MeterParameter + "A";
            }
            else
            {
                return "3×" + meter.MeterParameter + "A";
            }
        }

        public static string ContentMT(this CurrentTransformer meter)
        {
            if (meter.PolesNum == "1P")
            {
                return meter.MeterSwitchType + "A";
            }
            else
            {
                return "3×" + meter.MeterSwitchType + "A";
            }
        }

        public static string Content(this TransferSwitch transferSwitch)
        {
            return $"{transferSwitch.Model} {transferSwitch.RatedCurrent}A {transferSwitch.PolesNum}";
        }

        public static string CentralizedPowerContent()
        {
            return $"{PDSProject.Instance.projectGlobalConfiguration.fireEmergencyLightingModel}应急照明{PDSProject.Instance.projectGlobalConfiguration.fireEmergencyLightingType}";
        }
    }
}
