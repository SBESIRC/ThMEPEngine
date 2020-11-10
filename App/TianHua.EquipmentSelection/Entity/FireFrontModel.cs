using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.FanSelection.Model
{
    /// <summary>
    /// 消防电梯前室模型
    /// </summary>
    public class FireFrontModel : ThFanVolumeModel
    {
        /// <summary>
        /// 这是AK的值
        /// </summary>
        public double OverAk { get; set; }
        public int AAAA = 35400, BBBB = 36900;
        public int CCCC = 37100, DDDD = 40200;

        public enum LoadHeight
        {
            LoadHeightLow = 0,
            LoadHeightMiddle = 1,
            LoadHeightHigh = 2
        }
        public FireFrontModel()
        {
            FrontRoomDoors2 = new Dictionary<string, List<ThEvacuationDoor>>
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
                //double Ak = 0.0;
                OverAk = 0;
                int ValidFloorCount = 0;
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
                OverAk = Math.Round(OverAk,2);
                int V = 1;
                return Math.Round(OverAk * V * Math.Min(Count_Floor, 3) * 3600);
            }
        }
        /// <summary>
        /// 送风阀漏风
        /// </summary>
        public double LeakVolume
        {
            get
            {
                double Af = (double)Length_Valve * Width_Valve/1000000;
                int N3 = (Count_Floor - 3 > 0) ? (Count_Floor - 3) : 0;
                return Math.Round(0.083 * Af * N3 * 3600);
            }
        }
        /// <summary>
        /// 合计
        /// </summary>
        public override double TotalVolume
        {
            get
            {
                return DoorOpeningVolume + LeakVolume;
            }
        }
        /// <summary>
        /// 系统负担高度
        /// </summary>
        public LoadHeight Load { get; set; }

        /// <summary>
        /// 系统楼层数
        /// </summary>
        public int Count_Floor { get; set; }

        /// <summary>
        /// 前室门(楼层1，楼层2，楼层3)
        /// </summary>
        //public Dictionary<string, List<ThEvacuationDoor>> FrontRoomDoors2 { get; set; }

        public List<ThResult> Result { get; set; }
        /// <summary>
        /// 送风阀截面长
        /// </summary>
        public int  Length_Valve { get; set; }

        /// <summary>
        /// 送风阀截面宽
        /// </summary>
        public int Width_Valve { get; set; }

        /// <summary>
        /// 应用场景
        /// </summary>
        public override string FireScenario
        {
            get
            {
                return "消防电梯前室";
            }
        }
    }
}
