using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
    /// THAFAS V21.0
    /// 自动火灾报警系统控制总线Model
    /// 一个系统图里首先分楼层
    /// 其次每个楼层分不同的控制总线
    /// </summary>
    public class ThControlCircuitAlarmSystemModel
    {
        public List<ThAlarmControlWireCircuitModel> AlarmControlWireCircuits { get; set; }
    }
}
