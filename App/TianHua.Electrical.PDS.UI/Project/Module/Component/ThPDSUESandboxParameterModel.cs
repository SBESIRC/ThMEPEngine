using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.ProjectConfigure;
using TianHua.Electrical.PDS.UI.ViewModels;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSUESandboxParameterModel : NotifyPropertyChangedBase
    {
        public ThPDSConductorUsageModel[] ConductorUsages { get; } = new ConductorUse[]
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
        public ThPDSConductorUsageModel FireDistributionTrunk => ConductorUsages[0];
        public ThPDSConductorUsageModel FireDistributionBranchCircuiCables => ConductorUsages[1];
        public ThPDSConductorUsageModel FireDistributionWire => ConductorUsages[2];
        public ThPDSConductorUsageModel NonFireDistributionBranchCircuiCables => ConductorUsages[3];
        public ThPDSConductorUsageModel NonFireDistributionWire => ConductorUsages[4];
        public ThPDSConductorUsageModel FireDistributionControlCable => ConductorUsages[5];
        public ThPDSConductorUsageModel NonFireDistributionControlCable => ConductorUsages[6];
        public ThPDSConductorUsageModel FireControlSignalWire => ConductorUsages[7];
        public ThPDSConductorUsageModel NonFireControlSignalWire => ConductorUsages[8];
        string _DefaultLengthOfMunicipalPowerLine;
        public string DefaultLengthOfMunicipalPowerLine
        {
            get => _DefaultLengthOfMunicipalPowerLine;
            set
            {
                if (value != _DefaultLengthOfMunicipalPowerLine)
                {
                    _DefaultLengthOfMunicipalPowerLine = value;
                    OnPropertyChanged(nameof(DefaultLengthOfMunicipalPowerLine));
                }
            }
        }

        string _CurrentOfTrunkDistribution;
        public string CurrentOfTrunkDistribution
        {
            get => _CurrentOfTrunkDistribution;
            set
            {
                if (value != _CurrentOfTrunkDistribution)
                {
                    _CurrentOfTrunkDistribution = value;
                    OnPropertyChanged(nameof(CurrentOfTrunkDistribution));
                }
            }
        }

        string _DefaultFeeder;
        public string DefaultFeeder
        {
            get => _DefaultFeeder;
            set
            {
                if (value != _DefaultFeeder)
                {
                    _DefaultFeeder = value;
                    OnPropertyChanged(nameof(DefaultFeeder));
                }
            }
        }

        string _ElseFeeder;
        public string ElseFeeder
        {
            get => _ElseFeeder;
            set
            {
                if (value != _ElseFeeder)
                {
                    _ElseFeeder = value;
                    OnPropertyChanged(nameof(ElseFeeder));
                }
            }
        }

        string _DefaultLengthOfCircuitToTheFirstLevelDistributionBox;
        public string DefaultLengthOfCircuitToTheFirstLevelDistributionBox
        {
            get => _DefaultLengthOfCircuitToTheFirstLevelDistributionBox;
            set
            {
                if (value != _DefaultLengthOfCircuitToTheFirstLevelDistributionBox)
                {
                    _DefaultLengthOfCircuitToTheFirstLevelDistributionBox = value;
                    OnPropertyChanged(nameof(DefaultLengthOfCircuitToTheFirstLevelDistributionBox));
                }
            }
        }

        string _LoopSettingCurrent;
        public string LoopSettingCurrent
        {
            get => _LoopSettingCurrent;
            set
            {
                if (value != _LoopSettingCurrent)
                {
                    _LoopSettingCurrent = value;
                    OnPropertyChanged(nameof(LoopSettingCurrent));
                }
            }
        }

        string _DefaultLengthOfSubsequentLines;
        public string DefaultLengthOfSubsequentLines
        {
            get => _DefaultLengthOfSubsequentLines;
            set
            {
                if (value != _DefaultLengthOfSubsequentLines)
                {
                    _DefaultLengthOfSubsequentLines = value;
                    OnPropertyChanged(nameof(DefaultLengthOfSubsequentLines));
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

        public MaterialStructure FirePowerDistributionTrunkLineAndBranchTrunkLine
        {
            get =>FireDistributionTrunk.OuterSheathMaterial;
            set
            {
                if (value != FirePowerDistributionTrunkLineAndBranchTrunkLine)
                {
                    FireDistributionTrunk.OuterSheathMaterial= value;
                    OnPropertyChanged(nameof(FirePowerDistributionTrunkLineAndBranchTrunkLine));
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
