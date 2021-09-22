using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ThControlLibraryWPF.ControlUtils;
using ThMEPLighting.Common;

namespace ThMEPLighting.UI.ViewModels
{
    class ParkingLightViewModel : NotifyPropertyChangedBase
    {
        public ParkingLightViewModel()
        {
            GroupMaxCount = 25;
            ListLightDirections.Add(new UListItemData("垂直长边方向", 1));
            ListLightDirections.Add(new UListItemData("垂直短边方向", 2));
            LightDirSelect = ListLightDirections.FirstOrDefault();

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
        }
        private bool? _selectAll { get; set; }
        public bool? SelectAll
        {
            get { return _selectAll; }
            set
            {
                _selectAll = value;
                SelectAllLayerName();
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
        RelayCommand listCheckedChange;
        public ICommand ListCheckedChange
        {
            get
            {
                if (listCheckedChange == null)
                    listCheckedChange = new RelayCommand(() => UpdateSelectAllState());

                return listCheckedChange;
            }
        }
        private void SelectAllLayerName() 
        {
            if (_selectAll == null)
                return;
            foreach (var item in PickLayerNames)
                item.IsSelect = _selectAll.Value;
        }
        /// <summary>
        /// 根据当前选择的个数来更新全选框的状态
        /// </summary>
        public void UpdateSelectAllState()
        {
            if (_pickLayerNames == null)
                return;

            // 获取列表项中 IsSelected 值为 True 的个数，并通过该值来确定 IsSelectAllChecked 的值
            int count = _pickLayerNames.Where(item => item.IsSelect).Count();
            if (count == _pickLayerNames.Count)
            {
                SelectAll = true;
            }
            else if (count == 0)
            {
                SelectAll = false;
            }
            else
            {
                SelectAll = null;
            }
        }
    }
}
