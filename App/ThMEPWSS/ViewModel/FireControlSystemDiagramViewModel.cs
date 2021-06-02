using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.ViewModel
{
    public class ZoneSetupViewModel : NotifyPropertyChangedBase
    {
        private int _zoneID;

        public int ZoneID
        {
            get 
            { return _zoneID; }
            set 
            { 
                _zoneID = value;
                RaisePropertyChanged("ZoneID");
            }
        }

        private int _StartFloor;

        public int StartFloor
        {
            get
            { return _StartFloor; }
            set
            {
                _StartFloor = value;
                RaisePropertyChanged("StartFloor");
            }
        }

        private int _EndFloor;

        public int EndFloor
        {
            get { return _EndFloor; }
            set 
            { 
                _EndFloor = value;
                RaisePropertyChanged("EndFloor");
            }
        }


    }
    public class FireControlSystemDiagramViewModel : NotifyPropertyChangedBase
    {
        private double _FaucetFloor = 1800; //mm
        public double FaucetFloor
        {
            get
            {
                return _FaucetFloor;
            }
            set
            {
                _FaucetFloor = value;
                RaisePropertyChanged("FaucetFloor");
            }
        }

        private int _serialnumber = 1; 
        public int Serialnumber
        {
            get
            {
                return _serialnumber;
            }
            set
            {
                _serialnumber = value;
                RaisePropertyChanged("Serialnumber");
            }
        }

        private int _countsgeneral = 4;
        public int CountsGeneral
        {
            get
            {
                return _countsgeneral;
            }
            set
            {
                _countsgeneral = value;
                RaisePropertyChanged("CountsGeneral");
            }
        }

        private int _countsrefuge = 4;
        public int CountsRefuge
        {
            get
            {
                return _countsrefuge;
            }
            set
            {
                _countsrefuge = value;
                RaisePropertyChanged("CountsRefuge");
            }
        }

        private bool _isRoof =true;
        public bool IsRoof
        {
            get
            {
                return _isRoof;
            }
            set
            {
                _isRoof = value;
                RaisePropertyChanged("IsRoof");
            }
        }

        private bool _isTopLayer;
        public bool IsTopLayer
        {
            get
            {
                return _isTopLayer;
            }
            set
            {
                _isTopLayer = value;
                RaisePropertyChanged("IsTopLayer");
            }
        }

        private bool _isTower = true;
        public bool IsTower 
        { 
            get
            {
                return _isTower;
            }
            set
            {
                _isTower = value;
                RaisePropertyChanged("IsTower");
            }
        }

        private bool _isManagementBuilding;

        public bool IsManagementBuilding
        {
            get { return _isManagementBuilding; }
            set 
            { 
                _isManagementBuilding = value;
                RaisePropertyChanged("IsManagementBuilding");
            }
        }
        private ObservableCollection<ZoneSetupViewModel> _ZoneConfigs;
        public ObservableCollection<ZoneSetupViewModel> ZoneConfigs
        {
            get
            {
                return _ZoneConfigs;
            }
            set
            {
                _ZoneConfigs = value;
                RaisePropertyChanged("ZoneConfigs");
            }
        }

        public FireControlSystemDiagramViewModel()
        {
            
            ZoneConfigs = new ObservableCollection<ZoneSetupViewModel>();
            ZoneConfigs.Add(new ZoneSetupViewModel() { ZoneID = 1, StartFloor = 1 });
            ZoneConfigs.Add(new ZoneSetupViewModel() { ZoneID = 2 });
            ZoneConfigs.Add(new ZoneSetupViewModel() { ZoneID = 3 });
            ZoneConfigs.Add(new ZoneSetupViewModel() { ZoneID = 4 });
        }

    }
}
