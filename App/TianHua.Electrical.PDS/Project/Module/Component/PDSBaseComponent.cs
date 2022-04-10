using System.ComponentModel;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 元器件类型
    /// </summary>
    public enum ComponentType
    {
        [Description("导体")]
        Conductor,
        [Description("控制回路导体")]
        ControlConductor,
        [Description("隔离开关")]
        QL,
        [Description("接触器")]
        QAC,
        [Description("热继电器")]
        KH,
        [Description("断路器")]
        CB,
        [Description("剩余电流动作断路器")]
        一体式RCD,
        [Description("剩余电流动作断路器")]
        组合式RCD,
        [Description("自动转换开关")]
        ATSE,
        [Description("手动转换开关")]
        MTSE,
        [Description("直接表")]
        MT,
        [Description("间接表")]
        CT,
        [Description("控制保护开关")]
        CPS,
        [Description("熔断器")]
        FU,
        [Description("浪涌保护器")]
        SPD,
        [Description("软启动器")]
        SS,
        [Description("变频器")]
        FC,
        [Description("过欠电压保护器")]
        OUVP,
    }

    /// <summary>
    /// 元器件（抽象基类）
    /// </summary>
    public abstract class PDSBaseComponent
    {
        /// <summary>
        /// 元器件类型
        /// </summary>
        public ComponentType ComponentType { get; set; }


        /// <summary>
        /// 获取元器件电流规格
        /// </summary>
        /// <returns></returns>
        public virtual double GetCascadeRatedCurrent()
        {
            return 0;
        }
    }
}
