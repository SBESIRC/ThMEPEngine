using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    /// <summary>
    /// 双速电动机（CPS D-YY）
    /// </summary>
    public class TwoSpeedMotor_CPSDYYCircuit : PDSBaseOutCircuit
    {
        public TwoSpeedMotor_CPSDYYCircuit()
        {
            CircuitFormType = CircuitFormOutType.双速电动机_CPSdetailYY;
        }

        /// <summary>
        /// 坑位1: 断路器1
        /// </summary>
        public BreakerBaseComponent breaker1 { get; set; }

        /// <summary>
        /// 坑位2：断路器2
        /// </summary>
        public BreakerBaseComponent breaker2 { get; set; }

        /// <summary>
        /// 坑位3：接触器
        /// </summary>
        public Contactor contactor1 { get; set; }
    }
}
