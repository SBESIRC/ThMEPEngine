namespace TianHua.Electrical.PDS.Model
{
    /// <summary>
    /// 回路类型
    /// </summary>
    public enum ThPDSCircuitType
    {
        /// <summary>
        /// 照明
        /// </summary>
        Lighting,

        /// <summary>
        /// 动力
        /// </summary>
        PowerEquipment,

        /// <summary>
        /// 应急照明
        /// </summary>
        EmergencyLighting,

        /// <summary>
        /// 消防动力
        /// </summary>
        EmergencyPowerEquipment,

        /// <summary>
        /// 消防应急照明
        /// </summary>
        FireEmergencyLighting,

        /// <summary>
        /// 控制
        /// </summary>
        Control
    }

    /// <summary>
    /// 回路
    /// </summary>
    public class ThPDSCircuit
    {
        /// <summary>
        /// 特征编号
        /// </summary>
        public ThPDSID ID { get; set; }

        /// <summary>
        /// 回路类型
        /// </summary>
        public ThPDSCircuitType Type { get; set; }

        /// <summary>
        /// 额定电压
        /// </summary>
        public double KV { get; set; }

        /// <summary>
        /// 安装功率
        /// </summary>
        public double InstalledCapacity { get; set; }

        /// <summary>
        /// 相数
        /// </summary>
        public double Phase { get; set; }

        /// <summary>
        /// 需要系数
        /// </summary>
        public double DemandFactor { get; set; }

        /// <summary>
        /// 功率因数
        /// </summary>
        public double PowerFactor { get; set; }

        /// <summary>
        /// 位置信息
        /// </summary>
        public ThPDSLocation Location { get; set; }
    }
}
