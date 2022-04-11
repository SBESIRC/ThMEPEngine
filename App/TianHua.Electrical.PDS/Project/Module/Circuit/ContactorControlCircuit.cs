using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    [CircuitGroup(CircuitGroup.Group1)]
    public class ContactorControlCircuit : PDSBaseOutCircuit
    {
        /// <summary>
        /// 接触器控制 回路
        /// </summary>
        public ContactorControlCircuit()
        {
            this.CircuitFormType = CircuitFormOutType.接触器控制;
        }

        /// <summary>
        /// 坑位1: 断路器
        /// </summary>
        public Breaker breaker { get; set; }

        /// <summary>
        /// 坑位2：接触器
        /// </summary>
        public Contactor contactor { get; set; }

        /// <summary>
        /// 坑位3：预留
        /// </summary>
        public PDSBaseComponent reservedComponent { get; set; }

        /// <summary>
        /// 导体
        /// </summary>
        public Conductor Conductor { get; set; }
    }
}
