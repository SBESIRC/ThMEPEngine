using System;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit
{
    /// <summary>
    /// 二路进线ATSE
    /// </summary>
    [Serializable]
    public class TwoWayInCircuit : PDSBaseInCircuit
    {
        public TwoWayInCircuit()
        {
            CircuitFormType = CircuitFormInType.二路进线ATSE;
        }

        /// <summary>
        /// 坑位1: 隔离开关1
        /// </summary>
        public PDSBaseComponent isolatingSwitch1 { get; set; }
        
        /// <summary>
        /// 坑位2: 隔离开关2
        /// </summary>
        public PDSBaseComponent isolatingSwitch2 { get; set; }

        /// <summary>
        /// 坑位3: 转换开关
        /// </summary>
        public TransferSwitch transferSwitch { get; set; }

        /// <summary>
        /// 坑位4：OUVP/Meter
        /// </summary>
        public PDSBaseComponent reservedComponent { get; set; }
    }
}
