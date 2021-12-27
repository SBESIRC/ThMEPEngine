using System.Linq;
using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;
using ThMEPHVAC.Service;

namespace ThMEPHVAC.Model
{
    public class ThAirPortParameter: NotifyPropertyChangedBase
    {
        public ThAirPortParameter()
        {
            SystemTypes = new ObservableCollection<string>(ThMEPHAVCDataManager.GetSystemTypes());
            AirPortTypes = new ObservableCollection<string>(ThMEPHAVCDataManager.GetAirPortTypes());
            systemType = SystemTypes.FirstOrDefault();
            airPortType = AirPortTypes.FirstOrDefault();
        }
        public ObservableCollection<string> SystemTypes { get; set; }
        public ObservableCollection<string> AirPortTypes { get; set; }

        private string systemType = "";
        public string SystemType
        {
            get
            {
                return systemType;
            }
            set
            {
                systemType = value;
                RaisePropertyChanged("SystemType");
                UpdateInitAirPortType();
            }
        }

        private void UpdateInitAirPortType()
        {
           AirPortType = ThMEPHAVCDataManager.GetInitAirportType(systemType);
        }

        private string airPortType = "";
        public string AirPortType
        {
            get
            {
                return airPortType;
            }
            set
            {
                airPortType = value;
                RaisePropertyChanged("AirPortType");
            }
        }

        private double totalAirVolume = 0.0;
        public double TotalAirVolume
        {
            get
            {
                return totalAirVolume;
            }
            set
            {
                totalAirVolume = value;
                RaisePropertyChanged("TotalAirVolume");
            }
        }

        private int airPortNum = 1;
        public int AirPortNum
        {
            get
            {
                return airPortNum;
            }
            set
            {
                airPortNum = value;
                RaisePropertyChanged("AirPortNum");
            }
        }

        private double singleAirPortAirVolume = 0.0;
        public double SingleAirPortAirVolume
        {
            get
            {
                return singleAirPortAirVolume;
            }
            set
            {
                singleAirPortAirVolume = value;
                RaisePropertyChanged("SingleAirPortAirVolume");
            }
        }

        private int length = 0;
        public int Length
        {
            get
            {
                return length;
            }
            set
            {
                length = value;
                RaisePropertyChanged("Length");
            }
        }

        private int width = 0;
        public int Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
                RaisePropertyChanged("Width");
            }
        }
    }
}
