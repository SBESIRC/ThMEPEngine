using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace TianHua.Electrical.PDS.Project.Module.ProjectConfigure
{
    public class ProjectGlobalConfiguration
    {
        #region 导体及敷设管材选型
        //默认导体材料
        //理论上要用枚举，暂时不知道是什么用，暂时用string代替
        public string DefaultConductorMaterial = "铜";

        //消防配电干线及分支干线采用""矿物绝缘电力
        //对应ui的第二行，Combox只显示 FireDistributionTrunk.OuterSheathMaterial
        /// <summary>
        /// 消防配电干线
        /// </summary>
        public ConductorUse FireDistributionTrunk = new ConductorUse() { IsSpecialConductorType = true, OuterSheathMaterial = MaterialStructure.NG_A_BTLY, ConductorType = ConductorType.消防配电干线 };


        //UI的Table显示顺序为
        //ConductorType(导体用途) - ConductorMaterial(燃烧特性代号) - OuterSheathMaterial(材料特征及结构)
        /// <summary>
        /// 消防配电分支电路电缆
        /// </summary>
        public ConductorUse FireDistributionBranchCircuiCables = new ConductorUse() { OuterSheathMaterial=MaterialStructure.YJY, ConductorMaterial = "WDUZAN", ConductorType = ConductorType.消防配电分支线路 };

        /// <summary>
        /// 消防配电电线
        /// </summary>
        public ConductorUse FireDistributionWire = new ConductorUse() { OuterSheathMaterial=MaterialStructure.BYJ, ConductorMaterial = "WDUZCN", ConductorType = ConductorType.消防配电电线 };

        /// <summary>
        /// 非消防配电电缆
        /// </summary>
        public ConductorUse NonFireDistributionBranchCircuiCables = new ConductorUse() { OuterSheathMaterial=MaterialStructure.YJY, ConductorMaterial = "WDUZA", ConductorType = ConductorType.非消防配电电缆 };

        /// <summary>
        /// 非消防配电电线
        /// </summary>
        public ConductorUse NonFireDistributionWire = new ConductorUse() { OuterSheathMaterial=MaterialStructure.BYJ, ConductorMaterial = "WDUZC", ConductorType = ConductorType.非消防配电电线 };

        /// <summary>
        /// 消防配电控制电缆
        /// </summary>
        public ConductorUse FireDistributionControlCable = new ConductorUse() { OuterSheathMaterial=MaterialStructure.KYJY, ConductorMaterial = "WDUZBN", ConductorType = ConductorType.消防配电控制电缆 };

        /// <summary>
        /// 非消防配电控制电缆
        /// </summary>
        public ConductorUse NonFireDistributionControlCable = new ConductorUse() { OuterSheathMaterial=MaterialStructure.KYJY, ConductorMaterial = "WDUZB", ConductorType = ConductorType.非消防配电控制电缆 };

        /// <summary>
        /// 消防控制信号软线
        /// </summary>
        public ConductorUse FireControlSignalWire = new ConductorUse() { OuterSheathMaterial=MaterialStructure.RYJ, ConductorMaterial = "WDUZDN", ConductorType = ConductorType.消防控制信号软线 };

        /// <summary>
        /// 非消防控制信号软线
        /// </summary>
        public ConductorUse NonFireControlSignalWire = new ConductorUse() { OuterSheathMaterial=MaterialStructure.RYJ, ConductorMaterial = "WDUZD", ConductorType = ConductorType.非消防控制信号软线 };
        #endregion

        #region 管材铺设原则
        //通用要求
        public PipeMaterial UndergroundMaterial = PipeMaterial.SC;
        //消防线路
        public PipeMaterial FireOnTheGroundSmallDiameterMaterial = PipeMaterial.JDG;
        public PipeMaterial FireOnTheGroundLargeDiameterMaterial = PipeMaterial.SC;
        //非消防线路
        public PipeMaterial NonFireOnTheGroundSmallDiameterMaterial = PipeMaterial.JDG;
        public PipeMaterial NonFireOnTheGroundLargeDiameterMaterial = PipeMaterial.SC;
        #endregion

        #region 常用用电设备供电
        public string MotorUIChoise = "分立元件";//UI端的Combox要做成分立元件和CPS，两种模式
        //消防电动机
        public double FireMotorPower = 45;//kw
        public string FireStartType = "星三角启动";//这里也要做成Combox，选择有"星三角启动","软启动器启动","变频器启动"
        //普通电动机
        public double NormalMotorPower = 45;//kw
        public string NormalStartType = "星三角启动";//同上
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
        /// 燃烧特性代号
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
