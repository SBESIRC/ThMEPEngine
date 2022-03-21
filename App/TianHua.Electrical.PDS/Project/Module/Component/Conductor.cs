using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Configure;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    public class Conductor : PDSBaseComponent
    {
        public Conductor(double calculateCurrent, ThPDSPhase phase, ThPDSCircuitType circuitType , ThPDSLoadTypeCat_1 loadType,bool FireLoad,bool ViaConduit,bool ViaCableTray,string FloorNumber)
        {
            this.ComponentType = ComponentType.导体;
            ChooseMaterial(loadType, FireLoad, calculateCurrent, phase);
            ChooseCrossSectionalArea(calculateCurrent);
            ChooseLaying(FloorNumber,circuitType, ViaConduit, ViaCableTray);
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
                        this.OuterSheathMaterial = config.FireDistributionBranchCircuiCables.OuterSheathMaterial;
                        this.ConductorMaterial = config.FireDistributionBranchCircuiCables.ConductorMaterial;
                        IsWire  = false;
                    }
                    else
                    {
                        this.OuterSheathMaterial = config.FireDistributionWire.OuterSheathMaterial;
                        this.ConductorMaterial = config.FireDistributionWire.ConductorMaterial;
                        IsWire  = true;
                    }
                }
                else
                {
                    if (phase== ThPDSPhase.三相 && calculateCurrent >= 200)
                    {
                        this.OuterSheathMaterial = config.NonFireDistributionBranchCircuiCables.OuterSheathMaterial;
                        this.ConductorMaterial = config.NonFireDistributionBranchCircuiCables.ConductorMaterial;
                        IsWire  = false;
                    }
                    else
                    {
                        this.OuterSheathMaterial = config.NonFireDistributionWire.OuterSheathMaterial;
                        this.ConductorMaterial = config.NonFireDistributionWire.ConductorMaterial;
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
                        this.OuterSheathMaterial = config.FireDistributionBranchCircuiCables.OuterSheathMaterial;
                        this.ConductorMaterial = config.FireDistributionBranchCircuiCables.ConductorMaterial;
                        IsWire  = false;
                    }
                    else
                    {
                        this.OuterSheathMaterial = config.FireDistributionWire.OuterSheathMaterial;
                        this.ConductorMaterial = config.FireDistributionWire.ConductorMaterial;
                        IsWire  = true;
                    }
                }
                else
                {
                    if (phase== ThPDSPhase.三相 && calculateCurrent >= 200)
                    {
                        this.OuterSheathMaterial = config.NonFireDistributionBranchCircuiCables.OuterSheathMaterial;
                        this.ConductorMaterial = config.NonFireDistributionBranchCircuiCables.ConductorMaterial;
                        IsWire  = false;
                    }
                    else
                    {
                        this.OuterSheathMaterial = config.NonFireDistributionWire.OuterSheathMaterial;
                        this.ConductorMaterial = config.NonFireDistributionWire.ConductorMaterial;
                        IsWire  = true;
                    }
                }
            }
            else if (circuitType == ThPDSLoadTypeCat_1.Motor)
            {
                if (fireLoad)
                {
                    this.OuterSheathMaterial = config.FireDistributionBranchCircuiCables.OuterSheathMaterial;
                    this.ConductorMaterial = config.FireDistributionBranchCircuiCables.ConductorMaterial;
                }
                else
                {
                    this.OuterSheathMaterial=config.NonFireDistributionBranchCircuiCables.OuterSheathMaterial;
                    this.ConductorMaterial = config.NonFireDistributionBranchCircuiCables.ConductorMaterial;
                }
            }
            else if (circuitType == ThPDSLoadTypeCat_1.DistributionPanel)
            {
                if (fireLoad)
                {
                    this.OuterSheathMaterial = config.FireDistributionBranchCircuiCables.OuterSheathMaterial;
                    this.ConductorMaterial = config.FireDistributionBranchCircuiCables.ConductorMaterial;
                }
                else
                {
                    this.OuterSheathMaterial=config.NonFireDistributionBranchCircuiCables.OuterSheathMaterial;
                    this.ConductorMaterial = config.NonFireDistributionBranchCircuiCables.ConductorMaterial;
                }
            }
            else if (circuitType == ThPDSLoadTypeCat_1.LumpedLoad)
            {
                if (fireLoad)
                {
                    this.OuterSheathMaterial = config.FireDistributionBranchCircuiCables.OuterSheathMaterial;
                    this.ConductorMaterial = config.FireDistributionBranchCircuiCables.ConductorMaterial;
                }
                else
                {
                    this.OuterSheathMaterial=config.NonFireDistributionBranchCircuiCables.OuterSheathMaterial;
                    this.ConductorMaterial = config.NonFireDistributionBranchCircuiCables.ConductorMaterial;
                }
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
                this.ConductorCrossSectionalArea = Sphere.ToString();
                if (Sphere <= 16)
                {
                    this.PECrossSectionalArea = Sphere.ToString();
                }
                else if (Sphere <= 35)
                {
                    this.PECrossSectionalArea = "16";
                }
                else if (Sphere <= 400)
                {
                    this.PECrossSectionalArea = (Sphere/2).ToString();
                }
                else if (Sphere <= 800)
                {
                    this.PECrossSectionalArea = "200";
                }
                else
                {
                    this.PECrossSectionalArea = (Sphere/4).ToString();
                }
            }
        }

        /// <summary>
        /// 桥架/穿管敷设方式选型
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void ChooseLaying(string floorNumber, ThPDSCircuitType circuitType, bool viaConduit, bool viaCableTray)
        {
            //此处逻辑张皓还没讲完，后续补充
            LayingTyle = "CT/SC50-E";
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

        public string LayingTyle { get; set; }

        //public string Content { get { return "WDZAN-YJY-4x25+E16-CT/SC50-E"; } }
        //外护套材质-导体材质-导体根数x每根导体截面积-桥架敷设方式/穿管直径-穿管敷设方式
        //public string Content { get { return $"{OuterSheathMaterial}-{ConductorMaterial}-{ConductorInfo}-{BridgeLaying}/{PipeDiameter}-{Pipelaying}"; } }
        public string Content { get { return $"{OuterSheathMaterial}-{ConductorMaterial}-{ConductorInfo}-{LayingTyle}"; } }

        public bool IsWire { get; set; }

        /// <summary>
        /// 外护套材质
        /// </summary>
        public string OuterSheathMaterial { get;set; }
        
        /// <summary>
        /// 导体材质
        /// </summary>
        public string ConductorMaterial { get;set; }

        /// <summary>
        /// 级数
        /// </summary>
        public ThPDSPhase Phase { get; set; }

        public int NumberOfPhaseWire { get; set; }
        public string ConductorCrossSectionalArea { get; set; }
        public string PECrossSectionalArea { get; set; }
        public bool AllMotor { get; set; }

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
        public string PipeDiameter { get; set; }
    }
}
