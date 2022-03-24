using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSUESandboxParameterModel : NotifyPropertyChangedBase
    {
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

        string _DefaultConductorMaterial;
        public string DefaultConductorMaterial
        {
            get => _DefaultConductorMaterial;
            set
            {
                if (value != _DefaultConductorMaterial)
                {
                    _DefaultConductorMaterial = value;
                    OnPropertyChanged(nameof(DefaultConductorMaterial));
                }
            }
        }

        string _FirePowerDistributionTrunkLineAndBranchTrunkLine;
        public string FirePowerDistributionTrunkLineAndBranchTrunkLine
        {
            get => _FirePowerDistributionTrunkLineAndBranchTrunkLine;
            set
            {
                if (value != _FirePowerDistributionTrunkLineAndBranchTrunkLine)
                {
                    _FirePowerDistributionTrunkLineAndBranchTrunkLine = value;
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

        string _GeneralRequirements;
        public string GeneralRequirements
        {
            get => _GeneralRequirements;
            set
            {
                if (value != _GeneralRequirements)
                {
                    _GeneralRequirements = value;
                    OnPropertyChanged(nameof(GeneralRequirements));
                }
            }
        }

        string _FireFightingLineLess;
        public string FireFightingLineLess
        {
            get => _FireFightingLineLess;
            set
            {
                if (value != _FireFightingLineLess)
                {
                    _FireFightingLineLess = value;
                    OnPropertyChanged(nameof(FireFightingLineLess));
                }
            }
        }

        string _FireFightingLineMore;
        public string FireFightingLineMore
        {
            get => _FireFightingLineMore;
            set
            {
                if (value != _FireFightingLineMore)
                {
                    _FireFightingLineMore = value;
                    OnPropertyChanged(nameof(FireFightingLineMore));
                }
            }
        }

        string _NonFireFightingLineLess;
        public string NonFireFightingLineLess
        {
            get => _NonFireFightingLineLess;
            set
            {
                if (value != _NonFireFightingLineLess)
                {
                    _NonFireFightingLineLess = value;
                    OnPropertyChanged(nameof(NonFireFightingLineLess));
                }
            }
        }

        string _NonFireFightingLineMore;
        public string NonFireFightingLineMore
        {
            get => _NonFireFightingLineMore;
            set
            {
                if (value != _NonFireFightingLineMore)
                {
                    _NonFireFightingLineMore = value;
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
        string _SelectionOfMainCircuitOfMotor;
        public string SelectionOfMainCircuitOfMotor
        {
            get => _SelectionOfMainCircuitOfMotor;
            set
            {
                if (value != _SelectionOfMainCircuitOfMotor)
                {
                    _SelectionOfMainCircuitOfMotor = value;
                    OnPropertyChanged(nameof(SelectionOfMainCircuitOfMotor));
                }
            }
        }

        string _FireMotorPowerLess;
        public string FireMotorPowerLess
        {
            get => _FireMotorPowerLess;
            set
            {
                if (value != _FireMotorPowerLess)
                {
                    _FireMotorPowerLess = value;
                    OnPropertyChanged(nameof(FireMotorPowerLess));
                }
            }
        }

        string _FireMotorPowerMore;
        public string FireMotorPowerMore
        {
            get => _FireMotorPowerMore;
            set
            {
                if (value != _FireMotorPowerMore)
                {
                    _FireMotorPowerMore = value;
                    OnPropertyChanged(nameof(FireMotorPowerMore));
                }
            }
        }

        string _CommonMotorPowerLess;
        public string CommonMotorPowerLess
        {
            get => _CommonMotorPowerLess;
            set
            {
                if (value != _CommonMotorPowerLess)
                {
                    _CommonMotorPowerLess = value;
                    OnPropertyChanged(nameof(CommonMotorPowerLess));
                }
            }
        }

        string _CommonMotorPowerMore;
        public string CommonMotorPowerMore
        {
            get => _CommonMotorPowerMore;
            set
            {
                if (value != _CommonMotorPowerMore)
                {
                    _CommonMotorPowerMore = value;
                    OnPropertyChanged(nameof(CommonMotorPowerMore));
                }
            }
        }
    }
}
