using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace TianHua.Electrical.ViewModels
{
    public class FireAlarmViewModel : NotifyPropertyChangedBase
    {
        private int _SelectedIndexForH = 0;

        //H
        public int SelectedIndexForH 
        {
            get 
            { 
                return _SelectedIndexForH; 
            }
            set 
            {
                _SelectedIndexForH = value;

                OnPropertyChanged("SelectedIndexForH");
            }
        }

        private string _SelectedHValue = string.Empty;
        public string SelectedHValue
        {
            get
            {
                return _SelectedHValue;
            }
            set
            {
                _SelectedHValue = value;

                OnPropertyChanged("SelectedHValue");
            }
        }

        private int _SelectedIndexForAngle = 0;

        //Theta
        public int SelectedIndexForAngle 
        {
            get
            {
                return _SelectedIndexForAngle;
            }
            set
            {
                _SelectedIndexForAngle = value;

                OnPropertyChanged("SelectedIndexForAngle");
            }
        }

        //Theta value
        private string _SelectedValueForTheta = string.Empty;

        public string SelectedValueForTheta
        {
            get
            {
                return _SelectedValueForTheta;
            }
            set
            {
                _SelectedValueForTheta = value;

                OnPropertyChanged("SelectedValueForTheta");
            }
        }

        //D
        private double _valueOfD = 0;

        public double ValueOfD //mm
        {
            get { return _valueOfD; }
            set 
            { 
                _valueOfD = value;
                OnPropertyChanged("ValueOfD");
            }
        }

        //烟感温感
        private bool _IsSmokeTempratureSensorChecked = false;
        public bool IsSmokeTempratureSensorChecked 
        { 
            get
            {
                return _IsSmokeTempratureSensorChecked;
            }
            set
            {
                _IsSmokeTempratureSensorChecked = value;
                OnPropertyChanged("IsSmokeTempratureSensorChecked");
            }
        }

        //消防广播
        private bool _IsBroadcastChecked = false;
        public bool IsBroadcastChecked
        {
            get
            {
                return _IsBroadcastChecked;
            }
            set
            {
                _IsBroadcastChecked = value;
                OnPropertyChanged("IsBroadcastChecked");
            }
        }

        //楼层回路显示盘
        private bool _IsFloorLoopChecked = false;
        public bool IsFloorLoopChecked
        {
            get
            {
                return _IsFloorLoopChecked;
            }
            set
            {
                _IsFloorLoopChecked = value;
                OnPropertyChanged("IsFloorLoopChecked");
            }
        }

        //楼层回路显示盘: FL
        private bool _IsFLChecked = true;
        public bool IsFLChecked
        {
            get
            {
                return _IsFLChecked;
            }
            set
            {
                _IsFLChecked = value;
                OnPropertyChanged("IsFLChecked");
            }
        }
        //楼层回路显示盘: D
        private bool _IsDChecked = false;
        private bool IsDChecked
        {
            get
            {
                return _IsDChecked;
            }
            set
            {
                _IsDChecked = value;
                OnPropertyChanged("IsDChecked");
            }
        }

        //楼层回路显示盘: 住宅
        private bool _IsResidentChecked = true;
        public bool IsResidentChecked
        {
            get
            {
                return _IsResidentChecked;
            }
            set
            {
                _IsResidentChecked = value;
                OnPropertyChanged("IsResidentChecked");
            }
        }

        //楼层回路显示盘: 公建
        private bool _IsPublicChecked = false;
        private bool IsPublicChecked
        {
            get
            {
                return _IsPublicChecked;
            }
            set
            {
                _IsPublicChecked = value;
                OnPropertyChanged("IsPublicChecked");
            }
        }

        //防火门监控模块
        private bool _IsFireMonitorModuleChecked = false;
        public bool IsFireMonitorModuleChecked
        {
            get
            {
                return _IsFireMonitorModuleChecked;
            }
            set
            {
                _IsFireMonitorModuleChecked = value;
                OnPropertyChanged("IsFireMonitorModuleChecked");
            }
        }

        //消防电话
        private bool _IsFireProtectionPhoneChecked = false;
        public bool IsFireProtectionPhoneChecked
        {
            get
            {
                return _IsFireProtectionPhoneChecked;
            }
            set
            {
                _IsFireProtectionPhoneChecked = value;
                OnPropertyChanged("IsFireProtectionPhoneChecked");
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

        //块参照比例index
        private int _BlockRatioIndex = 0;
        public int BlockRatioIndex
        {
            get
            {
                return _BlockRatioIndex;
            }
            set
            {
                _BlockRatioIndex = value;
                OnPropertyChanged("BlockRatioIndex");
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

        //回路点位上限
        private int _BusLoopPointMaxCount = 32;
        public int BusLoopPointMaxCount
        {
            get
            {
                return _BusLoopPointMaxCount;
            }
            set
            {
                _BusLoopPointMaxCount = value;
                OnPropertyChanged("BusLoopPointMaxCount");
            }
        }
    }
}
