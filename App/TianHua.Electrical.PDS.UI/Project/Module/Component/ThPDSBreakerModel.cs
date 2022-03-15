using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSBreakerModel : NotifyPropertyChangedBase
    {
        readonly Breaker breaker;

        public ThPDSBreakerModel(Breaker breaker)
        {
            this.breaker = breaker;
        }
        [DisplayName("内容")]
        public string Content => breaker.Content;
        [DisplayName("元器件类型")]
        public string Type => "断路器";
        [DisplayName("模型")]
        public string BreakerType
        {
            get => breaker.BreakerType;
            set
            {
                breaker.BreakerType = value;
                OnPropertyChanged(nameof(Content));
            }
        }
        [DisplayName("壳架规格")]
        public string FrameSpecifications
        {
            get => breaker.FrameSpecifications;
            set
            {
                breaker.FrameSpecifications = value;
                OnPropertyChanged(nameof(Content));
            }
        }

        [DisplayName("极数")]
        public string PolesNum
        {
            get => breaker.PolesNum;
            set
            {
                breaker.PolesNum = value;
                OnPropertyChanged(nameof(Content));
            }
        }
        [DisplayName("额定电流")]
        public string RatedCurrent
        {
            get => breaker.RatedCurrent;
            set
            {
                breaker.RatedCurrent = value;
                OnPropertyChanged(nameof(Content));
            }
        }
        [DisplayName("脱扣器类型")]
        public string TripUnitType
        {
            get => breaker.TripUnitType;
            set
            {
                breaker.TripUnitType = value;
                OnPropertyChanged(nameof(Content));
            }
        }
        [DisplayName("附件")]
        public string Appendix { get => breaker.Appendix; set => breaker.Appendix = value; }
    }
}
