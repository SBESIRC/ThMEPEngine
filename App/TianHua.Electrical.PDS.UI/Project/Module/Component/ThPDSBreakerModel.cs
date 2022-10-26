using ThCADExtension;
using System.ComponentModel;
using System.Collections.Generic;
using TianHua.Electrical.PDS.Extension;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Component;
using HandyControl.Controls;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.UI.Editors;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSBreakerModel : NotifyPropertyChangedBase
    {
        protected readonly Breaker _breaker;

        public ThPDSBreakerModel(Breaker breaker)
        {
            _breaker = breaker;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public string Content => _breaker.Content();

        [ReadOnly(true)]
        [Category("元器件参数")]
        [DisplayName("元器件类型")]
        public string Type => _breaker.ComponentType.GetDescription();

        [DisplayName("型号")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSModelPropertyEditor), typeof(PropertyEditorBase))]
        public BreakerModel Model
        {
            get => _breaker.Model;
            set
            {
                _breaker.SetModel(value);
                OnPropertyChanged(null);
            }
        }

        [Category("元器件参数")]
        [DisplayName("壳架规格")]
        [Editor(typeof(ThPDSFrameSpecificationPropertyEditor), typeof(PropertyEditorBase))]
        public string FrameSpecification
        {
            get => _breaker.FrameSpecification;
            set
            {
                _breaker.SetFrameSpecification(value);
                OnPropertyChanged(null);
            }
        }

        [Category("元器件参数")]
        [DisplayName("分段能力")]
        [Editor(typeof(ThPDSIcuPropertyEditor), typeof(PropertyEditorBase))]
        public string Icu
        {
            get => _breaker.Icu;
            set
            {
                _breaker.SetIcu(value);
                OnPropertyChanged(null);
            }
        }

        [Browsable(true)]
        [Category("元器件参数")]
        [DisplayName("RCD附件")]
        public bool RCDAppendix
        {
            get => _breaker.GetRCDAppendix();
            set
            {
                _breaker.SetRCDAppendix(value);
                OnPropertyChanged(null);
            }
        }

        [Browsable(true)]
        [Category("元器件参数")]
        [DisplayName("分励脱扣附件")]
        public bool STAppendix
        {
            get => _breaker.STAppendix ?? false;
            set
            {
                _breaker.STAppendix = value;
                OnPropertyChanged(null);
            }
        }

        [Browsable(true)]
        [Category("元器件参数")]
        [DisplayName("报警附件")]
        public bool ALAppendix
        {
            get => _breaker.ALAppendix ?? false;
            set
            {
                _breaker.ALAppendix = value;
                OnPropertyChanged(null);
            }
        }

        [Browsable(true)]
        [Category("元器件参数")]
        [DisplayName("失压脱扣附件")]
        public bool URAppendix
        {
            get => _breaker.URAppendix ?? false;
            set
            {
                _breaker.URAppendix = value;
                OnPropertyChanged(null);
            }
        }

        [Browsable(true)]
        [Category("元器件参数")]
        [DisplayName("辅助触电附件")]
        public bool AXAppendix
        {
            get => _breaker.AXAppendix ?? false;
            set
            {
                _breaker.AXAppendix = value;
                OnPropertyChanged(null);
            }
        }

        [DisplayName("极数")]
        [Category("元器件参数")]
        [Editor(typeof(ThPDSPolesPropertyEditor), typeof(PropertyEditorBase))]
        public string PolesNum
        {
            get => _breaker.PolesNum;
            set
            {
                _breaker.SetPolesNum(value);
                OnPropertyChanged(null);
            }
        }

        [Category("元器件参数")]
        [DisplayName("额定电流")]
        [Editor(typeof(ThPDSRatedCurrentPropertyEditor), typeof(PropertyEditorBase))]
        public string RatedCurrent
        {
            get => _breaker.RatedCurrent;
            set
            {
                _breaker.SetRatedCurrent(value);
                OnPropertyChanged(null);
            }
        }

        [Category("元器件参数")]
        [DisplayName("脱扣器类型")]
        [Editor(typeof(ThPDSBreakerTripDevicePropertyEditor), typeof(PropertyEditorBase))]
        public string TripUnitType
        {
            get => _breaker.TripUnitType;
            set
            {
                _breaker.SetTripDevice(value);
                OnPropertyChanged(null);
            }
        }

        [Browsable(true)]
        [Category("元器件参数")]
        [DisplayName("RCD类型")]
        [Editor(typeof(ThPDSBreakerRCDTypePropertyEditor), typeof(PropertyEditorBase))]
        public RCDType RCDType
        {
            get => _breaker.RCDType;
            set
            {
                _breaker.SetRCDType(value);
                OnPropertyChanged(null);
            }
        }

        [Browsable(true)]
        [Category("元器件参数")]
        [DisplayName("剩余电流动作")]
        [Editor(typeof(ThPDSResidualCurrentPropertyEditor<ResidualCurrentSpecification>), typeof(PropertyEditorBase))]
        public ResidualCurrentSpecification ResidualCurrent
        {
            get => _breaker.ResidualCurrent;
            set
            {
                _breaker.SetResidualCurrent(value);
                OnPropertyChanged(null);
            }
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<RCDType> AlternativeRCDTypes
        {
            get => _breaker.GetRCDTypes();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<ResidualCurrentSpecification> AlternativeResidualCurrents
        {
            get => _breaker.GetResidualCurrents();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<BreakerModel> AlternativeModels
        {
            get => _breaker.GetModels();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativePolesNums
        {
            get => _breaker.GetPolesNums();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeRatedCurrents
        {
            get => _breaker.GetRatedCurrents();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeTripDevices
        {
            get => _breaker.GetTripDevices();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeFrameSpecifications
        {
            get => _breaker.GetFrameSpecifications();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public List<string> AlternativeIcus
        {
            get => _breaker.GetIcus();
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public bool IsRCDAppendixEnabled
        {
            get => _breaker.RCDAppendix.HasValue;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public bool IsSTAppendixEnabled
        {
            get => _breaker.STAppendix.HasValue;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public bool IsALAppendixEnabled
        {
            get => _breaker.ALAppendix.HasValue;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public bool IsURAppendixEnabled
        {
            get => _breaker.URAppendix.HasValue;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public bool IsAXAppendixEnabled
        {
            get => _breaker.AXAppendix.HasValue;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        public ComponentType ComponentType => _breaker.ComponentType;
    }
}
