using System;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPHVAC.LoadCalculation.Model
{
    [Serializable]
    public class OutdoorParameterData : NotifyPropertyChangedBase
    {
        private int selectIndex = 0;
        public int SelectIndex
        {
            get
            {
                return selectIndex;
            }
            set
            {
                selectIndex = value;
                this.RaisePropertyChanged("SummerTemperature");
                this.RaisePropertyChanged("WinterTemperature");
                this.RaisePropertyChanged("SummerTemperatureReadOnly");
                this.RaisePropertyChanged("WinterTemperatureReadOnly");
            }
        }
        public string Title { get; set; }

        private string summerTemperature = "30.8°C";
        public string SummerTemperature
        {
            get
            {
                if (selectIndex == 0)
                    return "32°C";
                else if (selectIndex == 1)
                    return "31.2°C";
                return summerTemperature;
            }
            set
            {
                if (selectIndex == 2)
                {
                    summerTemperature = value;
                }
                this.RaisePropertyChanged();
            }
        }

        private string winterTemperature = "3.7°C";
        public string WinterTemperature
        {
            get
            {
                if (selectIndex == 0)
                    return "3.7°C";
                else if (selectIndex == 1)
                    return "4.2°C";
                return winterTemperature;
            }
            set
            {
                if (selectIndex == 2)
                {
                    winterTemperature = value;
                }
                this.RaisePropertyChanged();
            }
        }

        public bool SummerTemperatureReadOnly
        {
            get
            {
                if (selectIndex != 2)
                    return true;
                else
                    return false;
            }
            set
            {
                this.RaisePropertyChanged();
            }
        }
        public bool WinterTemperatureReadOnly
        {
            get
            {
                if (selectIndex != 2)
                    return true;
                else
                    return false;
            }
            set
            {
                this.RaisePropertyChanged();
            }
        }
    }
}
