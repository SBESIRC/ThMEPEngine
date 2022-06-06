using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    public class DistributionMeteringConfiguration
    {
        /// <summary>
        /// 上海住宅配置
        /// </summary>
        public static List<ShanghaiResidentialInfo> ShanghaiResidential = new List<ShanghaiResidentialInfo>();

        /// <summary>
        /// 江苏住宅配置
        /// </summary>
        public static List<JiangsuResidentialInfo> JiangsuResidential = new List<JiangsuResidentialInfo>();
    }

    public class ShanghaiResidentialInfo
    {
        /// <summary>
        /// 功率(低)
        /// </summary>
        public double LowPower { get; set; }

        /// <summary>
        /// 功率(高)
        /// </summary>
        public double HighPower { get; set; }

        /// <summary>
        /// 项数
        /// </summary>
        public ThPDSPhase Phase { get; set; }

        /// <summary>
        /// 断路器规格
        /// </summary>
        public List<string> CB1 { get; set; }

        /// <summary>
        /// 电表规格
        /// </summary>
        public List<string> MT { get; set; }

        /// <summary>
        /// 断路器规格
        /// </summary>
        public List<string> CB2 { get; set; }

        /// <summary>
        /// 导体根数x每根导体截面积
        /// </summary>
        public string Conductor { get; set; }
    }
    
    public class JiangsuResidentialInfo
    {
        /// <summary>
        /// 功率(低)
        /// </summary>
        public double LowPower { get; set; }

        /// <summary>
        /// 功率(高)
        /// </summary>
        public double HighPower { get; set; }

        /// <summary>
        /// 项数
        /// </summary>
        public ThPDSPhase Phase { get; set; }

        /// <summary>
        /// 断路器规格
        /// </summary>
        public List<string> CB { get; set; }

        /// <summary>
        /// 导体根数x每根导体截面积
        /// </summary>
        public string Conductor { get; set; }
    }
}
