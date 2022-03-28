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
    /// 导体（配电回路）
    /// </summary>
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
        [Editor(typeof(ThPDSConductorWireNumbersPropertyEditor), typeof(PropertyEditorBase))]
        public int NumberOfPhaseWire
        {
            get => _conductor.NumberOfPhaseWire; 
            set
            {
                _conductor.SetNumberOfPhaseWire(value);
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(NumberOfPhaseWire));
            }
        }

        [DisplayName("相导体截面")]
        [Editor(typeof(ThPDSConductorCrossSectionalAreasPropertyEditor), typeof(PropertyEditorBase))]
        public double ConductorCrossSectionalArea
        {
            get => _conductor.ConductorCrossSectionalArea;
            set
            {
                _conductor.SetConductorCrossSectionalArea(value);
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(ConductorCrossSectionalArea));
            }
        }

        //[ReadOnly(true)]
        //[DisplayName("中性线导体截面")]
        //public double NeutralConductorCrossSectionalArea
        //{
        //    get => _conductor.NeutralConductorCrossSectionalArea;
        //}

        //[ReadOnly(true)]
        //[DisplayName("PE线导体截面")]
        //public double PECrossSectionalArea
        //{
        //    get => _conductor.PECrossSectionalArea;
        //}

        [ReadOnly(true)]
        [DisplayName("敷设方式")]
        [Editor(typeof(ThPDSEnumPropertyEditor<Pipelaying>), typeof(PropertyEditorBase))]
        public Pipelaying Pipelaying
        {
            get => _conductor.Pipelaying;
        }

        [ReadOnly(true)]
        [DisplayName("管材")]
        [Editor(typeof(ThPDSEnumPropertyEditor<PipeMaterial>), typeof(PropertyEditorBase))]
        public PipeMaterial PipeMaterial
        {
            get => _conductor.PipeMaterial;
        }

        [ReadOnly(true)]
        [DisplayName("穿管直径")]
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
        public List<double> AlternativeConductorCrossSectionalAreas
        {
            get => _conductor.GetConductorCrossSectionalAreas();
        }
    }
}
