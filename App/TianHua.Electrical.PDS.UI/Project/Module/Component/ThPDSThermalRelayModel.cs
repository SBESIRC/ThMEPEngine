using ThCADExtension;
using System.ComponentModel;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module.Component;
using HandyControl.Controls;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Editors;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    /// <summary>
    /// 热继电器
    /// </summary>
    public class ThPDSThermalRelayModel : NotifyPropertyChangedBase
    {
        private readonly ThermalRelay _thermalRelay;

        public ThPDSThermalRelayModel(ThermalRelay thermalRelay)
        {
            _thermalRelay = thermalRelay;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public string Content => $"{RatedCurrent}A";

        [ReadOnly(true)]
        [Category("元器件参数")]
        [DisplayName("元器件类型")]
        public string Type => _thermalRelay.ComponentType.GetDescription();

        [DisplayName("型号")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSModelPropertyEditor), typeof(PropertyEditorBase))]
        public string Model
        {
            get => _thermalRelay.Model;
            set
            {
                _thermalRelay.SetModel(value);
                OnPropertyChanged(nameof(Model));
                OnPropertyChanged(nameof(Content));
            }
        }

        [DisplayName("极数")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSPolesPropertyEditor), typeof(PropertyEditorBase))]
        public string PolesNum
        {
            get => _thermalRelay.PolesNum;
            set
            {
                _thermalRelay.SetPolesNum(value);
                OnPropertyChanged(nameof(PolesNum));
            }
        }

        [Category("元器件参数")]
        [DisplayName("电流整定范围")]
        [Editor(typeof(ThPDSRatedCurrentRangePropertyEditor), typeof(PropertyEditorBase))]
        public string RatedCurrent
        {
            get => _thermalRelay.RatedCurrent;
            set
            {
                _thermalRelay.RatedCurrent = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(RatedCurrent));
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeModels
        {
            get => _thermalRelay.GetModels();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativePolesNums
        {
            get => _thermalRelay.GetPolesNums();
        }
    }
}
