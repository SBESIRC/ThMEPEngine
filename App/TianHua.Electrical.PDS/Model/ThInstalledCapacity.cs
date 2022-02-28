using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Model
{
    public class ThInstalledCapacity
    {
        /// <summary>
        /// 平时功率
        /// </summary>
        public List<double> UsualPower { get; set; } = new List<double>();

        /// <summary>
        /// 消防功率
        /// </summary>
        public List<double> FirePower { get; set; } = new List<double>();
    }
}
