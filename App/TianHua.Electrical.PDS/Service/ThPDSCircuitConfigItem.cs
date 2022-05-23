using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSCircuitConfigItem
    {
        public ThPDSCircuitConfigItem()
        {
            CircuitType = ThPDSCircuitType.None;
            DemandFactor = 1.0;
            PowerFactor = 0.85;
            DefaultDescription = "";
        }

        /// <summary>
        /// 回路类型
        /// </summary>
        public ThPDSCircuitType CircuitType { get; set; }

        /// <summary>
        /// 内含文字
        /// </summary>
        public string TextKey { get; set; }

        /// <summary>
        /// 额定电压
        /// </summary>
        public double KV { get; set; }

        /// <summary>
        /// 相数
        /// </summary>
        public ThPDSPhase Phase { get; set; }

        /// <summary>
        /// 需要系数
        /// </summary>
        public double DemandFactor { get; set; }

        /// <summary>
        /// 功率因数
        /// </summary>
        public double PowerFactor { get; set; }

        /// <summary>
        /// 是否消防回路
        /// </summary>
        public bool FireLoad { get; set; }

        /// <summary>
        /// 默认负载描述
        /// </summary>
        public string DefaultDescription { get; set; }
    }
}
