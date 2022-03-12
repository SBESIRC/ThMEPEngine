using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit
{
    /// <summary>
    /// 集中电源
    /// </summary>
    public class CentralizedPowerCircuit : PDSBaseInCircuit
    {
        public CentralizedPowerCircuit()
        {
            CircuitFormType = CircuitFormInType.集中电源;
        }

        /// <summary>
        /// 坑位1: 隔离开关
        /// </summary>
        public IsolatingSwitch isolatingSwitch { get; set; }
    }
}
