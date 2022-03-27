using System;
using System.Linq;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 热继电器
    /// </summary>
    public class ThermalRelay : PDSBaseComponent
    {
        /// <summary>
        /// 计算电流
        /// </summary>
        /// <param name="calculateCurrent"></param>
        public ThermalRelay(double calculateCurrent)
        {
            ComponentType = ComponentType.KH;
            var thermalRelays = ThermalRelayConfiguration.thermalRelayInfos.
                Where(o => o.MinAmps <= calculateCurrent && o.MaxAmps >= calculateCurrent);
            var thermalRelay = thermalRelays.OrderBy(o => Math.Abs(2 * calculateCurrent - o.MinAmps - o.MaxAmps)).FirstOrDefault();
            if (thermalRelay.IsNull())
            {
                thermalRelay = ThermalRelayConfiguration.thermalRelayInfos.First();
            }
            ThermalRelayType = thermalRelay.ModelName;
            PolesNum = thermalRelay.Poles;
            RatedCurrent = $"{thermalRelay.MinAmps}~{thermalRelay.MaxAmps}";
        }

        /// <summary>
        /// 热继电器类型
        /// </summary>
        public string ThermalRelayType { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 电流整定范围
        /// </summary>
        public string RatedCurrent { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public string Content { get { return $"{ThermalRelayType} {RatedCurrent}A"; } }
    }
}
