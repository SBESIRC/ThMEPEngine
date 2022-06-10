using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ThControlLibraryWPF.ControlUtils;
using ThMEPHVAC.EQPMFanModelEnums;
using ThMEPHVAC.EQPMFanSelect;
using TianHua.Hvac.UI.EQPMFanSelect;

namespace TianHua.Hvac.UI.ViewModels
{
    class FanDataViewModel : NotifyPropertyChangedBase
    {
        public FanDataModel fanDataModel { get; }
        public FanDataViewModel(FanDataModel fanData)
        {
            fanDataModel = fanData;
        }
        public string ScenarioString
        {
            get
            {
                var str = CommonUtil.GetEnumDescription(fanDataModel.Scenario);
                return str;
            }
        }
        UListItemData _fanEnergyItem;
        public UListItemData FanEnergyItem
        {
            get { return _fanEnergyItem; }
            set
            {
                _fanEnergyItem = value;
                if (null != value && value.Value > -1)
                    fanDataModel.VentLev = (EnumFanEnergyConsumption)value.Value;
                this.RaisePropertyChanged();
            }
        }
        private UListItemData _motorEnergyItem { get; set; }
        public UListItemData MotorEnergyItem
        {
            get { return _motorEnergyItem; }
            set
            {
                _motorEnergyItem = value;
                if (null != value && value.Value > -1)
                    fanDataModel.EleLev = (EnumFanEnergyConsumption)value.Value;
                this.RaisePropertyChanged();
            }
        }

        private UListItemData _fanTypeItem { get; set; }
        public UListItemData FanTypeItem
        {
            get { return _fanTypeItem; }
            set
            {
                _fanTypeItem = value;
                if (null != value && value.Value > -1)
                    fanDataModel.VentStyle = (EnumFanModelType)value.Value;
                FanTypeChanged();
                InputChangeRefreshModel();
                this.RaisePropertyChanged();
            }
        }

        #region 气流方向相关
        private ObservableCollection<UListItemData> _airflowDirectionItems { get; set; }
        public ObservableCollection<UListItemData> AirflowDirectionItems
        {
            get
            {
                return _airflowDirectionItems;
            }
            set
            {
                _airflowDirectionItems = value;
                this.RaisePropertyChanged();
            }
        }
        private UListItemData _airflowDirectionItem { get; set; }

        public UListItemData AirflowDirectionItem
        {
            get { return _airflowDirectionItem; }
            set
            {
                _airflowDirectionItem = value;
                if (null != value && value.Value > -1)
                    fanDataModel.IntakeForm = (EnumFanAirflowDirection)value.Value;
                this.RaisePropertyChanged();
            }
        }
        #endregion

        private UListItemData _fanControlItem { get; set; }
        public UListItemData FanControlItem
        {
            get { return _fanControlItem; }
            set
            {
                _fanControlItem = value;
                if (null != value && value.Value > -1)
                    fanDataModel.Control = (EnumFanControl)value.Value;
                FanControlTypeChanged();
                InputChangeRefreshModel();
                this.RaisePropertyChanged();
            }
        }
        private UListItemData _fanMountTypeItem { get; set; }
        public UListItemData FanMountTypeItem
        {
            get { return _fanMountTypeItem; }
            set
            {
                _fanMountTypeItem = value;
                if (null != value && value.Value > -1)
                    fanDataModel.MountType = (EnumMountingType)value.Value;
                this.RaisePropertyChanged();
            }
        }

        private UListItemData _vibrationModeItem { get; set; }
        public UListItemData VibrationModeItem
        {
            get { return _vibrationModeItem; }
            set
            {
                _vibrationModeItem = value;
                if (null != value)
                    fanDataModel.VibrationMode = (EnumDampingType)value.Value;
                this.RaisePropertyChanged();
            }
        }


