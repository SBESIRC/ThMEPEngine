using System.ComponentModel;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 出线回路类型
    /// </summary>
    public enum CircuitFormOutType
    {
        [Description("常规")]
        常规,
        [Description("漏电")]
        漏电,
        [Description("接触器控制")]
        接触器控制,
        [Description("热继电器保护")]
        热继电器保护,
        [Description("配电计量（上海CT）")]
        配电计量_上海CT,
        [Description("配电计量（上海直接表）")]
        配电计量_上海直接表,
        [Description("配电计量（CT表在前）")]
        配电计量_CT表在前,
        [Description("配电计量（直接表在前）")]
        配电计量_直接表在前,
        [Description("配电计量（CT表在后）")]
        配电计量_CT表在后,
        [Description("配电计量（直接表在后）")]
        配电计量_直接表在后,
        [Description("电动机（分立元件）")]
        电动机_分立元件,
        [Description("电动机（CPS）")]
        电动机_CPS,
        [Description("电动机（分立元件星三角启动）")]
        电动机_分立元件星三角启动,
        [Description("电动机（CPS星三角启动）")]
        电动机_CPS星三角启动,
        [Description("双速电动机（分立元件 D-YY）")]
        双速电动机_分立元件detailYY,
        [Description("双速电动机（分立元件 Y-Y）")]
        双速电动机_分立元件YY,
        [Description("双速电动机（CPS D-YY）")]
        双速电动机_CPSdetailYY,
        [Description("双速电动机（CPS Y-Y）")]
        双速电动机_CPSYY,
        [Description("消防应急照明回路（WFEL）")]
        消防应急照明回路WFEL,
        [Description("SPD")]
        SPD,
        [Description("小母排")]
        小母排,
        [Description("None")]
        None
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
        SPD1,
        SPD2,
        SPD3,
        SPD4,
    }
}
