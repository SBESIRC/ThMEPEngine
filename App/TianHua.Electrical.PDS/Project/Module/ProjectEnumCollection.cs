using System.ComponentModel;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 出线回路类型
    /// </summary>
    public enum CircuitFormOutType
    {
        常规,
        漏电,
        接触器控制,
        热继电器保护,
        配电计量_上海CT,
        配电计量_上海直接表,
        配电计量_CT表在前,
        配电计量_直接表在前,
        配电计量_CT表在后,
        电动机_分立元件,
        电动机_CPS,
        电动机_分立元件星三角启动,
        电动机_CPS星三角启动,
        双速电动机_分立元件detailYY,
        双速电动机_分立元件YY,
        双速电动机_CPSdetailYY,
        双速电动机_CPSYY,
        消防应急照明回路WFEL,
        SPD,
        小母排,
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
}
