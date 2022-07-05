﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels
{
    public class StaircaseNoWindViewModel : NotifyPropertyChangedBase
    {
        [JsonIgnore]
        public CheckValue checkValue;
        /// <summary>
        /// 这是AK的值
        /// </summary>
        public double OverAk { get; set; }

        /// <summary>
        /// N2
        /// </summary>
        private double _n2;
        public double N2
        {
            get { return _n2; }
            set
            {
                _n2 = value;
                RefreshData();
                this.RaisePropertyChanged();
            }
        }

        public int StairN1 = 0;
        public int AAAA = 36100, BBBB = 39200;
        public int CCCC = 39600, DDDD = 45800;

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
                MiddleWind = "27075-39200";
                HighWind = "29700-45800";
                RefreshData();
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 楼梯间位置
        /// </summary>
        private StairPositionEnum _stairPosition;
        public StairPositionEnum StairPosition
        {
            get
            { return _stairPosition; }
            set
            {
                _stairPosition = value;
                RefreshData();
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 空间状态
        /// </summary>
        private BusinessTypeEnum _businessType;
        public BusinessTypeEnum BusinessType
        {
            get
            { return _businessType; }
            set
            {
                _businessType = value;
                RefreshData();
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
        private string _sectionLength;
        public string SectionLength
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
        private string _sectionWidth;
        public string SectionWidth
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
        /// 漏风疏散门数量N2
        /// </summary>
        private string _evacuationDoorNums;
        public string EvacuationDoorNums
        {
            get { return _evacuationDoorNums; }
            set
            {
                _evacuationDoorNums = value;
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
                OverAk = 0;
                int ValidFloorCount = 0;
                bool HasValidDoorInFloor = false;
                foreach (var floor in FrontRoomTabControl)
                {
                    foreach (var door in floor.FloorInfoItems)
                    {
                        if (door.DoorNum * door.DoorHeight * door.DoorWidth * door.DoorSpace == 0)
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
                int V = 1;
                OverAk = Math.Round(OverAk, 2);
                return Math.Round(OverAk * V * StairN1 * 3600);
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
                return Math.Round(0.827 * LeakArea * Math.Sqrt(12) * 1.25 * N2 * 3600);
            }
            set
            {
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 送风阀漏风面积和
        /// </summary>
        public double LeakArea
        {
            get
            {
                double leakarea = 0;
                int ValidFloorCount = FrontRoomTabControl.Count(f => f.FloorInfoItems.Any(d => d.DoorNum * d.DoorHeight * d.DoorWidth * d.DoorSpace != 0));
                foreach (var floor in FrontRoomTabControl)
                {
                    foreach (var door in floor.FloorInfoItems)
                    {
                        if (door.DoorNum * door.DoorHeight * door.DoorWidth * door.DoorSpace == 0)
                        {
                            continue;
                        }
                        if ((int)door.DoorType.Tag == 0)
                        {
                            leakarea += (door.DoorWidth + door.DoorHeight) * 2 * door.DoorSpace / 1000 * door.DoorNum;
                        }
                        else
                        {
                            leakarea += ((door.DoorWidth + door.DoorHeight) * 2 + door.DoorHeight) * door.DoorSpace / 1000 * door.DoorNum;
                        }
                    }
                }
                return ValidFloorCount == 0 ? leakarea : leakarea / ValidFloorCount;
            }
            set
            {
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 疏散门（前室）tab
        /// </summary>
        private ObservableCollection<TabControlInfo> _frontRoomTabControl = new ObservableCollection<TabControlInfo>();
        public ObservableCollection<TabControlInfo> FrontRoomTabControl
        {
            get { return _frontRoomTabControl; }
            set
            {
                _frontRoomTabControl = value;
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
            StairN1 = GetN1Value();
            OpenDorrAirSupply = OpenDorrAirSupply;
            VentilationLeakage = VentilationLeakage;
            LjTotal = LjTotal;
            FinalValue = FinalValue;
            if (checkValue != null)
            {
                checkValue(AAAA, BBBB);
            }
        }

        /// <summary>
        /// 更新N1值
        /// </summary>
        /// <returns></returns>
        private int GetN1Value()
        {
            if (StairPosition == StairPositionEnum.upGroud)
            {
                if (FloorType == FloorTypeEnum.lowFloor)
                {
                    return 2;
                }

                return 3;
            }
            else
            {
                if (BusinessType == BusinessTypeEnum.Residence)
                {
                    return 1;
                }
                return Math.Min(3, FloorNum);
            }

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

    public enum StairPositionEnum
    {
        upGroud = 0,

        downGroud = 1,
    }

    public enum BusinessTypeEnum
    {
        Residence = 0,

        Business = 1 ,
    }
}