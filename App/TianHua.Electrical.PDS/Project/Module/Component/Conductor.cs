using System;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module.Configure;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;
using TianHua.Electrical.PDS.Project.PDSProjectException;

namespace TianHua.Electrical.PDS.Project.Module.Component
{
    [Serializable]
    public class Conductor : PDSBaseComponent
    {
        /// <summary>
        /// 导体
        /// </summary>
        public Conductor(double calculateCurrent, ThPDSPhase phase, ThPDSCircuitType circuitType, ThPDSLoadTypeCat_1 loadType, bool FireLoad, bool ViaConduit, bool ViaCableTray, string FloorNumber, LayingSite layingSite1, LayingSite layingSite2, bool isResidentialDistributionPanel)
        {
            this.ComponentType = ComponentType.Conductor;
            this.LayingSite1 = layingSite1;
            this.LayingSite2 = layingSite2;
            this.IsResidentialDistributionPanel = isResidentialDistributionPanel;
            if (!ViaConduit && !ViaCableTray)
            {
                ViaCableTray = true;
            }
            Phase = phase;
            ChooseMaterial(loadType, FireLoad, calculateCurrent);
            ChooseCrossSectionalArea(calculateCurrent);
            ChooseLaying(FloorNumber, circuitType, phase, ViaConduit, ViaCableTray, FireLoad);
        }

        /// <summary>
        /// 导体
        /// </summary>
        public Conductor(string conductorConfig, double calculateCurrent, ThPDSPhase phase, ThPDSCircuitType circuitType, ThPDSLoadTypeCat_1 loadType, bool FireLoad, bool ViaConduit, bool ViaCableTray, string FloorNumber, LayingSite layingSite1, LayingSite layingSite2)
        {
            this.ComponentType = ComponentType.Conductor;
            this.LayingSite1 = layingSite1;
            this.LayingSite2 = layingSite2;
            this.IsMotor = true;
            this.Phase = phase;
            if (!ViaConduit && !ViaCableTray)
            {
                ViaCableTray = true;
            }
            //3x2.5+E2.5
            ChooseMaterial(loadType, FireLoad, calculateCurrent);
            ChooseCrossSectionalArea(conductorConfig);
            ChooseLaying(FloorNumber, circuitType, phase, ViaConduit, ViaCableTray, FireLoad);
        }

        /// <summary>
        /// 导体(消防应急照明回路导体)
        /// </summary>
        public Conductor(string conductorConfig,MaterialStructure materialStructure, double calculateCurrent, ThPDSPhase phase, ThPDSCircuitType circuitType, ThPDSLoadTypeCat_1 loadType, bool FireLoad, bool ViaConduit, bool ViaCableTray, string FloorNumber, LayingSite layingSite1, LayingSite layingSite2)
        {
            this.ComponentType = ComponentType.Conductor;
            this.LayingSite1 = layingSite1;
            this.LayingSite2 = layingSite2;
            this.Phase = phase;
            this.IsSpecifyMaterialStructure = true;
            this.SpecifyMaterialStructure = materialStructure;
            if (!ViaConduit && !ViaCableTray)
            {
                ViaCableTray = true;
            }
            //3x2.5+E2.5
            ChooseMaterial(loadType, FireLoad, calculateCurrent);
            ChooseCrossSectionalArea(conductorConfig);
            ChooseLaying(FloorNumber, circuitType, phase, ViaConduit, ViaCableTray, FireLoad);
        }

        /// <summary>
        /// 导体(控制回路导体)
        /// </summary>
        public Conductor(string conductorConfig, string conductorType, ThPDSPhase phase, ThPDSCircuitType circuitType, bool FireLoad, bool ViaConduit, bool ViaCableTray, string FloorNumber, LayingSite layingSite1, LayingSite layingSite2)
        {
            this.ComponentType = ComponentType.ControlConductor;
            this.LayingSite1 = layingSite1;
            this.LayingSite2 = layingSite2;
            this.IsMotor = true;
            this.IsControlCircuit = true;
            this.IsCustom = false;
            this.Phase = phase;
            if (!ViaConduit && !ViaCableTray)
            {
                ViaCableTray = true;
            }
            ChooseMaterial(conductorType);
            ChooseCrossSectionalArea(conductorConfig);
            ChooseLaying(FloorNumber, circuitType, phase, ViaConduit, ViaCableTray, FireLoad);
        }

