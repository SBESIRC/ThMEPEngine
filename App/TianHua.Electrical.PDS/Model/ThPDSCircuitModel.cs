namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSCircuitModel
    {
        public ThPDSCircuitModel()
        {
            CircuitType = ThPDSCircuitType.None;
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
        public int KV { get; set; }

        /// <summary>
        /// 相数
        /// </summary>
        public int Phase { get; set; }

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
    }
}
