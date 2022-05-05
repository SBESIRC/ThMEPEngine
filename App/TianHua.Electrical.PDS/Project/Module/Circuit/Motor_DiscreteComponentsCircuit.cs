using System;
using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    /// <summary>
    /// 电动机-分立元件 回路
    /// </summary>
    [Serializable]
    [CircuitGroup(CircuitGroup.Group2)]
    public class Motor_DiscreteComponentsCircuit : PDSBaseOutCircuit
    {
        public Motor_DiscreteComponentsCircuit()
        {
            CircuitFormType = CircuitFormOutType.电动机_分立元件;
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
        /// 坑位3：热继电器
        /// </summary>
        public ThermalRelay thermalRelay { get; set; }

        /// <summary>
        /// 导体
        /// </summary>
        public Conductor Conductor { get; set; }
    }
}
