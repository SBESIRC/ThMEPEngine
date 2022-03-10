using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Circuit;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 回路附加信息
    /// </summary>
    public class CircuitDetails
    {
        public PDSBaseOutCircuit CircuitForm { get; set; }
        public PDSProjectErrorType ErrorType { get; set; }

        

        /// <summary>
        /// 回路锁
        /// </summary>
        public bool CircuitLock { get; set; }

        public CircuitDetails()
        {
            CircuitLock = false;
        }
    }

    
}
