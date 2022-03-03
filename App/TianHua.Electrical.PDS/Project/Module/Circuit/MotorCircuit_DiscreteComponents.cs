using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    /// <summary>
    /// 电动机-分立元件 回路
    /// </summary>
    public class MotorCircuit_DiscreteComponents : PDSBaseCircuit
    {
        public MotorCircuit_DiscreteComponents()
        {
            CircuitFormType = "电动机(分立元件)";
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
    }
}
