using System.ComponentModel;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    /// <summary>
    /// 出线回路
    /// </summary>
    public abstract class PDSBaseOutCircuit
    {
        /// <summary>
        /// 出线回路类型
        /// </summary>
        public CircuitFormOutType CircuitFormType { get; set; }
        public bool IsAttachedSmallBusbar { get; set; }
        public SmallBusbar SmallBusbar { get; set; }
    }

    /// <summary>
    /// 相序
    /// </summary>
    public enum PhaseSequence
    {
        [Description("L1")]
        L1,
        [Description("L2")]
        L2,
        [Description("L3")]
        L3,
        [Description("L123")]
        L123,
    }
}
