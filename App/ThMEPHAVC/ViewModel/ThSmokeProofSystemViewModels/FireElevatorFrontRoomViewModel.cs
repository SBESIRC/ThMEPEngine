using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels
{
    public class FireElevatorFrontRoomViewModel : NotifyPropertyChangedBase
    {
        [JsonIgnore]
        public CheckValue checkValue;
        public FireElevatorFrontRoomViewModel()
        {
        }

        /// <summary>
        /// 这是AK的值
        /// </summary>
        public double OverAk { get; set; }

        /// <summary>
        /// 系统负担高度
        /// </summary>
        private FloorTypeEnum _floorType;
        public FloorTypeEnum FloorType
        {
            get
            { return _floorType; }
            set
            {
                _floorType = value;
                MiddleWind = "26550-36900";
                HighWind = "27825-40200";
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 系统楼层数
        /// </summary>
        private int _floorNum;
        public int FloorNum
        {
            get { return _floorNum; }
            set
            {
                _floorNum = value;
                RefreshData();
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 截面长
        /// </summary>
        private double _sectionLength;
        public double SectionLength
        {
            get { return _sectionLength; }
            set
            {
                _sectionLength = value;
                RefreshData();
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 截面宽
        /// </summary>
        private double _sectionWidth;
        public double SectionWidth
        {
            get { return _sectionWidth; }
            set
            {
                _sectionWidth = value;
                RefreshData();
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Lj合计
        /// </summary>
        public double LjTotal
        {
            get
            {
                return OpenDorrAirSupply + VentilationLeakage;
            }
            set
            {
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// L1门开启风量
        /// </summary>
        public double OpenDorrAirSupply
        {
            get
            {
                //double Ak = 0.0;
                OverAk = 0;
                int ValidFloorCount = 0;
                bool HasValidDoorInFloor = false;
                foreach (var floor in ListTabControl)
                {
                    foreach (var door in floor.FloorInfoItems)
                    {
                        if (door.DoorNum * door.DoorHeight * door.DoorWidth == 0)
                        {
                            continue;
                        }
                        OverAk += door.DoorWidth * door.DoorHeight * door.DoorNum;
                        HasValidDoorInFloor = true;
                    }
                    if (HasValidDoorInFloor)
                    {
                        ValidFloorCount++;
                    }
                    HasValidDoorInFloor = false;
                }
                if (ValidFloorCount != 0)
                {
                    OverAk = OverAk / ValidFloorCount;
                }
                OverAk = Math.Round(OverAk, 2);
                int V = 1;
                return Math.Round(OverAk * V * Math.Min(FloorNum, 3) * 3600);
            }
            set
            {
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// L3送风阀漏风
        /// </summary>
        public double VentilationLeakage
        {
            get
            {
                double Af = Convert.ToDouble(SectionLength) * Convert.ToDouble(SectionWidth) / 1000000;
                double N3 = (FloorNum - 3 > 0) ? (FloorNum - 3) : 0;
                return Math.Round(0.083 * Af * N3 * 3600);
            }
            set
            {
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// tab页
        /// </summary>
        private ObservableCollection<TabControlInfo> _listTabControl = new ObservableCollection<TabControlInfo>();
        public ObservableCollection<TabControlInfo> ListTabControl
        {
            get { return _listTabControl; }
            set
            {
                _listTabControl = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 选中的tabcontrol
        /// </summary>
        private int _selectTabControlIndex;
        public int SelectTabControlIndex
        {
            get { return _selectTabControlIndex; }
            set
            {
                _selectTabControlIndex = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 刷新计算数据
        /// </summary>
        public void RefreshData()
        {
            OpenDorrAirSupply = OpenDorrAirSupply;
            VentilationLeakage = VentilationLeakage;
            LjTotal = LjTotal;
            FinalValue = FinalValue;
            //checkValue();
        }

        /*查表数据*/
        /// <summary>
        /// 查表值
        /// </summary>
        private double _checkTableVal;
        public double CheckTableVal
        {
            get { return _checkTableVal; }
            set
            {
                _checkTableVal = value;
                RefreshData();
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 中层层高加压送风量
        /// </summary>
        private string _middleWind;
        public string MiddleWind
        {
            get { return _middleWind; }
            set
            {
                _middleWind = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 高层层高加压送风量
        /// </summary>
        private string _highWind;
        public string HighWind
        {
            get { return _highWind; }
            set
            {
                _highWind = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 最终值
        /// </summary>
        public double FinalValue
        {
            get { return Math.Max(CheckTableVal, LjTotal); }
            set
            {
                this.RaisePropertyChanged();
            }
        }
    }

    public class TabControlInfo : NotifyPropertyChangedBase
    {
        /// <summary>
        /// 楼层名
        /// </summary>
        private string _floorName;
        public string FloorName
        {
            get { return _floorName; }
            set
            {
                _floorName = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<FloorInfo> _floorInfoItems = new ObservableCollection<FloorInfo>();
        public ObservableCollection<FloorInfo> FloorInfoItems
        {
            get { return _floorInfoItems; }
            set
            {
                _floorInfoItems = value;
                this.RaisePropertyChanged();
            }
        }

        private FloorInfo _selectInfo { get; set; }
        public FloorInfo SelectInfoData
        {
            get { return _selectInfo; }
            set
            {
                _selectInfo = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public class FloorInfo : NotifyPropertyChangedBase
    {
        public FloorInfo() { }
        public FloorInfo(bool Init)
        {
            if (Init)
            {
                SetBlockScaleListType();
            }
        }

        /// <summary>
        ///  门形式
        /// </summary>
        private UListItemData _doorType { get; set; }
        public UListItemData DoorType
        {
            get { return _doorType; }
            set
            {
                _doorType = value;
                this.RaisePropertyChanged();
            }
        }

        private ObservableCollection<UListItemData> _doorTypeList = new ObservableCollection<UListItemData>();
        public ObservableCollection<UListItemData> DoorTypeList
        {
            get { return _doorTypeList; }
            set
            {
                _doorTypeList = value;
                this.RaisePropertyChanged();
            }
        }

        private void SetBlockScaleListType()
        {
            DoorTypeList.Add(new UListItemData("单开门", 0, 1));
            DoorTypeList.Add(new UListItemData("双开门", 1, 2));
            DoorType = DoorTypeList[1];
        }

        /// <summary>
        /// 门宽
        /// </summary>
        private double _doorWidth;
        public double DoorWidth
        {
            get { return _doorWidth; }
            set
            {
                _doorWidth = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 门高
        /// </summary>
        private double _doorHeight;
        public double DoorHeight
        {
            get { return _doorHeight; }
            set
            {
                _doorHeight = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 门数量
        /// </summary>
        private double _doorNum;
        public double DoorNum
        {
            get { return _doorNum; }
            set
            {
                _doorNum = value;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 门缝宽
        /// </summary>
        private double _doorSpace;
        public double DoorSpace
        {
            get { return _doorSpace; }
            set
            {
                _doorSpace = value;
                this.RaisePropertyChanged();
            }
        }
    }

    public enum FloorTypeEnum
    {
        /// <summary>
        /// x<=24
        /// </summary>
        lowFloor = 0,

        /// <summary>
        /// 24<x<=50
        /// </summary>
        middleFloor = 1,

        /// <summary>
        /// 50<x<=100
        /// </summary>
        highFloor = 2,
    }
}
