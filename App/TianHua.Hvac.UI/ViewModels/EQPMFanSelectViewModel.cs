using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using ThControlLibraryWPF.ControlUtils;
using ThMEPHVAC.EQPMFanModelEnums;
using ThMEPHVAC.EQPMFanSelect;
using TianHua.Hvac.UI.EQPMFanSelect;

namespace TianHua.Hvac.UI.ViewModels
{
    class EQPMFanSelectViewModel : NotifyPropertyChangedBase
    {
        public List<FanDataViewModel> allFanDataMoedels { get; }
        public EQPMFanSelectViewModel()
        {
            allFanDataMoedels = new List<FanDataViewModel>();
            FanInfos = new ObservableCollection<FanDataViewModel>();
            //FanInfos.NotifyCollectionPropertyNames.Add("InstallFloor");
           // FanInfos.NotifyCollectionPropertyNames.Add("InstallSpace");
            //FanInfos.NotifyCollectionPropertyNames.Add("VentNum");
            ShowType = EnumEQPMShowType.ShowByScenario;

            EnergyItems = new ObservableCollection<UListItemData>();
            var enumValues = CommonUtil.EnumDescriptionToList(typeof(EnumFanEnergyConsumption));
            enumValues.ForEach(c => EnergyItems.Add(c));

            FanControlItems = new ObservableCollection<UListItemData>();
            enumValues = CommonUtil.EnumDescriptionToList(typeof(EnumFanControl));
            enumValues.ForEach(c => FanControlItems.Add(c));

            FanMountTypeItems = new ObservableCollection<UListItemData>();
            enumValues = CommonUtil.EnumDescriptionToList(typeof(EnumMountingType));
            enumValues.ForEach(c => FanMountTypeItems.Add(c));

            VibrationModeItems = new ObservableCollection<UListItemData>();
            enumValues = CommonUtil.EnumDescriptionToList(typeof(EnumDampingType));
            enumValues.ForEach(c => VibrationModeItems.Add(c));

            ScenarioItems = new ObservableCollection<UListItemData>();
            enumValues = CommonUtil.EnumDescriptionToList(typeof(EnumScenario));
            enumValues.ForEach(c => ScenarioItems.Add(c));
            ScenarioSelectItem = ScenarioItems.Where(c => c.Value == (int)EnumScenario.FireSmokeExhaust).FirstOrDefault();

            FanCodeItems = new ObservableCollection<UListItemData>();
            var allStrs = EQPMFanCommon.ListFanPrefixDict.Select(c => c.Prefix).ToList().Distinct().ToList();
            int i = 0;
            allStrs.ForEach(c =>
            {
                FanCodeItems.Add(new UListItemData(c, i));
                i += 1;
            });
            FanCodeItem = FanCodeItems.FirstOrDefault();
            //FanInfos.CollectionChanged += new NotifyCollectionChangedEventHandler((s,e)=>
            //{
            //    if (FanInfos.Count() < 1)
            //        return;
            //    if (e.Action != NotifyCollectionChangedAction.Reset)
            //        return;
            //    CheckShowFanNumberIsRepeat();
            //});
        }
        private EnumEQPMShowType showType { get; set; }
        public EnumEQPMShowType ShowType
        {
            get 
            {
                return showType;
            }
            set 
            {
                showType = value;
                ScenarioSelectChange();
                CodeSelectChange();
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<FanDataViewModel> _fanInfos;
        public ObservableCollection<FanDataViewModel> FanInfos
        {
            get { return _fanInfos; }
            set
            {
                _fanInfos = value;
                CheckShowFanNumberIsRepeat();
                this.RaisePropertyChanged();
            }
        }
        private FanDataViewModel _selectFan { get; set; }
        public FanDataViewModel SelectFanData
        {
            get { return _selectFan; }
            set
            {
                _selectFan = value;
                HaveSelectItem = _selectFan != null;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<UListItemData> _scenarioItems { get; set; }
        public ObservableCollection<UListItemData> ScenarioItems
        {
            get
            {
                return _scenarioItems;
            }
            set
            {
                _scenarioItems = value;
                this.RaisePropertyChanged();
            }
        }
        private UListItemData _scenarioItem { get; set; }
        public UListItemData ScenarioSelectItem 
        {
            get { return _scenarioItem; }
            set 
            {
                _scenarioItem = value;
                if (null != value)
                { 
                    ScenarioSelectChange();
                    CheckShowFanNumberIsRepeat();
                }
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<UListItemData> _fanCodeItems { get; set; }
        public ObservableCollection<UListItemData> FanCodeItems
        {
            get { return _fanCodeItems; }
            set
            {
                _fanCodeItems = value;
                this.RaisePropertyChanged();
            }
        }
        private UListItemData _fanCodeItem { get; set; }
        public UListItemData FanCodeItem
        {
            get { return _fanCodeItem; }
            set
            {
                _fanCodeItem = value;
                CodeSelectChange();
                CheckShowFanNumberIsRepeat();
                this.RaisePropertyChanged();
            }
        }


        private ObservableCollection<UListItemData> _energyItems = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> EnergyItems
        {
            get
            {
                return _energyItems;
            }
            set
            {
                _energyItems = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<UListItemData> _fanControlItems { get; set; }
        public ObservableCollection<UListItemData> FanControlItems
        {
            get
            {
                return _fanControlItems;
            }
            set
            {
                _fanControlItems = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<UListItemData> _fanMountTypeItems { get; set; }
        public ObservableCollection<UListItemData> FanMountTypeItems
        {
            get
            {
                return _fanMountTypeItems;
            }
            set
            {
                _fanMountTypeItems = value;
                this.RaisePropertyChanged();
            }
        }
        private ObservableCollection<UListItemData> _vibrationModeItems { get; set; }
        public ObservableCollection<UListItemData> VibrationModeItems
        {
            get
            {
                return _vibrationModeItems;
            }
            set
            {
                _vibrationModeItems = value;
                this.RaisePropertyChanged();
            }
        }
        private bool _haveSelectItem { get; set; }
        public bool HaveSelectItem
        {
            get { return _haveSelectItem; }
            set 
            {
                _haveSelectItem = value;
                this.RaisePropertyChanged();
            }
        }
        private bool? _isOrderUp { get; set; }
        public bool? IsOrderUp 
        {
            get { return _isOrderUp; }
            set 
            {
                _isOrderUp = value;
                this.RaisePropertyChanged();
            }
        }
        void ScenarioSelectChange() 
        {
            if (ShowType != EnumEQPMShowType.ShowByScenario || ScenarioSelectItem ==null)
                return;
            var enumScenario = (EnumScenario)ScenarioSelectItem.Value;
            FanInfos.Clear();
            bool isAddDefault = true;
            foreach (var item in allFanDataMoedels) 
            {
                if (item == null || item.fanDataModel.Scenario != enumScenario)
                    continue;
                isAddDefault = false;
                FanInfos.Add(item);
            }
            if (isAddDefault)
                AddNewDeafultFanModel(enumScenario);
            var orderFans = EQPMFanDataUtils.OrderFanViewModels(FanInfos.ToList(),IsOrderUp ==false);
            if(IsOrderUp == null)
                orderFans = orderFans.OrderBy(c => c.fanDataModel.SortID).ToList();
            FanInfos.Clear();
            foreach (var item in orderFans) 
            {
                FanInfos.Add(item);
            }
        }
        public void AddNewDeafultFanModel(EnumScenario scenario) 
        {
            var addModel = new FanDataModel(scenario);
            var addItem = new FanDataViewModel(addModel);
            addItem.FanEnergyItem = EnergyItems.Where(c=>c.Value == (int)EnumFanEnergyConsumption.EnergyConsumption_2).FirstOrDefault();
            addItem.MotorEnergyItem = EnergyItems.Where(c => c.Value == (int)EnumFanEnergyConsumption.EnergyConsumption_2).FirstOrDefault();

            allFanDataMoedels.Add(addItem);
            FanInfos.Add(addItem);

            SetNewFanModelDefaultValue(scenario,ref addItem);
            CheckShowFanNumberIsRepeat();
        }
        public void SetNewFanModelDefaultValue(EnumScenario scenario,ref FanDataViewModel addItem) 
        {
            bool isChild = addItem.fanDataModel.IsChildFan;
            addItem.ServiceArea = "服务区域";
            switch (scenario)
            {
                case EnumScenario.FireAirSupplement:
                case EnumScenario.FirePressurizedAirSupply:
                case EnumScenario.FireSmokeExhaust:
                    //消防
                    addItem.fanDataModel.VolumeCalcModel.AirCalcFactor = 1.2;
                    addItem.FanMountTypeItem = FanMountTypeItems.Where(c => c.Value == (int)EnumMountingType.FloorBar).FirstOrDefault();
                    addItem.fanDataModel.Remark = "消防";
                    addItem.VibrationModeItem = VibrationModeItems.Where(c => c.Value == (int)EnumDampingType.NoDamping).FirstOrDefault();
                    addItem.FanControlItem = FanControlItems.Where(c => c.Value == (int)EnumFanControl.SingleSpeed).FirstOrDefault();

                    addItem.fanDataModel.DragModel.DuctLength = 0;
                    addItem.fanDataModel.DragModel.Friction = 3;
                    addItem.fanDataModel.DragModel.LocRes = 1.5;
                    addItem.fanDataModel.DragModel.Damper = 0;
                    addItem.fanDataModel.DragModel.EndReservedAirPressure = 0;
                    addItem.fanDataModel.DragModel.DynPress = 60;
                    addItem.fanDataModel.DragModel.SelectionFactor = 1.1;
                    addItem.FanTypeItem = addItem.FanTypeItems.Where(c => c.Value == (int)EnumFanModelType.AxialFlow).FirstOrDefault();
                    addItem.AirflowDirectionItem = addItem.AirflowDirectionItems.Where(c => c.Value == (int)EnumFanAirflowDirection.StraightInAndStraightOut).FirstOrDefault();
                    break;
                case EnumScenario.NormalAirSupply:
                case EnumScenario.NormalExhaust:
                    //平时
                    addItem.fanDataModel.VolumeCalcModel.AirCalcFactor = 1.1;
                    addItem.FanMountTypeItem = FanMountTypeItems.Where(c => c.Value == (int)EnumMountingType.Hoisting).FirstOrDefault();
                    addItem.fanDataModel.Remark = "";
                    addItem.VibrationModeItem = VibrationModeItems.Where(c => c.Value == (int)EnumDampingType.SDamping).FirstOrDefault();
                    addItem.FanControlItem = FanControlItems.Where(c => c.Value == (int)EnumFanControl.SingleSpeed).FirstOrDefault();

                    addItem.fanDataModel.DragModel.DuctLength = 0;
                    addItem.fanDataModel.DragModel.Friction = 1;
                    addItem.fanDataModel.DragModel.LocRes = 1.5;
                    addItem.fanDataModel.DragModel.Damper = 80;
                    addItem.fanDataModel.DragModel.EndReservedAirPressure = 0;
                    addItem.fanDataModel.DragModel.DynPress = 60;
                    addItem.fanDataModel.DragModel.SelectionFactor = 1.1;
                    addItem.FanTypeItem = addItem.FanTypeItems.Where(c => c.Value == (int)EnumFanModelType.ForwardTiltCentrifugation_Inner).FirstOrDefault();
                    addItem.AirflowDirectionItem = addItem.AirflowDirectionItems.Where(c => c.Value == (int)EnumFanAirflowDirection.StraightInAndStraightOut).FirstOrDefault();
                    break;
                case EnumScenario.FireAirSupplementAndNormalAirSupply:
                case EnumScenario.FireSmokeExhaustAndNormalExhaust:
                    //兼用
                    if (isChild)
                    {
                        addItem.fanDataModel.VolumeCalcModel.AirCalcFactor = 1.1;
                        addItem.FanMountTypeItem = FanMountTypeItems.Where(c => c.Value == (int)EnumMountingType.FloorBar).FirstOrDefault();
                        addItem.fanDataModel.Remark = "消防兼用";
                        addItem.VibrationModeItem = VibrationModeItems.Where(c => c.Value == (int)EnumDampingType.SDamping).FirstOrDefault();
                        addItem.FanControlItem = FanControlItems.Where(c => c.Value == (int)EnumFanControl.TwoSpeed).FirstOrDefault();

                        addItem.fanDataModel.DragModel.DuctLength = 0;
                        addItem.fanDataModel.DragModel.Friction = 1;
                        addItem.fanDataModel.DragModel.LocRes = 1.5;
                        addItem.fanDataModel.DragModel.Damper = 80;
                        addItem.fanDataModel.DragModel.EndReservedAirPressure = 0;
                        addItem.fanDataModel.DragModel.DynPress = 60;
                        addItem.fanDataModel.DragModel.SelectionFactor = 1.1;
                        addItem.FanTypeItem = addItem.FanTypeItems.Where(c => c.Value == (int)EnumFanModelType.ForwardTiltCentrifugation_Out).FirstOrDefault();
                        addItem.AirflowDirectionItem = addItem.AirflowDirectionItems.Where(c => c.Value == (int)EnumFanAirflowDirection.UpInStraightOut).FirstOrDefault();
                    }
                    else 
                    {
                        addItem.fanDataModel.VolumeCalcModel.AirCalcFactor = 1.2;
                        addItem.FanMountTypeItem = FanMountTypeItems.Where(c => c.Value == (int)EnumMountingType.FloorBar).FirstOrDefault();
                        addItem.fanDataModel.Remark = "消防兼用";
                        addItem.VibrationModeItem = VibrationModeItems.Where(c => c.Value == (int)EnumDampingType.SDamping).FirstOrDefault();
                        addItem.FanControlItem = FanControlItems.Where(c => c.Value == (int)EnumFanControl.TwoSpeed).FirstOrDefault();

                        addItem.fanDataModel.DragModel.DuctLength = 0;
                        addItem.fanDataModel.DragModel.Friction = 3;
                        addItem.fanDataModel.DragModel.LocRes = 1.5;
                        addItem.fanDataModel.DragModel.Damper = 0;
                        addItem.fanDataModel.DragModel.EndReservedAirPressure = 0;
                        addItem.fanDataModel.DragModel.DynPress = 60;
                        addItem.fanDataModel.DragModel.SelectionFactor = 1.1;
                        addItem.FanTypeItem = addItem.FanTypeItems.Where(c => c.Value == (int)EnumFanModelType.ForwardTiltCentrifugation_Out).FirstOrDefault();
                        addItem.AirflowDirectionItem = addItem.AirflowDirectionItems.Where(c => c.Value == (int)EnumFanAirflowDirection.UpInStraightOut).FirstOrDefault();
                    }
                    
                    break;
                case EnumScenario.KitchenFumeExhaust:
                case EnumScenario.KitchenFumeExhaustAndAirSupplement:
                    //油烟
                    addItem.fanDataModel.VolumeCalcModel.AirCalcFactor = 1.1;
                    addItem.FanMountTypeItem = FanMountTypeItems.Where(c => c.Value == (int)EnumMountingType.FloorBar).FirstOrDefault();
                    addItem.fanDataModel.Remark = "";
                    addItem.VibrationModeItem = VibrationModeItems.Where(c => c.Value == (int)EnumDampingType.SDamping).FirstOrDefault();
                    addItem.FanControlItem = FanControlItems.Where(c => c.Value == (int)EnumFanControl.SingleSpeed).FirstOrDefault();

                    addItem.fanDataModel.DragModel.DuctLength = 0;
                    addItem.fanDataModel.DragModel.Friction = 1;
                    addItem.fanDataModel.DragModel.LocRes = 1.5;
                    addItem.fanDataModel.DragModel.Damper = 80;
                    addItem.fanDataModel.DragModel.EndReservedAirPressure = 0;
                    addItem.fanDataModel.DragModel.DynPress = 60;
                    addItem.fanDataModel.DragModel.SelectionFactor = 1.1;
                    addItem.FanTypeItem = addItem.FanTypeItems.Where(c => c.Value == (int)EnumFanModelType.ForwardTiltCentrifugation_Inner).FirstOrDefault();
                    addItem.AirflowDirectionItem = addItem.AirflowDirectionItems.Where(c => c.Value == (int)EnumFanAirflowDirection.StraightInAndStraightOut).FirstOrDefault();
                    break;
                case EnumScenario.AccidentAirSupplement:
                case EnumScenario.EmergencyExhaust:
                case EnumScenario.NormalAirSupplyAndAccidentAirSupplement:
                case EnumScenario.NormalExhaustAndAccidentExhaust:
                    //事故
                    addItem.fanDataModel.VolumeCalcModel.AirCalcFactor = 1.1;
                    addItem.FanMountTypeItem = FanMountTypeItems.Where(c => c.Value == (int)EnumMountingType.Hoisting).FirstOrDefault();
                    addItem.fanDataModel.Remark = "事故";
                    addItem.VibrationModeItem = VibrationModeItems.Where(c => c.Value == (int)EnumDampingType.SDamping).FirstOrDefault();
                    addItem.FanControlItem = FanControlItems.Where(c => c.Value == (int)EnumFanControl.SingleSpeed).FirstOrDefault();

                    addItem.fanDataModel.DragModel.DuctLength = 0;
                    addItem.fanDataModel.DragModel.Friction = 1;
                    addItem.fanDataModel.DragModel.LocRes = 1.5;
                    addItem.fanDataModel.DragModel.Damper = 80;
                    addItem.fanDataModel.DragModel.EndReservedAirPressure = 0;
                    addItem.fanDataModel.DragModel.DynPress = 60;
                    addItem.fanDataModel.DragModel.SelectionFactor = 1.1;
                    addItem.FanTypeItem = addItem.FanTypeItems.Where(c => c.Value == (int)EnumFanModelType.ForwardTiltCentrifugation_Inner).FirstOrDefault();
                    addItem.AirflowDirectionItem = addItem.AirflowDirectionItems.Where(c => c.Value == (int)EnumFanAirflowDirection.StraightInAndStraightOut).FirstOrDefault();
                    break;
            }
        }
        void CodeSelectChange() 
        {
            if (ShowType != EnumEQPMShowType.ShowByFanCode || FanCodeItem ==null)
                return;
            var code = FanCodeItem.Name;
            FanInfos.Clear();
            //名称和应用场景不能一一对应，这里不进行添加操作
            foreach (var item in allFanDataMoedels)
            {
                if (string.IsNullOrEmpty(item.Name) || code != item.Name)
                    continue;
                FanInfos.Add(item);
            }
            var orderFans = EQPMFanDataUtils.OrderFanViewModels(FanInfos.ToList(), IsOrderUp == false);
            FanInfos.Clear();
            foreach (var item in orderFans)
            {
                FanInfos.Add(item);
            }
        }

        public void CheckShowFanNumberIsRepeat() 
        {
            if (null == FanInfos) 
                return;
            var fanCount = FanInfos.Count;
            for (int i = 0; i < fanCount; i++)
            {
                var current = FanInfos[i];
                if (current.IsChildFan)
                    continue;
                current.IsRepetitions = false;
            }
            for (int i = 0; i < fanCount; i++) 
            {
                var current = FanInfos[i];
                if (current.IsChildFan || current.IsRepetitions)
                    continue;
                if (string.IsNullOrEmpty(current.InstallSpace) || string.IsNullOrEmpty(current.InstallFloor) || string.IsNullOrEmpty(current.VentNum))
                    continue;
                var fanNum = string.Format("{0}-{1}-{2}-{3}", current.Name, current.InstallSpace, current.InstallFloor, current.VentNum);
                for (int j = 0; j < fanCount; j++) 
                {
                    if (i == j)
                        continue;
                    var check = FanInfos[j];
                    if (check.IsChildFan || current.IsRepetitions)
                        continue;
                    if (string.IsNullOrEmpty(current.InstallSpace) || string.IsNullOrEmpty(current.InstallFloor) || string.IsNullOrEmpty(current.VentNum))
                        continue;
                    if (current.ScenarioString != check.ScenarioString)
                        continue;
                    var checkNum = string.Format("{0}-{1}-{2}-{3}", check.Name, check.InstallSpace, check.InstallFloor, check.VentNum);
                    if (fanNum == checkNum)
                    {
                        current.IsRepetitions = true;
                        check.IsRepetitions = true;
                    }
                }
            }
        }
    }
}
