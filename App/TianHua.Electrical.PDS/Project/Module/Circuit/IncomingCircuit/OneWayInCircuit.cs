using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit
{
    /// <summary>
    /// 一路进线
    /// </summary>
    public class OneWayInCircuit : PDSBaseInCircuit
    {
        public OneWayInCircuit()
        {
            CircuitFormType = CircuitFormInType.一路进线;
        }

        /// <summary>
        /// 坑位1: 隔离开关
        /// </summary>
        public IsolatingSwitch isolatingSwitch { get; set; }
    }
}
