using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    [CircuitGroup(CircuitGroup.Group1)]
    public class DistributionMetering_ShanghaiMTCircuit : PDSBaseOutCircuit
    {
        /// <summary>
        /// 配电计量（上海直接表） 回路
        /// </summary>
        public DistributionMetering_ShanghaiMTCircuit()
        {
            this.CircuitFormType = CircuitFormOutType.配电计量_上海直接表;
        }

        /// <summary>
        /// 坑位1: 断路器
        /// </summary>
        public BreakerBaseComponent breaker1 { get; set; }

        /// <summary>
        /// 坑位2：电能表
        /// </summary>
        public Meter meter { get; set; }

        /// <summary>
        /// 坑位3：断路器
        /// </summary>
        public BreakerBaseComponent breaker2 { get; set; }

        /// <summary>
        /// 导体
        /// </summary>
        public Conductor Conductor { get; set; }
    }
}
