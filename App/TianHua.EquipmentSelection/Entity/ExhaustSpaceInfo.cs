using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.FanSelection.Model
{
    public class ExhaustSpaceInfo
    {
        /// <summary>
        /// 空间净高(m)
        /// </summary>
        public double SpaceNetHeight { get; set; }

        /// <summary>
        /// 空间类型
        /// </summary>
        public string SpaceType { get; set; }

        /// <summary>
        /// 是否有喷淋
        /// </summary>
        public bool HasSprinkler { get; set; }

        /// <summary>
        /// 最小风量
        /// </summary>
        public double MinVolume { get; set; }

        /// <summary>
        /// 自然排烟侧窗(口)部风速(m/s)
        /// </summary>
        public double WindSpeedNature { get; set; }
    }
}
