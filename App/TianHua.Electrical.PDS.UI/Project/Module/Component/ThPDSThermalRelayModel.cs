using System;
using ThCADExtension;
using System.ComponentModel;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    /// <summary>
    /// 热继电器
    /// </summary>
    public class ThPDSThermalRelayModel : NotifyPropertyChangedBase
    {
        private readonly ThermalRelay _thermalRelay;

        public ThPDSThermalRelayModel(ThermalRelay thermalRelay)
        {
            _thermalRelay = thermalRelay;
        }

        [ReadOnly(true)]
        [Browsable(false)]
        [DisplayName("内容")]
        public string Content => _thermalRelay.Content;

        [ReadOnly(true)]
        [DisplayName("元器件类型")]
        public string Type => _thermalRelay.ComponentType.GetDescription();

        [DisplayName("型号")]
        public ThermalRelayModel Model
        {
            get => (ThermalRelayModel)Enum.Parse(typeof(ThermalRelayModel), _thermalRelay.ThermalRelayType);
            set
            {
                _thermalRelay.ThermalRelayType = value.ToString();
                OnPropertyChanged(nameof(Model));
            }
        }

        [ReadOnly(true)]
        [DisplayName("极数")]
        public string PolesNum
        {
            get => _thermalRelay.PolesNum;
            set
            {
                _thermalRelay.PolesNum = value;
                OnPropertyChanged(nameof(PolesNum));
            }
        }

        [ReadOnly(true)]
        [DisplayName("电流整定范围")]
        public string RatedCurrent
        {
            get => _thermalRelay.RatedCurrent;
            set
            {
                _thermalRelay.RatedCurrent = value;
                OnPropertyChanged(nameof(RatedCurrent));
            }
        }
    }
}
