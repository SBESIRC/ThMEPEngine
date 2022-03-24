using System;
using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;
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

        [ReadOnly(true)]
        [DisplayName("内容")]
        public string Content => _breaker.Content;

        [ReadOnly(true)]
        [DisplayName("元器件类型")]
        public ComponentType Type => _breaker.ComponentType;

        [DisplayName("型号")]
        public BreakerModel Model
        {
            get => (BreakerModel)Enum.Parse(typeof(BreakerModel), _breaker.BreakerType);
            set
            {
                _breaker.SetModel(value.ToString());
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
            OnPropertyChanged(nameof(Model));
            OnPropertyChanged(nameof(Content));
            OnPropertyChanged(nameof(PolesNum));
            OnPropertyChanged(nameof(RatedCurrent));
            OnPropertyChanged(nameof(TripUnitType));
            OnPropertyChanged(nameof(FrameSpecifications));
        }
    }
}
