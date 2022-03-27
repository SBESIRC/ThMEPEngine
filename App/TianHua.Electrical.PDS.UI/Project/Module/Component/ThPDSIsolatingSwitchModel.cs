using ThCADExtension;
using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    /// <summary>
    /// 隔离开关
    /// </summary>
    public class ThPDSIsolatingSwitchModel : NotifyPropertyChangedBase
    {
        private readonly IsolatingSwitch _isolatingSwitch;
        public ThPDSIsolatingSwitchModel(IsolatingSwitch isolatingSwitch)
        {
            _isolatingSwitch = isolatingSwitch;
        }

        [ReadOnly(true)]
        [DisplayName("元器件类型")]
        public string Type
        {
            get => _isolatingSwitch.ComponentType.GetDescription();
        }

        [DisplayName("型号")]
        public string IsolatingSwitchType
        {
            get => _isolatingSwitch.IsolatingSwitchType;
            set
            {
                _isolatingSwitch.IsolatingSwitchType = value;
                OnPropertyChanged(nameof(IsolatingSwitchType));
            }
        }

        [DisplayName("极数")]
        public string PolesNum
        {
            get => _isolatingSwitch.PolesNum;
            set
            {
                _isolatingSwitch.PolesNum = value;
                OnPropertyChanged(nameof(PolesNum));
            }
        }

        [DisplayName("额定电流")]
        public string RatedCurrent
        {
            get => _isolatingSwitch.RatedCurrent;
            set
            {
                _isolatingSwitch.RatedCurrent = value;
                OnPropertyChanged(nameof(RatedCurrent));
            }
        }

        [Browsable(false)]
        [DisplayName("内容")]
        public string Content
        {
            get => _isolatingSwitch.Content;
        }
    }
}
