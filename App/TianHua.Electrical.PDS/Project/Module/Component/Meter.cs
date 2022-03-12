using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    /// <summary>
    /// 电表
    /// </summary>
    public abstract class Meter : PDSBaseComponent
    {

    }

    /// <summary>
    /// 电能表
    /// </summary>
    public class MeterTransformer : Meter
    {
        /// <summary>
        /// 电能表
        /// </summary>
        /// <param name="calculateCurrent">计算电流</param>
        public MeterTransformer(double calculateCurrent)
        {
            this.ComponentType = ComponentType.MT;
        }
        public string Content { get { return $"{MeterSwitchType}"; } }

        /// <summary>
        /// 电能表类型
        /// </summary>
        public string MeterSwitchType { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 额定电流
        /// </summary>
        public string RatedCurrent { get; set; }
    }

    /// <summary>
    /// 间接表
    /// </summary>
    public class CurrentTransformer : Meter
    {
        /// <summary>
        /// 间接表
        /// </summary>
        public CurrentTransformer(double calculateCurrent)
        {
            this.ComponentType = ComponentType.CT;
        }
        public string ContentMT { get { return $"{MTSwitchType}"; } }

        public string ContentCT { get { return $"{CTSwitchType}"; } }

        /// <summary>
        /// 电能表类型
        /// </summary>
        public string MTSwitchType { get; set; }

        /// <summary>
        /// 间接表类型
        /// </summary>
        public string CTSwitchType { get; set; }

        /// <summary>
        /// 极数
        /// </summary>
        public string PolesNum { get; set; }

        /// <summary>
        /// 额定电流
        /// </summary>
        public string RatedCurrent { get; set; }
    }
}
