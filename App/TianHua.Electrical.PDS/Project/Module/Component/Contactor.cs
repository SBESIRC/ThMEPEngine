using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 接触器
    /// </summary>
    public class Contactor : PDSBaseComponent
    {
        public Contactor()
        {
            ComponentType = ComponentType.接触器;
        }

        public string Content { get { return $"{ContactorType} {RatedCurrent}/{PolesNum}"; } }


        /// <summary>
        /// 接触器类型
        /// </summary>
        public string ContactorType { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 额定电流
        /// </summary>
        public string RatedCurrent { get; set; }
    }
}
