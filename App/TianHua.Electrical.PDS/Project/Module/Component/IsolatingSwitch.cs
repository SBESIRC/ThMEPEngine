using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 隔离开关
    /// </summary>
    public class IsolatingSwitch : PDSBaseComponent
    {
        public IsolatingSwitch()
        {
            ComponentType = ComponentType.隔离开关;
        }

        /// <summary>
        /// 隔离开关类型
        /// </summary>
        public string IsolatingSwitchType { get; set; }

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
