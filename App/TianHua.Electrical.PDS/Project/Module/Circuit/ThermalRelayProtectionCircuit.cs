using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    public class ThermalRelayProtectionCircuit : PDSBaseOutCircuit
    {
        /// <summary>
        /// 热继电器保护 回路
        /// </summary>
        public ThermalRelayProtectionCircuit()
        {
            this.CircuitFormType = CircuitFormOutType.热继电器保护;
        }

        /// <summary>
        /// 坑位1: 断路器
        /// </summary>
        public BreakerBaseComponent breaker { get; set; }

        /// <summary>
        /// 坑位2：热继电器
        /// </summary>
        public ThermalRelay thermalRelay { get; set; }

        /// <summary>
        /// 坑位3：预留
        /// </summary>
        public PDSBaseComponent reservedComponent { get; set; }
    }
}