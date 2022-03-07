namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 元器件类型
    /// </summary>
    public enum ComponentType
    {
        隔离开关,
        接触器,
        热继电器,
        断路器,
        剩余电流断路器,
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
    }

    /// <summary>
    /// 断路器（抽象基类）
    /// </summary>
    public abstract class BreakerBaseComponent : PDSBaseComponent
    {

    }
}
