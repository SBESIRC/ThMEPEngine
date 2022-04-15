using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using ThControlLibraryWPF;
using ThControlLibraryWPF.ControlUtils;
using ThMEPHVAC.EQPMFanModelEnums;
using ThMEPHVAC.EQPMFanSelect;
using TianHua.Hvac.UI.EQPMFanSelect;

namespace TianHua.Hvac.UI.ViewModels
{
    class FanModelSelectViewModel : NotifyPropertyChangedBase
    {
        public FanModelPicker BaseModelPick { get; }
        public CalcFanModel MainCalcFan { get; }
        public CalcFanModel ChildCalcFan { get; }
        public EnumFanControl FanControlType { get; }
        public EnumFanPowerType FanPowerType { get; }
        public EnumFanModelType FanModelType { get; }
        public FanDataModel MainFanModel { get; }
        public FanDataModel ChildFanModel { get; }
        bool HaveChildFan = false;
        public FanModelSelectViewModel(FanDataModel mainFanModel, FanDataModel childFanModel, FanModelPicker mainPicker, List<FanModelPicker> canUseFanModelPickers)
        {
            MainCalcFan = ModelCloneUtil.Copy(mainFanModel.FanModelTypeCalcModel);
            BaseModelPick = mainPicker;
            if (childFanModel != null)
            {
                HaveChildFan = true;
                ChildCalcFan = ModelCloneUtil.Copy(childFanModel.FanModelTypeCalcModel);
            }
            MainFanModel = mainFanModel;
            ChildFanModel = childFanModel;
            FanControlType = mainFanModel.Control;
            FanPowerType = mainFanModel.FanPowerType;
            FanModelType = mainFanModel.VentStyle;
            AllCCCFTypeItmes = new ObservableCollection<UListItemData>();
            IsAxisFan = FanModelType == EnumFanModelType.AxialFlow;
            int i = 0;
            foreach (var item in canUseFanModelPickers)
            {
                AllCCCFTypeItmes.Add(new UListItemData(item.Model, i, item));
                i += 1;
            }
            CCCFSelectItem = AllCCCFTypeItmes.Where(c => mainFanModel.FanModelCCCF == c.Name).FirstOrDefault();

            ShowVolume = "";
            if (FanControlType == EnumFanControl.TwoSpeed)
            {
                ShowVolume = string.Format("{0}/{1}", MainCalcFan.FanAirVolume, ChildCalcFan.FanAirVolume);
                ShowPa = string.Format("{0}/{1}", MainCalcFan.FanModelPa, ChildCalcFan.FanModelPa);
            }
            else
            {
                ShowVolume = string.Format("{0}", MainCalcFan.FanAirVolume);
                ShowPa = string.Format("{0}", MainCalcFan.FanModelPa);
            }
        }
        bool isAxisFan { get; set; }
        public bool IsAxisFan
        {
            get { return isAxisFan; }
            set
            {
                isAxisFan = value;
                this.RaisePropertyChanged();
            }
        }
        string shoVolume { get; set; }
        public string ShowVolume
        {
            get { return shoVolume; }
            set
            {
                shoVolume = value;
                this.RaisePropertyChanged();
            }
        }
        public string ShowPa { get; set; }
        private ObservableCollection<UListItemData> _allCCCFTypeItems { get; set; }
        public ObservableCollection<UListItemData> AllCCCFTypeItmes
        {
            get
            {
                return _allCCCFTypeItems;
            }
            set
            {
                _allCCCFTypeItems = value;
                this.RaisePropertyChanged();
            }
        }
        private UListItemData _selectCCCFItem { get; set; }
        public UListItemData CCCFSelectItem
        {
            get
            {
                return _selectCCCFItem;
            }
            set
            {
                _selectCCCFItem = value;
                SelectCCCFChanged();
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 数据来源
        /// </summary>
        public EnumValueSource ValueSource
        {
            get { return MainCalcFan.ValueSource; }
            set
            {
                MainCalcFan.ValueSource = value;
                if (HaveChildFan)
                    ChildCalcFan.ValueSource = value;
                this.RaisePropertyChanged();
            }
        }
        private SolidColorBrush color { get; set; }
        public SolidColorBrush MsgLabelForegroundColor 
        {
            get 
            {
                return color;
            }
            set 
            {
                color = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 风机型号表的ID
        /// </summary>
        public string FanModelID
        {
            get { return MainCalcFan.FanModelID; }
            set 
            {
                MainCalcFan.FanModelID = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 风机型号表的名称
        /// </summary>
        public string FanModelName
        {
            get { return MainCalcFan.FanModelName; }
            set
            {
                MainCalcFan.FanModelName = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 型号
        /// </summary>
        public string FanModelNum
        {
            get { return MainCalcFan.FanModelNum; }
            set
            {
                MainCalcFan.FanModelNum = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// CCCF规格
        /// </summary>
        public string FanModelCCCF
        {
            get { return MainCalcFan.FanModelCCCF; }
            set
            {
                MainCalcFan.FanModelCCCF = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 电机功率
        /// </summary>
        public string FanModelMotorHeightPower
        {
            get { return MainCalcFan.FanModelMotorPower; }
            set
            {
                MainCalcFan.FanModelMotorPower = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 电机功率
        /// </summary>
        public string FanModelMotorLowPower
        {
            get 
            {
                if (!HaveChildFan)
                    return "";
                return ChildCalcFan.FanModelMotorPower; 
            }
            set
            {
                if (HaveChildFan)
                  ChildCalcFan.FanModelMotorPower = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 电机功率 输入
        /// </summary>
        public string FanModelInputMotorHeightPower
        {
            get { return MainCalcFan.FanModelInputMotorPower; }
            set
            {
                MainCalcFan.FanModelInputMotorPower = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 电机功率 输入
        /// </summary>
        public string FanModelInputMotorLowPower
        {
            get 
            {
                if (!HaveChildFan)
                    return "";
                return ChildCalcFan.FanModelInputMotorPower;
            }
            set
            {
                if (HaveChildFan)
                    ChildCalcFan.FanModelInputMotorPower = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 噪音
        /// </summary>
        public string FanModelNoise
        {
            get { return MainCalcFan.FanModelNoise; }
            set
            {
                MainCalcFan.FanModelNoise = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 风机转速
        /// </summary>
        public string FanModelFanSpeed
        {
            get { return MainCalcFan.FanModelFanSpeed; }
            set
            {
                MainCalcFan.FanModelFanSpeed = value;
                this.RaisePropertyChanged();
            }
        }
        string showPower { get; set; }
        public string ShowFanModePower 
        {
            get 
            {
                return showPower;
            }
            set 
            {
                showPower = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 单位功耗
        /// </summary>
        public string FanModelHeightPower
        {
            get { return MainCalcFan.FanModelPower; }
            set
            {
                MainCalcFan.FanModelPower = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 单位功耗
        /// </summary>
        public string FanModelLowPower
        {
            get 
            {
                if (!HaveChildFan)
                    return "";
                return ChildCalcFan.FanModelPower; 
            }
            set
            {
                if(HaveChildFan)
                    ChildCalcFan.FanModelPower = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 长
        /// </summary>
        public string FanModelLength
        {
            get { return MainCalcFan.FanModelLength; }
            set
            {
                MainCalcFan.FanModelLength = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 宽
        /// </summary>
        public string FanModelWidth
        {
            get { return MainCalcFan.FanModelWidth; }
            set
            {
                MainCalcFan.FanModelWidth = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 高
        /// </summary>
        public string FanModelHeight
        {
            get { return MainCalcFan.FanModelHeight; }
            set
            {
                MainCalcFan.FanModelHeight = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 重量
        /// </summary>
        public string FanModelWeight
        {
            get { return MainCalcFan.FanModelWeight; }
            set
            {
                MainCalcFan.FanModelWeight = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 直径
        /// </summary>
        public string FanModelDIA
        {
            get { return MainCalcFan.FanModelDIA; }
            set
            {
                MainCalcFan.FanModelDIA = value;
                this.RaisePropertyChanged();
            }
        }
        private string msg { get; set; }
        public string ErrorMsg 
        {
            get { return msg; }
            set 
            {
                msg = value;
                this.RaisePropertyChanged();
            }
        }

        void SelectCCCFChanged() 
        {
            //只影响尺寸数据，不影响其它数据
            FanModelNum = "";
            FanModelFanSpeed = "";
            FanModelNoise = "";
            FanModelHeightPower = "-";
            FanModelLowPower = "-";
            FanModelLength = "";
            FanModelWidth = "";
            FanModelHeight = "";
            FanModelWeight = "";
            FanModelMotorHeightPower = "-";
            if (CCCFSelectItem == null)
                return;
            ChangeMainFanCalcModel();
            if (HaveChildFan) 
            {
                ChangeChildFanCalcModel();
            }
            MainCalcFan.FanModelCCCF = BaseModelPick.Model;
            EQPMFanDataService.Instance.CalcFanEfficiency(MainCalcFan, MainFanModel, ChildFanModel);
            RaisePropertyChanged("FanModelHeightPower");
            RaisePropertyChanged("FanModelLowPower");
            RaisePropertyChanged("FanModelMotorHeightPower");
            RaisePropertyChanged("FanModelMotorLowPower");
            ErrorMsg = FanModelSelectCheck.GetFanDataErrorMsg(MainFanModel);
            MsgLabelForegroundColor = FanModelSelectCheck.GetErrorTextColor(MainFanModel.FanSelectionStateMsg.FanSelectionState);
            MainCalcFan.FanModelCCCF = CCCFSelectItem.Name.ToString();
            MainCalcFan.FanModelName = CCCFSelectItem.Name.ToString();
        }

        void ChangeMainFanCalcModel() 
        {
            var pick = (FanModelPicker)CCCFSelectItem.Tag;
            EQPMFanDataUtils.SetFanModelParameter(MainFanModel,BaseModelPick, pick);
            FanModelNum = MainFanModel.FanModelTypeCalcModel.FanModelNum;
            FanModelFanSpeed = MainFanModel.FanModelTypeCalcModel.FanModelFanSpeed;
            FanModelNoise = MainFanModel.FanModelTypeCalcModel.FanModelNoise;
            FanModelHeightPower = MainFanModel.FanModelTypeCalcModel.FanModelPower;
            FanModelLength = MainFanModel.FanModelTypeCalcModel.FanModelLength;
            FanModelWidth = MainFanModel.FanModelTypeCalcModel.FanModelWidth;
            FanModelHeight = MainFanModel.FanModelTypeCalcModel.FanModelHeight;
            FanModelWeight = MainFanModel.FanModelTypeCalcModel.FanModelWeight;
            FanModelMotorHeightPower = MainFanModel.FanModelTypeCalcModel.FanModelMotorPower;
        }
        void ChangeChildFanCalcModel() 
        {
            var pick = (FanModelPicker)CCCFSelectItem.Tag;
            var points = new List<double>() { ChildFanModel.AirVolume, ChildFanModel.WindResis, 0 };
            var childPickers = EQPMFanDataService.Instance.GetFanCanUseFanModels(ChildFanModel, points, MainCalcFan.FanModelName);
            FanModelMotorLowPower = "-";
            if (childPickers.Count > 0)
            {
                var usePick = childPickers.Where(c => c.Model == pick.Model).FirstOrDefault();
                EQPMFanDataUtils.SetFanModelParameter(ChildFanModel, BaseModelPick, usePick);
                FanModelMotorLowPower = ChildFanModel.FanModelTypeCalcModel.FanModelMotorPower;
                FanModelLowPower = ChildFanModel.FanModelTypeCalcModel.FanModelPower;
            }
        }
    }
}
