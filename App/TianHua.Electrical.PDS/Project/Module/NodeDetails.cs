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
        public bool IsDualPower { get; set; }
        public double LowPower { get; set; }
        public double HighPower { get; set; }

        public bool IsOnlyLoad { get; set; }

        public List<PDSBaseElement> Elements { get; set; } //元器件

        public int PhaseSequence { get; set; }//相序
        public NodeDetails()
        {
            CircuitFormType = new OneWayInCircuit();
        }
    }
}
