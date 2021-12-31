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
                UpdateAirportSize();
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
                CalculateSingleAirPortAirVolume();
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
                CalculateSingleAirPortAirVolume();
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
                UpdateAirSpeed();
                UpdateAirportSize();
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
                UpdateAirSpeed();
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
                UpdateAirSpeed();
            }
        }
        private string airSpeed = "0.0m/s";
        public string AirSpeed
        {
            get
            {
                return airSpeed;
            }
            set
            {
                airSpeed = value;
                RaisePropertyChanged("AirSpeed");
            }
        }

        private void UpdateAirSpeed()
        {
            AirSpeed = CalculateAirSpeed();
        }

        private void UpdateAirportSize()
        {
            var size = ThMEPHAVCDataManager.CalculateAirPortSize(
                 SingleAirPortAirVolume, AirPortType);
            if (size != null)
            {
                Length = size.Item1;
                Width = size.Item2;
            }
        }

        private string CalculateAirSpeed()
        {
            double airSpeed = 0.0;
            switch (airPortType)
            {
                case "下送风口":
                case "下回风口":
                case "侧送风口":
                case "侧回风口":
                case "方形散流器":
                    if(Length>0.0 && Width>0.0 && SingleAirPortAirVolume>0.0)
                    {
                        airSpeed = ThAirSpeedCalculator.RecAirPortSpeed(SingleAirPortAirVolume, Length, Width);
                    }
                    break;
                case "圆形风口":
                    if(Length > 0.0 && SingleAirPortAirVolume > 0.0)
                    {
                        airSpeed = ThAirSpeedCalculator.CircleAirPortSpeed(SingleAirPortAirVolume, Length);
                    }
                    break;
            }
            return airSpeed.ToString("0.0")+"m/s";
        }
        private void CalculateSingleAirPortAirVolume()
        {
            if (AirPortNum != 0)
            {
                SingleAirPortAirVolume = TotalAirVolume / AirPortNum;
            }
        }
    }
}
