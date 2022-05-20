using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using ThMEPWSS.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.ViewModel
{
    public class ZoneSetupViewModel : NotifyPropertyChangedBase
    {
        private int _zoneID;
        public int ZoneID
        {
            get => _zoneID;
            set
            {
                _zoneID = value;
                RaisePropertyChanged(nameof(ZoneID));
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
                if (!int.TryParse(_startFloor, out int intStart) || !int.TryParse(_endFloor, out int intEnd))
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
                if (int.TryParse(_startFloor, out int intStart))
                    return intStart;
            }
            return null;
        }
        public int? GetIntEndFloor()
        {
            if (!string.IsNullOrEmpty(this._endFloor))
            {
                if (int.TryParse(_endFloor, out int endStart))
                    return endStart;
            }
            return null;
        }

        /// <summary>
        /// 分区管径
        /// </summary>
        private ObservableCollection<string> _DNListItems = new ObservableCollection<string>() { "DN100", "DN150", "DN200" };
        public ObservableCollection<string> DNListItems
        {
            get
            {
                return _DNListItems;
            }
            set
            {
                if (_DNListItems != value)
                {
                    _DNListItems = value;
                    this.RaisePropertyChanged(nameof(DNListItems));
                }
            }
        }
        /// <summary>
        /// 管径选择项
        /// </summary>
        private string _dnSelectItem = "DN100";
        public string DNSelectItem
        {
            get { return _dnSelectItem; }
            set
            {
                if (_dnSelectItem != value)
                {
                    _dnSelectItem = value;
                    this.RaisePropertyChanged(nameof(DNSelectItem));
                }
            }
        }
    }
    public class FireControlSystemDiagramViewModel : NotifyPropertyChangedBase
    {
        public static readonly FireControlSystemDiagramViewModel Singleton = CreateDefaultViewModel();
        public FireControlSystemDiagramViewModel() { }
        public static FireControlSystemDiagramViewModel CreateDefaultViewModel()
        {
            var o = new FireControlSystemDiagramViewModel
            {
                ZoneConfigs = new ObservableCollection<ZoneSetupViewModel>
            {
                new ZoneSetupViewModel() { ZoneID = 1, StartFloor = "1" },
                new ZoneSetupViewModel() { ZoneID = 2 },
                new ZoneSetupViewModel() { ZoneID = 3 },
                new ZoneSetupViewModel() { ZoneID = 4 },
            },
                FireTypes = new ObservableCollection<string> { "单栓", "单栓带卷盘", },
                HaveHandPumpConnection = false,
                HaveTestFireHydrant = true,
            };
            o.ComBoxFireTypeSelectItem = o.FireTypes.FirstOrDefault();
            o.SetHighlevelNozzleAndSemiPlatformNozzleParams = SetHighlevelNozzleAndSemiPlatformNozzlesViewModel.Singleton;
            return o;
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

        private bool _isRoofRing = true;
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

        private bool _haveHandPumpConnection { get; set; }
        public bool HaveHandPumpConnection
        {
            get { return _haveHandPumpConnection; }
            set
            {
                _haveHandPumpConnection = value;
                RaisePropertyChanged("HaveHandPumpConnection");
            }
        }

        private ObservableCollection<string> _fireTypes;
        public ObservableCollection<string> FireTypes
        {
            get { return _fireTypes; }
            set
            {
                _fireTypes = value;
                RaisePropertyChanged("FireTypes");
            }
        }
        private string _comboxFireTypeSelect;
        public string ComBoxFireTypeSelectItem
        {
            get { return _comboxFireTypeSelect; }
            set
            {
                _comboxFireTypeSelect = value;
                RaisePropertyChanged("ComBoxFireTypeSelectItem");
            }
        }
        SetHighlevelNozzleAndSemiPlatformNozzlesViewModel _SetHighlevelNozzleAndSemiPlatformNozzleParams;
        public SetHighlevelNozzleAndSemiPlatformNozzlesViewModel SetHighlevelNozzleAndSemiPlatformNozzleParams
        {
            get => _SetHighlevelNozzleAndSemiPlatformNozzleParams;
            set
            {
                if (value != _SetHighlevelNozzleAndSemiPlatformNozzleParams)
                {
                    _SetHighlevelNozzleAndSemiPlatformNozzleParams = value;
                    OnPropertyChanged(nameof(SetHighlevelNozzleAndSemiPlatformNozzleParams));
                }
            }
        }
        bool _IsTopRing;
        public bool IsTopRing
        {
            get => _IsTopRing;
            set
            {
                if (value != _IsTopRing)
                {
                    _IsTopRing = value;
                    OnPropertyChanged();
                }
            }
        }
        bool _IsDoublePipe = true;
        public bool IsDoublePipe
        {
            get => _IsDoublePipe;
            set
            {
                if (value != _IsDoublePipe)
                {
                    _IsDoublePipe = value;
                    OnPropertyChanged();
                }
            }
        }
        bool _IsMultiPipe;
        public bool IsMultiPipe
        {
            get => _IsMultiPipe;
            set
            {
                if (value != _IsMultiPipe)
                {
                    _IsMultiPipe = value;
                    OnPropertyChanged();
                }
            }
        }

    }
}
