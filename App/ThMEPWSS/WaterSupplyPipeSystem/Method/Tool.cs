using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.WaterSupplyPipeSystem.Data;
using ThMEPWSS.WaterSupplyPipeSystem.model;
using ThMEPWSS.WaterSupplyPipeSystem.tool;
using ThMEPWSS.WaterSupplyPipeSystem.ViewModel;

namespace ThMEPWSS.WaterSupplyPipeSystem.Method
{
    public static class Tool
    {
        public static int GetPrValveGroupFloor(int i, WaterSupplyVM uiConfigs, List<int[]> households)
        {
            var prValveStyle = uiConfigs.SetViewModel.PRValveStyleDynamicRadios[1].IsChecked;//true表示一户一阀
            var lowestFloor = uiConfigs.SetViewModel.tankViewModel.LowestFloor;
            for(int j =0; j < households.Count;j++)
            {
                if (households[j][i]>0)
                {
                    lowestFloor += j;
                    break;
                }
            }
            double tankElevation = uiConfigs.SetViewModel.tankViewModel.Elevation;
            double floorHeight = uiConfigs.SetViewModel.FloorLineSpace/1000;
            int floorNum = (int)Math.Floor((tankElevation - 42) / floorHeight) + 1;
            if(prValveStyle)
            {
                return Math.Max(floorNum, 3 + lowestFloor);
            }
            else
            {
                return Math.Max(floorNum, 4 + lowestFloor);
            }
            
        }

        public static int GetAreaIndex(WaterSupplyVM uiConfigs)
        {
            var areaIndex = 0;
            if (uiConfigs.SelectRadionButton.Content.Contains("组"))
            {
                areaIndex = Convert.ToInt32(uiConfigs.SelectRadionButton.Content.Split('组')[1]) + uiConfigs.StartNum - 1;//读取分区
            }
            return areaIndex;
        }


        public static bool GetFloorExist(List<int> highestStorey, List<int> lowestStorey, List<string> pipeNumber,
            out List<int> floorExist, out List<int> pipeFloorList)
        {
            floorExist = new List<int>();
            pipeFloorList = new List<int>();
            for (int i = 0; i < pipeNumber.Count; i++)
            {
                for (int j = lowestStorey[i]; j <= highestStorey[i]; j++)
                {
                    pipeFloorList.Add(j);
                    if (j == 0)
                    {
                        break;
                    }
                    if (floorExist.Contains(j))
                    {
                        MessageBox.Show("水管楼层重复");
                        return false;
                    }
                    else
                    {
                        floorExist.Add(j);
                    }
                }
            }
            return true;
        }


        public static List<int> GetNotExistFloor(int floorNumbers, List<List<int>> floorNumList)
        {
            var notExistFloor = new List<int>();//不存在的楼层号列表
            for (int i = 0; i < floorNumbers; i++)
            {
                var hasNum = false;
                foreach (var f in floorNumList)
                {
                    if (f.Contains(i + 1))
                    {
                        hasNum = true;
                        break;
                    }
                }
                if (!hasNum)
                {
                    notExistFloor.Add(i + 1);
                }
            }
            return notExistFloor;
        }


        public static double[] CreatePipeOffsetX(int floorNumbers, List<int> lowestStorey, Point3d insertPt)
        {
            double PipeOffset_X = 10000; //第一根竖管相对于楼板起始 X 的偏移量
            double PipeGap = -600;  //竖管间的偏移量

            //创建支管偏移数组
            double[] PipeOffsetX = new double[floorNumbers];
            for (int i = 0; i < PipeOffsetX.Length; i++)
            {
                PipeOffsetX[i] = PipeOffset_X + insertPt.X;
                for (int j = 1; j < lowestStorey.Count; j++)
                {
                    if (i + 1 >= lowestStorey[j])
                    {
                        PipeOffsetX[i] += PipeGap;
                    }
                }
            }
            return PipeOffsetX;
        }


        public static List<int> GetFlushFaucet(WaterSupplySetVM setViewModel, out bool rstFlush)
        {
            rstFlush = true;
            var FlushFaucet = ThWCompute.ExtractData(setViewModel.FaucetFloor, "冲洗龙头");//冲洗龙头层
            if (setViewModel.FaucetFloor != "")
            {
                var dataName = "冲洗龙头";
                FlushFaucet = ThWCompute.ExtractData(setViewModel.FaucetFloor, dataName);
                if (FlushFaucet.Count == 0)
                {
                    rstFlush = false;
                    return new List<int>();
                }
            }
            return FlushFaucet;
        }


        public static List<int> GetNoPRValve(WaterSupplySetVM setViewModel, out bool rstNoPRValve)
        {
            rstNoPRValve = true;
            var NoPRValve = ThWCompute.ExtractData(setViewModel.NoCheckValve, "无减压阀");
            if (setViewModel.NoCheckValve != "")
            {
                var dataName = "无减压阀";
                NoPRValve = ThWCompute.ExtractData(setViewModel.NoCheckValve, dataName);
                if (NoPRValve.Count == 0)
                {
                    rstNoPRValve = false;
                    return new List<int>();
                }
            }
            return NoPRValve;
        }


