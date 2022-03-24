using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSBreakerModel : NotifyPropertyChangedBase
    {
        private readonly Breaker _breaker;

        public ThPDSBreakerModel(Breaker breaker)
        {
            _breaker = breaker;
        }

        [DisplayName("内容")]
        [ReadOnlyAttribute(true)]
        public string Content => _breaker.Content;


        [DisplayName("元器件类型")]
        [ReadOnlyAttribute(true)]
        public ComponentType Type => _breaker.ComponentType;


        [DisplayName("型号")]
        public string BreakerType
        {
            get => _breaker.BreakerType;
            set
            {
                _breaker.SetModel(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("壳架规格")]
        public string FrameSpecifications
        {
            get => _breaker.FrameSpecifications;
            set
            {
                _breaker.SetFrameSpecifications(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("极数")]
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

        private void OnPropertyChanged()
        {
            OnPropertyChanged(nameof(Content));
            OnPropertyChanged(nameof(PolesNum));
            OnPropertyChanged(nameof(BreakerType));
            OnPropertyChanged(nameof(RatedCurrent));
            OnPropertyChanged(nameof(TripUnitType));
            OnPropertyChanged(nameof(FrameSpecifications));
        }
    }
}
