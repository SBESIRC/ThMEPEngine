using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.AFASRegion.Model
{
    public enum AFASDetector
    {
        /// <summary>
        /// 感温探测器20㎡
        /// </summary>
        TemperatureDetectorLow = 20,

        /// <summary>
        /// 感温探测器30㎡
        /// </summary>
        TemperatureDetectorHigh = 30,

        /// <summary>
        /// 感烟探测器60㎡
        /// </summary>
        SmokeDetectorLow = 60,

        /// <summary>
        /// 感烟探测器80㎡
        /// </summary>
        SmokeDetectorHigh = 80,
    }
}