        public static int GetFloorNumbers(List<List<int>> floorNumList)
        {
            var floorNumbers = 1;//楼层数统计
            foreach (var fn in floorNumList)
            {
                if (fn.Max() > floorNumbers)
                {
                    floorNumbers = fn.Max();
                }
            }
            return floorNumbers;
        }


        public static bool GetLowHighStorey(WaterSupplySetVM setViewModel, int floorNumbers, out List<string> pipeNumber,
            out List<int> lowestStorey, out List<int> highestStorey)
        {
            //立管编号及对应的最低、最高层
            pipeNumber = new List<string>();
            lowestStorey = new List<int>();
            highestStorey = new List<int>();

            for (int i = 0; i < setViewModel.PartitionDatas.Count; i++)
            {
                if (Convert.ToInt32(setViewModel.PartitionDatas[i].MinimumFloorNumber) <= 0)
                {
                    continue;
                }
                pipeNumber.Add(setViewModel.PartitionDatas[i].RiserNumber);
                lowestStorey.Add(Convert.ToInt32(setViewModel.PartitionDatas[i].MinimumFloorNumber));
                highestStorey.Add(Convert.ToInt32(setViewModel.PartitionDatas[i].HighestFloorNumber));
                if (lowestStorey[i] > highestStorey[i])
                {
                    MessageBox.Show("当前行最底层的值大于最高层");
                    return false;
                }
                if (lowestStorey[i] > floorNumbers || highestStorey[i] > floorNumbers)
                {
                    MessageBox.Show("水管楼层超过最高楼层");
                    return false;
                }
            }

            return true;
        }


        public static int GetMaxHouseholdNums(SysIn sysIn, List<List<CleaningToolsSystem>> floorCleanToolList)
        {
            var maxHouseholdNums = 0;
            var areaIndex = sysIn.AreaIndex;
            for (int i = 0; i < sysIn.FloorNumbers; i++)
            {
                if (floorCleanToolList[i][areaIndex].GetHouseholdNums() > maxHouseholdNums)
                {
                    maxHouseholdNums = floorCleanToolList[i][areaIndex].GetHouseholdNums();
                }
            }
            return maxHouseholdNums;
        }


        public static int GetHouseholdNum(int i, SysIn sysIn, SysProcess sysProcess)
        {
            var floorCleanToolList = sysProcess.FloorCleanToolList;
            var areaIndex = sysIn.AreaIndex;
            var HouseholdNum = floorCleanToolList[i][areaIndex].GetHouseholdNums();
            if (HouseholdNum == 0)
            {
                HouseholdNum = sysProcess.MaxHouseholdNums;
            }
            return HouseholdNum;
        }

        public static int GetHouseholdNum(int i, int areaIndex, SysIn sysIn, SysProcess sysProcess)
        {
            var floorCleanToolList = sysProcess.FloorCleanToolList;
            var HouseholdNum = floorCleanToolList[i][areaIndex].GetHouseholdNums();
            //if (HouseholdNum == 0)
            //{
            //    HouseholdNum = sysProcess.MaxHouseholdNums;
            //}
            return HouseholdNum;
        }


        public static double ComputeU0i(this double Ngi, SysIn sysIn)
        {
            double U0i = 0;
            if (Math.Abs(Ngi) > 1e-6)
            {
                U0i = 100 * sysIn.MaxDayQuota * sysIn.NumberOfHouseholds * sysIn.MaxDayHourCoefficient / (0.2 * Ngi * sysIn.T * 3600);
            }
            return U0i;
        }


        public static string GetDN(double U0i, double Ngi, int HouseholdNum, out double qg)
        {
            var pipeCompute = new PipeCompute(U0i, Ngi);
            var DN = "";
            qg = 0;
            if (Math.Abs(Ngi) > 1e-6)
            {
                DN = pipeCompute.PipeDiameterCompute(out double qg1);
                qg = qg1;
            }
            return DN;
        }

        public static string GetDN(double U0i, double Ngi)
        {
            var pipeCompute = new PipeCompute(U0i, Ngi);
            var DN = "";
            if (Math.Abs(Ngi) > 1e-6)
            {
                DN = pipeCompute.PipeDiameterCompute(out double qg1);
            }
            return DN;
        }

        public static string GetDN(double U0i, double Ngi, int HouseholdNum)
        {
            var pipeCompute = new PipeCompute(U0i, Ngi);
            var DN = "";
            if (Math.Abs(Ngi) > 1e-6)
            {
                DN = pipeCompute.PipeDiameterCompute();
            }
            return DN;
        }
    }
}
