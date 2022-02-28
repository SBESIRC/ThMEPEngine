using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.Project.Module
{
    /// <summary>
    /// 回路附加信息
    /// </summary>
    public class CircuitDetails
    {
        public CircuitFormOutType CircuitFormType { get; set; }
        public PDSProjectErrorType ErrorType { get; set; }
        public List<PDSBaseElement> Elements { get; set;} //元器件
        public int PhaseSequence { get; set; }//相序
        public CircuitDetails()
        {
            CircuitFormType = CircuitFormOutType.None;

        }
    }

    
}