        /// <summary>
        /// 导体(电表箱)
        /// </summary>
        public Conductor(string conductorConfig, double calculateCurrent, ThPDSPhase phase, ThPDSCircuitType circuitType, ThPDSLoadTypeCat_1 loadType, bool FireLoad, bool ViaConduit, bool ViaCableTray, string FloorNumber, LayingSite layingSite1, LayingSite layingSite2, bool isResidentialDistributionPanel)
        {
            this.ComponentType = ComponentType.Conductor;
            this.LayingSite1 = layingSite1;
            this.LayingSite2 = layingSite2;
            this.IsResidentialDistributionPanel = isResidentialDistributionPanel;
            this.Phase = phase;
            if (!ViaConduit && !ViaCableTray)
            {
                ViaCableTray = true;
            }
            //3x2.5+E2.5
            ChooseMaterial(loadType, FireLoad, calculateCurrent);
            ChooseCrossSectionalArea(conductorConfig);
            ChooseLaying(FloorNumber, circuitType, phase, ViaConduit, ViaCableTray, FireLoad);
        }

        private void ChooseMaterial(string conductorType)
        {
            if (string.IsNullOrWhiteSpace(conductorType))
            {
                IsBAControl = true;
                return;
            }
            var config = PDSProject.Instance.projectGlobalConfiguration;
            if (conductorType == "消防配电控制电缆")
            {
                this.ConductorUse = config.FireDistributionControlCable;
                IsWire =false;
            }
            else if (conductorType == "非消防配电控制电缆")
            {
                this.ConductorUse = config.NonFireDistributionControlCable;
                IsWire = false;
            }
            else if (conductorType == "消防控制信号软线")
            {
                this.ConductorUse = config.FireControlSignalWire;
                IsWire = true;
            }
            else if (conductorType == "非消防控制信号软线")
            {
                this.ConductorUse = config.NonFireControlSignalWire;
                IsWire = true;
            }
            else
            {
                throw new NotFoundComponentException("设备库内找不到对应规格的Conductor");
            }
            if (ConductorUse.ConductorMaterial.Contains('N'))
            {
                Refractory = true;
            }
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
            if(this.IsResidentialDistributionPanel)
            {
                if (calculateCurrent >= 200)
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
            else if (circuitType == ThPDSLoadTypeCat_1.Luminaire)
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
                        IsWire = true;
                    }
                    else
                    {
                        this.ConductorUse = config.FireDistributionBranchCircuiCables;
                        IsWire = false;
                    }
                }
                else
                {
                    if (Phase == ThPDSPhase.一相)
                    {
                        this.ConductorUse = config.NonFireDistributionWire;
                        IsWire =true;
                    }
                    else
                    {
                        this.ConductorUse = config.NonFireDistributionBranchCircuiCables;
                        IsWire =false;
                    }
                }
            }
            else if (circuitType == ThPDSLoadTypeCat_1.DistributionPanel)
            {
                if (fireLoad)
                {
                    if (Phase == ThPDSPhase.一相)
                    {
                        this.ConductorUse = config.FireDistributionWire;
                        IsWire =true;
                    }
                    else
                        this.ConductorUse = config.FireDistributionBranchCircuiCables;
                }
                else
                {
                    if (Phase == ThPDSPhase.一相)
                    {
                        this.ConductorUse = config.NonFireDistributionWire;
                        IsWire =true;
                    }
                    else
                    {
                        this.ConductorUse = config.NonFireDistributionBranchCircuiCables;
                        IsWire =false;
                    }
                }
            }
            else if (circuitType == ThPDSLoadTypeCat_1.LumpedLoad)
            {
                if (fireLoad)
                {
                    if (Phase == ThPDSPhase.一相)
                    {
                        this.ConductorUse = config.FireDistributionWire;
                        IsWire =true;
                    }
                    else
                    {
                        this.ConductorUse = config.FireDistributionBranchCircuiCables;
                        IsWire =false;
                    }
                }
                else
                {
                    if (Phase == ThPDSPhase.一相)
                    {
                        this.ConductorUse = config.NonFireDistributionWire;
                        IsWire =true;
                    }
                    else
                    {
                        this.ConductorUse = config.NonFireDistributionBranchCircuiCables;
                        IsWire =false;
                    }
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
                throw new NotFoundComponentException("设备库内找不到对应规格的ConductorConfig");
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
                if (config.Contains('-'))
                {
                    IsBAControl = true;
                }
                else
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
                        HasPELine = true;
                    }
                    else
                    {
                        HasPELine = false;
                    }
                    string[] conductorInfos = config.Split('x');
                    this.ConductorCount = int.Parse(conductorInfos[0]);
                    this.ConductorCrossSectionalArea = double.Parse(conductorInfos[1]);
                    AlternativeNumberOfPhaseWire = new List<int>() { NumberOfPhaseWire };
                    AlternativeConductorCrossSectionalAreas = new List<double>() { ConductorCrossSectionalArea };
                    AlternativeConductorCount = new List<int>() { ConductorCount };
                    CalculateCrossSectionalArea(ConductorCrossSectionalArea);
                }
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
            if (viaCableTray && viaConduit)
            {
                ConductorLayingPath = ConductorLayingPath.ViaCableTrayAndViaConduit;
            }
            else if (viaCableTray)
            {
                ConductorLayingPath = ConductorLayingPath.ViaCableTray;
            }
            else
            {
                ConductorLayingPath = ConductorLayingPath.ViaConduit;
            }
            var config = PDSProject.Instance.projectGlobalConfiguration;
            //ViaCableTray
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
                            this.BridgeLaying = BridgeLaying.MR;
                            break;
                        }
                }
                AlternativeConductorLayingPaths = new List<ConductorLayingPath>() { ConductorLayingPath.ViaCableTray, ConductorLayingPath.ViaConduit, ConductorLayingPath.ViaCableTrayAndViaConduit };
            }
            //PipeDiameter
            {
                if (!ConductorUse.IsNull() && ConductorUse.IsSpecialConductorType)
                {
                    this.PipeDiameter = 0;
                    LayingSite1 = LayingSite.CC;
                    LayingSite2 = LayingSite.None;
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
                }
                if (this.PipeDiameter > config.UniversalPipeDiameter)
                {
                    this.LayingSite1 = AdjustmentLayingSite(LayingSite1);
                    this.LayingSite2 = AdjustmentLayingSite(LayingSite2);
                }
                AlternativeLayingSites1 = (ProjectSystemConfiguration.LayingSiteC.Contains(LayingSite1) ? ProjectSystemConfiguration.LayingSiteC : ProjectSystemConfiguration.LayingSiteAll).ToList();
                AlternativeLayingSites2 = (ProjectSystemConfiguration.LayingSiteC.Contains(LayingSite1) ? ProjectSystemConfiguration.LayingSiteC : ProjectSystemConfiguration.LayingSiteAll).ToList();
                AlternativeLayingSites2.Insert(0, LayingSite.None);
            }
        }

        /// <summary>
        /// 计算敷设部位选型
        /// </summary>
        private LayingSite AdjustmentLayingSite(LayingSite layingSite)
        {
            switch (layingSite)
            {
                case LayingSite.CC:
                    {
                        return LayingSite.CE;
                    }
                case LayingSite.WC:
                    {
                        return LayingSite.WS;
                    }
                case LayingSite.CLC:
                    {
                        return LayingSite.AC;
                    }
                case LayingSite.BC:
                    {
                        return LayingSite.AB;
                    }
                case LayingSite.FC:
                    {
                        return LayingSite.FC;
                    }
                default:
                    {
                        return layingSite;
                    }
            }
        }

        public string Content
        {
            get
            {
                string val = $"{(IsBAControl ? "" : (IsSpecifyMaterialStructure ? ConductorUse.ConductorMaterial + "-" + SpecifyMaterialStructure : ConductorUse.Content)+"-"+ConductorInfo+"-")}{LayingTyle}";
                if (NumberOfPhaseWire > 1)
                {
                    val = $"{NumberOfPhaseWire}×({val})";
                }
                return val;
            }
        }

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
        /// 导体根数
        /// </summary>
        public int ConductorCount { get; set; }

        /// <summary>
        /// 相导体截面
        /// </summary>
        public double ConductorCrossSectionalArea { get; set; }

        /// <summary>
        /// 是否允许用户自定义
        /// 仅针对控制回路
        /// </summary>
        public bool IsCustom { get; set; }

        /// <summary>
        /// 中性线导体截面
        /// </summary>
        public double NeutralConductorCrossSectionalArea { get; set; }

        /// <summary>
        /// PE线导体截面
        /// </summary>
        public double PECrossSectionalArea { get; set; }

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
        /// 获取导体根数
        /// </summary>
        /// <returns></returns>
        public List<int> GetConductorCounts()
        {
            return AlternativeConductorCount;
        }

        public void SetConductorCount(int conductorCount)
        {
            if (this.IsControlCircuit)
            {
                this.ConductorCount = conductorCount;
            }
            else
            {
                throw new InvalidOperationException();
            }
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
            if (this.IsControlCircuit && !AlternativeConductorCrossSectionalAreas.Contains(conductorCrossSectionalArea))
            {
                //用户自定义
                this.ConductorCrossSectionalArea = conductorCrossSectionalArea;
            }
            else
            {
                this.ConductorCrossSectionalArea = conductorCrossSectionalArea;
                CalculateCrossSectionalArea(conductorCrossSectionalArea);
            }
        }

        public List<ConductorLayingPath> GetConductorLayingPaths()
        {
            return AlternativeConductorLayingPaths;
        }

        public void SetConductorLayingPath(ConductorLayingPath conductorLayingPath)
        {
            if(this.ConductorLayingPath != conductorLayingPath)
            {
                this.ConductorLayingPath = conductorLayingPath;
            }
        }

        public List<LayingSite> GetLayingSites1()
        {
            return AlternativeLayingSites1;
        }

        public void SetLayingSite1(LayingSite layingSite)
        {
            this.LayingSite1 = layingSite;
        }

        public List<LayingSite> GetLayingSites2()
        {
            return AlternativeLayingSites2;
        }

        public void SetLayingSite2(LayingSite layingSite)
        {
            this.LayingSite2 = layingSite;
        }

        public void SetBAControl()
        {
            this.IsBAControl = true;
            IsCustom = false;
        }

        public void SetControlCircuitConfig(string config)
        {
            var infos = config.Split('x');
            ConductorCount = int.Parse(infos[0]);
            AlternativeConductorCount = new List<int> { ConductorCount };
            ConductorCrossSectionalArea = double.Parse(infos[1]);
            AlternativeConductorCrossSectionalAreas = new List<double> { ConductorCrossSectionalArea };
            IsBAControl = false;
            IsCustom = false;
        }

        #region Private Property

        /// <summary>
        /// 桥架敷设方式/穿管直径-穿管敷设方式
        /// </summary>
        private string LayingTyle
        {
            get
            {
                var layingSiteStr = $"{LayingSite1}{(LayingSite2 == LayingSite.None ? "" : " "+ LayingSite2.ToString())}";
                var ViaConduitStr = !IsBAControl && ConductorUse.IsSpecialConductorType ? layingSiteStr : PipeMaterial.ToString() + PipeDiameter + "-" + layingSiteStr;
                if (ConductorLayingPath == ConductorLayingPath.ViaCableTrayAndViaConduit)
                {
                    return $"{this.BridgeLaying.ToString()}/ {ViaConduitStr }";
                }
                else if (ConductorLayingPath == ConductorLayingPath.ViaCableTray)
                {
                    return this.BridgeLaying.ToString();
                }
                else if (ConductorLayingPath == ConductorLayingPath.ViaConduit)
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
                    return $"2×{ConductorCrossSectionalArea}{(HasPELine ? "+E"+PECrossSectionalArea : "")}";
                }
                else
                {
                    string val = string.Empty;
                    if (IsMotor)
                    {
                        val = $"{ConductorCount}×{ConductorCrossSectionalArea}{(HasPELine ? "+E"+PECrossSectionalArea : "")}";
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
                    return val;
                }
            }
        }

        /// <summary>
        /// 备选电缆根数
        /// </summary>
        private List<int> AlternativeNumberOfPhaseWire { get; set; }

        /// <summary>
        /// 备选导体根数
        /// </summary>
        private List<int> AlternativeConductorCount { get; set; }

        /// <summary>
        /// 备选相导体截面
        /// </summary>
        private List<double> AlternativeConductorCrossSectionalAreas { get; set; }
        
        /// <summary>
        /// 备选敷设路径选择
        /// </summary>
        private List<ConductorLayingPath> AlternativeConductorLayingPaths { get; set; }
        
        /// <summary>
        /// 备选敷设路径选择
        /// </summary>
        private List<LayingSite> AlternativeLayingSites1 { get; set; }
        
        /// <summary>
        /// 备选敷设路径选择
        /// </summary>
        private List<LayingSite> AlternativeLayingSites2 { get; set; }

        /// <summary>
        /// 上级所有回路是否全是电动机
        /// </summary>
        private bool AllMotor { get; set; } = false;

        /// <summary>
        /// 导体耐火材质
        /// </summary>
        private bool Refractory { get; set; }

        /// <summary>
        /// 穿管管材
        /// </summary>
        public PipeMaterial PipeMaterial { get; set; } = PipeMaterial.None;

        /// <summary>
        /// 敷设部位1
        /// </summary>
        public LayingSite LayingSite1 { get; set; }

        /// <summary>
        /// 敷设部位2
        /// </summary>
        public LayingSite LayingSite2 { get; set; }

        /// <summary>
        /// 敷设路径
        /// </summary>
        public ConductorLayingPath ConductorLayingPath { get; set; }

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
        public BridgeLaying BridgeLaying { get; set; } = BridgeLaying.None;

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

        /// <summary>
        /// 是否是控制回路
        /// </summary>
        private bool IsControlCircuit { get; set; } = false;

        /// <summary>
        /// 是否是BA控制
        /// </summary>
        public bool IsBAControl { get; set; } = false;

        /// <summary>
        /// 是否是住户配电箱对应回路
        /// </summary>
        public bool IsResidentialDistributionPanel { get; set; } = false;

        /// <summary>
        /// 是否指定外护套材质
        /// </summary>
        private bool IsSpecifyMaterialStructure { get; set; } = false;

        /// <summary>
        /// 外护套材质
        /// </summary>
        private MaterialStructure SpecifyMaterialStructure;
        #endregion
    }
}
