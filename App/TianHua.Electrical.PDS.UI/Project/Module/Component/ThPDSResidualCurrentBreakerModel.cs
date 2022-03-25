using System;
using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    /// <summary>
    /// 剩余电流断路器（带漏电保护功能的断路器）
    /// </summary>
    public class ThPDSResidualCurrentBreakerModel : NotifyPropertyChangedBase
    {
        private readonly ResidualCurrentBreaker _rccb;

        public ThPDSResidualCurrentBreakerModel(ResidualCurrentBreaker breaker)
        {
            _rccb = breaker;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        [DisplayName("内容")]
        public string Content => _rccb.Content;

        [ReadOnly(true)]
        [DisplayName("元器件类型")]
        public ComponentType Type => _rccb.ComponentType;

        [DisplayName("型号")]
        public BreakerModel Model
        {
            get => _rccb.BreakerType;
            set
            {
                _rccb.SetModel(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("壳架规格")]
        public string FrameSpecifications
        {
            get => _rccb.FrameSpecifications;
            set
            {
                _rccb.SetFrameSpecification(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("极数")]
        public string PolesNum
        {
            get => _rccb.PolesNum;
            set
            {
                _rccb.SetPolesNum(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("额定电流")]
        public string RatedCurrent
        {
            get => _rccb.RatedCurrent;
            set
            {
                _rccb.SetRatedCurrent(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("脱扣器类型")]
        public string TripUnitType
        {
            get => _rccb.TripUnitType;
            set
            {
                _rccb.SetTripDevice(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("RCD类型")]
        public RCDType RCDType
        {
            get => _rccb.RCDType;
            set
            {
                _rccb.SetRCDType(value);
                OnPropertyChanged();
            }
        }

        [DisplayName("剩余电流动作")]
        public ResidualCurrentSpecification ResidualCurrent
        {
            get => _rccb.ResidualCurrent;
            set
            {
                _rccb.ResidualCurrent = value;
                OnPropertyChanged();
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
