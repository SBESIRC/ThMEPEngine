using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Project.Module;
using TianHua.Electrical.PDS.Project.Module.Component;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThermalRelayModel : NotifyPropertyChangedBase
    {
        readonly ThermalRelay thermalRelay;

        public ThermalRelayModel(ThermalRelay thermalRelay)
        {
            this.thermalRelay = thermalRelay;
        }
        [DisplayName(null)]
        public string Content => thermalRelay.Content;
        [DisplayName("元器件类型")]
        public string Type => "热继电器";
        [DisplayName("热继电器类型")]
        public string ThermalRelayType
        {
            get => thermalRelay.ThermalRelayType;
            set
            {
                thermalRelay.ThermalRelayType = value;
                OnPropertyChanged(nameof(Content));
            }
        }
        [DisplayName("极数")]
        public string PolesNum { get => thermalRelay.PolesNum; set => thermalRelay.PolesNum = value; }
        [DisplayName("额定电流")]
        public string RatedCurrent
        {
            get => thermalRelay.RatedCurrent;
            set
            {
                thermalRelay.RatedCurrent = value;
                OnPropertyChanged(nameof(Content));
            }
        }
    }
}
