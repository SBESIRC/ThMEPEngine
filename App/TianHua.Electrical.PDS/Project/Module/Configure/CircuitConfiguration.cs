using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Project.Module.Configure
{
    /// <summary>
    /// 回路创建配置
    /// </summary>
    public class CircuitConfiguration
    {
        /// <summary>
        /// 回路创建配置
        /// </summary>
        public static List<CircuitCreator> CircuitCreatorInfos = new List<CircuitCreator>();
    }

    /// <summary>
    /// 回路配置
    /// </summary>
    public class CircuitCreator
    {
        /// <summary>
        /// 菜单选项
        /// </summary>
        public string MenuOptions { get; set; }

        /// <summary>
        /// 子菜单选项
        /// </summary>
        public string SubmenuOptions { get; set; }

        /// <summary>
        /// 回路样式
        /// </summary>
        public string CircuitFormOutType { get; set; }

        /// <summary>
        /// 保护开关类型
        /// </summary>
        public string ProtectionSwitchType { get; set; }

        /// <summary>
        /// 功能描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 负载类型
        /// </summary>
        public PDSNodeType NodeType { get; set; }

        /// <summary>
        /// 负载类型
        /// </summary>
        public ThPDSLoadTypeCat_1 LoadTypeCat_1 { get; set; }

        /// <summary>
        /// 负载类型
        /// </summary>
        public ThPDSLoadTypeCat_2 LoadTypeCat_2 { get; set; }

        /// <summary>
        /// 负载类型
        /// </summary>
        public ThPDSLoadTypeCat_3 LoadTypeCat_3 { get; set; }

        /// <summary>
        /// 消防属性
        /// </summary>
        public bool FireLoad { get; set; }

        /// <summary>
        /// 默认项数
        /// </summary>
        public string Phase { get; set; }

        /// <summary>
        /// 导体用途
        /// </summary>
        public ConductorType ConductorType { get; set; }

        /// <summary>
        /// 辐射路径
        /// </summary>
        public ConductorLayingPath ConductorLaying { get; set; }

        /// <summary>
        /// 敷设部位1
        /// </summary>
        public LayingSite LayingSite1 { get; set; }

        /// <summary>
        /// 敷设部位2
        /// </summary>
        public LayingSite LayingSite2 { get; set; }

        /// <summary>
        /// 是否是双功率
        /// </summary>
        public bool IsDualPower { get; set; }

        /// <summary>
        /// 低功率
        /// </summary>
        public double LowPower { get; set; }

        /// <summary>
        /// 高功率
        /// </summary>
        public double HighPower { get; set; }
    }
}
