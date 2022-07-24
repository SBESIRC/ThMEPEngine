using System;
using System.Collections.Generic;
using System.ComponentModel;
using TianHua.Electrical.PDS.Project.Module;

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
        [Description("配电箱")]
        DistributionPanel,

        /// <summary>
        /// 电动机
        /// </summary>
        [Description("电动机")]
        Motor,

        /// <summary>
        /// 等效负载
        /// </summary>
        [Description("等效负载")]
        LumpedLoad,

        /// <summary>
        /// 灯具
        /// </summary>
        [Description("灯具")]
        Luminaire,

        /// <summary>
        /// 插座
        /// </summary>
        [Description("插座")]
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
        [Description("住户配电箱")]
        ResidentialDistributionPanel,

        /// <summary>
        /// 照明配电箱
        /// </summary>
        [Description("照明配电箱")]
        LightingDistributionPanel,

        /// <summary>
        /// 动力配电箱
        /// </summary>
        [Description("动力配电箱")]
        PowerDistributionPanel,

        /// <summary>
        /// 应急照明配电箱
        /// </summary>
        [Description("应急照明配电箱")]
        EmergencyLightingDistributionPanel,

        /// <summary>
        /// 消防动力配电箱
        /// </summary>
        [Description("消防动力配电箱")]
        EmergencyPowerDistributionPanel,

        /// <summary>
        /// 电表箱
        /// </summary>
        [Description("电表箱")]
        ElectricalMeterPanel,

        /// <summary>
        /// 设备控制箱
        /// </summary>
        [Description("设备控制箱")]
        ElectricalControlPanel,

        /// <summary>
        /// 隔离开关箱
        /// </summary>
        [Description("隔离开关箱")]
        IsolationSwitchPanel,

        /// <summary>
        /// 集中电源
        /// </summary>
        [Description("集中电源")]
        FireEmergencyLightingDistributionPanel,

        /// <summary>
        /// 泵
        /// </summary>
        [Description("泵")]
        Pump,

        /// <summary>
        /// 风机
        /// </summary>
        [Description("风机")]
        Fan,

        /// <summary>
        /// 防火卷帘
        /// </summary>
        [Description("防火卷帘")]
        FireResistantShutter,

        /// <summary>
        /// 电动门
        /// </summary>
        [Description("电动门")]
        ElectricDoor,

        /// <summary>
        /// 电动窗
        /// </summary>
        [Description("电动窗")]
        ElectricWindow,

        /// <summary>
        /// 交流充电桩
        /// </summary>
        [Description("交流充电桩")]
        ACCharger,

        /// <summary>
        /// 直流非车载充电机
        /// </summary>
        [Description("直流非车载充电机")]
        DCCharger,

        /// <summary>
        /// 机械停车设备
        /// </summary>
        [Description("机械停车设备")]
        MechanicalParkingDevice,

        /// <summary>
        /// 电梯
        /// </summary>
        [Description("电梯")]
        Elevator,

        /// <summary>
        /// 扶梯
        /// </summary>
        [Description("扶梯")]
        Escalator,

        /// <summary>
        /// 自动人行道
        /// </summary>
        [Description("自动人行道")]
        MovingSidewalk,

        /// <summary>
        /// 空调设备（空调机组、新风机组、冷却塔、多联机、冷冻/冷却水机组，风冷/地源热泵机组等）
        /// </summary>
        [Description("空调设备")]
        AirConditioningEquipment,

        /// <summary>
        /// 锅炉
        /// </summary>
        [Description("锅炉")]
        Boiler,

        /// <summary>
        /// 车道灯具
        /// </summary>
        [Description("车道灯具")]
        LaneLights,

        /// <summary>
        /// 防水灯具
        /// </summary>
        [Description("防水灯具")]
        WaterproofLights,

        /// <summary>
        /// 室外灯具
        /// </summary>
        [Description("室外灯具")]
        OutdoorLights,

        /// <summary>
        /// 消防应急照明灯具
        /// </summary>
        [Description("消防应急照明灯具")]
        FireEmergencyLuminaire,

        /// <summary>
        /// 备用照明灯具
        /// </summary>
        [Description("备用照明灯具")]
        EmergencyLuminaire,

        /// <summary>
        /// 单相插座
        /// </summary>
        [Description("单相插座")]
        OnePhaseSocket,

        /// <summary>
        /// 三相插座
        /// </summary>
        [Description("三相插座")]
        ThreePhaseSocket,

        /// <summary>
        /// 低压插座
        /// </summary>
        [Description("低压插座")]
        LVSocket,

        /// <summary>
        /// 未知负载
        /// </summary>
        [Description("未知负载")]
        None,
    }

    /// <summary>
    /// 三级负载类型
    /// </summary>
    public enum ThPDSLoadTypeCat_3
    {
        /// <summary>
        /// 消防排烟风机
        /// </summary>
        [Description("消防排烟风机")]
        SmokeExhaustFan,

        /// <summary>
        /// 消防补风风机
        /// </summary>
        [Description("消防补风风机")]
        MakeupAirFan,

        /// <summary>
        /// 消防加压送风风机
        /// </summary>
        [Description("消防加压送风风机")]
        StaircasePressurizationFan,

        /// <summary>
        /// 消防排烟兼平时排风风机
        /// </summary>
        [Description("消防排烟兼平时排风风机")]
        ExhaustFan_Smoke,

        /// <summary>
        /// 消防补风兼平时送风风机
        /// </summary>
        [Description("消防补风兼平时送风风机")]
        SupplyFan_Smoke,

        /// <summary>
        /// 平时排风风机
        /// </summary>
        [Description("平时排风风机")]
        ExhaustFan,

        /// <summary>
        /// 平时送风风机
        /// </summary>
        [Description("平时送风风机")]
        SupplyFan,

        /// <summary>
        /// 厨房排油烟风机
        /// </summary>
        [Description("厨房排油烟风机")]
        KitchenExhaustFan,

        /// <summary>
        /// 事故风机
        /// </summary>
        [Description("事故风机")]
        EmergencyFan,

        /// <summary>
        /// 事故后风机
        /// </summary>
        [Description("事故后风机")]
        PostEmergencyFan,

        /// <summary>
        /// 生活水泵
        /// </summary>
        [Description("生活水泵")]
        DomesticWaterPump,

        /// <summary>
        /// 稳压泵
        /// </summary>
        [Description("稳压泵")]
        RegulatorsPump,

        /// <summary>
        /// 消防泵/喷淋泵/消火栓泵
        /// </summary>
        [Description("消防泵")]
        FirePump,

        /// <summary>
        /// 潜水泵
        /// </summary>
        [Description("潜水泵")]
        SubmersiblePump,

        /// <summary>
        /// 未知负载
        /// </summary>
        [Description("未知负载")]
        None,
    }

    /// <summary>
    /// 负载
    /// </summary>
    [Serializable]
    public class ThPDSLoad : IEquatable<ThPDSLoad>
    {
        public ThPDSLoad()
        {
            LoadUID = System.Guid.NewGuid().ToString();
            ID = new ThPDSID();
            LoadTypeCat_1 = ThPDSLoadTypeCat_1.LumpedLoad;
            LoadTypeCat_2 = ThPDSLoadTypeCat_2.None;
            LoadTypeCat_3 = ThPDSLoadTypeCat_3.None;
            CircuitType = ThPDSCircuitType.None;
            InstalledCapacity = new ThInstalledCapacity();
            AttributesCopy = "";
            Phase = ThPDSPhase.三相;
            DemandFactor = 0.8;
            PowerFactor = 0.85;
            FireLoadWithNull = ThPDSFireLoad.Unknown;
            LocationList = new List<ThPDSLocation>();
            
            CableLayingMethod1 = LayingSite.CC;
            CableLayingMethod2 = LayingSite.None;
            PrimaryAvail = 1;
            SpareAvail = 0;
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
        /// 回路类型
        /// </summary>
        public ThPDSCircuitType CircuitType { get; set; }

        /// <summary>
        /// 是否是消防设备
        /// </summary>
        private ThPDSFireLoad FireLoadWithNull { get; set; }

        /// <summary>
        /// 是否是消防设备
        /// </summary>
        public bool FireLoad
        {
            get
            {
                return FireLoadWithNull == ThPDSFireLoad.FireLoad;
            }
        }

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
        public ThPDSLocation Location
        {
            get
            {
                if (LocationList.Count > 0)
                {
                    return LocationList[0];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 位置信息
        /// </summary>
        private List<ThPDSLocation> LocationList { get; set; }

        /// <summary>
        /// 是否变频
        /// </summary>
        public bool FrequencyConversion { get; set; }

        /// <summary>
        /// 用于存储需要复制的块名
        /// </summary>
        public string AttributesCopy { get; set; }

        /// <summary>
        /// Cable laying method 1
        /// </summary>
        public LayingSite CableLayingMethod1 { get; set; }

        /// <summary>
        /// Cable laying method 2
        /// </summary>
        public LayingSite CableLayingMethod2 { get; set; }

        #region
        public virtual bool Equals(ThPDSLoad other)
        {
            if (other != null)
            {
                return this.LoadUID.Equals(other.LoadUID);
            }
            return false;
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as ThPDSLoad);
        }
        public override int GetHashCode()
        {
            return this.ID.LoadID.GetHashCode();
        }
        #endregion
        public ThPDSFireLoad GetFireLoad()
        {
            return FireLoadWithNull;
        }

        public void SetFireLoad(ThPDSFireLoad fireLoad)
        {
            FireLoadWithNull = fireLoad;
        }

        public void SetFireLoad(bool fireLoad)
        {
            if (fireLoad)
            {
                FireLoadWithNull = ThPDSFireLoad.FireLoad;
            }
            else
            {
                FireLoadWithNull = ThPDSFireLoad.NonFireLoad;
            }
        }

        public void SetLocation(ThPDSLocation Location)
        {
            if (LocationList == null)
            {
                LocationList = new List<ThPDSLocation>();
            }
            LocationList.Add(Location);
        }

        public List<ThPDSLocation> GetLocationList()
        {
            return LocationList;
        }

        public ThPDSLoad Clone()
        {
            var load = new ThPDSLoad
            {
                LoadUID = System.Guid.NewGuid().ToString(),
                ID = this.ID.Clone(),
                LoadTypeCat_1 = this.LoadTypeCat_1,
                LoadTypeCat_3 = this.LoadTypeCat_3,
                CircuitType = this.CircuitType,
                InstalledCapacity = this.InstalledCapacity,
                AttributesCopy = this.AttributesCopy,
                Phase = this.Phase,
                DemandFactor = this.DemandFactor,
                PowerFactor = this.PowerFactor,
                FireLoadWithNull = this.FireLoadWithNull,
                LocationList = this.LocationList,
                CableLayingMethod1 = this.CableLayingMethod1,
                CableLayingMethod2 = this.CableLayingMethod2,
                PrimaryAvail = this.PrimaryAvail,
                SpareAvail = this.SpareAvail,
            };
            return load;
        }
    }
}
