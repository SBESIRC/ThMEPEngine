using System.ComponentModel;

namespace TianHua.Electrical.PDS.Model
{
    /// <summary>
    /// 回路类型
    /// </summary>
    public enum ThPDSCircuitType
    {
        [Description("照明回路")]
        Lighting,
        [Description("插座回路")]
        Socket,
        [Description("动力回路")]
        PowerEquipment,
        [Description("应急照明回路")]
        EmergencyLighting,
        [Description("消防动力回路")]
        EmergencyPowerEquipment,
        [Description("消防应急照明回路")]
        FireEmergencyLighting,
        [Description("控制回路")]
        Control,
        [Description("未知")]
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
        /// 位置信息
        /// </summary>
        public ThPDSLocation Location { get; set; }

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
