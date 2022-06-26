using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.UI.Models
{
    /// <summary>
    /// 变压器
    /// </summary>
    public class ThPDSTransformer
    {
        /// <summary>
        /// 变压器编号
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// 负载率
        /// </summary>
        public double LoadRate { get; set; }
    }
}
