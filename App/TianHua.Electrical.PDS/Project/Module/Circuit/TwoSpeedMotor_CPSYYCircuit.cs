using System;
using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    /// <summary>
    /// 双速电动机（CPS Y-Y）
    /// </summary>
    [Serializable]
    [CircuitGroup(CircuitGroup.Group3)]
    public class TwoSpeedMotor_CPSYYCircuit : PDSBaseOutCircuit
    {
        public TwoSpeedMotor_CPSYYCircuit()
        {
            CircuitFormType = CircuitFormOutType.双速电动机_CPSYY;
        }

        /// <summary>
        /// 坑位1: 低速CPS
        /// </summary>
        public CPS cps1 { get; set; }

        /// <summary>
        /// 坑位2: 低速导体
        /// </summary>
        public Conductor conductor1 { get; set; }

        /// <summary>
        /// 坑位3: 高速CPS
        /// </summary>
        public CPS cps2 { get; set; }

        /// <summary>
        /// 坑位4: 高速导体
        /// </summary>
        public Conductor conductor2 { get; set; }
    }
}
