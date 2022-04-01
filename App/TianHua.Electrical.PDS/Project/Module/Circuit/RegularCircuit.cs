using TianHua.Electrical.PDS.Project.Module.Component;
using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    /// <summary>
    /// 常规回路
    /// </summary>
   [CircuitGroup(CircuitGroup.Group1)]
    public class RegularCircuit : PDSBaseOutCircuit
    {
        public RegularCircuit()
        {
            CircuitFormType = CircuitFormOutType.常规;
        }

        /// <summary>
        /// 坑位1: 断路器
        /// </summary>
        public BreakerBaseComponent breaker { get; set; }

        /// <summary>
        /// 坑位2：预留
        /// </summary>
        public PDSBaseComponent reservedComponent1 { get; set; }

        /// <summary>
        /// 坑位3：预留
        /// </summary>
        public PDSBaseComponent reservedComponent2 { get; set; }

        /// <summary>
        /// 导体
        /// </summary>
        public Conductor Conductor { get; set; }
    }
}