        private ObservableCollection<UListItemData> _fanTypeItems { get; set; }
        public ObservableCollection<UListItemData> FanTypeItems
        {
            get
            {
                return _fanTypeItems;
            }
            set
            {
                _fanTypeItems = value;
                this.RaisePropertyChanged();
            }
        }


        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return fanDataModel.Name; }
            set
            {
                fanDataModel.Name = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 子项
        /// </summary>
        public string InstallSpace
        {
            get { return fanDataModel.InstallSpace; }
            set
            {
                fanDataModel.InstallSpace = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 风机安装楼层
        /// </summary>
        public string InstallFloor
        {
            get { return fanDataModel.InstallFloor; }
            set
            {
                fanDataModel.InstallFloor = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 风机序号
        /// </summary>
        public string VentNum
        {
            get { return fanDataModel.VentNum; }
            set
            {
                fanDataModel.VentNum = value;
                CalcFanQuan();
                this.RaisePropertyChanged();
            }
        }
        public string ServiceArea
        {
            get { return fanDataModel.ServiceArea; }
            set 
            {
                fanDataModel.ServiceArea = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 风机编号
        /// </summary>
        public string FanModelCCCF
        {
            get { return fanDataModel.FanModelCCCF; }
            set
            {
                fanDataModel.FanModelCCCF = value;
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 风量
        /// </summary>
        public int AirVolume
        {
            get { return fanDataModel.AirVolume; }
            set
            {
                fanDataModel.AirVolume = value;
                InputChangeRefreshModel();
                this.RaisePropertyChanged();
            }
        }
        /// <summary>
        /// 风阻：正整数
        /// </summary>
        public int WindResis
        {
            get { return fanDataModel.WindResis; }
            set
            {
                fanDataModel.WindResis = value;
                InputChangeRefreshModel();
                this.RaisePropertyChanged();
            }
        }
        public bool IsChildFan
        {
            get { return fanDataModel.IsChildFan; }
            set
            {
                fanDataModel.IsChildFan = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsRepetitions
        {
            get { return fanDataModel.IsRepetitions; }
            set
            {
                fanDataModel.IsRepetitions = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsSelectFanError
        {
            get { return fanDataModel.IsSelectFanError; }
            set
            {
                fanDataModel.IsSelectFanError = value;
                this.RaisePropertyChanged();
            }
        }
        void FanControlTypeChanged()
        {
            FanTypeItems = new ObservableCollection<UListItemData>();
            if (FanControlItem == null)
                return;
            var enumValues = CommonUtil.EnumDescriptionToList(typeof(EnumFanModelType));
            enumValues.ForEach(c => FanTypeItems.Add(c));
            FanTypeItem = FanTypeItems.FirstOrDefault();
        }
        void FanTypeChanged()
        {
            if (fanDataModel.IsChildFan)
                return;
            AirflowDirectionItems = new ObservableCollection<UListItemData>();
            switch (fanDataModel.VentStyle)
            {
                case EnumFanModelType.AxialFlow:
                    var enumAxialValues = CommonUtil.EnumDescriptionToList(typeof(EnumFanAirflowDirection), new List<int> { (int)EnumFanAirflowDirection.StraightInAndStraightOut });
                    enumAxialValues.ForEach(c => AirflowDirectionItems.Add(c));
                    break;
                default:
                    var enumValues = CommonUtil.EnumDescriptionToList(typeof(EnumFanAirflowDirection));
                    enumValues.ForEach(c => AirflowDirectionItems.Add(c));
                    break;
            }
            AirflowDirectionItem = AirflowDirectionItems.FirstOrDefault();
        }
        private FanModelPicker selectPicker { get; set; }
        public FanModelPicker SelectModelPicker
        {
            get { return selectPicker; }
            set
            {
                selectPicker = value;
                RefreshFanModelByPicker();
            }
        }
        public FanModelPicker BaseModelPicker { get; set; }
        public List<FanModelPicker> CanUseFanModelPickers { get; set; }
        public void RefreshFanModel(FanDataModel pModel,bool isRead=false)
        {
            if (fanDataModel == null)
                return;
            if (!IsChildFan && (FanTypeItem == null || AirflowDirectionItem == null))
                return;
            if (pModel != null)
            {
                fanDataModel.VentStyle = pModel.VentStyle;
                fanDataModel.Control = EnumFanControl.TwoSpeed;
                //fanDataModel.MountType = pModel.
            }
            CanUseFanModelPickers = new List<FanModelPicker>();
            fanDataModel.FanSelectionStateMsg = new FanSelectionStateInfo();
            fanDataModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighNotFound;
            IsSelectFanError = fanDataModel.FanSelectionStateMsg.FanSelectionState != EnumFanSelectionState.HighAndLowBothSafe;

            fanDataModel.FanModelTypeCalcModel.FanAirVolume = fanDataModel.AirVolume.ToString();
            fanDataModel.FanModelTypeCalcModel.FanModelPa = fanDataModel.WindResis.ToString();

            var points = new List<double>() { fanDataModel.AirVolume, fanDataModel.WindResis, 0 };
            string ccfName = "";
            if (pModel != null)
            {
                ccfName = pModel.FanModelCCCF;
            }
            else if (!string.IsNullOrEmpty(fanDataModel.FanModelCCCF) && !fanDataModel.FanModelCCCF.Contains("未知")) 
            {
                ccfName = fanDataModel.FanModelCCCF;
            }
            CanUseFanModelPickers = EQPMFanDataService.Instance.GetFanCanUseFanModels(fanDataModel, points, "");
            BaseModelPicker = CanUseFanModelPickers.FirstOrDefault();
            CanUseFanModelPickers = EQPMFanDataService.Instance.GetFanCanUseFanModels(fanDataModel, points, ccfName);
            if(null == CanUseFanModelPickers || CanUseFanModelPickers.Count<1)
                CanUseFanModelPickers = EQPMFanDataService.Instance.GetFanCanUseFanModels(fanDataModel, points, "");
            if (CanUseFanModelPickers != null && CanUseFanModelPickers.Count > 0)
            {
                if (null == BaseModelPicker)
                    BaseModelPicker = CanUseFanModelPickers.First();
                //SelectModelPicker = CanUseFanModelPickers.First();
                var tempCanUsePickers = new List<FanModelPicker>();
                switch (fanDataModel.VentStyle)
                {
                    case EnumFanModelType.AxialFlow:
                        tempCanUsePickers = EQPMFanDataService.Instance.GetAxialFanModelPickers(fanDataModel, BaseModelPicker);
                        break;
                    default:
                        tempCanUsePickers = EQPMFanDataService.Instance.GetFanModelPickers(fanDataModel, BaseModelPicker);
                        break;
                }
                if(string.IsNullOrEmpty(ccfName))
                    SelectModelPicker = CanUseFanModelPickers.First();
                else
                    SelectModelPicker = tempCanUsePickers.Where(c => c.Model == ccfName).FirstOrDefault();
                if (tempCanUsePickers.Count > 0) 
                {
                    CanUseFanModelPickers.Clear();
                    CanUseFanModelPickers.AddRange(tempCanUsePickers);
                }
                EQPMFanDataUtils.SetFanModelParameter(fanDataModel,BaseModelPicker, SelectModelPicker);
            }
            if (pModel != null)
            {
                EQPMFanDataService.Instance.FanCalaEfficiency(pModel, pModel);
                EQPMFanDataService.Instance.FanCalaEfficiency(pModel,fanDataModel);
            }
            else if (!fanDataModel.IsChildFan)
            {
                EQPMFanDataService.Instance.FanCalaEfficiency(fanDataModel, fanDataModel);
            }
            IsSelectFanError = fanDataModel.FanSelectionStateMsg.FanSelectionState != EnumFanSelectionState.HighAndLowBothSafe;
            if (fanDataModel.FanModelTypeCalcModel.ValueSource == EnumValueSource.IsCalcValue)
            {
                fanDataModel.FanModelPowerDescribe = fanDataModel.FanModelTypeCalcModel.FanModelMotorPower;
                if (null != pModel)
                {
                    pModel.FanModelPowerDescribe = string.Format("{0}/{1}", pModel.FanModelTypeCalcModel.FanModelMotorPower, fanDataModel.FanModelTypeCalcModel.FanModelMotorPower);
                }
            }
            else
            {
                fanDataModel.FanModelPowerDescribe = fanDataModel.FanModelTypeCalcModel.FanModelInputMotorPower;
                if (null != pModel)
                {
                    pModel.FanModelPowerDescribe = string.Format("{0}/{1}", pModel.FanModelTypeCalcModel.FanModelInputMotorPower, fanDataModel.FanModelTypeCalcModel.FanModelInputMotorPower);
                }
            }

        }
        void InputChangeRefreshModel() 
        {
            if (IsChildFan)
                return;
            FanModelCCCF = "未知型号";
            RefreshFanModel(null);
        }
        void RefreshFanModelByPicker()
        {
            if (null == SelectModelPicker)
                return;
            FanModelCCCF = SelectModelPicker.Model;
            if (!fanDataModel.IsPointSafe)
            {
                fanDataModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighAndLowBothSafe;
            }
            else
            {
                fanDataModel.FanSelectionStateMsg.FanSelectionState = EnumFanSelectionState.HighUnsafe;
            }
            fanDataModel.FanModelTypeCalcModel.FanModelCCCF = fanDataModel.FanModelCCCF;

        }

        void CalcFanQuan()
        {
            if (string.IsNullOrEmpty(fanDataModel.VentNum))
            {
                return;
            }
            var calculator = new VentSNCalculator();
            var res = calculator.SerialNumbers(fanDataModel.VentNum);
            if (res.Count > 0)
            {
                fanDataModel.ListVentQuan = res;
            }
            else
            {
                fanDataModel.ListVentQuan = new List<int>() { 1 };
                fanDataModel.VentNum = "1";
            }
        }
    }
}
