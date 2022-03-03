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
        //public CircuitFormOutType CircuitFormType { get; set; }
        public PDSBaseCircuit CircuitForm { get; set; }
        public PDSProjectErrorType ErrorType { get; set; }
        //public List<PDSBaseElement> Elements { get; set;} //元器件
        public PhaseSequence PhaseSequence { get; set; }//相序
        public CircuitDetails()
        {
            //PhaseSequence = PhaseSequence.L123;
        }
    }

    
}
