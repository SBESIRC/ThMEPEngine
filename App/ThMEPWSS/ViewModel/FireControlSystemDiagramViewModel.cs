using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPWSS.ViewModel
{
    public class ZoneSetupViewModel : NotifyPropertyChangedBase
    {
        private int _zoneID;

        public int ZoneID
        {
            get 
            { return _zoneID; }
            set 
            { 
                _zoneID = value;
                RaisePropertyChanged("ZoneID");
            }
        }
        /// <summary>
        /// 开始楼层
        /// </summary>
        private string _startFloor { get; set; }
        /// <summary>
        /// 开始楼层
        /// </summary>
        public string StartFloor 
        {
            get { return _startFloor; }
            set 
            {
                _startFloor = value;
                RaisePropertyChanged("StartFloor");
            }
        }
        /// <summary>
        /// 结束楼层
        /// </summary>
        private string _endFloor { get; set; }
        /// <summary>
        /// 结束楼层
        /// </summary>
        public string EndFloor 
        {
            get { return _endFloor; }
            set 
            {
                _endFloor = value;
                RaisePropertyChanged("EndFloor");
            }
        }
        public bool IsEffective() 
        {
            if (string.IsNullOrEmpty(this._startFloor) && string.IsNullOrEmpty(this._endFloor))
            {
                return true;
            }
            else if (string.IsNullOrEmpty(this._startFloor) || string.IsNullOrEmpty(this._endFloor))
            {
                return false;
            }
            else 
            {
                int intStart = -9999;
                int intEnd = 9999;
                if(!int.TryParse(_startFloor,out intStart) || !int.TryParse(_endFloor,out intEnd)) 
                    return false;
                if (intStart >= intEnd)
                    return false;
                return true;
            }
        }
        public int? GetIntStartFloor()
        {
            if (!string.IsNullOrEmpty(this._startFloor)) 
            {
                int intStart = -9999;
                if (int.TryParse(_startFloor, out intStart)) 
                    return intStart;
            }
            return null;
        }
        public int? GetIntEndFloor() 
        {
            if (!string.IsNullOrEmpty(this._endFloor))
            {
                int endStart = -9999;
                if (int.TryParse(_endFloor, out endStart))
                    return endStart;
            }
            return null;
        }
    }
    public class FireControlSystemDiagramViewModel : NotifyPropertyChangedBase
    {
        public FireControlSystemDiagramViewModel()
        {
            ZoneConfigs = new ObservableCollection<ZoneSetupViewModel>();
            ZoneConfigs.Add(new ZoneSetupViewModel() { ZoneID = 1, StartFloor = "1" });
            ZoneConfigs.Add(new ZoneSetupViewModel() { ZoneID = 2 });
            ZoneConfigs.Add(new ZoneSetupViewModel() { ZoneID = 3 });
            ZoneConfigs.Add(new ZoneSetupViewModel() { ZoneID = 4 });

            FireTypes = new ObservableCollection<UListItemData>();
            FireTypes.Add(new UListItemData("单栓", 1));
            FireTypes.Add(new UListItemData("单栓带卷盘", 2));
            ComBoxFireTypeSelectItem = FireTypes.FirstOrDefault();
        }

        private double _FaucetFloor = 1800; //mm
        /// <summary>
        /// 楼层线间距
        /// </summary>
        public double FaucetFloor
        {
            get
            {
                return _FaucetFloor;
            }
            set
            {
                _FaucetFloor = value;
                RaisePropertyChanged("FaucetFloor");
            }
        }

        private string _serialnumber = "1";
        /// <summary>
        /// 单元编号
        /// </summary>
        public string Serialnumber
        {
            get
            {
                return _serialnumber;
            }
            set
            {
                _serialnumber = value;
                RaisePropertyChanged("Serialnumber");
            }
        }

        private int _countsgeneral = 4;
        /// <summary>
        /// 普通层消火栓数量
        /// </summary>
        public int CountsGeneral
        {
            get
            {
                return _countsgeneral;
            }
            set
            {
                _countsgeneral = value;
                RaisePropertyChanged("CountsGeneral");
            }
        }

        private int _countsrefuge = 4;
        /// <summary>
        /// 避难层消火栓数量
        /// </summary>
        public int CountsRefuge
        {
            get
            {
                return _countsrefuge;
            }
            set
            {
                _countsrefuge = value;
                RaisePropertyChanged("CountsRefuge");
            }
        }

        private bool _isRoofRing =true;
        /// <summary>
        /// 屋顶成环
        /// </summary>
        public bool IsRoofRing
        {
            get
            {
                return _isRoofRing;
            }
            set
            {
                _isRoofRing = value;
                RaisePropertyChanged("IsRoof");
            }
        }

        private bool _isTopLayerRing;
        /// <summary>
        /// 顶层成环
        /// </summary>
        public bool IsTopLayerRing
        {
            get
            {
                return _isTopLayerRing;
            }
            set
            {
                _isTopLayerRing = value;
                RaisePropertyChanged("IsTopLayer");
            }
        }

        private bool _isTower = true;
        /// <summary>
        /// 塔楼（生成对象）
        /// </summary>
        public bool IsTower 
        { 
            get
            {
                return _isTower;
            }
            set
            {
                _isTower = value;
                RaisePropertyChanged("IsTower");
            }
        }

        private bool _isManagementBuilding;
        /// <summary>
        /// 管理用房（生成对象）
        /// </summary>
        public bool IsManagementBuilding
        {
            get { return _isManagementBuilding; }
            set 
            { 
                _isManagementBuilding = value;
                RaisePropertyChanged("IsManagementBuilding");
            }
        }
        
        private bool _haveTestFireHydrant { get; set; }
        public bool HaveTestFireHydrant 
        {
            get { return _haveTestFireHydrant; }
            set 
            {
                _haveTestFireHydrant = value;
                RaisePropertyChanged("HaveTestFireHydrant");
            }
        }
        private ObservableCollection<ZoneSetupViewModel> _ZoneConfigs;
        public ObservableCollection<ZoneSetupViewModel> ZoneConfigs
        {
            get
            {
                return _ZoneConfigs;
            }
            set
            {
                _ZoneConfigs = value;
                RaisePropertyChanged("ZoneConfigs");
            }
        }

        

        private ObservableCollection<UListItemData> _fireTypes { get; set; }
        public ObservableCollection<UListItemData> FireTypes 
        {
            get { return _fireTypes; }
            set 
            {
                _fireTypes = value;
                RaisePropertyChanged("FireTypes");
            }
        }
        private UListItemData _comboxFireTypeSelect { get; set; }
        public UListItemData ComBoxFireTypeSelectItem 
        {
            get { return _comboxFireTypeSelect; }
            set 
            {
                _comboxFireTypeSelect = value;
                RaisePropertyChanged("ComBoxFireTypeSelectItem");
            }
        }

    }
}
