using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    /// <summary>
    /// LV_Conductor_Selector_Default.xlsx
    /// </summary>
    public class ConductorConfigration
    {
        /// <summary>
        /// 电缆配置
        /// </summary>
        public static List<ConductorComponentInfo> CableConductorInfos = new List<ConductorComponentInfo>();

        /// <summary>
        /// 电线配置
        /// </summary>
        public static List<ConductorComponentInfo> WireConductorInfos = new List<ConductorComponentInfo>();
    }

    [Serializable]
    public class ConductorComponentInfo
    {
        /// <summary>
        /// 整定电流
        /// </summary>
        public double Iset { get; set; }

        /// <summary>
        /// 相线截面
        /// </summary>
        public double Sphere { get; set; }

        /// <summary>
        /// 相线数
        /// </summary>
        public int NumberOfPhaseWire { get; set; }
    }
}

