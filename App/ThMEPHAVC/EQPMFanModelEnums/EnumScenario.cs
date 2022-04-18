using System.ComponentModel;

namespace ThMEPHVAC.EQPMFanModelEnums
{
    /// <summary>
    /// 应用场景
    /// </summary>
    public enum EnumScenario
    {
        /// <summary>
        /// 消防排烟
        /// </summary>
        [Description("消防排烟")]
        FireSmokeExhaust = 100,
        /// <summary>
        /// 消防补风
        /// </summary>
        [Description("消防补风")]
        FireAirSupplement = 101,
        /// <summary>
        /// 消防加压送风
        /// </summary>
        [Description("消防加压送风")]
        FirePressurizedAirSupply = 110,
        /// <summary>
        /// 厨房排油烟
        /// </summary>
        [Description("厨房排油烟")]
        KitchenFumeExhaust = 200,
        /// <summary>
        /// 厨房排油烟补风
        /// </summary>
        [Description("厨房排油烟补风")]
        KitchenFumeExhaustAndAirSupplement = 201,
        /// <summary>
        /// 平时送风
        /// </summary>
        [Description("平时送风")]
        NormalAirSupply = 300,
        /// <summary>
        /// 平时排风
        /// </summary>
        [Description("平时排风")]
        NormalExhaust = 301,
        /// <summary>
        /// 消防排烟兼平时排风
        /// </summary>
        [Description("消防排烟兼平时排风")]
        FireSmokeExhaustAndNormalExhaust = 400,
        /// <summary>
        /// 消防补风兼平时送风
        /// </summary>
        [Description("消防补风兼平时送风")]
        FireAirSupplementAndNormalAirSupply = 401,
        /// <summary>
        /// 事故排风
        /// </summary>
        [Description("事故排风")]
        EmergencyExhaust = 500,
        /// <summary>
        /// 事故补风
        /// </summary>
        [Description("事故补风")]
        AccidentAirSupplement = 501,
        /// <summary>
        /// 平时送风兼事故补风
        /// </summary>
        [Description("平时送风兼事故补风")]
        NormalAirSupplyAndAccidentAirSupplement = 600,
        /// <summary>
        /// 平时排风兼事故排风
        /// </summary>
        [Description("平时排风兼事故排风")]
        NormalExhaustAndAccidentExhaust = 601,
    }
    /// <summary>
    /// 风机控制方式
    /// </summary>
    public enum EnumFanControl
    {
        /// <summary>
        /// 单速
        /// </summary>
        [Description("单速")]
        SingleSpeed = 10,
        /// <summary>
        /// 双速
        /// </summary>
        [Description("双速")]
        TwoSpeed = 20,
        /// <summary>
        /// 变频
        /// </summary>
        [Description("变频")]
        Inverters =30,
    }
    /// <summary>
    /// 电源类型
    /// </summary>
    public enum EnumFanPowerType 
    {
        /// <summary>
        /// 普通电源
        /// </summary>
        [Description("普通电源")]
        OrdinaryPower =10,
        /// <summary>
        /// 消防电源
        /// </summary>
        [Description("消防电源")]
        FireFightingPower =20,
        /// <summary>
        /// 事故电源
        /// </summary>
        [Description("事故电源")]
        EmergencyPower =30,
    }
    /// <summary>
    /// 风机形式
    /// </summary>
    public enum EnumFanModelType 
    {
        /// <summary>
        /// 前倾离心(电机内置)
        /// </summary>
        [Description("前倾离心(电机内置)")]
        ForwardTiltCentrifugation_Inner = 10,
        /// <summary>
        /// 前倾离心(电机外置)
        /// </summary>
        [Description("前倾离心(电机外置)")]
        ForwardTiltCentrifugation_Out = 11,
        /// <summary>
        /// 后倾离心(电机内置)
        /// </summary>
        [Description("后倾离心(电机内置)")]
        BackwardTiltCentrifugation_Inner = 20,
        /// <summary>
        /// 后倾离心(电机外置)
        /// </summary>
        [Description("后倾离心(电机外置)")]
        BackwardTiltCentrifugation_Out = 21,
        /// <summary>
        /// 轴流
        /// </summary>
        [Description("轴流")]
        AxialFlow = 90,
    }
    /// <summary>
    /// 气流方向
    /// </summary>
    public enum EnumFanAirflowDirection 
    {
        /// <summary>
        /// 直进直出
        /// </summary>
        [Description("直进直出")]
        StraightInAndStraightOut = 10,
        /// <summary>
        /// 直进上出
        /// </summary>
        [Description("直进上出")]
        StraightInAndUpOut = 11,
        /// <summary>
        /// 直进下出
        /// </summary>
        [Description("直进下出")]
        StraightInAndDownOut = 12,
        /// <summary>
        /// 侧进直出
        /// </summary>
        [Description("侧进直出")]
        SideEntryStraightOut = 20,
        /// <summary>
        /// 上进直出
        /// </summary>
        [Description("上进直出")]
        UpInStraightOut = 30,
        /// <summary>
        /// 下进直出
        /// </summary>
        [Description("下进直出")]
        DownInStraightOut = 31,
    }
    /// <summary>
    /// 能耗
    /// </summary>
    public enum EnumFanEnergyConsumption 
    {
        /// <summary>
        /// 1级
        /// </summary>
        [Description("1级")]
        EnergyConsumption_1 = 1,
        /// <summary>
        /// 2级
        /// </summary>
        [Description("2级")]
        EnergyConsumption_2 = 2,
        /// <summary>
        /// 3级
        /// </summary>
        [Description("3级")]
        EnergyConsumption_3 = 3,
    }
    /// <summary>
    /// 安装方式
    /// </summary>
    public enum EnumMountingType 
    {
        /// <summary>
        /// 吊装
        /// </summary>
        [Description("吊装")]
        Hoisting =10,
        /// <summary>
        /// 落地条形
        /// </summary>
        [Description("落地条形")]
        FloorBar =20,
        /// <summary>
        /// 落地方形
        /// </summary>
        [Description("落地方形")]
        FloorSquare =30,
    }
    /// <summary>
    /// 减震方式
    /// </summary>
    public enum EnumDampingType 
    {
        /// <summary>
        /// 无减震
        /// </summary>
        [Description("-")]
        NoDamping =-1,
        /// <summary>
        /// R
        /// </summary>
        [Description("R")]
        RDamping = 10,
        /// <summary>
        /// S
        /// </summary>
        [Description("S")]
        SDamping =20,
    }

    /// <summary>
    /// 数据来源
    /// </summary>
    public enum EnumValueSource
    {
        /// <summary>
        /// 计算值
        /// </summary>
        [Description("计算值")]
        IsCalcValue = 10,
        /// <summary>
        /// 输入值
        /// </summary>
        [Description("输入值")]
        IsInputValue = 20,
    }
}
