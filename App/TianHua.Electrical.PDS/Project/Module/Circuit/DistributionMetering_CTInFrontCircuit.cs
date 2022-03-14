using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    public class DistributionMetering_CTInFrontCircuit : PDSBaseOutCircuit
    {
        public DistributionMetering_CTInFrontCircuit()
        {
            this.CircuitFormType = CircuitFormOutType.配电计量_CT表在前;
        }

        /// <summary>
        /// 坑位1：电能表
        /// </summary>
        public Meter meter { get; set; }

        /// <summary>
        /// 坑位2: 断路器
        /// </summary>
        public BreakerBaseComponent breaker { get; set; }

        // <summary>
        /// 坑位3：预留
        /// </summary>
        public PDSBaseComponent reservedComponent { get; set; }
    }
}
