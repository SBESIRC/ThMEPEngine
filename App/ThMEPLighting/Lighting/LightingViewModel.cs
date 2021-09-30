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
    public enum LightTypeEnum
    {
        circleCeiling,
        domeCeiling,
        inductionCeiling,
        downlight,
        //emergencyLight
    }
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
        #region illuminationLight
        bool _IsIlluminationLightChecked = false;
        public bool IsIlluminationLightChecked
        {
            get { return _IsIlluminationLightChecked; }
            set
            {
                _IsIlluminationLightChecked = value;
                OnPropertyChanged("IsIlluminationLightChecked");
            }
        }

        //照明灯类型
        private LightTypeEnum _LightingType = LightTypeEnum.circleCeiling;
        public LightTypeEnum LightingType
        {
            get
            {
                return _LightingType;
            }
            set
            {
                _LightingType = value;
                OnPropertyChanged("LightingType");
            }
        }

        //是否做应急照明
        private bool _IfLayoutEmgChecked = false;
        public bool IfLayoutEmgChecked
        {
            get { return _IfLayoutEmgChecked; }
            set
            {
                _IfLayoutEmgChecked = value;
                OnPropertyChanged("IfLayoutEmgChecked");
            }

        }
        //照明灯半径
        double _RadiusNormal = 3000;
        public double RadiusNormal
        {
            get { return _RadiusNormal; }
            set
            {
                _RadiusNormal = value;
                OnPropertyChanged("RadiusNormal");
            }
        }

        //照明灯半径
        double _RadiusEmg = 6000;
        public double RadiusEmg
        {
            get { return _RadiusEmg; }
            set
            {
                _RadiusEmg = value;
                OnPropertyChanged("RadiusEmg");
            }
        }
        //照明灯兼用
        bool _IfEmgUsedForNormal = false;
        public bool IfEmgUsedForNormal
        {
            get => _IfEmgUsedForNormal;
            set
            {
                _IfEmgUsedForNormal = value;
                OnPropertyChanged("IfEmgUsedForNormal");
            }
        }

        //是否考虑梁
        private bool _ShouldConsiderBeam = true;
        public bool ShouldConsiderBeam
        {
            get
            {
                return _ShouldConsiderBeam;
            }
            set
            {
                _ShouldConsiderBeam = value;
                OnPropertyChanged("ShouldConsiderBeam");
            }
        }
        //需要加这个值。否则存不住结果
        private bool _NotShouldConsiderBeam = false;
        public bool NotShouldConsiderBeam
        {
            get
            {
                return _NotShouldConsiderBeam;
            }
            set
            {
                _NotShouldConsiderBeam = value;
                OnPropertyChanged("NotShouldConsiderBeam");
            }
        }
        #endregion 

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

        //块参照比例index
        private int _ScaleSelectIndex = 0;
        public int ScaleSelectIndex
        {
            get
            {
                return _ScaleSelectIndex;
            }
            set
            {
                _ScaleSelectIndex = value;
                OnPropertyChanged("ScaleSelectIndex");
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
        public LightingViewModel()
        {
            ScaleSelectIndex = 0;
            LightingType = LightTypeEnum.circleCeiling;
            ShouldConsiderBeam = true;
            IsIlluminationLightChecked = true;
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
