
using System;

namespace TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit
{
    /// <summary>
    /// 进线回路
    /// </summary>
    [Serializable]
    public abstract class PDSBaseInCircuit
    {
        public CircuitFormInType CircuitFormType { get; set; }
    }
}
