using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    /// <summary>
    /// 电动机-CPS 回路
    /// </summary>
    [CircuitGroup(CircuitGroup.Group2)]
    public class Motor_CPSCircuit : PDSBaseOutCircuit
    {
        public Motor_CPSCircuit()
        {
            CircuitFormType = CircuitFormOutType.电动机_CPS;
        }

        /// <summary>
        /// 坑位1: CPS
        /// </summary> 
        public CPS cps { get; set; }

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
