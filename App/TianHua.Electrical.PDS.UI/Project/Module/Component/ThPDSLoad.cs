using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.UI.Project.Module.Component
{
    public class ThPDSLoad : NotifyPropertyChangedBase
    {
        string _LoadUID;
        public string LoadUID
        {
            get => _LoadUID;
            set
            {
                if (value != _LoadUID)
                {
                    _LoadUID = value;
                    OnPropertyChanged(nameof(LoadUID));
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

        string _CalculateCurrent;
        public string CalculateCurrent
        {
            get => _CalculateCurrent;
            set
            {
                if (value != _CalculateCurrent)
                {
                    _CalculateCurrent = value;
                    OnPropertyChanged(nameof(CalculateCurrent));
                }
            }
        }

        string _LoadTypeCat_1;
        public string LoadTypeCat_1
        {
            get => _LoadTypeCat_1;
            set
            {
                if (value != _LoadTypeCat_1)
                {
                    _LoadTypeCat_1 = value;
                    OnPropertyChanged(nameof(LoadTypeCat_1));
                }
            }
        }

        string _LoadTypeCat_2;
        public string LoadTypeCat_2
        {
            get => _LoadTypeCat_2;
            set
            {
                if (value != _LoadTypeCat_2)
                {
                    _LoadTypeCat_2 = value;
                    OnPropertyChanged(nameof(LoadTypeCat_2));
                }
            }
        }

        string _DefaultCircuitType;
        public string DefaultCircuitType
        {
            get => _DefaultCircuitType;
            set
            {
                if (value != _DefaultCircuitType)
                {
                    _DefaultCircuitType = value;
                    OnPropertyChanged(nameof(DefaultCircuitType));
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

        string _PrimaryAvail;
        public string PrimaryAvail
        {
            get => _PrimaryAvail;
            set
            {
                if (value != _PrimaryAvail)
                {
                    _PrimaryAvail = value;
                    OnPropertyChanged(nameof(PrimaryAvail));
                }
            }
        }

        string _SpareAvail;
        public string SpareAvail
        {
            get => _SpareAvail;
            set
            {
                if (value != _SpareAvail)
                {
                    _SpareAvail = value;
                    OnPropertyChanged(nameof(SpareAvail));
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

        string _FrequencyConversion;
        public string FrequencyConversion
        {
            get => _FrequencyConversion;
            set
            {
                if (value != _FrequencyConversion)
                {
                    _FrequencyConversion = value;
                    OnPropertyChanged(nameof(FrequencyConversion));
                }
            }
        }

        string _AttributesCopy;
        public string AttributesCopy
        {
            get => _AttributesCopy;
            set
            {
                if (value != _AttributesCopy)
                {
                    _AttributesCopy = value;
                    OnPropertyChanged(nameof(AttributesCopy));
                }
            }
        }
    }
}
