using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.Garage.Model
{
    public class ThLightArrangeUiParameter
    {
        /// <summary>
        /// 单排布置
        /// </summary>
        public bool IsSingleRow { get; set; }
        /// <summary>
        /// 线槽宽度
        /// </summary>
        public double Width { get; set; }
        /// <summary>
        /// 线槽间距
        /// </summary>
        public double DoubleRowOffsetDis { get; set; }
        /// <summary>
        /// 灯间距
        /// </summary>
        public double Interval { get; set; }
        /// <summary>
        /// 指定回路数量
        /// </summary>
        public int LoopNumber { get; set; }
        /// <summary>
        /// 自动计算回路数量
        /// </summary>
        public bool AutoCalculate { get; set; }
    }
}
