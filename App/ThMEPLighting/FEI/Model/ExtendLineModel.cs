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
        public Polyline line { get; set; }

        public List<Line> startLane = new List<Line>();

        public List<Line> endLane = new List<Line>();

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
        /// 没有遇到洞口生成的延伸线
        /// </summary>
        firstLevel,

        /// <summary>
        /// 遇到洞口生成的延伸线
        /// </summary>
        secondLevel
    }
}
