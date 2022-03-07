using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    /// <summary>
    /// 常规回路
    /// </summary>
    public class RegularCircuit : PDSBaseCircuit
    {
        public RegularCircuit()
        {
            CircuitFormType = "常规";
        }

        /// <summary>
        /// 坑位1: 断路器
        /// </summary>
        public Breaker breaker { get; set; }

        /// <summary>
        /// 坑位2：预留
        /// </summary>
        public PDSBaseComponent reservedComponent1 { get; set; }

        /// <summary>
        /// 坑位3：预留
        /// </summary>
        public PDSBaseComponent reservedComponent2 { get; set; }
    }
}
