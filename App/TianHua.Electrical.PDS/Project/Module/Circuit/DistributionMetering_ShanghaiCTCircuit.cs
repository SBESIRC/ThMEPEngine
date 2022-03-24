using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    public class DistributionMetering_ShanghaiCTCircuit : PDSBaseOutCircuit
    {
        /// <summary>
        /// 配电计量（上海CT） 回路
        /// </summary>
        public DistributionMetering_ShanghaiCTCircuit()
        {
            this.CircuitFormType = CircuitFormOutType.配电计量_上海CT;
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
