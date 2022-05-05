using System.ComponentModel;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 出线回路类型
    /// </summary>
    public enum CircuitFormOutType
    {
        [Description("None")]
        None = 0,
        [Description("常规")]
        常规 = 1,
        [Description("漏电")]
        漏电 = 2,
        [Description("接触器控制")]
        接触器控制 = 3,
        [Description("热继电器保护")]
        热继电器保护 = 4,
        [Description("配电计量（上海CT）")]
        配电计量_上海CT = 5,
        [Description("配电计量（上海直接表）")]
        配电计量_上海直接表 = 6,
        [Description("配电计量（CT表在前）")]
        配电计量_CT表在前 = 7,
        [Description("配电计量（直接表在前）")]
        配电计量_直接表在前 = 8,
        [Description("配电计量（CT表在后）")]
        配电计量_CT表在后 = 9,
        [Description("配电计量（直接表在后）")]
        配电计量_直接表在后 = 10,
        [Description("电动机（分立元件）")]
        电动机_分立元件 = 11,
        [Description("电动机（CPS）")]
        电动机_CPS = 12,
        [Description("电动机（分立元件星三角启动）")]
        电动机_分立元件星三角启动 = 13,
        [Description("电动机（CPS星三角启动）")]
        电动机_CPS星三角启动 = 14,
        [Description("双速电动机（分立元件 D-YY）")]
        双速电动机_分立元件detailYY = 15,
        [Description("双速电动机（分立元件 Y-Y）")]
        双速电动机_分立元件YY = 16,
        [Description("双速电动机（CPS D-YY）")]
        双速电动机_CPSdetailYY = 17,
        [Description("双速电动机（CPS Y-Y）")]
        双速电动机_CPSYY = 18,
        [Description("消防应急照明回路（WFEL）")]
        消防应急照明回路WFEL = 19,
    }

    /// <summary>
    /// 进线回路类型
    /// </summary>
    public enum CircuitFormInType
    {
        [Description("None")]
        None,
        [Description("1路进线")]
        一路进线,
        [Description("2路进线ATSE")]
        二路进线ATSE,
        [Description("3路进线")]
        三路进线,
        [Description("集中电源")]
        集中电源,
    }

    /// <summary>
    /// 错误信息
    /// </summary>
    public enum PDSProjectErrorType
    {
        Alarm = 0,//报警，需要用户去关注并确认掉
        NotIncluded = 1,//未包含在项目中，只有二次比对才会出线此错误，需要用户去关注并确认掉
        Warning = 2,//警告，需要用户关注，但可无需确认
        None = 3,//未解析或无需解析错误类型，用户无需关注
        Info = 4,//信息，用户无需关注
        Normal = 5,//正常，用户无需关注
    }

    /// <summary>
    /// 浪涌保护器
    /// </summary>
    public enum SurgeProtectionDeviceType
    {
        None,
        SPD1,
        SPD2,
        SPD3,
        SPD4,
    }

    /// <summary>
    /// 桥架敷设
    /// </summary>
    public enum BridgeLaying
    {
        [Description("None")]
        None,
        [Description("金属槽盒")]
        MR,
        [Description("塑料槽盒")]
        PR,
        [Description("电缆托盘")]
        CT,
        [Description("电缆梯架")]
        CL,
        [Description("电缆支架")]
        CR,
    }

    /// <summary>
    /// 穿管敷设
    /// </summary>
    public enum Pipelaying
    {
        [Description("无穿管")]
        None = 1,
        [Description("穿管明敷")]
        E = 2,
        [Description("穿管暗敷")]
        C = 3,
        [Description("沿或跨梁（屋架）明敷")]
        AB = 4,
        [Description("沿或跨柱明敷")]
        AC = 5,
        [Description("沿吊顶或顶板面明敷")]
        CE = 6,
        [Description("吊顶内明敷")]
        SCE = 7,
        [Description("沿墙面明敷")]
        WS = 8,
        [Description("沿屋面明敷")]
        RS = 9,
        [Description("顶板内暗敷")]
        CC = 10,
        [Description("梁内暗敷")]
        BC = 11,
        [Description("柱内暗敷")]
        CLC = 12,
        [Description("墙内暗敷")]
        WC = 13,
        [Description("地板或地面下暗敷")]
        FC = 14,
    }

    /// <summary>
    /// 穿管管材
    /// </summary>
    public enum PipeMaterial
    {
        [Description("无管材")]
        None = 1,
        [Description("热镀锌低压流体输送用焊接钢管")]
        SC = 2,
        [Description("碳素结构钢电线套管")]
        MT = 3,
        [Description("套接紧定式钢管")]
        JDG = 4,
        [Description("可挠金属电线保护套管")]
        CP = 5,
        [Description("难燃（材料燃烧性能等级B1）以上硬质塑料套管")]
        PC = 6,
        [Description("阻燃半硬质塑料套管")]
        FPC = 7,
        [Description("塑料波纹套管")]
        KPC = 8,
    }

    /// <summary>
    /// 材料特征及结构
    /// </summary>
    public enum MaterialStructure
    {
        [Description("YJY")]
        YJY,
        [Description("BYJ")]
        BYJ,
        [Description("KYJY")]
        KYJY,
        [Description("RYJ")]
        RYJ,
        [Description("BTTZ")]
        BTTZ,
        [Description("BTTRZ")]
        BTTRZ,
        [Description("RTTZ")]
        RTTZ,
        [Description("NG-A(BTLY)")]
        NG_A_BTLY,
    }

    public enum ConductorLevel
    {
        A,
        B,
        C,
        D,
    }

    /// <summary>
    /// 导体类型
    /// </summary>
    public enum ConductorType
    {
        非消防配电电线,
        非消防配电电缆,
        消防配电电线,
        消防配电干线,
        消防配电分支线路,
        消防配电控制电缆,
        非消防配电控制电缆,
        消防控制信号软线,
        非消防控制信号软线,
    }

    /// <summary>
    /// 断路器型号
    /// </summary>
    public enum BreakerModel
    {
        MCB,    // Miniature Circuit Breaker
        RCBO,   // Residual Current Operated Circuit Breaker
        MCCB,   // Molded Case Circuit Breaker
        RCCB,   // Residual Current Circuit Breaker
        ACB,    // air circuit breaker
    }

    /// <summary>
    /// RCD类型
    /// </summary>
    public enum RCDType
    {
        A,
        AC,
        B,
        F,
    }

    /// <summary>
    /// 断路器附件类型
    /// </summary>
    public enum AppendixType
    {
        [Description("无")]
        无 = 1,
        [Description("分励脱扣")]
        ST = 2,
        [Description("报警")]
        AL = 3,
        [Description("辅助触点")]
        AX = 4,
        [Description("失压脱扣")]
        UR = 5,
        [Description("Residual Current")]
        RC = 6,
    }

    /// <summary>
    /// 剩余电流规格
    /// </summary>
    public enum ResidualCurrentSpecification
    {
        [Description("10mA")]
        Specification10 = 1,
        [Description("30mA")]
        Specification30 = 2,
        [Description("100mA")]
        Specification100 = 3,
        [Description("300mA")]
        Specification300 = 4,
        [Description("500mA")]
        Specification500 = 5,
    }

    /// <summary>
    /// 箱体尺寸
    /// </summary>
    public enum BoxSize
    {
        [Description("非标")] 
        Non_Standard = 1,
        [Description("PZ30")]
        PZ30 = 2,
        [Description("400Wx250Hx150D")]
        LowHeight1 = 3,
        [Description("500Wx250Hx150D")]
        LowHeight2 = 4,
        [Description("500Wx600Hx300D")]
        LowHeight3 = 5,
        [Description("600Wx400Hx250D")]
        LowHeight4 = 6,
        [Description("600Wx800Hx400D")]
        LowHeight5 = 7,
        [Description("600Wx1200Hx400D")]
        HighHeight1 = 8,
        [Description("600Wx1600Hx400D")]
        HighHeight2 = 9,
        [Description("800Wx1600Hx400D")]
        HighHeight3 = 10,
        [Description("800Wx1800Hx400D")]
        HighHeight4 = 11,
    }

    /// <summary>
    /// 安装方式
    /// </summary>
    public enum BoxInstallationType
    {
        [Description("挂墙明装")]
        挂墙明装 = 1,
        [Description("落地安装，基础高300")]
        落地安装 = 2,
        [Description("嵌墙明装")]
        嵌墙明装 = 3,
    }
}
