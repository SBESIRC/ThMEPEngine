using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Project.Module;

namespace TianHua.Electrical.PDS.Project.Module
{
    
    public class CircuitDetails
    {
        public ThPDSProjectGraphEdge<ThPDSProjectGraphNode> edge { get; set; }
        public CircuitFormType circuitFormType { get; set; }
        public CircuitDetails(ThPDSProjectGraphEdge<ThPDSProjectGraphNode> Edge)
        {

        }

        public List<CircuitDetails> circuitDetails { get; set; }
    }

    /// <summary>
    /// 回路映射样式
    /// </summary>
    public enum CircuitFormType
    {
        常规,
        漏电,
        接触器控制,
        热继电器保护,
        配电计量_上海CT,
        配电计量_上海直接表,
        配电计量_CT表在前,
        配电计量_直接表在前,
        配电计量_CT表在后,
        电动机_分立元件,
        电动机_CPS,
        电动机_分立元件星三角启动,
        电动机_CPS星三角启动,
        双速电动机_分立元件detailYY,
        双速电动机_分立元件YY,
        双速电动机_CPSdetailYY,
        双速电动机_CPSYY,
        消防应急照明回路WFEL,
        SPD,
        小母排,
    }
}
