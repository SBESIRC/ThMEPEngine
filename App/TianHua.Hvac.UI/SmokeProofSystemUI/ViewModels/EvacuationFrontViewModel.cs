using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThControlLibraryWPF.ControlUtils;

namespace TianHua.Hvac.UI.SmokeProofSystemUI.ViewModels
{
    class EvacuationFrontViewModel : NotifyPropertyChangedBase
    {
        /// <summary>
        /// 这是AK的值
        /// </summary>
        public double OverAk { get; set; }

        /// <summary>
        /// 门开启风量
        /// </summary>
        public double OpenDorrAirSupply
        {
            get
            {
                int ValidFloorCount = 0;
                OverAk = 0;
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
                int V = 1;
                OverAk = Math.Round(OverAk, 2);
                return Math.Round(OverAk * V * 3600);
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
        /// 刷新计算数据
        /// </summary>
        public void RefreshData()
        {
            OpenDorrAirSupply = OpenDorrAirSupply;
        }
    }
}
