using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.WaterSupplyPipeSystem.Data;
using ThMEPWSS.WaterSupplyPipeSystem.Method;
using ThMEPWSS.WaterSupplyPipeSystem.model;

namespace ThMEPWSS.WaterSupplyPipeSystem.HalfFloorCase
{
    public class HalfFloor
    {
        public string LayingMethod { get; set; }//入户方式 true:穿梁  false:埋地
        public string SupplyFloor { get; set; }//供水楼层 true:+0.5  false:-0.5
        public string HalfFloorLayMethod { get; set; }//半平台敷设方式 true：穿梁 false：埋地
        public string EntryPoint { get; set; }// 入户点位置 true:入户门 false：半平台
        public string ThroughWaterWell { get; set; }// 是否穿水井底板

        public string FirstFloorMeterLocation { get; set; }//一层水表位置， true：大堂 false：半平台
        public string OutRoofStairwell { get; set; }//是否有出屋面楼梯间

        public string CaseCode { get; set; }
        public Dictionary<string, int> ValidCodeDic { get; set; }

       
        public HalfFloor(WaterSupplySetVM setVM)
        {
            CreateValidCodeDic();
            GetCaseCode(setVM);
        }

        private void CreateValidCodeDic()
        {
            ValidCodeDic = new Dictionary<string, int>();
            ValidCodeDic.Add("11110", 1);
            ValidCodeDic.Add("11010", 2);
            ValidCodeDic.Add("01110", 3);
            ValidCodeDic.Add("01010", 4);
            ValidCodeDic.Add("01000", 5);
            ValidCodeDic.Add("01100", 6);
            ValidCodeDic.Add("10000", 7);
            ValidCodeDic.Add("10100", 8);
            ValidCodeDic.Add("10110", 9);
            ValidCodeDic.Add("10010", 10);
            ValidCodeDic.Add("00110", 11);
            ValidCodeDic.Add("00010", 12);
            ValidCodeDic.Add("10111", 13);
            ValidCodeDic.Add("00111", 14);
        }
        private string BoolToStr(bool flag)
        {
            if (flag) return "1";
            else return "0";
        }
        private void GetCaseCode(WaterSupplySetVM setVM)
        {
            var halfVM = setVM.halfViewModel;

            LayingMethod = BoolToStr(setVM.LayingDynamicRadios[0].IsChecked);
            SupplyFloor = BoolToStr(halfVM.SupplyFloorDynamicRadios[0].IsChecked);
            HalfFloorLayMethod = BoolToStr(halfVM.HalfLayingDynamicRadios[0].IsChecked);
            EntryPoint = BoolToStr(halfVM.EntryLocationDynamicRadios[0].IsChecked);
            ThroughWaterWell = BoolToStr(halfVM.PipeThroughWellDynamicRadios[0].IsChecked);
            FirstFloorMeterLocation = BoolToStr(halfVM.FirstFloorMeterLocationDynamicRadios[0].IsChecked);
            OutRoofStairwell = BoolToStr(halfVM.OutRoofStairwellDynamicRadios[0].IsChecked);
            CaseCode =  LayingMethod + SupplyFloor + HalfFloorLayMethod + EntryPoint + ThroughWaterWell;
        }

        public bool IsValidCode()
        {
            return ValidCodeDic.ContainsKey(CaseCode);

        }
        public void Draw(SysIn sysIn, SysProcess sysProcess)
        {
            int halfType = ValidCodeDic[CaseCode];
            //创建支管对象
            var BranchPipe = new List<HalfBranchPipe>();

            for (int i = 0; i < sysIn.FloorNumbers; i++)
            {
                var HouseholdNum = Tool.GetHouseholdNum(i, sysIn, sysProcess);
                double Ngi = ThWCompute.InnerProduct(sysProcess.FloorCleanToolList[i][sysIn.AreaIndex].GetCleaningTools(), sysIn.WaterEquivalent) / HouseholdNum;
                double U0i = Ngi.ComputeU0i(sysIn);

                var DN = Tool.GetDN(U0i, Ngi, HouseholdNum);

                var halfBranchPipe = new HalfBranchPipe(i, DN, sysIn, sysProcess, halfType);
                if(i <= 1)//前两层
                {
                    var firstFloor = (i==0);//第一层
                    CaseChoose.Init1Floor(halfBranchPipe, halfType, FirstFloorMeterLocation, firstFloor);
                }
                else if(i > sysIn.FloorNumbers-3)//最后两层
                {
                    var upperFloor = (i == sysIn.FloorNumbers - 1);
                    CaseChoose.InitUpFloor(halfBranchPipe, halfType, OutRoofStairwell, upperFloor);
                }
                else
                {
                    CaseChoose.Init(halfBranchPipe, halfType);
                }

                

                BranchPipe.Add(halfBranchPipe);
            }
            //支管绘制
            for (int i = 0; i < BranchPipe.Count; i++)
            {
                if (sysIn.PipeFloorList.Contains(i + 1))
                {
                    BranchPipe[i].DrawBranchPipe();
                }
            }
            var layFlag = true;
            var elevateFlag = true;
            for (int i = 0; i < sysIn.FloorNumbers; i++)
            {
                if (sysIn.PipeFloorList.Contains(i + 1))
                {
                    Details.Add(i, BranchPipe, ref layFlag, ref elevateFlag, sysIn, sysProcess);
                }
            }


            HalfBranchPipe.Draw(sysIn, sysProcess);
            return;
        }
    }
}
