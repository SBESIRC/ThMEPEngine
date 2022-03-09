using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThermalRelay : NotifyPropertyChangedBase
    {
        string _Content;
        public string Content
        {
            get => _Content;
            set
            {
                if (value != _Content)
                {
                    _Content = value;
                    OnPropertyChanged(nameof(Content));
                }
            }
        }

        string _ThermalRelayType;
        public string ThermalRelayType
        {
            get => _ThermalRelayType;
            set
            {
                if (value != _ThermalRelayType)
                {
                    _ThermalRelayType = value;
                    OnPropertyChanged(nameof(ThermalRelayType));
                }
            }
        }

        string _PolesNum;
        public string PolesNum
        {
            get => _PolesNum;
            set
            {
                if (value != _PolesNum)
                {
                    _PolesNum = value;
                    OnPropertyChanged(nameof(PolesNum));
                }
            }
        }

        string _RatedCurrent;
        public string RatedCurrent
        {
            get => _RatedCurrent;
            set
            {
                if (value != _RatedCurrent)
                {
                    _RatedCurrent = value;
                    OnPropertyChanged(nameof(RatedCurrent));
                }
            }
        }
    }
}
