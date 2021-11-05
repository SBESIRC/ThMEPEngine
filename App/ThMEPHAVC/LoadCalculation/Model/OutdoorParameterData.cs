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

        private double summerTemperature = 32;
        public double SummerTemperature
        {
            get
            {
                if (selectIndex == 1)
                    return 32;
                else if (selectIndex == 2)
                    return 31.2;
                return summerTemperature;
            }
            set
            {
                if (selectIndex == 0)
                {
                    summerTemperature = value;
                }
                this.RaisePropertyChanged();
            }
        }

        private double winterTemperature = 3.7;
        public double WinterTemperature
        {
            get
            {
                if (selectIndex == 1)
                    return 3.7;
                else if (selectIndex == 2)
                    return 4.2;
                return winterTemperature;
            }
            set
            {
                if (selectIndex == 0)
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
                if (selectIndex != 0)
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
                if (selectIndex != 0)
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
