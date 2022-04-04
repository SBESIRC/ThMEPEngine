using TianHua.Electrical.PDS.Project.Module;
using ThControlLibraryWPF.ControlUtils;
using Microsoft.Toolkit.Mvvm.Input;

namespace TianHua.Electrical.PDS.UI.Services
{
    public class ThPDSDistributionPanelConfig : NotifyPropertyChangedBase
    {
        private RelayCommand _batchGenerate;
        public RelayCommand BatchGenerate
        {
            get => _batchGenerate;
            set
            {
                if (value != _batchGenerate)
                {
                    _batchGenerate = value;
                    OnPropertyChanged(nameof(BatchGenerate));
                }
            }
        }

        private ThPDSDistributionPanelConfigState _current;
        public ThPDSDistributionPanelConfigState Current
        {
            get => _current;
            set
            {
                if (value != _current)
                {
                    _current = value;
                    OnPropertyChanged(nameof(Current));
                }
            }
        }
    }

    public class ThPDSDistributionPanelConfigState : NotifyPropertyChangedBase
    {
        private readonly ThPDSProjectGraphNode _node;
        public ThPDSDistributionPanelConfigState(ThPDSProjectGraphNode node)
        {
            this._node = node;
        }

        public bool FirePowerMonitoring
        {
            get => _node.Details.FirePowerMonitoring;
            set
            {
                if (value != FirePowerMonitoring)
                {
                    _node.Details.FirePowerMonitoring = value;
                    OnPropertyChanged(nameof(FirePowerMonitoring));
                }
            }
        }

        public bool ElectricalFireMonitoring
        {
            get => _node.Details.ElectricalFireMonitoring;
            set
            {
                if (value != ElectricalFireMonitoring)
                {
                    _node.Details.ElectricalFireMonitoring = value;
                    OnPropertyChanged(nameof(ElectricalFireMonitoring));
                }
            }
        }

        public SurgeProtectionDeviceType SurgeProtection
        {
            get => _node.Details.SurgeProtection;
            set
            {
                if (value != SurgeProtection)
                {
                    _node.Details.SurgeProtection = value;
                    OnPropertyChanged(nameof(SurgeProtection));
                }
            }
        }

        private double _busLength;
        public double BusLength
        {
            get => _busLength;
            set
            {
                if (value != _busLength)
                {
                    _busLength = value;
                    OnPropertyChanged(nameof(BusLength));
                }
            }
        }
    }
}
