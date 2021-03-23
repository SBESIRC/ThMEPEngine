using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.FEI.Model
{
    public class ExtendLineModel
    {
        public Line line { get; set; }

        public List<Line> startLane = new List<Line>();

        public Priority priority { get; set; }
    }

    public enum Priority
    {
        /// <summary>
        /// 起点延长线
        /// </summary>
        startExtendLine,

        /// <summary>
        /// 起点合并延长线
        /// </summary>
        MergeStartLine,

        /// <summary>
        /// 没有遇到洞口处于两端生成的延伸线
        /// </summary>
        firstLevel,

        /// <summary>
        /// 没有遇到洞口处于中间生成的延伸线
        /// </summary>
        secondLevel,    

        /// <summary>
        /// 遇到洞口时处于两端偏移生成的延伸线
        /// </summary>
        thirdLevel,

        /// <summary>
        /// 遇到洞口时处于中间偏移生成的延伸线
        /// </summary>
        LowestLevel,  
    }
}
