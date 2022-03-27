using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    /// <summary>
    /// LV_AC_Cable_Condiut_MatchTable_19DX101
    /// </summary>
    public class CableCondiutConfiguration
    {
        /// <summary>
        /// 电缆配置
        /// </summary>
        public static List<CableCondiutInfo> CableInfos = new List<CableCondiutInfo>();

        /// <summary>
        /// 电线配置
        /// </summary>
        public static List<CableCondiutInfo> CondiutInfos = new List<CableCondiutInfo>();
    }
    public class CableCondiutInfo
    {
        /// <summary>
        /// 耐火外护套
        /// </summary>
        public bool FireCoating { get; set; }

        /// <summary>
        /// 相线截面
        /// </summary>
        public double WireSphere { get; set; }

        /// <summary>
        /// 相数
        /// </summary>
        public string Phase { get; set; }

        /// <summary>
        /// SC穿管管径
        /// </summary>
        public int DIN_SC { get; set; }

        /// <summary>
        /// JDG穿管管径
        /// </summary>
        public int DIN_JDG { get; set; }

        /// <summary>
        /// PC穿管管径
        /// </summary>
        public int DIN_PC { get; set; }
    }
}
