using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.UI.Project.Module.Circuit
{
    public class ThPDSBlockInfo : NotifyPropertyChangedBase
    {
        string _BlockName;
        public string BlockName
        {
            get => _BlockName;
            set
            {
                if (value != _BlockName)
                {
                    _BlockName = value;
                    OnPropertyChanged(nameof(BlockName));
                }
            }
        }

        ThPDSLoadTypeCat_1 _Cat_1;
        public ThPDSLoadTypeCat_1 Cat_1
        {
            get => _Cat_1;
            set
            {
                if (value != _Cat_1)
                {
                    _Cat_1 = value;
                    OnPropertyChanged(nameof(Cat_1));
                }
            }
        }

        ThPDSLoadTypeCat_2 _Cat_2;
        public ThPDSLoadTypeCat_2 Cat_2
        {
            get => _Cat_2;
            set
            {
                if (value != _Cat_2)
                {
                    _Cat_2 = value;
                    OnPropertyChanged(nameof(Cat_2));
                }
            }
        }

        string _Properties;
        public string Properties
        {
            get => _Properties;
            set
            {
                if (value != _Properties)
                {
                    _Properties = value;
                    OnPropertyChanged(nameof(Properties));
                }
            }
        }

        ThPDSCircuitType _DefaultCircuitType;
        public ThPDSCircuitType DefaultCircuitType
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
    }
}