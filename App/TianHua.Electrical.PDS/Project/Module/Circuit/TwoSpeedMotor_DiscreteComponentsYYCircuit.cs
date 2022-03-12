using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    public class TwoSpeedMotor_DiscreteComponentsYYCircuit : PDSBaseOutCircuit
    {
        /// <summary>
        /// 双速电动机（分立元件 Y-Y）
        /// </summary>
        public TwoSpeedMotor_DiscreteComponentsYYCircuit()
        {
            CircuitFormType = CircuitFormOutType.双速电动机_分立元件YY;
        }

        /// <summary>
        /// 坑位1: 断路器
        /// </summary>
        public BreakerBaseComponent breaker { get; set; }

        /// <summary>
        /// 坑位2：接触器1
        /// </summary>
        public Contactor contactor1 { get; set; }

        /// <summary>
        /// 坑位3：热继电器1
        /// </summary>
        public ThermalRelay thermalRelay1 { get; set; }

        /// <summary>
        /// 坑位4：接触器2
        /// </summary>
        public Contactor contactor2 { get; set; }

        /// <summary>
        /// 坑位5：热继电器2
        /// </summary>
        public ThermalRelay thermalRelay2 { get; set; }
    }
}
