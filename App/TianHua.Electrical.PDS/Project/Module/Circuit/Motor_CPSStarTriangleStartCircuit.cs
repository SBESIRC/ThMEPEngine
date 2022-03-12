using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.Project.Module.Circuit
{
    /// <summary>
    /// 电动机-CPS星三角启动 回路
    /// </summary>
    public class Motor_CPSStarTriangleStartCircuit : PDSBaseOutCircuit
    {
        public Motor_CPSStarTriangleStartCircuit()
        {
            CircuitFormType = CircuitFormOutType.电动机_CPS星三角启动;
        }

        /// <summary>
        /// 坑位1: CPS
        /// </summary>
        public CPS cps { get; set; }

        /// <summary>
        /// 坑位2：接触器1
        /// </summary>
        public Contactor contactor1 { get; set; }

        /// <summary>
        /// 坑位3：接触器2
        /// </summary>
        public Contactor contactor2 { get; set; }
    }
}
