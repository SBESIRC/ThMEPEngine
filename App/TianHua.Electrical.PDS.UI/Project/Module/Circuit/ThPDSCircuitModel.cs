using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.UI.Project.Module.Circuit
{
    public class ThPDSCircuitModel : NotifyPropertyChangedBase
    {
        string _CircuitType;
        public string CircuitType
        {
            get => _CircuitType;
            set
            {
                if (value != _CircuitType)
                {
                    _CircuitType = value;
                    OnPropertyChanged(nameof(CircuitType));
                }
            }
        }

        string _TextKey;
        public string TextKey
        {
            get => _TextKey;
            set
            {
                if (value != _TextKey)
                {
                    _TextKey = value;
                    OnPropertyChanged(nameof(TextKey));
                }
            }
        }

        string _KV;
        public string KV
        {
            get => _KV;
            set
            {
                if (value != _KV)
                {
                    _KV = value;
                    OnPropertyChanged(nameof(KV));
                }
            }
        }

        string _Phase;
        public string Phase
        {
            get => _Phase;
            set
            {
                if (value != _Phase)
                {
                    _Phase = value;
                    OnPropertyChanged(nameof(Phase));
                }
            }
        }

        string _DemandFactor;
        public string DemandFactor
        {
            get => _DemandFactor;
            set
            {
                if (value != _DemandFactor)
                {
                    _DemandFactor = value;
                    OnPropertyChanged(nameof(DemandFactor));
                }
            }
        }

        string _PowerFactor;
        public string PowerFactor
        {
            get => _PowerFactor;
            set
            {
                if (value != _PowerFactor)
                {
                    _PowerFactor = value;
                    OnPropertyChanged(nameof(PowerFactor));
                }
            }
        }

        string _FireLoad;
        public string FireLoad
        {
            get => _FireLoad;
            set
            {
                if (value != _FireLoad)
                {
                    _FireLoad = value;
                    OnPropertyChanged(nameof(FireLoad));
                }
            }
        }
    }
}
