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
        /// 构造函数
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
            Model = thermalRelay.Model;
            PolesNum = thermalRelay.Poles;
            RatedCurrent = $"{thermalRelay.MinAmps}~{thermalRelay.MaxAmps}";
        }

        /// <summary>
        /// 型号
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 电流整定范围
        /// </summary>
        public string RatedCurrent { get; set; }
    }
}
