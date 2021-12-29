using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using ThMEPHVAC.IndoorFanModels;
using TianHua.Hvac.UI.IndoorFanModels;
using TianHua.Hvac.UI.UI.IndoorFan;

namespace TianHua.Hvac.UI.ViewModels
{
    class IndoorFanViewModel : NotifyPropertyChangedBase
    {
        public string RadioGroupId { get; }
        private IndoorFanLayoutModel indoorFanModel { get; set; }
        public List<string> DefaultFileIds { get; }
        public List<string> DefaultWorkingIds { get; }
        public IndoorFanViewModel()
        {
            this.DefaultFileIds = new List<string>();
            this.DefaultWorkingIds = new List<string>();
            this.RadioGroupId = Guid.NewGuid().ToString();
            indoorFanModel = new IndoorFanLayoutModel();
            indoorFanModel.CreateBlastPipe = true;
            indoorFanModel.HotColdType = EnumHotColdType.Cold;
            indoorFanModel.FanType = EnumFanType.FanCoilUnitTwoControls;
            indoorFanModel.AirReturnType = EnumAirReturnType.AirReturnPipe;
            indoorFanModel.MaxFanTypeIsAuto = EnumMaxFanNumber.Auto;
            indoorFanModel.CorrectionFactor = 1;
            _indoorFanFiles = new ObservableCollection<IndoorFanFile>();
            _fanTypeWorkingCodition = new ObservableCollection<WoringConditonBase>();
            _fanInfos = new ObservableCollection<IndoorFanBase>();
            _tabItemWorkingCodition = new ObservableCollection<TabRadioItem>();
            _maxFanInfos = new ObservableCollection<IndoorFanBase>();
        }

