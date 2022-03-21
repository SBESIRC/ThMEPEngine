using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.PDS.Project.Module.ProjectConfigure
{
    public class UIProjectGlobalConfiguration
    {
        /// <summary>
        /// 消防配电分支电路电缆
        /// </summary>
        public ConductorUse FireDistributionBranchCircuiCables = new ConductorUse() { OuterSheathMaterial="YJY", ConductorMaterial = "WDSUAN" };

        /// <summary>
        /// 消防配电电线
        /// </summary>
        public ConductorUse FireDistributionWire = new ConductorUse() { OuterSheathMaterial="BYJ", ConductorMaterial = "WDSUCN" };

        /// <summary>
        /// 非消防配电电缆
        /// </summary>
        public ConductorUse NonFireDistributionBranchCircuiCables = new ConductorUse() { OuterSheathMaterial="YJY", ConductorMaterial = "WDZUC" };

        /// <summary>
        /// 非消防配电电线
        /// </summary>
        public ConductorUse NonFireDistributionWire = new ConductorUse() { OuterSheathMaterial="BYJ", ConductorMaterial = "WDZUC" };
    }

    /// <summary>
    /// 导体用途
    /// </summary>
    public class ConductorUse
    {
        /// <summary>
        /// 外护套材质
        /// </summary>
        public string OuterSheathMaterial { get; set; }
        /// <summary>
        /// 导体材质
        /// </summary>
        public string ConductorMaterial { get; set; }
    }
}
