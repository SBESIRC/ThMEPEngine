using ThCADExtension;
using System.ComponentModel;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Project.Module.Component;
using HandyControl.Controls;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Editors;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public abstract class ThPDSTransferSwitchBaseModel : NotifyPropertyChangedBase
    {
        private readonly TransferSwitch _transferSwitch;

        public ThPDSTransferSwitchBaseModel(TransferSwitch transferSwitch)
        {
            _transferSwitch = transferSwitch;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public string Content => $"{Model} {RatedCurrent}A {PolesNum}";

        [ReadOnly(true)]
        [Category("元器件参数")]
        [DisplayName("元器件类型")]
        public string Type => _transferSwitch.ComponentType.GetDescription();

        [DisplayName("型号")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSModelPropertyEditor), typeof(PropertyEditorBase))]
        public string Model
        {
            get => _transferSwitch.Model;
            set
            {
                _transferSwitch.Model = value;
                OnPropertyChanged(nameof(Model));
                OnPropertyChanged(nameof(Content));
            }
        }

        [DisplayName("极数")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSPolesPropertyEditor), typeof(PropertyEditorBase))]
        public string PolesNum
        {
            get => _transferSwitch.PolesNum;
            set
            {
                _transferSwitch.PolesNum = value;
                OnPropertyChanged(nameof(PolesNum));
            }
        }

        [Category("元器件参数")]
        [DisplayName("额定电流")]
        [Editor(typeof(ThPDSRatedCurrentPropertyEditor), typeof(PropertyEditorBase))]
        public string RatedCurrent
        {
            get => _transferSwitch.RatedCurrent;
            set
            {
                _transferSwitch.SetRatedCurrent(value);
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(RatedCurrent));
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeRatedCurrents
        {
            get => _transferSwitch.GetRatedCurrents();
        }
    }
}
