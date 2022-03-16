namespace TianHua.Electrical.PDS.Model
{
    /// <summary>
    /// 一级负载类型
    /// </summary>
    public enum ThPDSLoadTypeCat_1
    {
        /// <summary>
        /// 配电箱
        /// </summary>
        DistributionPanel,

        /// <summary>
        /// 电动机
        /// </summary>
        Motor,

        /// <summary>
        /// 等效负载
        /// </summary>
        LumpedLoad,

        /// <summary>
        /// 灯具
        /// </summary>
        Luminaire,

        /// <summary>
        /// 插座
        /// </summary>
        Socket,
    }

    /// <summary>
    /// 二级负载类型
    /// </summary>
    public enum ThPDSLoadTypeCat_2
    {
        /// <summary>
        /// 住户配电箱
        /// </summary>
        ResidentialDistributionPanel,

        /// <summary>
        /// 照明配电箱
        /// </summary>
        LightingDistributionPanel,

        /// <summary>
        /// 动力配电箱
        /// </summary>
        PowerDistributionPanel,

        /// <summary>
        /// 应急照明配电箱
        /// </summary>
        EmergencyLightingDistributionPanel,

        /// <summary>
        /// 消防动力配电箱
        /// </summary>
        EmergencyPowerDistributionPanel,

        /// <summary>
        /// 电表箱
        /// </summary>
        ElectricalMeterPanel,

        /// <summary>
        /// 设备控制箱
        /// </summary>
        ElectricalControlPanel,

        /// <summary>
        /// 隔离开关箱
        /// </summary>
        IsolationSwitchPanel,

        /// <summary>
        /// 集中电源
        /// </summary>
        FireEmergencyLightingDistributionPanel,

        /// <summary>
        /// 泵
        /// </summary>
        Pump,

        /// <summary>
        /// 风机
        /// </summary>
        Fan,

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
        /// 交流充电桩
        /// </summary>
        ACCharger,

        /// <summary>
        /// 直流非车载充电机
        /// </summary>
        DCCharger,

        /// <summary>
        /// 机械停车设备
        /// </summary>
        MechanicalParkingDevice,

        /// <summary>
        /// 电梯
        /// </summary>
        Elevator,

        /// <summary>
        /// 扶梯
        /// </summary>
        Escalator,

        /// <summary>
        /// 自动人行道
        /// </summary>
        MovingSidewalk,

        /// <summary>
        /// 空调设备（空调机组、新风机组、冷却塔、多联机、冷冻/冷却水机组，风冷/地源热泵机组等）
        /// </summary>
        AirConditioningEquipment,

        /// <summary>
        /// 锅炉
        /// </summary>
        Boiler,

        /// <summary>
        /// 车道灯具
        /// </summary>
        LaneLights,

        /// <summary>
        /// 防水灯具
        /// </summary>
        WaterproofLights,

        /// <summary>
        /// 室外灯具
        /// </summary>
        OutdoorLights,

        /// <summary>
        /// 消防应急照明灯具
        /// </summary>
        FireEmergencyLuminaire,

        /// <summary>
        /// 备用照明灯具
        /// </summary>
        EmergencyLuminaire,

        /// <summary>
        /// 单相插座
        /// </summary>
        OnePhaseSocket,

        /// <summary>
        /// 三相插座
        /// </summary>
        ThreePhaseSocket,

        /// <summary>
        /// 低压插座
        /// </summary>
        LVSocket,

        /// <summary>
        /// 未知
        /// </summary>
        None,
    }

    public enum ThPDSLoadTypeCat_3
    {
        /// <summary>
        /// 消防排烟风机
        /// </summary>
        SmokeExhaustFan,

        /// <summary>
        /// 消防补风风机
        /// </summary>
        MakeupAirFan,

        /// <summary>
        /// 消防加压送风风机
        /// </summary>
        StaircasePressurizationFan,

        /// <summary>
        /// 消防排烟兼平时排风风机
        /// </summary>
        ExhaustFan_Smoke,

        /// <summary>
        /// 消防补风兼平时送风风机
        /// </summary>
        SupplyFan_Smoke,

        /// <summary>
        /// 平时排风风机
        /// </summary>
        ExhaustFan,

        /// <summary>
        /// 平时送风风机
        /// </summary>
        SupplyFan,

        /// <summary>
        /// 厨房排油烟风机
        /// </summary>
        KitchenExhaustFan,

        /// <summary>
        /// 事故风机
        /// </summary>
        EmergencyFan,

        /// <summary>
        /// 生活水泵
        /// </summary>
        DomesticWaterPump,

        /// <summary>
        /// 消防泵/喷淋泵/消火栓泵
        /// </summary>
        FirePump,

        /// <summary>
        /// 潜水泵
        /// </summary>
        SubmersiblePump,

        /// <summary>
        /// 未知
        /// </summary>
        None,
    }

    /// <summary>
    /// 负载
    /// </summary>
    public class ThPDSLoad
    {
        public ThPDSLoad()
        {
            LoadUID = System.Guid.NewGuid().ToString();
            ID = new ThPDSID();
            LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
            DefaultCircuitType = ThPDSCircuitType.None;
            InstalledCapacity = new ThInstalledCapacity();
            AttributesCopy = "";
            Phase = ThPDSPhase.三相;
            DemandFactor = 1.0;
            PowerFactor = 0.85;
        }

        /// <summary>
        /// 负载GUID
        /// </summary>
        public string LoadUID { get; set; }

        /// <summary>
        /// 特征编号
        /// </summary>
        public ThPDSID ID { get; set; }

        /// <summary>
        /// 额定电压
        /// </summary>
        public double KV { get; set; }

        /// <summary>
        /// 计算电流
        /// </summary>
        public double CalculateCurrent { get; set; }

        /// <summary>
        /// 一级负载类型
        /// </summary>
        public ThPDSLoadTypeCat_1 LoadTypeCat_1 { get; set; }

        /// <summary>
        /// 二级负载类型
        /// </summary>
        public ThPDSLoadTypeCat_2 LoadTypeCat_2 { get; set; }

        /// <summary>
        /// 三级负载类型
        /// </summary>
        public ThPDSLoadTypeCat_3 LoadTypeCat_3 { get; set; }

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
        /// 位置信息
        /// </summary>
        public ThPDSLocation Location { get; set; }

        /// <summary>
        /// 是否变频
        /// </summary>
        public bool FrequencyConversion { get; set; }

        /// <summary>
        /// 用于存储需要复制的块名
        /// </summary>
        public string AttributesCopy { get; set; }
    }
}
