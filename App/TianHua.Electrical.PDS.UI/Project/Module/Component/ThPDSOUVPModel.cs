using ThCADExtension;
using System.ComponentModel;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Extension;
using TianHua.Electrical.PDS.Project.Module.Component;
using HandyControl.Controls;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Editors;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSOUVPModel : NotifyPropertyChangedBase
    {
        private readonly OUVP _ouvp;
        public ThPDSOUVPModel(OUVP oUVP)
        {
            _ouvp = oUVP;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public object Content => _ouvp.Content();

        [ReadOnly(true)]
        [Category("元器件参数")]
        [DisplayName("元器件类型")]
        public string Type => _ouvp.ComponentType.GetDescription();

        [DisplayName("型号")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSModelPropertyEditor), typeof(PropertyEditorBase))]
        public string Model
        {
            get => _ouvp.Model;
            set
            {
                _ouvp.SetModel(value);
                OnPropertyChanged(nameof(Model));
            }
        }

        [ReadOnly(true)]
        [DisplayName("极数")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSPolesPropertyEditor), typeof(PropertyEditorBase))]
        public string PolesNum
        {
            get => _ouvp.PolesNum;
            set
            {
                _ouvp.SetPolesNum(value);
                OnPropertyChanged(nameof(PolesNum));
            }
        }

        [Category("元器件参数")]
        [DisplayName("额定电流")]
        [Editor(typeof(ThPDSRatedCurrentPropertyEditor), typeof(PropertyEditorBase))]
        public double RatedCurrent
        {
            get => _ouvp.RatedCurrent;
            set
            {
                _ouvp.SetRatedCurrent(value);
                OnPropertyChanged(nameof(RatedCurrent));
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeModels
        {
            get
            {
                return _ouvp.GetModels();
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativePolesNums
        {
            get
            {
                return _ouvp.GetPolesNums();
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<double> AlternativeRatedCurrents
        {
            get
            {
                return _ouvp.GetRatedCurrents();
            }
        }
    }
}
