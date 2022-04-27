using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    /// <summary>
    /// Secondary_Circuit_MatchTable_Default.xlsx
    /// </summary>
    public class SecondaryCircuitConfiguration
    {
        /// <summary>
        /// 消防控制回路信息
        /// </summary>
        public static List<SecondaryCircuitInfo> FireSecondaryCircuitInfos = new List<SecondaryCircuitInfo>();

        /// <summary>
        /// 非消防控制回路信息
        /// </summary>
        public static List<SecondaryCircuitInfo> NonFireSecondaryCircuitInfos = new List<SecondaryCircuitInfo>();

        /// <summary>
        /// 控制回路配置
        /// </summary>
        public static Dictionary<string,List<SecondaryCircuitInfo>> SecondaryCircuitConfigs = new Dictionary<string, List<SecondaryCircuitInfo>>();
    }

    public class SecondaryCircuitInfo
    {
        /// <summary>
        /// 二次回路代号
        /// </summary>
        public string SecondaryCircuitCode { get; set; }

        /// <summary>
        /// 功能描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 导体根数x每根导体截面积
        /// </summary>
        public string Conductor { get; set; }

        /// <summary>
        /// 导体选择类别
        /// </summary>
        public string ConductorCategory { get; set; }
    }
}
