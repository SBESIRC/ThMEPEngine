using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPLighting.Lighting.ViewModels
{
    public enum RadioLightOptions { L1, L2, L3, L4, L5}
    public enum RadioDualUseOptions { Yes, No }
    public class EnumBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? parameter : Binding.DoNothing;
        }
    }
    public class LightingViewModel : NotifyPropertyChangedBase
    {
        private RadioLightOptions _LightOption = RadioLightOptions.L1;
        public RadioLightOptions LightOption
        {
            get
            {
                return _LightOption;
            }
            set
            {
                _LightOption = value;
                OnPropertyChanged("LightOption");
            }
        }

        private double _Radius = 1000; //mm

        public double LightingRadius
        {
            get { return _Radius; }
            set 
            {
                _Radius = value;
                OnPropertyChanged("LightingRadius");
            }
        }

        private double _EvacuationRadius = 1000; //mm

        public double EvacuationRadius
        {
            get { return _EvacuationRadius; }
            set 
            {
                _EvacuationRadius = value;
                OnPropertyChanged("EvacuationRadius");
            }
        }

        private RadioDualUseOptions _DualUseOption =  RadioDualUseOptions.No;

        public RadioDualUseOptions DualUseOption
        {
            get { return _DualUseOption; }
            set
            {
                _DualUseOption = value;
                OnPropertyChanged("DualUseOption");
            }
        }

        //块参照比例index
        private int _BlockRatioIndex = 0;
        public int BlockRatioIndex
        {
            get
            {
                return _BlockRatioIndex;
            }
            set
            {
                _BlockRatioIndex = value;
                OnPropertyChanged("BlockRatioIndex");
            }
        }

        //块参照比例string
        private string _BlockRatio = string.Empty;
        public string BlockRatio
        {
            get
            {
                return _BlockRatio;
            }
            set
            {
                _BlockRatio = value;
                OnPropertyChanged("BlockRatio");
            }
        }
    }
}
