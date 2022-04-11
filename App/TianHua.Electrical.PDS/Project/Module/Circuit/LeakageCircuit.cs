using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    /// <summary>
    /// 漏电回路
    /// </summary>
    [CircuitGroup(CircuitGroup.Group1)]
    public class LeakageCircuit : PDSBaseOutCircuit
    {
        public LeakageCircuit()
        {
            CircuitFormType = CircuitFormOutType.漏电;
        }

        /// <summary>
        /// 坑位1: 断路器
        /// </summary>
        public Breaker breaker { get; set; }

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