        private ObservableCollection<IndoorFanFile> _indoorFanFiles;
        public ObservableCollection<IndoorFanFile> IndoorFanFiles 
        {
            get { return _indoorFanFiles; }
            set 
            {
                _indoorFanFiles = value;
                this.RaisePropertyChanged();
            }
        }
        private IndoorFanFile _indoorFanFile { get; set; }
        public IndoorFanFile SelectInfoFanFile 
        {
            get { return _indoorFanFile; }
            set 
            {
                _indoorFanFile = value;
                SaveButtonCanUse = _indoorFanFile != null ? !_indoorFanFile.IsDefult : true;
                this.RaisePropertyChanged();
                SelectFanFileChange();
            }
        }
        public IndoorFanLayoutModel FanLayoutModel 
        {
            get { return indoorFanModel; }
        }
        private bool selectFileIsDefault { get; set; }
        public bool SaveButtonCanUse
        {
            get
            {
                return selectFileIsDefault;
            }
            set 
            {
                selectFileIsDefault = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 选中的风机类型
        /// </summary>
        public EnumFanType SelectFanType
        {
            get { return indoorFanModel.FanType; }
            set
            {
                indoorFanModel.FanType = value;
                CanLayer = indoorFanModel.FanType != EnumFanType.IntegratedAirConditionin;
                this.RaisePropertyChanged();
                RaisePropertyChanged("ShowFanType");
                SelectFanTypeChange();
            }
        }
        public EnumShowFanType ShowFanType 
        {
            get 
            {
                var showFanType = EnumShowFanType.FanCoilUnit;
                switch (indoorFanModel.FanType) 
                {
                    case EnumFanType.FanCoilUnitFourControls:
                    case EnumFanType.FanCoilUnitTwoControls:
                        showFanType = EnumShowFanType.FanCoilUnit;
                        break;
                    case EnumFanType.VRFConditioninConduit:
                    case EnumFanType.VRFConditioninFourSides:
                        showFanType = EnumShowFanType.VRFConditionin;
                        break;
                    case EnumFanType.IntegratedAirConditionin:
                        showFanType = EnumShowFanType.IntegratedAirConditionin;
                        break;
                }
                return showFanType;
            }
        }
        /// <summary>
        /// 风机对应的工况信息
        /// </summary>

        private ObservableCollection<TabRadioItem> _tabItemWorkingCodition;
        public ObservableCollection<TabRadioItem> TabItemWorkingCoditions
        {
            get { return _tabItemWorkingCodition; }
            set
            {
                _tabItemWorkingCodition = value;
                this.RaisePropertyChanged();
            }
        }
        private TabRadioItem _selectTabRadioItem { get; set; }
        public TabRadioItem SelectWorkingCodition 
        {
            get { return _selectTabRadioItem; }
            set 
            {
                _selectTabRadioItem = value;
                this.RaisePropertyChanged();
                SelectFanWorkingCoditionChange();
            }
        }
        private ObservableCollection<WoringConditonBase> _fanTypeWorkingCodition;
        public ObservableCollection<WoringConditonBase> FanTypeWorkingCodition
        {
            get { return _fanTypeWorkingCodition; }
            set
            {
                _fanTypeWorkingCodition = value;
                this.RaisePropertyChanged();
            }
        }
        
        private ObservableCollection<IndoorFanBase> _fanInfos;
        public ObservableCollection<IndoorFanBase> FanInfos
        {
            get { return _fanInfos; }
            set
            {
                _fanInfos = value;
                this.RaisePropertyChanged();
            }
        }
        private IndoorFanBase _selectFan { get; set; }
        public IndoorFanBase SelectIndoorFan {
            get { return _selectFan; }
            set 
            {
                _selectFan = value;
                this.RaisePropertyChanged();
            }
        }
        public EnumHotColdType HotColdType 
        {
            get { return indoorFanModel.HotColdType; }
            set 
            {
                indoorFanModel.HotColdType = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 最大机组型号是否自动
        /// </summary>
        public EnumMaxFanNumber MaxFanType
        {
            get { return indoorFanModel.MaxFanTypeIsAuto; }
            set
            {
                indoorFanModel.MaxFanTypeIsAuto = value;
                this.RaisePropertyChanged();
            }
        }
        public EnumAirReturnType AirReturnType
        {
            get { return indoorFanModel.AirReturnType; }
            set
            {
                indoorFanModel.AirReturnType = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 是否生成送风管
        /// </summary>
        public bool CreateBlastPipe
        {
            get { return indoorFanModel.CreateBlastPipe; }
            set
            {
                indoorFanModel.CreateBlastPipe = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 优先布置朝向
        /// </summary>
        public EnumFanDirction FanLayoutDirction
        {
            get { return indoorFanModel.FanDirction; }
            set
            {
                indoorFanModel.FanDirction = value;
                this.RaisePropertyChanged();
            }
        }
        public double CorrectionFactor
        {
            get { return indoorFanModel.CorrectionFactor; }
            set
            {
                indoorFanModel.CorrectionFactor = value;
                this.RaisePropertyChanged();
            }
        }
        private bool canLayer { get; set; }
        public bool CanLayer
        {
            get { return canLayer; }
            set
            {
                canLayer = value;
                this.RaisePropertyChanged();
            }
        }


        private TabRadioItem _layoutWorkingCodition { get; set; }
        public TabRadioItem LayoutSelectWorkingCodition
        {
            get { return _layoutWorkingCodition; }
            set
            {
                _layoutWorkingCodition = value;
                this.RaisePropertyChanged();
                SelectLayoutFanWorkingCoditionChange();
            }
        }
        private ObservableCollection<IndoorFanBase> _maxFanInfos;
        public ObservableCollection<IndoorFanBase> LayoutMaxFanInfos
        {
            get { return _maxFanInfos; }
            set
            {
                _maxFanInfos = value;
                this.RaisePropertyChanged();
            }
        }
        private IndoorFanBase layoutSelectMaxFan { get; set; }
        public IndoorFanBase LayoutMaxFan 
        {
            get
            { 
                return layoutSelectMaxFan; 
            }
            set 
            {
                layoutSelectMaxFan = value;
                string maxFanName = "";
                if (null != value)
                    maxFanName = layoutSelectMaxFan.FanNumber;
                indoorFanModel.MaxFanType = maxFanName;
                this.RaisePropertyChanged();
            }
        }

        private void SelectFanFileChange() 
        {
            if (_indoorFanFile == null)
                return;
            SelectFanType = EnumFanType.FanCoilUnitTwoControls;
        }
        private void SelectFanTypeChange() 
        {
            var fanWorkings = _indoorFanFile.FileFanDatas.Where(c => c.FanType == SelectFanType).ToList();
            SelectWorkingCodition = null;
            TabItemWorkingCoditions.Clear();
            foreach (var item in fanWorkings) 
            {
                var tabRadioButton = new TabRadioButton();
                tabRadioButton.CanEdit = true;
                tabRadioButton.GroupName = RadioGroupId;
                if (item.ShowWorkingData == null)
                    continue;
                tabRadioButton.CanDelete = CheckWorkingCanDel(item.ShowWorkingData.WorkingId);
                tabRadioButton.Id = item.ShowWorkingData.WorkingId;
                tabRadioButton.Content = item.ShowWorkingData.WorkingCoditionName;
                if (string.IsNullOrEmpty(tabRadioButton.Content))
                    continue;
                var tabItem = new TabRadioItem(tabRadioButton);
                TabItemWorkingCoditions.Add(tabItem);
            }

            if (TabItemWorkingCoditions.Count > 0)
                SelectWorkingCodition = TabItemWorkingCoditions.First();
            LayoutSelectWorkingCodition = TabItemWorkingCoditions.FirstOrDefault();
        }
        private void SelectFanWorkingCoditionChange() 
        {
            FanTypeWorkingCodition.Clear();
            FanInfos.Clear();
            if (_selectTabRadioItem == null)
                return;
            var fanWorkings = _indoorFanFile.FileFanDatas.Where(c => c.FanType == SelectFanType).ToList();
            foreach (var item in fanWorkings)
            {
                if (item.ShowWorkingData.WorkingId != _selectTabRadioItem.Id)
                    continue;
                foreach (var work in item.ShowWorkingData.ShowWorkingDatas)
                    FanTypeWorkingCodition.Add(work);
                foreach (var fan in item.FanAllDatas)
                    FanInfos.Add(fan);
            }
            
        }
        private void SelectLayoutFanWorkingCoditionChange()
        {
            LayoutMaxFanInfos.Clear();
            indoorFanModel.TargetFanInfo.Clear();
            if (LayoutSelectWorkingCodition == null)
                return;
            var fanWorkings = _indoorFanFile.FileFanDatas.Where(c => c.FanType == SelectFanType).ToList();
            foreach (var item in fanWorkings)
            {
                if (item.ShowWorkingData.WorkingId != _layoutWorkingCodition.Id)
                    continue;
                foreach (var fan in item.FanAllDatas)
                { 
                    LayoutMaxFanInfos.Add(fan);
                    indoorFanModel.TargetFanInfo.Add(fan);
                }
            }
            LayoutMaxFan = LayoutMaxFanInfos.LastOrDefault();
        }
        bool CheckWorkingCanDel(string workingId) 
        {
            if (DefaultWorkingIds == null || DefaultWorkingIds.Count < 1)
                return true;
            return !DefaultWorkingIds.Any(c=>c == workingId);
        }
    }
}
