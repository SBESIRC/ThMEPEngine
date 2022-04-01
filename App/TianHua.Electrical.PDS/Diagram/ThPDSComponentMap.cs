using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Diagram
{
    public static class ThPDSComponentMap
    {
        public static readonly Dictionary<string, string> ComponentMap = new Dictionary<string, string>
        {
            {"断路器", "E-BCB101"},
            {"隔离开关", "E-BQL102-1"},
            {"剩余电流动作断路器", "E-BCB102"},
            {"电动机综合保护开关", "E-BCB103"},
            {"熔断器", "E-BFU101"},
            {"热继电器", "E-BKH102"},
            {"接触器", "E-BKM101"},
            {"电流互感器", "E-BCT102"},
            {"电能表", "E-BMT101"},
            {"自动转换开关", "E-BTS101"},
            {"手动转换开关", "E-BTS102"},
            {"浪涌保护器", "E-BSP101"},
            {"软启动器", "E-BSS101"},
            {"变频器", "E-BFC101"},
        };
    }
}
