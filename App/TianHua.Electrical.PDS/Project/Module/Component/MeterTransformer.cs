using System;
using System.Linq;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 电能表
    /// </summary>
    public class MeterTransformer : Meter
    {
        /// <summary>
        /// 电能表
        /// </summary>
        /// <param name="calculateCurrent">计算电流</param>
        public MeterTransformer(double calculateCurrent, string polesNum)
        {
            this.ComponentType = ComponentType.MT;
            if (polesNum != "1P" && polesNum != "3P")
            {
                throw new NotSupportedException();
            }
            var meters = MeterTransformerConfiguration.MeterComponentInfos.Where(o => o.Amps > calculateCurrent).ToList();
            if (meters.Count == 0)
            {
                throw new NotSupportedException();
            }
            Meters = meters;
            PolesNum = polesNum;
            var meter = meters.First();
            MeterSwitchType = meter.parameter;
            AlternativeParameters = meters.Select(o => o.parameter).ToList();
        }
        public string Content
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
            if (Meters.Any(o => o.parameter == parameters))
            {
                MeterSwitchType = parameters;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 电能表类型
        /// </summary>
        private string MeterSwitchType { get; set; }

        /// <summary>
        /// 断路器信息
        /// </summary>
        private List<MTComponentInfo> Meters { get; set; }
    }
}
