using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Circuit;
using TianHua.Electrical.PDS.Project.Module.Circuit.IncomingCircuit;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 节点附加信息
    /// </summary>
    public class NodeDetails
    {
        //public CircuitFormInType CircuitFormType { get; set; }
        public PDSBaseInCircuit CircuitFormType { get; set; }
        public PDSProjectErrorType ErrorType { get; set; }

        /// <summary>
        /// 是否是双功率
        /// </summary>
        public bool IsDualPower { get; set; }
        /// <summary>
        /// 是否已统计功率
        /// </summary>
        public bool IsStatisticalPower { get; set; }
        public double LowPower { get; set; }
        public double HighPower { get; set; }

        public bool IsOnlyLoad { get; set; }

        /// <summary>
        /// 相序
        /// </summary>
        public PhaseSequence PhaseSequence { get; set; }

        public NodeDetails()
        {
            CircuitFormType = new OneWayInCircuit();
        }
    }
}
