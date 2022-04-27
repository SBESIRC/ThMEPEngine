using System;
using System.Collections.Generic;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    /// <summary>
    /// 电能表配置
    /// </summary>
    public class MeterTransformerConfiguration
    {
        /// <summary>
        /// 电能表MT配置
        /// </summary>
        public static List<MTComponentInfo> MeterComponentInfos = new List<MTComponentInfo>();
    }

    [Serializable]
    public class MTComponentInfo
    {
        /// <summary>
        /// 额定电流
        /// </summary>
        public double Amps { get; set; }

        /// <summary>
        /// 参数
        /// </summary>
        public string parameter { get; set; }
    }
}
