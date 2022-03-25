using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSConductorModel : NotifyPropertyChangedBase
    {
        readonly Conductor conductor;
        public ThPDSConductorModel(Conductor conductor)
        {
            this.conductor = conductor;
        }
        public string ConductorInfo
        {
            get => conductor.ConductorInfo;
        }
        public string Content => conductor.Content;

        public bool IsWire
        {
            get => conductor.IsWire; set
            {
                conductor.IsWire = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(ConductorInfo));
            }
        }

        [DisplayName("外护套材质")]
        public string OuterSheathMaterial
        {
            get => conductor.OuterSheathMaterial;
        }

        [DisplayName("导体材质")]
        public string ConductorMaterial
        {
            get => conductor.ConductorMaterial;
        }

        [DisplayName("级数")]
        public Model.ThPDSPhase Phase
        {
            get => conductor.Phase; set
            {
                conductor.Phase = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(ConductorInfo));
            }
        }

        public int NumberOfPhaseWire
        {
            get => conductor.NumberOfPhaseWire; set
            {
                conductor.NumberOfPhaseWire = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(ConductorInfo));
            }
        }

        public double ConductorCrossSectionalArea
        {
            get => conductor.ConductorCrossSectionalArea; set
            {
                conductor.ConductorCrossSectionalArea = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(ConductorInfo));
            }
        }

        public double PECrossSectionalArea
        {
            get => conductor.PECrossSectionalArea; set
            {
                conductor.PECrossSectionalArea = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(ConductorInfo));
            }
        }

        [DisplayName("桥架敷设方式")]
        public PDS.Project.Module.BridgeLaying BridgeLaying
        {
            get => conductor.BridgeLaying; set
            {
                conductor.BridgeLaying = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(ConductorInfo));
            }
        }

        [DisplayName("穿管敷设方式")]
        public PDS.Project.Module.Pipelaying Pipelaying
        {
            get => conductor.Pipelaying; set
            {
                conductor.Pipelaying = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(ConductorInfo));
            }
        }

        [DisplayName("穿管直径")]
        public int PipeDiameter
        {
            get => conductor.PipeDiameter; set
            {
                conductor.PipeDiameter = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(ConductorInfo));
            }
        }
    }
}
