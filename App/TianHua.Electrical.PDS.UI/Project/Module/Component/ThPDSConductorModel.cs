using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSConductorModel : NotifyPropertyChangedBase
    {
        private readonly Conductor _conductor;
        public ThPDSConductorModel(Conductor conductor)
        {
            _conductor = conductor;
        }

        [ReadOnly(true)]
        [DisplayName("燃料特性代号")]
        public string ConductorMaterial
        {
            get => _conductor.ConductorMaterial;
        }

        [ReadOnly(true)]
        [DisplayName("材料特征及结构")]
        public string OuterSheathMaterial
        {
            get => _conductor.OuterSheathMaterial;
        }

        [DisplayName("电缆根数")]
        public int NumberOfPhaseWire
        {
            get => _conductor.NumberOfPhaseWire; 
            set
            {
                _conductor.NumberOfPhaseWire = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(NumberOfPhaseWire));
            }
        }

        [DisplayName("相导体截面")]
        public double ConductorCrossSectionalArea
        {
            get => _conductor.ConductorCrossSectionalArea;
            set
            {
                _conductor.ConductorCrossSectionalArea = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(ConductorCrossSectionalArea));
            }
        }

        [DisplayName("PE线导体截面")]
        public double PECrossSectionalArea
        {
            get => _conductor.PECrossSectionalArea; 
            set
            {
                _conductor.PECrossSectionalArea = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(PECrossSectionalArea));
            }
        }

        [DisplayName("敷设方式")]
        public BridgeLaying BridgeLaying
        {
            get => _conductor.BridgeLaying; 
            set
            {
                _conductor.BridgeLaying = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(BridgeLaying));
            }
        }

        [DisplayName("穿管直径")]
        public int PipeDiameter
        {
            get => _conductor.PipeDiameter; 
            set
            {
                _conductor.PipeDiameter = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(PipeDiameter));
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public string Content
        {
            get => _conductor.Content;
        }
    }
}
