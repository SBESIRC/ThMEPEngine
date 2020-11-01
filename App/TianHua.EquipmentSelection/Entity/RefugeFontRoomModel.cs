using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.FanSelection.Model
{
    /// <summary>
    /// 避难走道前室模型
    /// </summary>
    public class RefugeFontRoomModel :ThFanVolumeModel
    {
        public double OverAk { get; set; }
        public RefugeFontRoomModel()
        {
            FrontRoomDoors2 = new Dictionary<string, List<ThEvacuationDoor>>()
            {
                {"楼层一",new List<ThEvacuationDoor>() },
                {"楼层二",new List<ThEvacuationDoor>() },
                {"楼层三",new List<ThEvacuationDoor>() }
            };
        }

        /// <summary>
        /// 门开启风量
        /// </summary>
        public double DoorOpeningVolume
        {
            get
            {
                int ValidFloorCount = 0;
                OverAk = 0;
                bool HasValidDoorInFloor = false;
                foreach (var floor in FrontRoomDoors2)
                {
                    foreach (var door in floor.Value)
                    {
                        if (door.Count_Door_Q * door.Height_Door_Q * door.Width_Door_Q == 0)
                        {
                            continue;
                        }
                        OverAk += door.Width_Door_Q * door.Height_Door_Q * door.Count_Door_Q;
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
        }
        /// <summary>
        /// 前室门
        /// </summary>
        //public Dictionary<string, List<ThEvacuationDoor>> FrontRoomDoors2 { get; set; }

        /// <summary>
        /// 前室门宽
        /// </summary>
        public double Width_Door_Q { get; set; }

        /// <summary>
        /// 前室门高
        /// </summary>
        public double Height_Door_Q { get; set; }

        /// <summary>
        /// 前室门数量
        /// </summary>
        public int Count_Door_Q { get; set; }

        /// <summary>
        /// 应用场景
        /// </summary>
        public override string FireScenario
        {
            get
            {
                return "避难走道前室";
            }
        }

    }
}
