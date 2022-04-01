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
        [Category("元器件参数")]
        [DisplayName("元器件类型")]
        public string Type => _meter.ComponentType.GetDescription();

        [ReadOnly(true)]
        [DisplayName("极数")]
        [Category("元器件参数")]
        public string PolesNum
        {
            get => _meter.PolesNum;
        }
    }
}
