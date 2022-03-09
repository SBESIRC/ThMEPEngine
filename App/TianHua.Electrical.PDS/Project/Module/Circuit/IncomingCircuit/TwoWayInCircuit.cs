using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit
{
    /// <summary>
    /// 二路进线ATSE
    /// </summary>
    public class TwoWayInCircuit : PDSBaseInCircuit
    {
        public TwoWayInCircuit()
        {
            CircuitFormType = CircuitFormInType.二路进线ATSE;
        }

        /// <summary>
        /// 坑位1: 隔离开关1
        /// </summary>
        public IsolatingSwitch isolatingSwitch1 { get; set; }
        
        /// <summary>
        /// 坑位1: 隔离开关2
        /// </summary>
        public IsolatingSwitch isolatingSwitch2 { get; set; }
    }
}
