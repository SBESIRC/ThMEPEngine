namespace TianHua.Electrical.PDS.Model
{
    /// <summary>
    /// 回路类型
    /// </summary>
    public enum ThPDSCircuitType
    {
        /// <summary>
        /// 照明回路
        /// </summary>
        Lighting,

        /// <summary>
        /// 插座回路
        /// </summary>
        Socket,

        /// <summary>
        /// 动力回路
        /// </summary>
        PowerEquipment,

        /// <summary>
        /// 应急照明回路
        /// </summary>
        EmergencyLighting,

        /// <summary>
        /// 消防动力回路
        /// </summary>
        EmergencyPowerEquipment,

        /// <summary>
        /// 消防应急照明回路
        /// </summary>
        FireEmergencyLighting,

        /// <summary>
        /// 控制回路
        /// </summary>
        Control,

        /// <summary>
        /// 未知
        /// </summary>
        None,
    }

    /// <summary>
    /// 回路
    /// </summary>
    public class ThPDSCircuit
    {
        public ThPDSCircuit()
        {
            CircuitUID = System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 回路GUID
        /// </summary>
        public string CircuitUID { get; set; }

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
        public ThInstalledCapacity InstalledCapacity { get; set; }

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
        /// 位置信息
        /// </summary>
        public ThPDSLocation Location { get; set; }

        /// <summary>
        /// 是否消防回路
        /// </summary>
        public bool FireLoad { get; set; }

        /// <summary>
        /// 回路是否利用桥架
        /// </summary>
        public bool ViaCableTray { get; set; }

        /// <summary>
        /// 回路是否利用管线
        /// </summary>
        public bool ViaConduit { get; set; }
    }
}
