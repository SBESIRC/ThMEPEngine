using System.ComponentModel;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module;
using HandyControl.Controls;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Editors;
using TianHua.Electrical.PDS.UI.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module
{
    /// <summary>
    /// 二级回路（控制回路）
    /// </summary>
    public class ThPDSSecondaryCircuitModel : NotifyPropertyChangedBase
    {
        private readonly SecondaryCircuit _sc;
        private readonly ThPDSConductorModel _conductorModel;
        public ThPDSSecondaryCircuitModel(SecondaryCircuit sc)
        {
            _sc = sc;
            _conductorModel = new ThPDSConductorModel(_sc.Conductor);
        }

        [ReadOnly(true)]
        [Category("控制回路参数")]
        [DisplayName("回路编号")]
        public string CircuitID => _sc.CircuitID;

        [Category("控制回路参数")]
        [DisplayName("功能描述")]
        [Editor(typeof(ThPDSSecondaryCircuitDescriptionEditPropertyEditor), typeof(PropertyEditorBase))]
        public string CircuitDescription
        {
            get
            {
                return _sc.CircuitDescription;
            }
            set
            {
                _sc.SetDescription(value);
                OnPropertyChanged(nameof(CircuitDescription));
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> Descriptions => _sc.GetDescriptions();

        [ReadOnly(true)]
        [Browsable(false)]
        public ThPDSConductorModel ConductorModel => _conductorModel;
    }
}
