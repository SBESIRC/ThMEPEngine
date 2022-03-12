using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
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
        public BreakerBaseComponent breaker { get; set; }

        /// <summary>
        /// 坑位2：接触器
        /// </summary>
        public Contactor contactor { get; set; }

        /// <summary>
        /// 坑位3：预留
        /// </summary>
        public PDSBaseComponent reservedComponent { get; set; }
    }
}
