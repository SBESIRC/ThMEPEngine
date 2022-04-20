using ThCADExtension;
using System.ComponentModel;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Diagram;
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
        public string Content => _contactor.Content();

        [ReadOnly(true)]
        [Category("元器件参数")]
        [DisplayName("元器件类型")]
        public string Type => _contactor.ComponentType.GetDescription();

        [DisplayName("型号")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSModelPropertyEditor), typeof(PropertyEditorBase))]
        public string Model
        {
            get => _contactor.Model;
            set
            {
                _contactor.SetModel(value);
                OnPropertyChanged(nameof(Model));
                OnPropertyChanged(nameof(Content));
            }
        }

        [DisplayName("极数")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSPolesPropertyEditor), typeof(PropertyEditorBase))]
        public string PolesNum
        {
            get => _contactor.PolesNum;
            set
            {
                _contactor.SetPolesNum(value);
                OnPropertyChanged(nameof(PolesNum));
                OnPropertyChanged(nameof(Content));
            }
        }

        [Category("元器件参数")]
        [DisplayName("额定电流")]
        [Editor(typeof(ThPDSRatedCurrentPropertyEditor), typeof(PropertyEditorBase))]
        public string RatedCurrent
        {
            get => _contactor.RatedCurrent;
            set
            {
                _contactor.SetRatedCurrent(value);
                OnPropertyChanged(nameof(Content));
                OnPropertyChanged(nameof(RatedCurrent));
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativePolesNums
        {
            get => _contactor.GetPolesNums();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeRatedCurrents
        {
            get => _contactor.GetRatedCurrents();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeModels
        {
            get => _contactor.GetModels();
        }
    }
}
