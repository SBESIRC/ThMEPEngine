using ThCADExtension;
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
    /// 断路器基类
    /// </summary>
    public abstract class ThPDSBreakerBaseModel : NotifyPropertyChangedBase
    {
        protected readonly BreakerBaseComponent _breaker;

        public ThPDSBreakerBaseModel(BreakerBaseComponent component)
        {
            _breaker = component;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        [DisplayName("内容")]
        public string Content => _breaker.Content;

        [ReadOnly(true)]
        [DisplayName("元器件类型")]
        public string Type => _breaker.ComponentType.GetDescription();

        [DisplayName("型号")]
        [Editor(typeof(ThPDSBreakerModelPropertyEditor), typeof(PropertyEditorBase))]
        public BreakerModel Model
        {
            get => _breaker.BreakerType;
            set
            {
                _breaker.SetModel(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("壳架规格")]
        [Editor(typeof(ThPDSBreakerFrameSpecificationPropertyEditor), typeof(PropertyEditorBase))]
        public string FrameSpecifications
        {
            get => _breaker.FrameSpecifications;
            set
            {
                _breaker.SetFrameSpecification(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("极数")]
        [Editor(typeof(ThPDSBreakerPolesNumPropertyEditor), typeof(PropertyEditorBase))]
        public string PolesNum
        {
            get => _breaker.PolesNum;
            set
            {
                _breaker.SetPolesNum(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("额定电流")]
        [Editor(typeof(ThPDSBreakerRatedCurrentPropertyEditor), typeof(PropertyEditorBase))]
        public string RatedCurrent
        {
            get => _breaker.RatedCurrent;
            set
            {
                _breaker.SetRatedCurrent(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("脱扣器类型")]
        [Editor(typeof(ThPDSBreakerTripDevicePropertyEditor), typeof(PropertyEditorBase))]
        public string TripUnitType
        {
            get => _breaker.TripUnitType;
            set
            {
                _breaker.SetTripDevice(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("附件")]
        public string Appendix
        {
            get => _breaker.Appendix;
            set
            {
                _breaker.Appendix = value;
                OnPropertyChanged(nameof(Appendix));
            }
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

        protected virtual void OnPropertyChanged()
        {
            OnPropertyChanged(nameof(Model));
            OnPropertyChanged(nameof(Content));
            OnPropertyChanged(nameof(PolesNum));
            OnPropertyChanged(nameof(RatedCurrent));
            OnPropertyChanged(nameof(TripUnitType));
            OnPropertyChanged(nameof(FrameSpecifications));
        }
    }
}
