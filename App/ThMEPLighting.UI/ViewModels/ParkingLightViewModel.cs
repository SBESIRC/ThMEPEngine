using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThControlLibraryWPF.ControlUtils;
using CommunityToolkit.Mvvm.Input;
using ThMEPLighting.Common;

namespace ThMEPLighting.UI.ViewModels
{
    class ParkingLightViewModel : NotifyPropertyChangedBase
    {
        public Parkingillumination parkingStallIllumination { get; }
        public ParkingLightViewModel()
        {
            GroupMaxCount = 25;
            ListLightDirections.Add(new UListItemData("垂直长边方向", 1));
            ListLightDirections.Add(new UListItemData("垂直短边方向", 2));
            LightDirSelect = ListLightDirections.FirstOrDefault();

            var sources = CommonUtil.EnumDescriptionToList(typeof(EnumParkingSource));
            foreach (var item in sources)
                ListParkSources.Add(item);
            ParkSourcesSelect = ListParkSources.Where(c=>c.Value == (int)EnumParkingSource.BlokcAndLayer).FirstOrDefault();

            List<int> values = new List<int>
            {
                (int)ThEnumBlockScale.DrawingScale1_100,
                (int)ThEnumBlockScale.DrawingScale1_150,
            };
            var intSacles = CommonUtil.EnumDescriptionToList(typeof(ThEnumBlockScale), values);
            foreach (var raise in intSacles)
            {
                ListScales.Add(raise);
            }
            ScaleSelect = ListScales.Where(c => c.Value == (int)ThEnumBlockScale.DrawingScale1_100).FirstOrDefault();


            parkingStallIllumination = new Parkingillumination();
            parkingStallIllumination.MastIllumination = 30;
            parkingStallIllumination.LightRatedIllumination = 1800;
            parkingStallIllumination.LightRatedPower = 18;
            parkingStallIllumination.UtilizationCoefficient = 0.8;
            parkingStallIllumination.MaintenanceFactor = 0.7;
            parkingStallIllumination.ShowResult = true;
            ListParkIlluminationSources = new ObservableCollection<UListItemData>();
            var itemIllumination = CommonUtil.EnumDescriptionToList(typeof(EnumIllumination));
            foreach (var item in itemIllumination)
            {
                ListParkIlluminationSources.Add(item);
            }
            ParkIlluminationSelect = ListParkIlluminationSources.Where(c => c.Value == (int)EnumIllumination.Illumination_30).FirstOrDefault();

            ListParkFactorSources = new ObservableCollection<UListItemData>();
            var itemUse = CommonUtil.EnumDescriptionToList(typeof(EnumMaintenanceFactor));
            foreach (var item in itemUse)
            {
                ListParkFactorSources.Add(item);
            }
            ParkFactorSelect = ListParkFactorSources.Where(c => c.Value == (int)EnumMaintenanceFactor.MaintenanceFactor_0_8).FirstOrDefault();
        }
        private bool? _selectAllLayer { get; set; }
        public bool? SelectAllLayer
        {
            get { return _selectAllLayer; }
            set
            {
                _selectAllLayer = value;
                SelectAllLayerName();
                this.RaisePropertyChanged();
            }
        }
        private bool? _selectAllBlock { get; set; }
        public bool? SelectAllBlock
        {
            get { return _selectAllBlock; }
            set
            {
                _selectAllBlock = value;
                SelectAllBlockName();
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<UListItemData> _lightDirections = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> ListLightDirections
        {
            get { return _lightDirections; }
            set { _lightDirections = value; this.RaisePropertyChanged(); }
        }
        private UListItemData _lightDirSelect { get; set; }
        public UListItemData LightDirSelect
        {
            get { return _lightDirSelect; }
            set
            {
                _lightDirSelect = value;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<UListItemData> _parkSources = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> ListParkSources
        {
            get { return _parkSources; }
            set { _parkSources = value; this.RaisePropertyChanged(); }
        }
        private UListItemData _parkSourcesSelect { get; set; }
        public UListItemData ParkSourcesSelect
        {
            get { return _parkSourcesSelect; }
            set
            {
                _parkSourcesSelect = value;
                this.RaisePropertyChanged();
            }
        }
        private int _groupMaxCount { get; set; }
        public int GroupMaxCount
        {
            get { return _groupMaxCount; }
            set { _groupMaxCount = value; this.RaisePropertyChanged(); }
        }
        private ObservableCollection<UListItemData> _scales = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> ListScales
        {
            get { return _scales; }
            set { _scales = value; this.RaisePropertyChanged(); }
        }
        private UListItemData _selectScale { get; set; }
        public UListItemData ScaleSelect
        {
            get { return _selectScale; }
            set { _selectScale = value;this.RaisePropertyChanged(); }
        }
        private ObservableCollection<MultiCheckItem> _pickLayerNames = new ObservableCollection<MultiCheckItem>();
        public ObservableCollection<MultiCheckItem> PickLayerNames 
        {
            get { return _pickLayerNames; }
            set 
            {
                _pickLayerNames = value;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<MultiCheckItem> _pickBlockNames = new ObservableCollection<MultiCheckItem>();
        public ObservableCollection<MultiCheckItem> PickBlockNames
        {
            get { return _pickBlockNames; }
            set
            {
                _pickBlockNames = value;
                this.RaisePropertyChanged();
            }
        }
        RelayCommand<string> listCheckedChange;
        public ICommand ListCheckedChange
        {
            get
            {
                if (listCheckedChange == null)
                    listCheckedChange = new RelayCommand<string>((type) => UpdateSelectAllState(type));

                return listCheckedChange;
            }
        }
        /// <summary>
        /// 根据当前选择的个数来更新全选框的状态
        /// </summary>
        public void UpdateSelectAllState(string type)
        {
            if (string.IsNullOrEmpty(type))
                return;
            var typeName = type.ToUpper();
            if (typeName.Equals("LAYER"))
            {
                UpdateSelectAllLayerState();
            }
            else if (typeName.Equals("BLOCK"))
            {
                UpdateSelectAllBlockState();
            }
        }
        private void SelectAllLayerName() 
        {
            if (_selectAllLayer == null)
                return;
            foreach (var item in PickLayerNames)
                item.IsSelect = _selectAllLayer.Value;
        }
        private void SelectAllBlockName()
        {
            if (_selectAllBlock == null)
                return;
            foreach (var item in PickBlockNames)
                item.IsSelect = _selectAllBlock.Value;
        }
        /// <summary>
        /// 根据当前选择的个数来更新全选框的状态
        /// </summary>
        public void UpdateSelectAllLayerState()
        {
            if (SelectAllLayer == null)
                return;
            // 获取列表项中 IsSelected 值为 True 的个数，并通过该值来确定 IsSelectAllChecked 的值
            int count = _pickLayerNames.Where(item => item.IsSelect).Count();
            if (count == _pickLayerNames.Count)
            {
                SelectAllLayer = true;
            }
            else if (count == 0)
            {
                SelectAllLayer = false;
            }
            else
            {
                SelectAllLayer = null;
            }
        }
        /// <summary>
        /// 根据当前选择的个数来更新全选框的状态
        /// </summary>
        public void UpdateSelectAllBlockState()
        {
            if (SelectAllBlock == null)
                return;
            // 获取列表项中 IsSelected 值为 True 的个数，并通过该值来确定 IsSelectAllChecked 的值
            int count = _pickBlockNames.Where(item => item.IsSelect).Count();
            if (count == _pickBlockNames.Count)
            {
                SelectAllBlock = true;
            }
            else if (count == 0)
            {
                SelectAllBlock = false;
            }
            else
            {
                SelectAllBlock = null;
            }
        }
        
        private ObservableCollection<UListItemData> _parkIlluminationSources = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> ListParkIlluminationSources
        {
            get { return _parkIlluminationSources; }
            set 
            { 
                _parkIlluminationSources = value; 
                this.RaisePropertyChanged(); 
            }
        }
        private UListItemData _parkIlluminationSelect { get; set; }
        public UListItemData ParkIlluminationSelect
        {
            get { return _parkIlluminationSelect; }
            set
            {
                _parkIlluminationSelect = value;
                if (null != _parkIlluminationSelect)
                {
                    var str = _parkIlluminationSelect.Name.ToString();
                    double.TryParse(str, out double dFactor);
                    parkingStallIllumination.MastIllumination = dFactor;
                }
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<UListItemData> _parkFactorSources = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> ListParkFactorSources
        {
            get { return _parkFactorSources; }
            set
            {
                _parkFactorSources = value;
                this.RaisePropertyChanged();
            }
        }
        private UListItemData _parkFactorSelect { get; set; }
        public UListItemData ParkFactorSelect
        {
            get { return _parkFactorSelect; }
            set
            {
                _parkFactorSelect = value;
                if (null != _parkFactorSelect)
                {
                    var str = _parkFactorSelect.Name.ToString();
                    double.TryParse(str, out double dFactor);
                    parkingStallIllumination.MaintenanceFactor = dFactor;
                }
                this.RaisePropertyChanged();
            }
        }
        public double LuminousFlux 
        {
            get { return parkingStallIllumination.LightRatedIllumination; }
            set 
            {
                parkingStallIllumination.LightRatedIllumination = value;
                if (value > 3500 || value < 900)
                    parkingStallIllumination.LightRatedIllumination = 1800;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 灯具额定功率（W）
        /// </summary>
        public double LightRatedPower
        {
            get { return parkingStallIllumination.LightRatedPower; }
            set 
            {
                parkingStallIllumination.LightRatedPower = value<0.1?18:value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 利用系数
        /// </summary>
        public double UtilizationCoefficient 
        {
            get { return parkingStallIllumination.UtilizationCoefficient; }
            set 
            {
                parkingStallIllumination.UtilizationCoefficient = value;
                if (value <= 0 || value >= 2)
                    parkingStallIllumination.UtilizationCoefficient = 0.8;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 显示计算结果
        /// </summary>
        public bool ShowResult 
        {
            get { return parkingStallIllumination.ShowResult; }
            set 
            {
                parkingStallIllumination.ShowResult = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
