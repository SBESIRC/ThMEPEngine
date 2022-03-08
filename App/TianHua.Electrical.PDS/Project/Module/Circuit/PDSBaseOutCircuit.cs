using System;
using System.Collections.Generic;
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
        L1,
        L2,
        L3,
        L123,
    }
}
