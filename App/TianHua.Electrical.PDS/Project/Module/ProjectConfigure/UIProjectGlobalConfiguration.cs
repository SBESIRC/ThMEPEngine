using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace TianHua.Electrical.PDS.Project.Module.ProjectConfigure
{
    public class UIProjectGlobalConfiguration
    {
        #region 导体及敷设管材选型
        /// <summary>
        /// 消防配电分支电路电缆
        /// </summary>
        public ConductorUse FireDistributionBranchCircuiCables = new ConductorUse() { OuterSheathMaterial=MaterialStructure.YJY, ConductorMaterial = "WDSUAN", ConductorType = ConductorType.消防配电分支线路 };

        /// <summary>
        /// 消防配电电线
        /// </summary>
        public ConductorUse FireDistributionWire = new ConductorUse() { OuterSheathMaterial=MaterialStructure.BYJ, ConductorMaterial = "WDSUCN", ConductorType = ConductorType.消防配电电线 };

        /// <summary>
        /// 非消防配电电缆
        /// </summary>
        public ConductorUse NonFireDistributionBranchCircuiCables = new ConductorUse() { OuterSheathMaterial=MaterialStructure.YJY, ConductorMaterial = "WDZUC", ConductorType = ConductorType.非消防配电电缆 };

        /// <summary>
        /// 非消防配电电线
        /// </summary>
        public ConductorUse NonFireDistributionWire = new ConductorUse() { OuterSheathMaterial=MaterialStructure.BYJ, ConductorMaterial = "WDZUC", ConductorType = ConductorType.非消防配电电线 };

        /// <summary>
        /// 消防配电干线
        /// </summary>
        public ConductorUse FireDistributionTrunk = new ConductorUse() { IsSpecialConductorType = true, OuterSheathMaterial = MaterialStructure.NG_A_BTLY, ConductorType = ConductorType.消防配电干线 };
        #endregion

        #region 管材铺设原则
        public PipeMaterial UndergroundMaterial = PipeMaterial.SC;
        public PipeMaterial FireOnTheGroundSmallDiameterMaterial = PipeMaterial.JDG;
        public PipeMaterial FireOnTheGroundLargeDiameterMaterial = PipeMaterial.SC;
        public PipeMaterial NonFireOnTheGroundSmallDiameterMaterial = PipeMaterial.JDG;
        public PipeMaterial NonFireOnTheGroundLargeDiameterMaterial = PipeMaterial.SC;
        #endregion
    }

    /// <summary>
    /// 导体用途
    /// </summary>
    public class ConductorUse
    {
        /// <summary>
        /// 导体用途
        /// </summary>
        public ConductorType ConductorType { get; set; }

        /// <summary>
        /// 外护套材质
        /// </summary>
        public MaterialStructure OuterSheathMaterial { get; set; }

        /// <summary>
        /// 导体材质
        /// </summary>
        public string ConductorMaterial { get; set; }

        public bool IsSpecialConductorType { get; set; }

        public string Content { 
            get 
            { 
                if(IsSpecialConductorType)
                    return OuterSheathMaterial.GetDescription(); 
                else
                    return ConductorMaterial + "-" + OuterSheathMaterial.GetDescription();
            } 
        }
    }
}
