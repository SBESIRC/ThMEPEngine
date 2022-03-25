namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 元器件类型
    /// </summary>
    public enum ComponentType
    {
        导体,
        隔离开关,
        接触器,
        热继电器,
        断路器,
        剩余电流断路器,
        ATSE,
        MTSE,
        MT,
        CT,
        CPS,
        RCD,
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
