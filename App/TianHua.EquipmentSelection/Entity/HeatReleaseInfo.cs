using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.FanSelection.Model
{
    public class HeatReleaseInfo
    {
        /// <summary>
        /// 建筑类别
        /// </summary>
        public string BuildType { get; set; }

        /// <summary>
        /// 是否有喷淋
        /// </summary>
        public bool HasSprinkler { get; set; }

        /// <summary>
        /// 热释放速率
        /// </summary>
        public double ReleaseSpeed { get; set; }
    }
}
