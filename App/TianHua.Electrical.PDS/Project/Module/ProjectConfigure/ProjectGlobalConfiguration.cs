using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace TianHua.Electrical.PDS.Project.Module.ProjectConfigure
{
    public class ProjectGlobalConfiguration
    {
        #region 供配电系统
        public double MunicipalPowerCircuitDefaultLength = 1000;//m 市政电源线路默认长度
        public double TreeTrunkDistributionCurrent = 250;//A 树干式配电电流
        public Feeder DefaultFeeder = Feeder.T接电缆;//默认馈线
        public Feeder OtherFeeder = Feeder.预分支电缆;//

        public double SecondaryDistributionBoxDefaultLength = 100;//m 二级配电箱线路默认长度
        public double LoopSettingCurrent = 630;//A 回路整定电流

        public double SubsequentDefaultLength = 50;//m 后续线路默认长度
        public double CalculateCurrentMagnification = 1.1;//计算电流放大倍率
        #endregion

        #region 导体及敷设管材选型
        //默认导体材料
        public ConductorMaterial DefaultConductorMaterial = ConductorMaterial.铜;

        //消防配电干线及分支干线采用""矿物绝缘电力
        //对应ui的第二行，Combox只显示 FireDistributionTrunk.OuterSheathMaterial
        /// <summary>
        /// 消防配电干线
        /// </summary>
        public ConductorUse FireDistributionTrunk = new ConductorUse() { IsSpecialConductorType = true, OuterSheathMaterial = MaterialStructure.NG_A_BTLY, ConductorType = ConductorType.消防配电干线 ,ItemsSource= new MaterialStructure[] { MaterialStructure .YJY , MaterialStructure .YJV, } };

        /// <summary>
        /// 消防配电干线外护套材质范围
        /// </summary>
        public List<MaterialStructure> FireDistributionTrunkOuterSheathMaterials = new List<MaterialStructure>() { MaterialStructure.BTTZ, MaterialStructure.BTTRZ, MaterialStructure.RTTZ, MaterialStructure.NG_A_BTLY };


        //UI的Table显示顺序为
        //ConductorType(导体用途) - ConductorMaterial(燃烧特性代号) - OuterSheathMaterial(材料特征及结构)
        /// <summary>
        /// 消防配电分支电路电缆
        /// </summary>
        public ConductorUse FireDistributionBranchCircuiCables = new ConductorUse() { OuterSheathMaterial = MaterialStructure.YJY, HalogenFree = true, LowSmoke = true, LowToxicity = true, FlameRetardant = true, Refractory = true, Level = ConductorLevel.A, ConductorType = ConductorType.消防配电分支线路, ItemsSource = new MaterialStructure[] { MaterialStructure.YJY, MaterialStructure.YJV, } };

        /// <summary>
        /// 消防配电电线
        /// </summary>
        public ConductorUse FireDistributionWire = new ConductorUse() { OuterSheathMaterial = MaterialStructure.BYJ, HalogenFree = true, LowSmoke = true, LowToxicity = true, FlameRetardant = true, Refractory = true, Level = ConductorLevel.C, ConductorType = ConductorType.消防配电电线, ItemsSource = new MaterialStructure[] { MaterialStructure.BYJ, MaterialStructure.BV, } };

        /// <summary>
        /// 非消防配电电缆
        /// </summary>
        public ConductorUse NonFireDistributionBranchCircuiCables = new ConductorUse() { OuterSheathMaterial = MaterialStructure.YJY, HalogenFree = true, LowSmoke = true, LowToxicity = true, FlameRetardant = true, Refractory = false, Level = ConductorLevel.C, ConductorType = ConductorType.非消防配电电缆 , ItemsSource = new MaterialStructure[] { MaterialStructure.YJY, MaterialStructure.BV, } };

        /// <summary>
        /// 非消防配电电线
        /// </summary>
        public ConductorUse NonFireDistributionWire = new ConductorUse() { OuterSheathMaterial = MaterialStructure.BYJ, HalogenFree = true, LowSmoke = true, LowToxicity = true, FlameRetardant = true, Refractory = false, Level = ConductorLevel.C, ConductorType = ConductorType.非消防配电电线, ItemsSource = new MaterialStructure[] { MaterialStructure.BYJ, MaterialStructure.KYJV, } };

        /// <summary>
        /// 消防配电控制电缆
        /// </summary>
        public ConductorUse FireDistributionControlCable = new ConductorUse() { OuterSheathMaterial = MaterialStructure.KYJY, HalogenFree = true, LowSmoke = true, LowToxicity = true, FlameRetardant = true, Refractory = true, Level = ConductorLevel.A, ConductorType = ConductorType.消防配电控制电缆, ItemsSource = new MaterialStructure[] { MaterialStructure.KYJY, MaterialStructure.KYJV, } };

        /// <summary>
        /// 非消防配电控制电缆
        /// </summary>
        public ConductorUse NonFireDistributionControlCable = new ConductorUse() { OuterSheathMaterial = MaterialStructure.KYJY, HalogenFree = true, LowSmoke = true, LowToxicity = true, FlameRetardant = true, Refractory = false, Level = ConductorLevel.A, ConductorType = ConductorType.非消防配电控制电缆 , ItemsSource = new MaterialStructure[] { MaterialStructure.KYJY, MaterialStructure.KYJV, } };

        /// <summary>
        /// 消防控制信号软线
        /// </summary>
        public ConductorUse FireControlSignalWire = new ConductorUse() { OuterSheathMaterial = MaterialStructure.RYJ, HalogenFree = true, LowSmoke = true, LowToxicity = true, FlameRetardant = true, Refractory = true, Level = ConductorLevel.C, ConductorType = ConductorType.消防控制信号软线, ItemsSource = new MaterialStructure[] { MaterialStructure.RYJ, MaterialStructure.RVV, } };

        /// <summary>
        /// 非消防控制信号软线
        /// </summary>
        public ConductorUse NonFireControlSignalWire = new ConductorUse() { OuterSheathMaterial = MaterialStructure.RYJ, HalogenFree = true, LowSmoke = true, LowToxicity = true, FlameRetardant = true, Refractory = false, Level = ConductorLevel.C, ConductorType = ConductorType.非消防控制信号软线, ItemsSource = new MaterialStructure[] { MaterialStructure.RYJ, MaterialStructure.RVV, } };
        #endregion

        #region 管材铺设原则
        //通用要求
        public PipeMaterial UndergroundMaterial = PipeMaterial.SC;
        public int UniversalPipeDiameter = 32;
        //消防线路
        public PipeMaterial FireOnTheGroundSmallDiameterMaterial = PipeMaterial.JDG;
        public int FirePipeDiameter = 50;
        public PipeMaterial FireOnTheGroundLargeDiameterMaterial = PipeMaterial.SC;
        //非消防线路
        public PipeMaterial NonFireOnTheGroundSmallDiameterMaterial = PipeMaterial.JDG;
        public int NonFirePipeDiameter = 50;
        public PipeMaterial NonFireOnTheGroundLargeDiameterMaterial = PipeMaterial.SC;
        #endregion

        #region 常用用电设备供电
        public MotorUIChoise MotorUIChoise = MotorUIChoise.分立元件;
        public double FireMotorPower = 45;//kw
        public FireStartType FireStartType = FireStartType.星三角启动;
        public double NormalMotorPower = 45;//kw
        public FireStartType NormalStartType = FireStartType.星三角启动;//
        public MeterBoxCircuitType MeterBoxCircuitType = MeterBoxCircuitType.国标_表在前;//电表箱出线回路类型
        public FireEmergencyLightingModel fireEmergencyLightingModel = FireEmergencyLightingModel.A型;
        public FireEmergencyLightingType fireEmergencyLightingType = FireEmergencyLightingType.集中电源;
        public CircuitSystem circuitSystem = CircuitSystem.双线制;

        public int ACChargerPower = 7;//交流电桩额定功率 kw
        public int DCChargerPower = 30;//直流电桩额定功率 kw
        #endregion
    }
    public enum FireEmergencyLightingModel
    {
        A型,
        B型,
    }
    public enum FireEmergencyLightingType
    {
        集中电源,
        应急照明配电箱,
    }
    public enum CircuitSystem
    {
        双线制,
        四线制,
    }
    public enum Feeder
    {
        T接电缆,
        预分支电缆,
        密集型母线槽,
    }
    public enum ConductorMaterial
    {
        铜,
    }
    public enum FireStartType
    {
        星三角启动,
        //软启动器启动,
        //变频器启动,
    }
    public enum MotorUIChoise
    {
        分立元件,
        CPS,
    }

    public enum MeterBoxCircuitType
    {
        [Description("上海住宅")]
        上海住宅,
        [Description("江苏住宅")]
        江苏住宅,
        [Description("国标(表在前)")]
        国标_表在前,
        [Description("国标(表在后)")]
        国标_表在后,
    }

    /// <summary>
    /// 导体用途
    /// </summary>
    [Serializable]
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
        /// 无卤
        /// </summary>
        public bool HalogenFree { get; set; }

        /// <summary>
        /// 低烟
        /// </summary>
        public bool LowSmoke { get; set; }

        /// <summary>
        /// 低毒
        /// </summary>
        public bool LowToxicity { get; set; }

        /// <summary>
        /// 阻燃
        /// </summary>
        public bool FlameRetardant { get; set; }

        /// <summary>
        /// 耐火
        /// </summary>
        public bool Refractory { get; set; }

        /// <summary>
        /// 阻燃级别
        /// </summary>
        public ConductorLevel Level { get; set; }

        /// <summary>
        /// 燃烧特性代号
        /// </summary>
        public string ConductorMaterial
        {
            get
            {
                return (HalogenFree ? "W" : "") + (LowSmoke ? "D" : "") + (LowToxicity ? "U" : "") + (FlameRetardant ? "Z" : "") + Level.ToString() + (Refractory ? "N" : "");
            }
        }

        public string ConductorMaterialAndStructure
        {
            get
            {
                return OuterSheathMaterial.GetEnumDescription();
            }
            set
            {
                OuterSheathMaterial = value.GetEnumName<MaterialStructure>();
            }
        }
        public MaterialStructure[] ItemsSource;
        public IEnumerable<string> ConductorMaterialAndStructureItemsSource
        {
            get
            {
                foreach (var v in ItemsSource)
                {
                    yield return v.GetEnumDescription();
                }
            }
        }

        /// <summary>
        /// 导体标注样式预览
        /// </summary>
        public string ConductorDimensionStyle
        {
            get
            {
                return $"{ConductorMaterial}-{OuterSheathMaterial.GetDescription()}";
            }
        }

        public bool IsSpecialConductorType { get; set; }

        public string Content
        {
            get
            {
                if (IsSpecialConductorType)
                    return OuterSheathMaterial.GetDescription();
                else
                    return ConductorDimensionStyle;
            }
        }
    }
}
