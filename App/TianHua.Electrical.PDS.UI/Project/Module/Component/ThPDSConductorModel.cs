using System.ComponentModel;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Component;
using HandyControl.Controls;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Editors;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    /// <summary>
    /// 导体
    /// </summary>
    public class ThPDSConductorModel : NotifyPropertyChangedBase
    {
        private readonly Conductor _conductor;
        public ThPDSConductorModel(Conductor conductor)
        {
            _conductor = conductor;
        }

        [ReadOnly(true)]
        [Category("电线电缆参数")]
        [DisplayName("燃料特性代号")]
        public string ConductorMaterial
        {
            get => _conductor.ConductorMaterial;
        }

        [ReadOnly(true)]
        [Category("电线电缆参数")]
        [DisplayName("材料特征及结构")]
        public string OuterSheathMaterial
        {
            get => _conductor.OuterSheathMaterial;
        }

        [Browsable(true)]
        [DisplayName("电缆根数")]
        [Category("电线电缆参数")]
        [Editor(typeof(ThPDSConductorWireNumbersPropertyEditor), typeof(PropertyEditorBase))]
        public int NumberOfPhaseWire
        {
            get => _conductor.NumberOfPhaseWire; 
            set
            {
                _conductor.SetNumberOfPhaseWire(value);
                OnPropertyChanged(nameof(NumberOfPhaseWire));
                OnPropertyChanged(nameof(Content));
            }
        }


        [Browsable(true)]
        [DisplayName("控制线芯数")]
        [Category("电线电缆参数")]
        [Editor(typeof(ThPDSConductorWireNumbersPropertyEditor), typeof(PropertyEditorBase))]
        public int ConductorCount
        {
            get => _conductor.ConductorCount;
            set
            {
                _conductor.SetConductorCount(value);
                OnPropertyChanged(nameof(ConductorCount));
                OnPropertyChanged(nameof(Content));
            }
        }

        [Browsable(true)]
        [Category("电线电缆参数")]
        [DisplayName("相导体截面")]
        [Editor(typeof(ThPDSConductorCrossSectionalAreasPropertyEditor), typeof(PropertyEditorBase))]
        public double ConductorCrossSectionalArea
        {
            get => _conductor.ConductorCrossSectionalArea;
            set
            {
                _conductor.SetConductorCrossSectionalArea(value);
                OnPropertyChanged(nameof(ConductorCrossSectionalArea));
                OnPropertyChanged(nameof(Content));
            }
        }

        [Browsable(true)]
        [Category("电线电缆参数")]
        [DisplayName("控制线导体截面")]
        [Editor(typeof(ThPDSConductorCrossSectionalAreasPropertyEditor), typeof(PropertyEditorBase))]
        public double ControlConductorCrossSectionalArea
        {
            get => _conductor.ConductorCrossSectionalArea;
            set
            {
                _conductor.SetConductorCrossSectionalArea(value);
                OnPropertyChanged(nameof(ControlConductorCrossSectionalArea));
                OnPropertyChanged(nameof(Content));
            }
        }

        [ReadOnly(true)]
        [DisplayName("敷设方式")]
        [Category("电线电缆参数")]
        [Editor(typeof(ThPDSEnumPropertyEditor<Pipelaying>), typeof(PropertyEditorBase))]
        public Pipelaying Pipelaying
        {
            get => _conductor.Pipelaying;
        }

        [ReadOnly(true)]
        [DisplayName("管材")]
        [Category("电线电缆参数")]
        [Editor(typeof(ThPDSEnumPropertyEditor<PipeMaterial>), typeof(PropertyEditorBase))]
        public PipeMaterial PipeMaterial
        {
            get => _conductor.PipeMaterial;
        }

        [ReadOnly(true)]
        [DisplayName("穿管直径")]
        [Category("电线电缆参数")]
        public int PipeDiameter
        {
            get => _conductor.PipeDiameter;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public string Content
        {
            get => _conductor.Content;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<int> AlternativeWireNumbers
        {
            get => _conductor.GetNumberOfPhaseWires();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<int> ConductorCounts => _conductor.GetConductorCounts();

        [ReadOnly(true)]
        [Browsable(false)]
        public List<double> AlternativeConductorCrossSectionalAreas
        {
            get => _conductor.GetConductorCrossSectionalAreas();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public ComponentType ComponentType => _conductor.ComponentType;

        [ReadOnly(true)]
        [Browsable(false)]
        public bool IsCustom => _conductor.IsCustom;
    }
}
