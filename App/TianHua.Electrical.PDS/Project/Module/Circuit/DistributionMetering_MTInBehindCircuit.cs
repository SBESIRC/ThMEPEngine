using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    [CircuitGroup(CircuitGroup.Group1)]
    public class DistributionMetering_CTInBehindCircuit : PDSBaseOutCircuit
    {
        public DistributionMetering_CTInBehindCircuit()
        {
            this.CircuitFormType = CircuitFormOutType.配电计量_CT表在后;
        }

        /// <summary>
        /// 坑位1: 断路器
        /// </summary>
        public BreakerBaseComponent breaker { get; set; }

        /// <summary>
        /// 坑位2：电能表
        /// </summary>
        public Meter meter { get; set; }

        // <summary>
        /// 坑位3：预留
        /// </summary>
        public PDSBaseComponent reservedComponent { get; set; }

        /// <summary>
        /// 导体
        /// </summary>
        public Conductor Conductor { get; set; }
    }
}
