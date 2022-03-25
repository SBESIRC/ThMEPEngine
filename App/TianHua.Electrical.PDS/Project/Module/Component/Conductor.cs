using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
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
        public Conductor(double calculateCurrent, ThPDSPhase phase, ThPDSCircuitType circuitType , ThPDSLoadTypeCat_1 loadType,bool FireLoad,bool ViaConduit,bool ViaCableTray,string FloorNumber)
        {
            this.ComponentType = ComponentType.导体;
            ChooseMaterial(loadType, FireLoad, calculateCurrent, phase);
            ChooseCrossSectionalArea(calculateCurrent);
            ChooseLaying(FloorNumber,circuitType, phase, ViaConduit, ViaCableTray, FireLoad);
        }

        /// <summary>
        /// 选型材料
        /// </summary>
        /// <param name="circuitType"></param>
        /// <param name="fireLoad"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void ChooseMaterial(ThPDSLoadTypeCat_1 circuitType, bool fireLoad, double calculateCurrent, ThPDSPhase phase)
        {
            var config = PDSProject.Instance.projectGlobalConfiguration;
            if (circuitType == ThPDSLoadTypeCat_1.Luminaire)
            {
                if (fireLoad)
                {
                    if (phase== ThPDSPhase.三相 && calculateCurrent >= 200)
                    {
                        this.conductorUse = config.FireDistributionBranchCircuiCables;
                        IsWire  = false;
                    }
                    else
                    {
                        this.conductorUse = config.FireDistributionWire;
                        IsWire  = true;
                    }
                }
                else
                {
                    if (phase== ThPDSPhase.三相 && calculateCurrent >= 200)
                    {
                        this.conductorUse = config.NonFireDistributionBranchCircuiCables;
                        IsWire  = false;
                    }
                    else
                    {
                        this.conductorUse = config.NonFireDistributionWire;
                        IsWire  = true;
                    }
                    
                }
            }
            else if (circuitType == ThPDSLoadTypeCat_1.Socket)
            {
                if (fireLoad)
                {
                    if (phase== ThPDSPhase.三相 && calculateCurrent >= 200)
                    {
                        this.conductorUse = config.FireDistributionBranchCircuiCables;
                        IsWire  = false;
                    }
                    else
                    {
                        this.conductorUse = config.FireDistributionWire;
                        IsWire  = true;
                    }
                }
                else
                {
                    if (phase== ThPDSPhase.三相 && calculateCurrent >= 200)
                    {
                        this.conductorUse = config.NonFireDistributionBranchCircuiCables;
                        IsWire  = false;
                    }
                    else
                    {
                        this.conductorUse = config.NonFireDistributionWire;
                        IsWire  = true;
                    }
                }
            }
            else if (circuitType == ThPDSLoadTypeCat_1.Motor)
            {
                if (fireLoad)
                {
                    if (phase == ThPDSPhase.一相)
                    {
                        this.conductorUse = config.FireDistributionWire;
                    }
                    else
                    {
                        this.conductorUse = config.FireDistributionBranchCircuiCables;
                    }
                }
                else
                {
                    if (phase == ThPDSPhase.一相)
                        this.conductorUse=config.NonFireDistributionWire;
                    else
                        this.conductorUse=config.NonFireDistributionBranchCircuiCables;
                }
            }
            else if (circuitType == ThPDSLoadTypeCat_1.DistributionPanel)
            {
                if (fireLoad)
                {
                    if (phase == ThPDSPhase.一相)
                    {
                        this.conductorUse = config.FireDistributionWire;
                    }
                    else
                        this.conductorUse = config.FireDistributionTrunk;
                }
                else
                {
                    if (phase == ThPDSPhase.一相)
                        this.conductorUse=config.NonFireDistributionWire;
                    else
                        this.conductorUse=config.NonFireDistributionBranchCircuiCables;
                }
            }
            else if (circuitType == ThPDSLoadTypeCat_1.LumpedLoad)
            {
                if (fireLoad)
                {
                    if (phase == ThPDSPhase.一相)
                    {
                        this.conductorUse = config.FireDistributionWire;
                    }
                    else
                    {
                        this.conductorUse = config.FireDistributionBranchCircuiCables;
                    }
                }
                else
                {
                    if (phase == ThPDSPhase.一相)
                        this.conductorUse=config.NonFireDistributionWire;
                    else
                        this.conductorUse=config.NonFireDistributionBranchCircuiCables;
                }
            }
            if(conductorUse.ConductorMaterial.Contains('N'))
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
            var configs = IsWire ? ConductorConfigration.WireConductorInfos : ConductorConfigration.CableConductorInfos;
            var config = configs.FirstOrDefault(o => o.Iset > calculateCurrent);
            if (config.IsNull())
            {
                throw new NotSupportedException();
            }
            else
            {
                var Sphere = config.Sphere;
                this.NumberOfPhaseWire = config.NumberOfPhaseWire;
                this.ConductorCrossSectionalArea = Sphere;
                if (Sphere <= 16)
                {
                    this.PECrossSectionalArea = Sphere;
                }
                else if (Sphere <= 35)
                {
                    this.PECrossSectionalArea = 16;
                }
                else if (Sphere <= 400)
                {
                    this.PECrossSectionalArea = Sphere/2;
                }
                else if (Sphere <= 800)
                {
                    this.PECrossSectionalArea = 200;
                }
                else
                {
                    this.PECrossSectionalArea = Sphere/4;
                }
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
                if (conductorUse.IsSpecialConductorType)
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

        /// <summary>
        /// 导体根数x每根导体截面积
        /// </summary>
        public string ConductorInfo 
        { 
            get
            {
                if(Phase == ThPDSPhase.一相)
                {
                    return $"1×{ConductorCrossSectionalArea}+E{PECrossSectionalArea}";
                }
                else
                {
                    string val = string.Empty;
                    if(AllMotor)
                    {
                        val = $"3×{ConductorCrossSectionalArea}+2×E{PECrossSectionalArea}";
                    }
                    else
                    {
                        val = $"4×{ConductorCrossSectionalArea}+E{PECrossSectionalArea}";
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
        /// 桥架敷设方式/穿管直径-穿管敷设方式
        /// </summary>
        public string LayingTyle
        {
            get
            {
                var ViaConduitStr = conductorUse.IsSpecialConductorType ? Pipelaying.ToString() : PipeMaterial + PipeDiameter + "-" + Pipelaying;
                if (ViaCableTray && ViaConduit)
                {
                    return $"{this.BridgeLaying}/ {ViaConduitStr }";
                }
                else if(ViaCableTray)
                {
                    return this.BridgeLaying.ToString();
                }
                else if(ViaConduit)
                {
                    return ViaConduitStr;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        //public string Content { get { return "WDZAN-YJY-4x25+E16-CT/SC50-E"; } }
        //外护套材质-导体材质-导体根数x每根导体截面积-桥架敷设方式/穿管直径-穿管敷设方式
        //public string Content { get { return $"{OuterSheathMaterial}-{ConductorMaterial}-{ConductorInfo}-{BridgeLaying}/{PipeDiameter}-{Pipelaying}"; } }
        public string Content { get { return $"{conductorUse.Content}-{ConductorInfo}-{LayingTyle}"; } }

        public bool IsWire { get; set; }

        /// <summary>
        /// 导体材质
        /// </summary>
        public ConductorUse conductorUse { get; set; }

        /// <summary>
        /// 材料特征及结构
        /// </summary>
        public string OuterSheathMaterial { get { return conductorUse.OuterSheathMaterial.GetDescription(); } }
        
        /// <summary>
        /// 导体选择类别
        /// </summary>
        public string ConductorMaterial { get { return conductorUse.ConductorType.ToString(); } }

        /// <summary>
        /// 级数
        /// </summary>
        public ThPDSPhase Phase { get; set; }

        public int NumberOfPhaseWire { get; set; }
        public double ConductorCrossSectionalArea { get; set; }
        public double PECrossSectionalArea { get; set; }
        private bool AllMotor { get; set; }
        /// <summary>
        /// 导体耐火材质
        /// </summary>
        private bool Refractory { get; set;}

        /// <summary>
        /// 是否穿桥架
        /// </summary>
        private bool ViaCableTray { get; set; }

        /// <summary>
        /// 穿管
        /// </summary>
        private bool ViaConduit { get; set; }

        /// <summary>
        /// 桥架敷设方式
        /// </summary>
        public BridgeLaying BridgeLaying { get; set; }

        /// <summary>
        /// 穿管敷设方式
        /// </summary>
        public Pipelaying Pipelaying { get; set; }

        /// <summary>
        /// 穿管直径
        /// </summary>
        public int PipeDiameter { get; set; }

        /// <summary>
        /// 穿管管材
        /// </summary>
        public PipeMaterial PipeMaterial { get; set; }
    }
}
