using ThCADExtension;
using System.ComponentModel;
using TianHua.Electrical.PDS.Project.Module.Component;
using HandyControl.Controls;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Editors;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    /// <summary>
    /// 接触器
    /// </summary>
    public class ThPDSContactorModel : NotifyPropertyChangedBase
    {
        private readonly Contactor _contactor;

        public ThPDSContactorModel(Contactor contactor)
        {
            _contactor = contactor;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        [DisplayName("内容")]
        public string Content => $"{RatedCurrent}/{PolesNum}";


        [ReadOnly(true)]
        [DisplayName("元器件类型")]
        public string Type => _contactor.ComponentType.GetDescription();


        [DisplayName("型号")]
        [Editor(typeof(ThPDSModelPropertyEditor), typeof(PropertyEditorBase))]
        public string Model
        {
            get => _contactor.Model;
            set
            {
                _contactor.Model = value;
                OnPropertyChanged(nameof(Model));
                OnPropertyChanged(nameof(Content));
            }
        }

        [DisplayName("极数")]
        [Editor(typeof(ThPDSPolesPropertyEditor), typeof(PropertyEditorBase))]
        public string PolesNum
        {
            get => _contactor.PolesNum;
            set
            {
                _contactor.PolesNum = value;
                OnPropertyChanged(nameof(PolesNum));
                OnPropertyChanged(nameof(Content));
            }
        }

        [DisplayName("额定电流")]
        [Editor(typeof(ThPDSRatedCurrentPropertyEditor), typeof(PropertyEditorBase))]
        public string RatedCurrent
        {
            get => _contactor.RatedCurrent;
            set
            {
                _contactor.RatedCurrent = value;
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(RatedCurrent));
            }
        }
    }
}
