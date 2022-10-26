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
    public class ThPDSCPSModel : NotifyPropertyChangedBase
    {
        private readonly CPS _cps;
        public ThPDSCPSModel(CPS cps)
        {
            this._cps = cps;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public string Content => _cps.Content();

        [ReadOnly(true)]
        [Category("元器件参数")]
        [DisplayName("元器件类型")]
        public string Type => _cps.ComponentType.GetDescription();

        [DisplayName("型号")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSModelPropertyEditor), typeof(PropertyEditorBase))]
        public string Model
        {
            get => _cps.Model;
            set
            {
                _cps.SetModel(value);
                OnPropertyChanged(null);
            }
        }

        [Category("元器件参数")]
        [DisplayName("壳架规格")]
        [Editor(typeof(ThPDSFrameSpecificationPropertyEditor), typeof(PropertyEditorBase))]
        public string FrameSpecification
        {
            get => _cps.FrameSpecification;
            set
            {
                _cps.SetFrameSpecification(value);
                OnPropertyChanged(null);
            }
        }

        [DisplayName("极数")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSPolesPropertyEditor), typeof(PropertyEditorBase))]
        public string PolesNum
        {
            get => _cps.PolesNum;
            set
            {
                _cps.SetPolesNum(value);
                OnPropertyChanged(null);
            }
        }

        [Category("元器件参数")]
        [DisplayName("额定电流")]
        [Editor(typeof(ThPDSRatedCurrentPropertyEditor), typeof(PropertyEditorBase))]
        public double RatedCurrent
        {
            get => _cps.RatedCurrent;
            set
            {
                _cps.SetRatedCurrent(value);
                OnPropertyChanged(null);
            }
        }

        [Category("元器件参数")]
        [DisplayName("组合形式")]
        [Editor(typeof(ThPDSCombinationPropertyEditor), typeof(PropertyEditorBase))]
        public string Combination
        {
            get => _cps.Combination;
            set
            {
                _cps.SetCombination(value);
                OnPropertyChanged(null);
            }
        }

        [Category("元器件参数")]
        [DisplayName("级别代号")]
        [Editor(typeof(ThPDSCodeLevelPropertyEditor), typeof(PropertyEditorBase))]
        public string CodeLevel
        {
            get => _cps.CodeLevel;
            set
            {
                _cps.SetCodeLevel(value);
                OnPropertyChanged(null);
            }
        }

        [Category("元器件参数")]
        [DisplayName("分段能力")]
        [Editor(typeof(ThPDSIcuPropertyEditor), typeof(PropertyEditorBase))]
        public string Icu
        {
            get => _cps.Icu;
            set
            {
                _cps.SetIcu(value);
                OnPropertyChanged(null);
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeModels
        {
            get => _cps.GetModels();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeFrameSpecifications
        {
            get => _cps.GetFrameSpecifications();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativePolesNums
        {
            get => _cps.GetPolesNums();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<double> AlternativeRatedCurrents
        {
            get => _cps.GetRatedCurrents();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeCombinations
        {
            get => _cps.GetCombinations();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeCodeLevels
        {
            get => _cps.GetCodeLevels();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeIcus
        {
            get => _cps.GetIcus();
        }
    }
}
