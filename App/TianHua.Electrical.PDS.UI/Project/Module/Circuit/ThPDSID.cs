using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using TianHua.Electrical.PDS.Model;


namespace TianHua.Electrical.PDS.UI.Project.Module.Circuit
{
    public class ThPDSID : NotifyPropertyChangedBase
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

        string _LoadID;
        public string LoadID
        {
            get => _LoadID;
            set
            {
                if (value != _LoadID)
                {
                    _LoadID = value;
                    OnPropertyChanged(nameof(LoadID));
                }
            }
        }

        string _CircuitID;
        public string CircuitID
        {
            get => _CircuitID;
            set
            {
                if (value != _CircuitID)
                {
                    _CircuitID = value;
                    OnPropertyChanged(nameof(CircuitID));
                }
            }
        }

        string _CircuitNumber;
        public string CircuitNumber
        {
            get => _CircuitNumber;
            set
            {
                if (value != _CircuitNumber)
                {
                    _CircuitNumber = value;
                    OnPropertyChanged(nameof(CircuitNumber));
                }
            }
        }

        string _SourcePanelID;
        public string SourcePanelID
        {
            get => _SourcePanelID;
            set
            {
                if (value != _SourcePanelID)
                {
                    _SourcePanelID = value;
                    OnPropertyChanged(nameof(SourcePanelID));
                }
            }
        }
    }
}
