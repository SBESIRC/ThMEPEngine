using System;
using System.Linq;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.Service;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPLighting.ViewModel
{
    public enum LightingLayoutTypeEnum
    {
        /// <summary>
        /// 照明灯具
        /// </summary>
        IlluminationLighting,
        /// <summary>
        /// 车道照明
        /// </summary>
        GarageLighting,
        /// <summary>
        /// 疏散指示
        /// </summary>
        EvacuationIndicator,
        /// <summary>
        /// 应急照明
        /// </summary>
        EmergencyLighting,
    }
    public enum LightTypeEnum
    {
        /// <summary>
        /// 圆形吸顶灯
        /// </summary>
        circleCeiling,
        /// <summary>
        /// 半球吸顶灯
        /// </summary>
        domeCeiling,
        /// <summary>
        /// 感应吸顶灯
        /// </summary>
        inductionCeiling,
        /// <summary>
        /// 筒灯
        /// </summary>
        downlight,
        //emergencyLight
    }
    public enum CableTraySpecification
    {
        [Description("50*25")]
        _50x25,
        [Description("100*50")]
        _100x50,
        [Description("150*75")]
        _150x75,
        [Description("200*100")]
        _200x100,
        [Description("300*100")]
        _300x100,
        [Description("400*100")]
        _400x100,
        [Description("500*100")]
        _500x100,
        [Description("600*100")]
        _600x100,
        [Description("800*100")]
        _800x100,
        [Description("200*150")]
        _200x150,
        [Description("300*150")]
        _300x150,
        [Description("400*150")]
        _400x150,
        [Description("500*150")]
        _500x150,
        [Description("600*150")]
        _600x150,
        [Description("800*150")]
        _800x150,
        [Description("300*200")]
        _300x200,
        [Description("400*200")]
        _400x200,
        [Description("500*200")]
        _500x200,
        [Description("600*200")]
        _600x200,
        [Description("800*200")]
        _800x200,
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
        private LightingLayoutTypeEnum _LightingLayoutType = LightingLayoutTypeEnum.IlluminationLighting;
        public LightingLayoutTypeEnum LightingLayoutType
        {
            get
            {
                return _LightingLayoutType;
            }
            set
            {
                _LightingLayoutType = value;
                OnPropertyChanged("LightingLayoutType");
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

        //板厚
        private double _RoofThickness = 100;
        public double RoofThickness
        {
            get
            {
                return _RoofThickness;
            }
            set
            {
                _RoofThickness = value;
                OnPropertyChanged("RoofThickness");
            }
        }
        //避梁距离
        private double _BufferDist = 500;
        public double BufferDist
        {
            get
            {
                return _BufferDist;
            }
            set
            {
                _BufferDist = value;
                OnPropertyChanged("BufferDist");
            }
        }
        //选择楼层或房间
        private bool _SelectFloor = true;
        public bool SelectFloor
        {
            get
            {
                return _SelectFloor;
            }
            set
            {
                _SelectFloor = value;
                OnPropertyChanged("SelectFloor");
            }
        }

        private bool _SelectRoom = false;
        public bool SelectRoom
        {
            get
            {
                return _SelectRoom;
            }
            set
            {
                _SelectRoom = value;
                OnPropertyChanged("SelectRoom");
            }
        }
        //选择地上地下
        private bool _FloorDown = false;
        public bool FloorDown
        {
            get
            {
                return _FloorDown;
            }
            set
            {
                _FloorDown = value;
                OnPropertyChanged("FloorDown");
            }
        }
        private bool _FloorUp = true;
        public bool FloorUp
        {
            get
            {
                return _FloorUp;
            }
            set
            {
                _FloorUp = value;
                OnPropertyChanged("FloorUp");
            }
        }
        #endregion
        #region garageLight
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

        string _LayoutMode = "等间距布置";
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

        string _ConnectMode = "弧线连接";
        public string ConnectMode
        {
            get => _ConnectMode;
            set
            {
                if (value != _ConnectMode)
                {
                    _ConnectMode = value;
                    OnPropertyChanged(nameof(ConnectMode));
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

        int _StartingNumber = 1;
        public int StartingNumber
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

        private string _Specification = CableTraySpecification._150x75.GetDescription();
        public string Specification
        {
            get
            {
                return _Specification;
            }
            set
            {
                _Specification = value;
                OnPropertyChanged("Specification");
            }
        }

        private ObservableCollection<string> _Specifications = new ObservableCollection<string>();
        public ObservableCollection<string> Specifications
        {
            get
            {
                return _Specifications;
            }
            set
            {
                _Specifications = value;
                OnPropertyChanged("Specifications");
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

        bool _IsTCHCableTray = false;
        public bool IsTCHCableTray
        {
            get => _IsTCHCableTray;
            set
            {
                _IsTCHCableTray = value;
                OnPropertyChanged("IsTCHCableTray");
            }
        }

        bool _IsDoubleRow = false;
        public bool IsDoubleRow
        {
            get => _IsDoubleRow;
            set
            {
                _IsDoubleRow = value;
                OnPropertyChanged("IsDoubleRow");
            }
        }

        #endregion
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
            ShouldConsiderBeam = true;
            LightingType = LightTypeEnum.circleCeiling;
            LightingLayoutType = LightingLayoutTypeEnum.IlluminationLighting;

            Specification = CableTraySpecification._150x75.GetDescription();
            foreach (CableTraySpecification suit in Enum.GetValues(typeof(CableTraySpecification)))
            {
                Specifications.Add(suit.GetDescription());
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

            bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    if (value != _isSelected)
                    {
                        _isSelected = value;
                        OnPropertyChanged(nameof(IsSelected));
                    }
                }
            }
        }
        private ObservableCollection<Item> _items = new ObservableCollection<Item>();
        public ObservableCollection<Item> Items => _items;
        public void Add(string text)
        {
            if (_items.Where(o => o.Text == text).Any() == false)
            {
                _items.Add(new Item() { Text = text, IsSelected = true });
            }
        }
        private List<string> LaneLineLayers
        {
            get
            {
                return _items.Where(o => o.IsSelected).Select(o => o.Text).ToList();
            }
        }
        public void ExtractTCD()
        {
            ThMEPLightingService.Instance.LanelineLayers = LaneLineLayers;
            CommandHandlerBase.ExecuteFromCommandLine(false, "THTCDX");
        }
        public void UpdateLaneLineLayers()
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                AddAdSignLayers();
                var removeLayerTexts = _items.Select(o => o.Text).Where(o => !acadDb.Layers.Contains(o)).ToList();
                removeLayerTexts.ForEach(o => Remove(o));
            }
        }

        private void Remove(string layer)
        {
            var querys = _items.Where(o => o.Text == layer).Select(o => o.Text).ToList();
            foreach (string text in querys)
            {
                var item = _items.Where(o => o.Text == text).First();
                _items.Remove(item);
            }
        }

        public void AddAdSignLayers()
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                ThLaneLineLayerManager.GeometryXrefLayers(acadDb.Database).ForEach(o => Add(o));
            }
        }
    }
}
