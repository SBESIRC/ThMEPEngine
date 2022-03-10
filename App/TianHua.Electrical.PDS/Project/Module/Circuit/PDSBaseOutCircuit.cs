using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    /// <summary>
    /// 出线回路
    /// </summary>
    public abstract class PDSBaseOutCircuit
    {
        public CircuitFormOutType CircuitFormType { get; set; }
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
