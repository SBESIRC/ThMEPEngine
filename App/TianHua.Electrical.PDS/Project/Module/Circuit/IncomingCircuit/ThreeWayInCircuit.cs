using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit
{
    /// <summary>
    /// 三路进线
    /// </summary>
    public class ThreeWayInCircuit : PDSBaseInCircuit
    {
        public ThreeWayInCircuit()
        {
            CircuitFormType = CircuitFormInType.三路进线;
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
