using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.WaterSupplyPipeSystem.HalfFloorCase
{
    public static class CaseChoose
    {
        public static void Init(HalfBranchPipe halfBranchPipe, int halfType)
        {
            switch (halfType)
            {
                case 1:
                    Case1.Init(halfBranchPipe);
                    break;
                case 2:
                    Case2.Init(halfBranchPipe);
                    break;
                case 3:
                    Case3.Init(halfBranchPipe);
                    break;
                case 4:
                    Case4.Init(halfBranchPipe);
                    break;
                case 5:
                    Case5.Init(halfBranchPipe);
                    break;
                case 6:
                    Case6.Init(halfBranchPipe);
                    break;
                case 7:
                    Case7.Init(halfBranchPipe);
                    break;
                case 8:
                    Case8.Init(halfBranchPipe);
                    break;
                case 9:
                    Case9.Init(halfBranchPipe);
                    break;
                case 10:
                    Case10.Init(halfBranchPipe);
                    break;
                case 11:
                    Case11.Init(halfBranchPipe);
                    break;
                case 12:
                    Case12.Init(halfBranchPipe);
                    break;
                case 13:
                    Case13.Init(halfBranchPipe);
                    break;
                case 14:
                    Case14.Init(halfBranchPipe);
                    break;
            }
        }

        public static void Init1Floor(HalfBranchPipe halfBranchPipe, int halfType, string firstFloorMeterLocation, bool firstFloor)
        {
            switch (halfType)
            {
                case 1:
                    Case1.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                    break;
                case 2:
                    Case2.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                    break;
                case 3:
                    Case3.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                    break;
                case 4:
                    Case4.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                    break;
                case 5:
                    Case5.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                    break;
                case 6:
                    Case6.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                    break;
                case 7:
                    Case7.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                    break;
                case 8:
                    Case8.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                    break;
                case 9:
                    Case9.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                    break;
                case 10:
                    Case10.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                    break;
                case 11:
                    Case11.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                    break;
                case 12:
                    Case12.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                    break;
                case 13:
                    Case13.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                    break;
                case 14:
                    Case14.Init1Floor(halfBranchPipe, firstFloorMeterLocation, firstFloor);
                    break;
            }
        }

        public static void InitUpFloor(HalfBranchPipe halfBranchPipe, int halfType, string outRoofStairwell, bool upperFloor)
        {
            if(halfType > 6 && outRoofStairwell.Equals("0"))//-0.5 && 无出屋面楼梯间
            {
                switch (halfType)
                {
                    case 7:
                        Case7.InitUpFloor(halfBranchPipe, upperFloor);
                        break;
                    case 8:
                        Case8.InitUpFloor(halfBranchPipe, upperFloor);
                        break;
                    case 9:
                        Case9.InitUpFloor(halfBranchPipe, upperFloor);
                        break;
                    case 10:
                        Case10.InitUpFloor(halfBranchPipe, upperFloor);
                        break;
                    case 11:
                        Case11.InitUpFloor(halfBranchPipe, upperFloor);
                        break;
                    case 12:
                        Case12.InitUpFloor(halfBranchPipe, upperFloor);
                        break;
                    case 13:
                        Case13.InitUpFloor(halfBranchPipe, upperFloor);
                        break;
                    case 14:
                        Case14.InitUpFloor(halfBranchPipe, upperFloor);
                        break;
                }
            }
            else
            {
                Init(halfBranchPipe, halfType);
            }
            
        }
    }
}
