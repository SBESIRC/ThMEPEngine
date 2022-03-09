using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.UI.Project.Module.Circuit
{
    public class ThPDSCircuit : NotifyPropertyChangedBase
    {
        string _CircuitUID;
        public string CircuitUID
        {
            get => _CircuitUID;
            set
            {
                if (value != _CircuitUID)
                {
                    _CircuitUID = value;
                    OnPropertyChanged(nameof(CircuitUID));
                }
            }
        }

        string _Type;
        public string Type
        {
            get => _Type;
            set
            {
                if (value != _Type)
                {
                    _Type = value;
                    OnPropertyChanged(nameof(Type));
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

        int _Phase;
        public int Phase
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

        string _ViaCableTray;
        public string ViaCableTray
        {
            get => _ViaCableTray;
            set
            {
                if (value != _ViaCableTray)
                {
                    _ViaCableTray = value;
                    OnPropertyChanged(nameof(ViaCableTray));
                }
            }
        }

        string _ViaConduit;
        public string ViaConduit
        {
            get => _ViaConduit;
            set
            {
                if (value != _ViaConduit)
                {
                    _ViaConduit = value;
                    OnPropertyChanged(nameof(ViaConduit));
                }
            }
        }
    }
}
