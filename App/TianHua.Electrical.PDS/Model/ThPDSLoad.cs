using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Model
{
    /// <summary>
    /// 一级负载类型
    /// </summary>
    public enum ThPDSLoadTypeCat_1
    {
        /// <summary>
        /// 电动机
        /// </summary>
        Motor,

        /// <summary>
        /// 风机
        /// </summary>
        Fan,

        /// <summary>
        /// 水泵
        /// </summary>
        Pump,

        /// <summary>
        /// 防火卷帘
        /// </summary>
        FireResistantShutter,

        /// <summary>
        /// 电动门
        /// </summary>
        ElectricDoor,

        /// <summary>
        /// 电动窗
        /// </summary>
        ElectricWindow,

        /// <summary>
        /// 电梯
        /// </summary>
        Elevator,

        /// <summary>
        /// 自动扶梯
        /// </summary>
        Escalator,

        /// <summary>
        /// 自动人行道
        /// </summary>
        MovingSidewalk,

        /// <summary>
        /// 机械停车设备
        /// </summary>
        MechanicalParkingDevice,

        /// <summary>
        /// 配电箱（住户/租户配电箱、照明/动力配电箱、隔离开关箱）
        /// </summary>
        DistributionPanel,

        /// <summary>
        /// 照明灯具
        /// </summary>
        Luminaire,

        /// <summary>
        /// 插座
        /// </summary>
        Socket,

        /// <summary>
        /// 等效负载
        /// </summary>
        LumpedLoad,

        /// <summary>
        /// 空调设备（空调机组、新风机组、冷却塔、多联机、冷冻/冷却水机组，风冷/地源热泵机组等）
        /// </summary>
        AirConditioningEquipment,

        /// <summary>
        /// 锅炉
        /// </summary>
        Boiler,

        /// <summary>
        /// 充电桩（快充、慢充）
        /// </summary>
        Charger,

        /// <summary>
        /// 无
        /// </summary>
        None,
    }

    /// <summary>
    /// 二级负载类型
    /// </summary>
    public enum ThPDSLoadTypeCat_2
    {
        ResidentialDistributionPanel,
        LightingDistributionPanel,
        PowerDistributionPanel,
        EmergencyLightingDistributionPanel,
        EmergencyPowerDistributionPanel,
        ElectricalMeterPanel,
        ElectricalControlPanel,
        IsolationSwitchPanel,
        FireEmergencyLightingDistributionPanel,
        SubmersiblePump,
        RollerShutter,
        ElectricWindow,
        ACCharger,
        DCCharger,
        LaneLights,
        WaterproofLights,
        OutdoorLights,
        FireEmergencyLuminaire,
        EmergencyLuminaire,
        LVSocket,
        None,
    }

    /// <summary>
    /// 负载
    /// </summary>
    public class ThPDSLoad
    {
        /// <summary>
        /// 特征编号
        /// </summary>
        public ThPDSID ID { get; set; }

        /// <summary>
        /// 额定电压
        /// </summary>
        public double KV { get; set; }

        /// <summary>
        /// 一级负载类型
        /// </summary>
        public ThPDSLoadTypeCat_1 LoadTypeCat_1 { get; set; }

        /// <summary>
        /// 二级负载类型
        /// </summary>
        public ThPDSLoadTypeCat_2 LoadTypeCat_2 { get; set; }

        /// <summary>
        /// 默认回路类型
        /// </summary>
        public ThPDSCircuitType DefaultCircuitType { get; set; }

        /// <summary>
        /// 是否是消防设备
        /// </summary>
        public bool FireLoad { get; set; }

        /// <summary>
        /// 主用设备数量
        /// </summary>
        public int PrimaryAvail { get; set; }

        /// <summary>
        /// 备用设备数量
        /// </summary>
        public int SpareAvail { get; set; }

        /// <summary>
        /// 安装功率
        /// </summary>
        public ThInstalledCapacity InstalledCapacity { get; set; }

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

        /// <summary>
        /// 是否变频
        /// </summary>
        public bool FrequencyConversion { get; set; }
    }
}
