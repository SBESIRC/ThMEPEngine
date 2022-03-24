using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    /// <summary>
    /// 电动机-分立元件星三角启动 回路
    /// </summary>
    public class Motor_DiscreteComponentsStarTriangleStartCircuit : PDSBaseOutCircuit
    {
        public Motor_DiscreteComponentsStarTriangleStartCircuit()
        {
            CircuitFormType = CircuitFormOutType.电动机_分立元件星三角启动;
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
        /// 坑位3：热继电器
        /// </summary>
        public ThermalRelay thermalRelay { get; set; }

        /// <summary>
        /// 坑位4：接触器2
        /// </summary>
        public Contactor contactor2 { get; set; }

        /// <summary>
        /// 坑位5：接触器3
        /// </summary>
        public Contactor contactor3 { get; set; }

        /// <summary>
        /// 导体
        /// </summary>
        public Conductor Conductor1 { get; set; }

        /// <summary>
        /// 导体
        /// </summary>
        public Conductor Conductor2 { get; set; }
    }
}
