using TianHua.Electrical.PDS.Project.Module.Circuit.Extension;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    /// <summary>
    /// 双速电动机-分立元件星三角启动 回路
    /// </summary>
    [CircuitGroup(CircuitGroup.Group3)]
    public class TwoSpeedMotor_DiscreteComponentsDYYCircuit : PDSBaseOutCircuit
    {
        public TwoSpeedMotor_DiscreteComponentsDYYCircuit()
        {
            CircuitFormType = CircuitFormOutType.双速电动机_分立元件detailYY;
        }

        /// <summary>
        /// 坑位1: 断路器
        /// </summary>
        public Breaker breaker { get; set; }

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

        /// <summary>
        /// 坑位6：接触器3 
        /// </summary>
        public Contactor contactor3 { get; set; }

        /// <summary>
        /// 低速导体
        /// </summary>
        public Conductor conductor1 { get; set; }

        /// <summary>
        /// 高速导体
        /// </summary>
        public Conductor conductor2 { get; set; }
    }
}
