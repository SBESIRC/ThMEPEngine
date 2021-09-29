using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.AFASRegion.Model
{
    public class ScoringModel
    {
        /// <summary>
        /// 是否是一个合法图形，如不合法，则不允许合并
        /// </summary>
        public bool IsLegalRegion { get; set; } = true;

        /// <summary>
        /// 是否是一个标准矩形
        /// </summary>
        public bool IsPerfectUnionRegion { get; set; } = false;

        /// <summary>
        /// 合并的共边性（是否共边权重达到既定值）
        /// </summary>
        public bool IsCoedge { get; set; } = true;

        /// <summary>
        /// 是否是个面积差距极大区域
        /// </summary>
        public bool IsAreaGap { get; set; } = false;

        /// <summary>
        /// 合并得分
        /// </summary>
        public double Score { get; set; } = 0;
    }
}
