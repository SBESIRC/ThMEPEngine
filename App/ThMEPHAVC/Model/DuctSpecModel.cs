using System.Collections.Generic;

namespace ThMEPHVAC.Model
{
    public class DuctSpecModel
    {
        /// <summary>
        /// 风量（双速风机）
        /// </summary>
        public string StrAirVolume { get; set; }
        /// <summary>
        /// 风量
        /// </summary>
        public double AirVolume { get; set; }

        /// <summary>
        /// 风速
        /// </summary>
        public double AirSpeed { get; set; }

        /// <summary>
        /// 最大风速
        /// </summary>
        public double MaxAirSpeed { get; set; }

        /// <summary>
        /// 最小风速
        /// </summary>
        public double MinAirSpeed { get; set; }


        /// <summary>
        /// 机房外管段
        /// </summary>
        public List<string> ListOuterTube { get; set; }


        public string OuterTube { get; set; }


        /// <summary>
        /// 机房内管段
        /// </summary>
        public List<string> ListInnerTube { get; set; }


        public string InnerTube { get; set; }

    }
}
