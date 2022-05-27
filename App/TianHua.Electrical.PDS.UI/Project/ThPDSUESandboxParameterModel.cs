using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Extension;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.Project
{
    public class ThPDSUESandboxParameterModel : NotifyPropertyChangedBase
    {
        public string CentralizedPowerContent => ThPDSComponentContentExtension.CentralizedPowerContent();
        public ThPDSConductorUsageModel[] ConductorUsages => _ConductorUsages.Skip(1).ToArray();
        private ThPDSConductorUsageModel[] _ConductorUsages { get; } = new ConductorUse[]
        {
                PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FireDistributionTrunk,
                PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FireDistributionBranchCircuiCables,
                PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FireDistributionWire,
                PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.NonFireDistributionBranchCircuiCables,
                PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.NonFireDistributionWire,
                PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FireDistributionControlCable,
                PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.NonFireDistributionControlCable,
                PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FireControlSignalWire,
                PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.NonFireControlSignalWire,
        }.Select(x => new ThPDSConductorUsageModel(x)).ToArray();
        public ThPDSConductorUsageModel FireDistributionTrunk => _ConductorUsages[0];
        public ThPDSConductorUsageModel FireDistributionBranchCircuiCables => _ConductorUsages[1];
        public ThPDSConductorUsageModel FireDistributionWire => _ConductorUsages[2];
        public ThPDSConductorUsageModel NonFireDistributionBranchCircuiCables => _ConductorUsages[3];
        public ThPDSConductorUsageModel NonFireDistributionWire => _ConductorUsages[4];
        public ThPDSConductorUsageModel FireDistributionControlCable => _ConductorUsages[5];
        public ThPDSConductorUsageModel NonFireDistributionControlCable => _ConductorUsages[6];
        public ThPDSConductorUsageModel FireControlSignalWire => _ConductorUsages[7];
        public ThPDSConductorUsageModel NonFireControlSignalWire => _ConductorUsages[8];
        public MeterBoxCircuitType MeterBoxCircuitType
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.MeterBoxCircuitType;
            set
            {
                if (value != MeterBoxCircuitType)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.MeterBoxCircuitType = value;
                    OnPropertyChanged(null);
                }
            }
        }
        public int UniversalPipeDiameter
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.UniversalPipeDiameter;
            set
            {
                if (value != UniversalPipeDiameter)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.UniversalPipeDiameter = value;
                    OnPropertyChanged(null);
                }
            }
        }
        public int FirePipeDiameter
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FirePipeDiameter;
            set
            {
                if (value != FirePipeDiameter)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FirePipeDiameter = value;
                    OnPropertyChanged(null);
                }
            }
        }
        public int NonFirePipeDiameter
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.NonFirePipeDiameter;
            set
            {
                if (value != NonFirePipeDiameter)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.NonFirePipeDiameter = value;
                    OnPropertyChanged(null);
                }
            }
        }
        public int[] UniversalPipeDiameterItemsSource { get; } = new int[] { 20, 25, 32, 40 };
        public int[] FirePipeDiameterItemsSource { get; } = new int[] { 20, 25, 32, 40, 50 };
        public int[] NonFirePipeDiameterItemsSource { get; } = new int[] { 20, 25, 32, 40, 50 };
        public double DefaultLengthOfMunicipalPowerLine
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.MunicipalPowerCircuitDefaultLength;
            set
            {
                if (value != DefaultLengthOfMunicipalPowerLine)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.MunicipalPowerCircuitDefaultLength = value;
                    OnPropertyChanged(null);
                }
            }
        }
        public int ACChargerPower
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.ACChargerPower;
            set
            {
                if (value != ACChargerPower)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.ACChargerPower = value;
                    OnPropertyChanged(null);
                }
            }
        }
        public int DCChargerPower
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.DCChargerPower;
            set
            {
                if (value != DCChargerPower)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.DCChargerPower = value;
                    OnPropertyChanged(null);
                }
            }
        }
        public double CurrentOfTrunkDistribution
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.TreeTrunkDistributionCurrent;
            set
            {
                if (value != CurrentOfTrunkDistribution)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.TreeTrunkDistributionCurrent = value;
                    OnPropertyChanged(null);
                }
            }
        }

        public Feeder DefaultFeeder
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.DefaultFeeder;
            set
            {
                if (value != DefaultFeeder)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.DefaultFeeder = value;
                    OnPropertyChanged(null);
                }
            }
        }
        public Feeder ElseFeeder
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.OtherFeeder;
            set
            {
                if (value != ElseFeeder)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.OtherFeeder = value;
                    OnPropertyChanged(null);
                }
            }
        }

        public double DefaultLengthOfCircuitToTheFirstLevelDistributionBox
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.SecondaryDistributionBoxDefaultLength;
            set
            {
                if (value != DefaultLengthOfCircuitToTheFirstLevelDistributionBox)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.SecondaryDistributionBoxDefaultLength = value;
                    OnPropertyChanged(null);
                }
            }
        }

        public double LoopSettingCurrent
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.LoopSettingCurrent;
            set
            {
                if (value != LoopSettingCurrent)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.LoopSettingCurrent = value;
                    OnPropertyChanged(null);
                }
            }
        }

        public double DefaultLengthOfSubsequentLines
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.SubsequentDefaultLength;
            set
            {
                if (value != DefaultLengthOfSubsequentLines)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.SubsequentDefaultLength = value;
                    OnPropertyChanged(null);
                }
            }
        }

        public ConductorMaterial DefaultConductorMaterial
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.DefaultConductorMaterial;
            set
            {
                if (value != DefaultConductorMaterial)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.DefaultConductorMaterial = value;
                    OnPropertyChanged(nameof(DefaultConductorMaterial));
                }
            }
        }
        public List<string> FirePowerDistributionTrunkLineAndBranchTrunkLineItemsSource => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FireDistributionTrunkOuterSheathMaterials.Select(x => x.GetEnumDescription()).ToList();
        public string FirePowerDistributionTrunkLineAndBranchTrunkLine
        {
            get => FireDistributionTrunk.OuterSheathMaterial.GetEnumDescription();
            set
            {
                if (value != FirePowerDistributionTrunkLineAndBranchTrunkLine)
                {
                    FireDistributionTrunk.OuterSheathMaterial = value.GetEnumName<MaterialStructure>();
                    OnPropertyChanged(nameof(FirePowerDistributionTrunkLineAndBranchTrunkLine));
                }
            }
        }
        public double CalculateCurrentMagnification
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.CalculateCurrentMagnification;
            set
            {
                if (value != CalculateCurrentMagnification)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.CalculateCurrentMagnification = value;
                    OnPropertyChanged(null);
                }
            }
        }
        string _AmbientTemperature;
        public string AmbientTemperature
        {
            get => _AmbientTemperature;
            set
            {
                if (value != _AmbientTemperature)
                {
                    _AmbientTemperature = value;
                    OnPropertyChanged(nameof(AmbientTemperature));
                }
            }
        }

        string _SoilThermalResistanceCoefficient;
        public string SoilThermalResistanceCoefficient
        {
            get => _SoilThermalResistanceCoefficient;
            set
            {
                if (value != _SoilThermalResistanceCoefficient)
                {
                    _SoilThermalResistanceCoefficient = value;
                    OnPropertyChanged(nameof(SoilThermalResistanceCoefficient));
                }
            }
        }

        string _FireFightingDistributionCable;
        public string FireFightingDistributionCable
        {
            get => _FireFightingDistributionCable;
            set
            {
                if (value != _FireFightingDistributionCable)
                {
                    _FireFightingDistributionCable = value;
                    OnPropertyChanged(nameof(FireFightingDistributionCable));
                }
            }
        }

        string _NonFirePowerDistributionCable;
        public string NonFirePowerDistributionCable
        {
            get => _NonFirePowerDistributionCable;
            set
            {
                if (value != _NonFirePowerDistributionCable)
                {
                    _NonFirePowerDistributionCable = value;
                    OnPropertyChanged(nameof(NonFirePowerDistributionCable));
                }
            }
        }

        string _ReductionFactorOfBundleLaying;
        public string ReductionFactorOfBundleLaying
        {
            get => _ReductionFactorOfBundleLaying;
            set
            {
                if (value != _ReductionFactorOfBundleLaying)
                {
                    _ReductionFactorOfBundleLaying = value;
                    OnPropertyChanged(nameof(ReductionFactorOfBundleLaying));
                }
            }
        }

        public PipeMaterial GeneralRequirements
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.UndergroundMaterial;
            set
            {
                if (value != GeneralRequirements)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.UndergroundMaterial = value;
                    OnPropertyChanged(nameof(GeneralRequirements));
                }
            }
        }

        public PipeMaterial FireFightingLineLess
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FireOnTheGroundSmallDiameterMaterial;
            set
            {
                if (value != FireFightingLineLess)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FireOnTheGroundSmallDiameterMaterial = value;
                    OnPropertyChanged(nameof(FireFightingLineLess));
                }
            }
        }


        public PipeMaterial FireFightingLineMore
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FireOnTheGroundLargeDiameterMaterial;
            set
            {
                if (value != FireFightingLineMore)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FireOnTheGroundLargeDiameterMaterial = value;
                    OnPropertyChanged(nameof(FireFightingLineMore));
                }
            }
        }

        public PipeMaterial NonFireFightingLineLess
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.NonFireOnTheGroundSmallDiameterMaterial;
            set
            {
                if (value != NonFireFightingLineLess)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.NonFireOnTheGroundSmallDiameterMaterial = value;
                    OnPropertyChanged(nameof(NonFireFightingLineLess));
                }
            }
        }

        public PipeMaterial NonFireFightingLineMore
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.NonFireOnTheGroundLargeDiameterMaterial;
            set
            {
                if (value != NonFireFightingLineMore)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.NonFireOnTheGroundLargeDiameterMaterial = value;
                    OnPropertyChanged(nameof(NonFireFightingLineMore));
                }
            }
        }

        string _FunctionTableItems;
        public string FunctionTableItems
        {
            get => _FunctionTableItems;
            set
            {
                if (value != _FunctionTableItems)
                {
                    _FunctionTableItems = value;
                    OnPropertyChanged(nameof(FunctionTableItems));
                }
            }
        }
        public MotorUIChoise SelectionOfMainCircuitOfMotor
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.MotorUIChoise;
            set
            {
                if (value != SelectionOfMainCircuitOfMotor)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.MotorUIChoise = value;
                    OnPropertyChanged(nameof(SelectionOfMainCircuitOfMotor));
                }
            }
        }

        public double FireMotorPowerLess
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FireMotorPower;
            set
            {
                if (value != FireMotorPowerLess)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FireMotorPower = value;
                    OnPropertyChanged(nameof(FireMotorPowerLess));
                }
            }
        }

        public FireStartType FireMotorPowerMore
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FireStartType;
            set
            {
                if (value != FireMotorPowerMore)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.FireStartType = value;
                    OnPropertyChanged(nameof(FireMotorPowerMore));
                }
            }
        }

        public double CommonMotorPowerLess
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.NormalMotorPower;
            set
            {
                if (value != CommonMotorPowerLess)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.NormalMotorPower = value;
                    OnPropertyChanged(nameof(CommonMotorPowerLess));
                }
            }
        }
        public FireEmergencyLightingModel FireEmergencyLightingModel
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.fireEmergencyLightingModel;
            set
            {
                if (value != FireEmergencyLightingModel)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.fireEmergencyLightingModel = value;
                    OnPropertyChanged(null);
                }
            }
        }
        public FireEmergencyLightingType FireEmergencyLightingType
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.fireEmergencyLightingType;
            set
            {
                if (value != FireEmergencyLightingType)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.fireEmergencyLightingType = value;
                    OnPropertyChanged(null);
                }
            }
        }
        public CircuitSystem CircuitSystem
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.circuitSystem;
            set
            {
                if (value != CircuitSystem)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.circuitSystem = value;
                    OnPropertyChanged(null);
                }
            }
        }
        public FireStartType CommonMotorPowerMore
        {
            get => PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.NormalStartType;
            set
            {
                if (value != CommonMotorPowerMore)
                {
                    PDSProjectVM.Instance.GlobalParameterViewModel.Configuration.NormalStartType = value;
                    OnPropertyChanged(nameof(CommonMotorPowerMore));
                }
            }
        }
    }
}
