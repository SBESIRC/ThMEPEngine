﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.FanSelection.Model
{
    /// <summary>
    /// 楼梯间（前室不送风）
    /// </summary>
    public class StaircaseNoAirModel :ThFanVolumeModel
    {
        /// <summary>
        /// 这是AK的值
        /// </summary>
        public double OverAk { get; set; }
        public int StairN1 = 0;
        public int AAAA = 36100, BBBB = 39200;
        public int CCCC = 39600, DDDD = 45800;

        public enum LoadHeight
        {
            LoadHeightLow = 0,
            LoadHeightMiddle = 1,
            LoadHeightHigh = 2
        }
        public enum StairLocation
        {
            OnGround = 0,
            UnderGound = 1,
        }

        public enum SpaceState
        {
            Residence = 0,
            Business = 1,
        }

        public StaircaseNoAirModel()
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
                OverAk = 0;
                int ValidFloorCount = 0;
                bool HasValidDoorInFloor = false;
                foreach (var floor in FrontRoomDoors2)
                {
                    foreach (var door in floor.Value)
                    {
                        if (door.Count_Door_Q * door.Height_Door_Q * door.Width_Door_Q * door.Crack_Door_Q == 0)
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
                return Math.Round(OverAk * V * StairN1 * 3600);
            }
        }

        /// <summary>
        /// N2
        /// </summary>
        public double N2 { get; set; }
        //public double N2
        //{
        //    get
        //    {
        //        int ValidFloorCount = FrontRoomDoors2.Count(f => f.Value.Any(d => d.Count_Door_Q * d.Crack_Door_Q * d.Height_Door_Q * d.Width_Door_Q != 0));
        //        double n2 = (Count_Floor - StairN1) * FrontRoomDoors2.Sum(
        //            f => f.Value.Where(
        //                d => d.Count_Door_Q * d.Crack_Door_Q * d.Height_Door_Q * d.Width_Door_Q != 0).Sum(d => d.Count_Door_Q));
        //        return n2 > 0 ? n2 / ValidFloorCount : 0;
        //    }
        //}

        /// <summary>
        /// 送风阀漏风面积和
        /// </summary>
        public double LeakArea
        {
            get
            {
                double leakarea = 0;
                int ValidFloorCount = FrontRoomDoors2.Count(f => f.Value.Any(d => d.Count_Door_Q * d.Height_Door_Q * d.Width_Door_Q * d.Crack_Door_Q != 0));
                foreach (var floor in FrontRoomDoors2)
                {
                    foreach (var door in floor.Value)
                    {
                        if (door.Count_Door_Q * door.Height_Door_Q * door.Width_Door_Q * door.Crack_Door_Q == 0)
                        {
                            continue;
                        }
                        if (door.Type == ThEvacuationDoorType.单扇)
                        {
                            leakarea += (door.Width_Door_Q + door.Height_Door_Q) * 2 * door.Crack_Door_Q / 1000 * door.Count_Door_Q;
                        }
                        else
                        {
                            leakarea += ((door.Width_Door_Q + door.Height_Door_Q) * 2 + door.Height_Door_Q) * door.Crack_Door_Q / 1000 * door.Count_Door_Q;
                        }
                    }
                }
                return ValidFloorCount == 0 ? leakarea : leakarea / ValidFloorCount;
            }
        }


        /// <summary>
        /// 送风阀漏风
        /// </summary>
        public double LeakVolume
        {
            get
            {
                return Math.Round(0.827 * LeakArea * Math.Sqrt(12) * 1.25 * N2 * 3600);
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
        /// 楼梯间位置
        /// </summary>
        public StairLocation Stair { get; set; }

        /// <summary>
        /// 空间业态
        /// </summary>
        public SpaceState Type_Area { get; set; }

        /// <summary>
        /// 前室门
        /// </summary>
        //public Dictionary<string, List<ThEvacuationDoor>> FrontRoomDoors2 { get; set; }

        /// <summary>
        /// 系统楼层数
        /// </summary>
        public int Count_Floor { get; set; }

        /// <summary>
        /// 应用场景
        /// </summary>
        public override string FireScenario
        {
            get
            {
                return "楼梯间（前室不送风）";
            }
        }


    }

}
