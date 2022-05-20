using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Diagram
{
    public static class ThPDSComponentMap
    {
        public static readonly Dictionary<string, string> ComponentMap = new Dictionary<string, string>
        {
            {"断路器", "E-BCB101"},
            {"隔离开关", "E-BQL102-1"},
            {"剩余电流动作断路器一体式RCD", "E-BCB102"},
            {"剩余电流动作断路器组合式RCD", "E-BCB102"},
            {"控制保护开关", "E-BCB103"},
            {"熔断器", "E-BFU101"},
            {"热继电器", "E-BKH102"},
            {"接触器", "E-BKM101"},
            {"电流互感器", "E-BCT102"},
            {"电能表", "E-BMT101"},
            {"过欠电压保护器", "过欠电压保护器"},
            {"间接表", "间接表"},
            {"直接表", "直接表"},
            {"自动转换开关", "E-BTS101"},
            {"手动转换开关", "E-BTS102"},
            {"浪涌保护器", "E-BSP101"},
            {"软启动器", "E-BSS101"},
            {"变频器", "E-BFC101"},
        };
    }
}
