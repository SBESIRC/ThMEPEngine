using System;
using System.Linq;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 间接表
    /// </summary>
    public class CurrentTransformer : Meter
    {
        /// <summary>
        /// 间接表
        /// </summary>
        public CurrentTransformer(double calculateCurrent, string polesNum)
        {
            this.ComponentType = ComponentType.CT;
            if (polesNum != "1P" && polesNum != "3P")
            {
                throw new NotSupportedException();
            }
            var meters = CurrentTransformerConfiguration.CTComponentInfos.Where(o => o.Amps > calculateCurrent).ToList();
            if (meters.Count == 0)
            {
                throw new NotSupportedException();
            }
            CurrentTransformers = meters;
            PolesNum = polesNum;
            var meter = meters.First();
            CurrentTransformerSwitchType = meter.parameter;
            AlternativeParameters = meters.Select(o => o.parameter).ToList();
        }
        public string ContentCT
        {
            get
            {
                if (PolesNum == "1P")
                {
                    return CurrentTransformerSwitchType + "A";
                }
                else
                {
                    return "3×" + CurrentTransformerSwitchType + "A";
                }
            }
        }
        public string ContentMT
        {
            get
            {
                if (PolesNum == "1P")
                {
                    return MeterSwitchType;
                }
                else
                {
                    return "3×" + MeterSwitchType;
                }
            }
        }

        public override List<string> GetParameters()
        {
            return AlternativeParameters;
        }
        public override void SetParameters(string parameters)
        {
            if (CurrentTransformers.Any(o => o.parameter == parameters))
            {
                CurrentTransformerSwitchType = parameters;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 间接表类型
        /// </summary>
        private string MeterSwitchType = "1.5(6)";

        /// <summary>
        /// 间接表类型
        /// </summary>
        private string CurrentTransformerSwitchType { get; set; }

        /// <summary>
        /// 断路器信息
        /// </summary>
        private List<CTComponentInfo> CurrentTransformers { get; set; }
    }
}
