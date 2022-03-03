using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 热继电器
    /// </summary>
    public class ThermalRelay : PDSBaseComponent
    {
        public ThermalRelay()
        {
            ComponentType = ComponentType.热继电器;
        }
        public string Content { get { return $"{ThermalRelayType} {PolesNum}A"; } }
        /// <summary>
        /// 热继电器类型
        /// </summary>
        public string ThermalRelayType { get; set; }

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
