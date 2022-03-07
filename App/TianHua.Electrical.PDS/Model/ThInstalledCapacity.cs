using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Model
{
    public class ThInstalledCapacity
    {
        public ThInstalledCapacity()
        {
            UsualPower = new List<double>();
            FirePower = new List<double>();
        }

        /// <summary>
        /// 平时功率
        /// </summary>
        public List<double> UsualPower { get; set; }

        /// <summary>
        /// 消防功率
        /// </summary>
        public List<double> FirePower { get; set; }
    }
}
