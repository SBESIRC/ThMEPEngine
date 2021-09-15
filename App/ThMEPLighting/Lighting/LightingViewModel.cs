using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPLighting.Lighting.ViewModels
{

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
    public class RadioUiValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return Equals(value, parameter);
            if (value is bool b)
            {
                return b == bool.Parse((string)parameter);
            }
            if (value is string str)
            {
                return str == (string)parameter;
            }
            if (value.GetType().IsEnum)
            {
                return value.ToString() == (string)parameter;
            }
            return Equals(value, parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                if (targetType == typeof(bool))
                {
                    return bool.Parse((string)parameter);
                }
                if (targetType == typeof(string))
                {
                    return parameter;
                }
                if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, (string)parameter);
                }
            }
            return Binding.DoNothing;
        }
    }
    public class LightingViewModel : NotifyPropertyChangedBase
    {
        string _LightingLamps = "圆形吸顶灯";
        public string LightingLamps
        {
            get => _LightingLamps;
            set
            {
                if (value != _LightingLamps)
                {
                    _LightingLamps = value;
                    OnPropertyChanged(nameof(LightingLamps));
                }
            }
        }

        double _LayoutRadiusOfNormalLightingLamps = 3000;
        public double LayoutRadiusOfNormalLightingLamps
        {
            get => _LayoutRadiusOfNormalLightingLamps;
            set
            {
                if (value != _LayoutRadiusOfNormalLightingLamps)
                {
                    _LayoutRadiusOfNormalLightingLamps = value;
                    OnPropertyChanged(nameof(LayoutRadiusOfNormalLightingLamps));
                }
            }
        }

        double _LayoutRadiusOfEmergencyLightingLamps = 6000;
        public double LayoutRadiusOfEmergencyLightingLamps
        {
            get => _LayoutRadiusOfEmergencyLightingLamps;
            set
            {
                if (value != _LayoutRadiusOfEmergencyLightingLamps)
                {
                    _LayoutRadiusOfEmergencyLightingLamps = value;
                    OnPropertyChanged(nameof(LayoutRadiusOfEmergencyLightingLamps));
                }
            }
        }

        bool _WhetherEmergencyLightingIsAlsoUsedAsOrdinaryLighting = false;
        public bool WhetherEmergencyLightingIsAlsoUsedAsOrdinaryLighting
        {
            get => _WhetherEmergencyLightingIsAlsoUsedAsOrdinaryLighting;
            set
            {
                if (value != _WhetherEmergencyLightingIsAlsoUsedAsOrdinaryLighting)
                {
                    _WhetherEmergencyLightingIsAlsoUsedAsOrdinaryLighting = value;
                    OnPropertyChanged(nameof(WhetherEmergencyLightingIsAlsoUsedAsOrdinaryLighting));
                }
            }
        }

        string _IlluminanceControl = "单排布置";
        public string IlluminanceControl
        {
            get => _IlluminanceControl;
            set
            {
                if (value != _IlluminanceControl)
                {
                    _IlluminanceControl = value;
                    OnPropertyChanged(nameof(IlluminanceControl));
                }
            }
        }

        string _InstallationMode = "线槽安装";
        public string InstallationMode
        {
            get => _InstallationMode;
            set
            {
                if (value != _InstallationMode)
                {
                    _InstallationMode = value;
                    OnPropertyChanged(nameof(InstallationMode));
                }
            }
        }

        string _LayoutMode = "按柱跨布置";
        public string LayoutMode
        {
            get => _LayoutMode;
            set
            {
                if (value != _LayoutMode)
                {
                    _LayoutMode = value;
                    OnPropertyChanged(nameof(LayoutMode));
                }
            }
        }

        string _NumberOfCircuits = "自动计算";
        public string NumberOfCircuits
        {
            get => _NumberOfCircuits;
            set
            {
                if (value != _NumberOfCircuits)
                {
                    _NumberOfCircuits = value;
                    OnPropertyChanged(nameof(NumberOfCircuits));
                }
            }
        }

        int _NumberOfCircuitsAutomaticCalculationOfNLoop = 25;
        public int NumberOfCircuitsAutomaticCalculationOfNLoop
        {
            get => _NumberOfCircuitsAutomaticCalculationOfNLoop;
            set
            {
                if (value != _NumberOfCircuitsAutomaticCalculationOfNLoop)
                {
                    _NumberOfCircuitsAutomaticCalculationOfNLoop = value;
                    OnPropertyChanged(nameof(NumberOfCircuitsAutomaticCalculationOfNLoop));
                }
            }
        }

        int _NumberOfCircuitsSpecifyTheNumberOfNPerCircuits = 4;
        public int NumberOfCircuitsSpecifyTheNumberOfNPerCircuits
        {
            get => _NumberOfCircuitsSpecifyTheNumberOfNPerCircuits;
            set
            {
                if (value != _NumberOfCircuitsSpecifyTheNumberOfNPerCircuits)
                {
                    _NumberOfCircuitsSpecifyTheNumberOfNPerCircuits = value;
                    OnPropertyChanged(nameof(NumberOfCircuitsSpecifyTheNumberOfNPerCircuits));
                }
            }
        }

        string _StartingNumber = "01";
        public string StartingNumber
        {
            get => _StartingNumber;
            set
            {
                if (value != _StartingNumber)
                {
                    _StartingNumber = value;
                    OnPropertyChanged(nameof(StartingNumber));
                }
            }
        }

        double _TrunkingWidth = 300;
        public double TrunkingWidth
        {
            get => _TrunkingWidth;
            set
            {
                if (value != _TrunkingWidth)
                {
                    _TrunkingWidth = value;
                    OnPropertyChanged(nameof(TrunkingWidth));
                }
            }
        }

        double _DoubleRowSpacing = 2700;
        public double DoubleRowSpacing
        {
            get => _DoubleRowSpacing;
            set
            {
                if (value != _DoubleRowSpacing)
                {
                    _DoubleRowSpacing = value;
                    OnPropertyChanged(nameof(DoubleRowSpacing));
                }
            }
        }

        double _LampSpacing = 2700;
        public double LampSpacing
        {
            get => _LampSpacing;
            set
            {
                if (value != _LampSpacing)
                {
                    _LampSpacing = value;
                    OnPropertyChanged(nameof(LampSpacing));
                }
            }
        }

        string _EvacuationInstructions = "优先壁装";
        public string EvacuationInstructions
        {
            get => _EvacuationInstructions;
            set
            {
                if (value != _EvacuationInstructions)
                {
                    _EvacuationInstructions = value;
                    OnPropertyChanged(nameof(EvacuationInstructions));
                }
            }
        }

        string _MarkerLampSize = "中型";
        public string MarkerLampSize
        {
            get => _MarkerLampSize;
            set
            {
                if (value != _MarkerLampSize)
                {
                    _MarkerLampSize = value;
                    OnPropertyChanged(nameof(MarkerLampSize));
                }
            }
        }

        bool _DisplayEvacuationRoute;
        public bool DisplayEvacuationRoute
        {
            get => _DisplayEvacuationRoute;
            set
            {
                if (value != _DisplayEvacuationRoute)
                {
                    _DisplayEvacuationRoute = value;
                    OnPropertyChanged(nameof(DisplayEvacuationRoute));
                }
            }
        }

        string _BasementEmergencyLighting = "疏散照明壁灯";
        public string BasementEmergencyLighting
        {
            get => _BasementEmergencyLighting;
            set
            {
                if (value != _BasementEmergencyLighting)
                {
                    _BasementEmergencyLighting = value;
                    OnPropertyChanged(nameof(BasementEmergencyLighting));
                }
            }
        }

        string _LaneLayout = "车道单侧布置";
        public string LaneLayout
        {
            get => _LaneLayout;
            set
            {
                if (value != _LaneLayout)
                {
                    _LaneLayout = value;
                    OnPropertyChanged(nameof(LaneLayout));
                }
            }
        }

        string _GlobalScale = "1:100";
        public string GlobalScale
        {
            get => _GlobalScale;
            set
            {
                if (value != _GlobalScale)
                {
                    _GlobalScale = value;
                    OnPropertyChanged(nameof(GlobalScale));
                }
            }
        }

        public class Item : NotifyPropertyChangedBase
        {
            string _text;
            public string Text
            {
                get => _text;
                set
                {
                    if (value != _text)
                    {
                        _text = value;
                        OnPropertyChanged(nameof(Text));
                    }
                }
            }
        }

        ObservableCollection<Item> _items = new ObservableCollection<Item>();
        public ObservableCollection<Item> Items => _items;
    }




}
