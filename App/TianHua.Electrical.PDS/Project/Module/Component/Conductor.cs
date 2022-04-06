using System;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    public class Conductor : PDSBaseComponent
    {
        /// <summary>
        /// 导体
        /// </summary>
        public Conductor(double calculateCurrent, ThPDSPhase phase, ThPDSCircuitType circuitType, ThPDSLoadTypeCat_1 loadType, bool FireLoad, bool ViaConduit, bool ViaCableTray, string FloorNumber)
        {
            this.ComponentType = ComponentType.Conductor;
            Phase = phase;
            ChooseMaterial(loadType, FireLoad, calculateCurrent);
            ChooseCrossSectionalArea(calculateCurrent);
            ChooseLaying(FloorNumber, circuitType, phase, ViaConduit, ViaCableTray, FireLoad);
        }

        public Conductor(string conductorConfig , double calculateCurrent, ThPDSPhase phase, ThPDSCircuitType circuitType, ThPDSLoadTypeCat_1 loadType, bool FireLoad, bool ViaConduit, bool ViaCableTray, string FloorNumber)
        {
            this.ComponentType = ComponentType.Conductor;
            this.IsMotor = true;
            this.Phase = phase;
            //3x2.5+E2.5
            ChooseMaterial(loadType, FireLoad, calculateCurrent);
            ChooseCrossSectionalArea(conductorConfig);
            ChooseLaying(FloorNumber, circuitType, phase, ViaConduit, ViaCableTray, FireLoad);
        }

        /// <summary>
        /// 选型材料
        /// </summary>
        /// <param name="circuitType"></param>
        /// <param name="fireLoad"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ChooseMaterial(ThPDSLoadTypeCat_1 circuitType, bool fireLoad, double calculateCurrent)
        {
            var config = PDSProject.Instance.projectGlobalConfiguration;
            if (circuitType == ThPDSLoadTypeCat_1.Luminaire)
            {
                if (fireLoad)
                {
                    if (Phase == ThPDSPhase.三相 && calculateCurrent >= 200)
                    {
                        this.ConductorUse = config.FireDistributionBranchCircuiCables;
                        IsWire = false;
                    }
                    else
                    {
                        this.ConductorUse = config.FireDistributionWire;
                        IsWire = true;
                    }
                }
                else
                {
                    if (Phase == ThPDSPhase.三相 && calculateCurrent >= 200)
                    {
                        this.ConductorUse = config.NonFireDistributionBranchCircuiCables;
                        IsWire = false;
                    }
                    else
                    {
                        this.ConductorUse = config.NonFireDistributionWire;
                        IsWire = true;
                    }

                }
            }
            else if (circuitType == ThPDSLoadTypeCat_1.Socket)
            {
                if (fireLoad)
                {
                    if (Phase == ThPDSPhase.三相 && calculateCurrent >= 200)
                    {
                        this.ConductorUse = config.FireDistributionBranchCircuiCables;
                        IsWire = false;
                    }
                    else
                    {
                        this.ConductorUse = config.FireDistributionWire;
                        IsWire = true;
                    }
                }
                else
                {
                    if (Phase == ThPDSPhase.三相 && calculateCurrent >= 200)
                    {
                        this.ConductorUse = config.NonFireDistributionBranchCircuiCables;
                        IsWire = false;
                    }
                    else
                    {
                        this.ConductorUse = config.NonFireDistributionWire;
                        IsWire = true;
                    }
                }
            }
            else if (circuitType == ThPDSLoadTypeCat_1.Motor)
            {
                if (fireLoad)
                {
                    if (Phase == ThPDSPhase.一相)
                    {
                        this.ConductorUse = config.FireDistributionWire;
                    }
                    else
                    {
                        this.ConductorUse = config.FireDistributionBranchCircuiCables;
                    }
                }
                else
                {
                    if (Phase == ThPDSPhase.一相)
                        this.ConductorUse = config.NonFireDistributionWire;
                    else
                        this.ConductorUse = config.NonFireDistributionBranchCircuiCables;
                }
            }
            else if (circuitType == ThPDSLoadTypeCat_1.DistributionPanel)
            {
                if (fireLoad)
                {
                    if (Phase == ThPDSPhase.一相)
                    {
                        this.ConductorUse = config.FireDistributionWire;
                    }
                    else
                        this.ConductorUse = config.FireDistributionTrunk;
                }
                else
                {
                    if (Phase == ThPDSPhase.一相)
                        this.ConductorUse = config.NonFireDistributionWire;
                    else
                        this.ConductorUse = config.NonFireDistributionBranchCircuiCables;
                }
            }
            else if (circuitType == ThPDSLoadTypeCat_1.LumpedLoad)
            {
                if (fireLoad)
                {
                    if (Phase == ThPDSPhase.一相)
                    {
                        this.ConductorUse = config.FireDistributionWire;
                    }
                    else
                    {
                        this.ConductorUse = config.FireDistributionBranchCircuiCables;
                    }
                }
                else
                {
                    if (Phase == ThPDSPhase.一相)
                        this.ConductorUse = config.NonFireDistributionWire;
                    else
                        this.ConductorUse = config.NonFireDistributionBranchCircuiCables;
                }
            }
            if (ConductorUse.ConductorMaterial.Contains('N'))
            {
                Refractory = true;
            }
        }

        /// <summary>
        /// 选型横截面积
        /// </summary>
        /// <param name="calculateCurrent"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ChooseCrossSectionalArea(double calculateCurrent)
        {
            var Allconfigs = IsWire ? ConductorConfigration.WireConductorInfos : ConductorConfigration.CableConductorInfos;
            var configs = Allconfigs.Where(o => o.Iset > calculateCurrent).ToList();
            if (configs.Count <= 0)
            {
                throw new NotSupportedException();
            }
            else
            {
                var config = configs.First();
                this.NumberOfPhaseWire = config.NumberOfPhaseWire;
                var Sphere = config.Sphere;
                this.ConductorCrossSectionalArea = Sphere;
                //电缆根数 只有1和2
                if (NumberOfPhaseWire == 1)
                {
                    AlternativeNumberOfPhaseWire = new List<int>() { 1, 2 };
                }
                else
                {
                    AlternativeNumberOfPhaseWire = new List<int>() { 2 };
                }
                AlternativeConductorCrossSectionalAreas = configs.Select(o => o.Sphere).ToList();
                CalculateCrossSectionalArea(Sphere);
            }
        }

        /// <summary>
        /// 选型横截面积
        /// </summary>
        /// <param name="calculateCurrent"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ChooseCrossSectionalArea(string conductorConfig)
        {
            //case
            //3x2.5+E2.5 ; 3x2.5 ; 2x(3x70) ; 母线槽500A
            string config = conductorConfig;
            var Allconfigs = IsWire ? ConductorConfigration.WireConductorInfos : ConductorConfigration.CableConductorInfos;
            {
                if (config.Contains('('))
                {
                    this.NumberOfPhaseWire = 2;
                    int index1 = config.IndexOf('(');
                    int index2 = config.IndexOf(')');
                    config = config.Substring(index1 + 1, index2 - index1);
                }
                else
                {
                    this.NumberOfPhaseWire = 1;
                }
                if (config.Contains('E'))
                {
                    int index = config.IndexOf('+');
                    config = config.Substring(0, index);
                }
                string[] conductorInfos = config.Split('x');
                this.ConductorCrossSectionalArea = double.Parse(conductorInfos[1]);
                AlternativeNumberOfPhaseWire = new List<int>() { NumberOfPhaseWire };
                AlternativeConductorCrossSectionalAreas = new List<double>() { ConductorCrossSectionalArea };
                CalculateCrossSectionalArea(ConductorCrossSectionalArea);
            }
        }

        /// <summary>
        /// 计算PE线导体横截面积
        /// </summary>
        /// <param name="conductorCrossSectionalArea"></param>
        private void CalculateCrossSectionalArea(double conductorCrossSectionalArea)
        {
            if (conductorCrossSectionalArea <= 16)
            {
                this.PECrossSectionalArea = conductorCrossSectionalArea;
            }
            else if (conductorCrossSectionalArea <= 35)
            {
                this.PECrossSectionalArea = 16;
            }
            else if (conductorCrossSectionalArea <= 400)
            {
                this.PECrossSectionalArea = conductorCrossSectionalArea / 2;
            }
            else if (conductorCrossSectionalArea <= 800)
            {
                this.PECrossSectionalArea = 200;
            }
            else
            {
                this.PECrossSectionalArea = conductorCrossSectionalArea / 4;
            }
            if (AllMotor)
            {
                NeutralConductorCrossSectionalArea = PECrossSectionalArea;
            }
            else
            {
                NeutralConductorCrossSectionalArea = ConductorCrossSectionalArea;
            }
        }

        /// <summary>
        /// 桥架/穿管敷设方式选型
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void ChooseLaying(string floorNumber, ThPDSCircuitType circuitType, ThPDSPhase phase, bool viaConduit, bool viaCableTray, bool fireLoad)
        {
            var config = PDSProject.Instance.projectGlobalConfiguration;
            ViaCableTray = viaCableTray;
            ViaConduit = viaConduit;
            if (viaCableTray)
            {
                switch (circuitType)
                {
                    case ThPDSCircuitType.Lighting:
                        {
                            this.BridgeLaying = BridgeLaying.MR;
                            break;
                        }
                    case ThPDSCircuitType.Socket:
                        {
                            this.BridgeLaying = BridgeLaying.MR;
                            break;
                        }
                    case ThPDSCircuitType.EmergencyLighting:
                        {
                            this.BridgeLaying = BridgeLaying.MR;
                            break;
                        }
                    case ThPDSCircuitType.PowerEquipment:
                        {
                            this.BridgeLaying = BridgeLaying.CT;
                            break;
                        }
                    case ThPDSCircuitType.EmergencyPowerEquipment:
                        {
                            this.BridgeLaying = BridgeLaying.CT;
                            break;
                        }
                    case ThPDSCircuitType.FireEmergencyLighting:
                        {
                            this.BridgeLaying = BridgeLaying.MR;
                            break;
                        }
                    case ThPDSCircuitType.Control:
                        {
                            this.BridgeLaying = BridgeLaying.MR;
                            break;
                        }
                    default:
                        {
                            throw new NotSupportedException();
                        }
                }
            }
            if (viaConduit)
            {
                if (ConductorUse.IsSpecialConductorType)
                {
                    this.PipeDiameter = 0;
                    Pipelaying = Pipelaying.E;
                }
                else
                {
                    bool IsBasement = floorNumber.Contains('B');
                    var phapeNum = phase == ThPDSPhase.一相 ? "1P" : "3P";
                    var CableCondiutConfigs = (IsWire ? CableCondiutConfiguration.CondiutInfos : CableCondiutConfiguration.CableInfos).Where(o => (IsWire || o.FireCoating == Refractory) && o.Phase == phapeNum);
                    var CableCondiutConfig = CableCondiutConfigs.First(o => o.WireSphere >= ConductorCrossSectionalArea);
                    if (IsBasement)
                    {
                        this.PipeMaterial = config.UndergroundMaterial;
                        this.PipeDiameter = CableCondiutConfig.DIN_SC;
                    }
                    else
                    {
                        var SmallDiameterMaterial = fireLoad ? config.FireOnTheGroundSmallDiameterMaterial : config.NonFireOnTheGroundSmallDiameterMaterial;
                        var LargeDiameterMaterial = fireLoad ? config.FireOnTheGroundLargeDiameterMaterial :
                            config.NonFireOnTheGroundLargeDiameterMaterial;
                        this.PipeMaterial = SmallDiameterMaterial;
                        if (PipeMaterial == PipeMaterial.JDG && CableCondiutConfig.DIN_JDG > 0)
                        {
                            this.PipeDiameter = CableCondiutConfig.DIN_JDG;
                        }
                        else if (PipeMaterial == PipeMaterial.PC && CableCondiutConfig.DIN_PC > 0)
                        {
                            this.PipeDiameter = CableCondiutConfig.DIN_PC;
                        }
                        else
                        {
                            this.PipeMaterial = LargeDiameterMaterial;
                            this.PipeDiameter = CableCondiutConfig.DIN_SC;
                        }
                    }

                    //计算铺设方式
                    //管径不超过20的SC管、管径不超过25的JDG或PC管用于照明回路、插座回路、应急照明回路、消防应急照明回路、控制回路时，默认穿管暗敷，其他情况均为穿管明敷。
                    if ((this.PipeMaterial == PipeMaterial.SC && this.PipeDiameter <= 20)
                        || (this.PipeMaterial == PipeMaterial.JDG && this.PipeDiameter <= 25)
                        || (this.PipeMaterial == PipeMaterial.PC && this.PipeDiameter <= 25)
                        && (circuitType != ThPDSCircuitType.PowerEquipment))
                    {
                        Pipelaying = Pipelaying.C;
                    }
                    else
                    {
                        Pipelaying = Pipelaying.E;
                    }
                }
            }
        }

        //public string Content { get { return "WDZAN-YJY-4x25+E16-CT/SC50-E"; } }
        //外护套材质-导体材质-导体根数x每根导体截面积-桥架敷设方式/穿管直径-穿管敷设方式
        //public string Content { get { return $"{OuterSheathMaterial}-{ConductorMaterial}-{ConductorInfo}-{BridgeLaying}/{PipeDiameter}-{Pipelaying}"; } }
        public string Content { get { return $"{ConductorUse.Content}-{ConductorInfo}-{LayingTyle}"; } }

        /// <summary>
        /// 燃烧特性代号
        /// </summary>
        public string ConductorMaterial { get { return ConductorUse.ConductorMaterial; } }

        /// <summary>
        /// 材料特征及结构
        /// </summary>
        public string OuterSheathMaterial { get { return ConductorUse.OuterSheathMaterial.GetDescription(); } }

        /// <summary>
        /// 电缆根数
        /// </summary>
        public int NumberOfPhaseWire { get; set; }

        /// <summary>
        /// 相导体截面
        /// </summary>
        public double ConductorCrossSectionalArea { get; set; }

        /// <summary>
        /// 中性线导体截面
        /// </summary>
        public double NeutralConductorCrossSectionalArea { get; set; }

        /// <summary>
        /// PE线导体截面
        /// </summary>
        public double PECrossSectionalArea { get; set; }

        /// <summary>
        /// 穿管敷设方式
        /// </summary>
        public Pipelaying Pipelaying { get; set; }

        /// <summary>
        /// 穿管直径
        /// </summary>
        public int PipeDiameter { get; set; }

        /// <summary>
        /// 获取全部电缆根数
        /// </summary>
        /// <returns></returns>
        public List<int> GetNumberOfPhaseWires()
        {
            return AlternativeNumberOfPhaseWire;
        }

        public void SetNumberOfPhaseWire(int numberOfPhaseWire)
        {
            this.NumberOfPhaseWire = numberOfPhaseWire;
        }

        /// <summary>
        /// 获取全部相导体截面
        /// </summary>
        /// <returns></returns>
        public List<double> GetConductorCrossSectionalAreas()
        {
            return AlternativeConductorCrossSectionalAreas;
        }

        public void SetConductorCrossSectionalArea(double conductorCrossSectionalArea)
        {
            this.ConductorCrossSectionalArea = conductorCrossSectionalArea;
            CalculateCrossSectionalArea(conductorCrossSectionalArea);
        }

        #region Private Property

        /// <summary>
        /// 桥架敷设方式/穿管直径-穿管敷设方式
        /// </summary>
        private string LayingTyle
        {
            get
            {
                var ViaConduitStr = ConductorUse.IsSpecialConductorType ? Pipelaying.ToString() : PipeMaterial + PipeDiameter + "-" + Pipelaying;
                if (ViaCableTray && ViaConduit)
                {
                    return $"{this.BridgeLaying}/ {ViaConduitStr }";
                }
                else if (ViaCableTray)
                {
                    return this.BridgeLaying.ToString();
                }
                else if (ViaConduit)
                {
                    return ViaConduitStr;
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// 导体根数x每根导体截面积
        /// </summary>
        private string ConductorInfo
        {
            get
            {
                if (Phase == ThPDSPhase.一相)
                {
                    return $"1×{ConductorCrossSectionalArea}{(HasPELine ? "+E"+PECrossSectionalArea : "")}";
                }
                else
                {
                    string val = string.Empty;
                    if (IsMotor)
                    {
                        val = $"3×{ConductorCrossSectionalArea}{(HasPELine ? "+E"+PECrossSectionalArea : "")}";
                    }
                    else
                    {
                        if (AllMotor)
                        {
                            val = $"3×{ConductorCrossSectionalArea}{(HasPELine ? "+2×E"+PECrossSectionalArea : "")}";
                        }
                        else
                        {
                            val = $"4×{ConductorCrossSectionalArea}{(HasPELine ? "+E"+PECrossSectionalArea : "")}";
                        }
                    }
                    if (NumberOfPhaseWire != 1)
                    {
                        val = $"{NumberOfPhaseWire}×({val})";
                    }
                    return val;
                }
            }
        }

        /// <summary>
        /// 备选电缆根数
        /// </summary>
        private List<int> AlternativeNumberOfPhaseWire { get; set; }

        /// <summary>
        /// 备选相导体截面
        /// </summary>
        private List<double> AlternativeConductorCrossSectionalAreas { get; set; }

        /// <summary>
        /// 上级所有回路是否全是电动机
        /// </summary>
        private bool AllMotor { get; set; } = false;

        /// <summary>
        /// 导体耐火材质
        /// </summary>
        private bool Refractory { get; set; }

        /// <summary>
        /// 是否穿桥架
        /// </summary>
        private bool ViaCableTray { get; set; }

        /// <summary>
        /// 穿管管材
        /// </summary>
        public PipeMaterial PipeMaterial { get; set; }

        /// <summary>
        /// 穿管
        /// </summary>
        private bool ViaConduit { get; set; }

        /// <summary>
        /// 级数
        /// </summary>
        private ThPDSPhase Phase { get; set; }

        /// <summary>
        /// 导体材质
        /// </summary>
        private ConductorUse ConductorUse { get; set; }

        /// <summary>
        /// 桥架敷设方式
        /// </summary>
        public BridgeLaying BridgeLaying { get; set; }

        /// <summary>
        /// 是否是电线回路 Y(电线)/F(电缆)
        /// </summary>
        private bool IsWire { get; set; }

        /// <summary>
        /// 是否拥有PE线
        /// </summary>
        private bool HasPELine { get; set; } = true;

        /// <summary>
        /// 是否是电动机回路
        /// </summary>
        private bool IsMotor { get; set; } = false;
        #endregion
    }
}
