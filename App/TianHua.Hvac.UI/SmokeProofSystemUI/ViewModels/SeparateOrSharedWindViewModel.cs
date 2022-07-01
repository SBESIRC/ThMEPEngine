﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;
using static TianHua.Hvac.UI.SmokeProofSystemUI.SmokeProofUserControl.SeparateOrSharedWindUserControl;

namespace TianHua.Hvac.UI.SmokeProofSystemUI.ViewModels
{
    class SeparateOrSharedWindViewModel : NotifyPropertyChangedBase
    {
        public CheckValue checkValue;
        /// <summary>
        /// 这是AK的值
        /// </summary>
        public double OverAk { get; set; }

        /// <summary>
        /// 这是AL的值
        /// </summary>
        public double OverAl { get; set; }

        public int AAAA = 24800, BBBB = 25800;
        public int CCCC = 26000, DDDD = 28100;

        private FloorTypeEnum _floorType;
        public FloorTypeEnum FloorType
        {
            get
            { return _floorType; }
            set
            {
                _floorType = value;
                MiddleWind = "18600-25800";
                HighWind = "19500-28100";
                RefreshData();
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// 系统楼层数
        /// </summary>
        private double _floorNum;
        public double FloorNum
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
                double V = 0.7;
                OverAk = Math.Round(OverAk, 2);
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
        /// 刷新计算数据
        /// </summary>
        public void RefreshData()
        {
            OpenDorrAirSupply = OpenDorrAirSupply;
            VentilationLeakage = VentilationLeakage;
            LjTotal = LjTotal;
            FinalValue = FinalValue;
            checkValue(AAAA, BBBB);
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
}