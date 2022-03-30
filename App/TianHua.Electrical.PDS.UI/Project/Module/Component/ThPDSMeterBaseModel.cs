using ThCADExtension;
using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    /// <summary>
    /// 电表（基类）
    /// </summary>
    public abstract class ThPDSMeterBaseModel : NotifyPropertyChangedBase
    {
        protected Meter _meter;

        public ThPDSMeterBaseModel(Meter meter)
        {
            _meter = meter;
        }

        [ReadOnly(true)]
        [DisplayName("元器件类型")]
        public string Type => _meter.ComponentType.GetDescription();

        [DisplayName("极数")]
        public string PolesNum
        {
            get => _meter.PolesNum;
            set
            {
                _meter.PolesNum = value;
                OnPropertyChanged(nameof(PolesNum));
            }
        }
    }
}
