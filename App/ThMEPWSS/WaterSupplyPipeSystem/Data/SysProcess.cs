using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.WaterSupplyPipeSystem.Method;
using ThMEPWSS.WaterSupplyPipeSystem.model;

namespace ThMEPWSS.WaterSupplyPipeSystem.Data
{
    public class SysProcess
    {
        public List<int[]> Households { get; set; }//厨房数
        public int MaxHouseholds { get; set; }//最大厨房数
        public List<List<CleaningToolsSystem>> FloorCleanToolList { get; set; }//最大厨房数
        public List<ThWSSDStorey> StoreyList { get; set; }//楼板线
        public int MaxHouseholdNums { get; set; }//
        public double[] PipeOffsetX { get; set; }
        public List<double> BranchPipeX { get; set; }
        public List<double[]> NGLIST { get; set; }
        public List<double[]> U0LIST { get; set; }


        public SysProcess()
        {
             NGLIST = new List<double[]>();
             U0LIST = new List<double[]>();
            BranchPipeX = new List<double>();
        }


        public void Set(SysIn sysIn)
        {
            Households = ThWCompute.CountKitchenNums(sysIn);
            MaxHouseholds = ThWCompute.GetMaxHouseholds(Households, sysIn.FlushFaucet);
            FloorCleanToolList = ThWCompute.CountCleanToolNums(sysIn, Households);
            StoreyList = ThWCompute.CreateStoreysList(sysIn, Households);
            MaxHouseholdNums = Tool.GetMaxHouseholdNums(sysIn, FloorCleanToolList);
            PipeOffsetX = Tool.CreatePipeOffsetX(sysIn.FloorNumbers, sysIn.LowestStorey, sysIn.InsertPt);
            GetBranchPipeX();
        }

        public void SetTank(SysIn sysIn)
        {
            Households = ThWCompute.CountKitchenNums(sysIn);
            MaxHouseholds = ThWCompute.GetMaxHouseholds(Households, sysIn.FlushFaucet);
            FloorCleanToolList = ThWCompute.CountCleanToolNums(sysIn, Households);
            MaxHouseholdNums = Tool.GetMaxHouseholdNums(sysIn, FloorCleanToolList);
        }


        private void GetBranchPipeX()
        {
            double lastPipeOffsetX = PipeOffsetX[0];
            for (int i =0; i < PipeOffsetX.Length;i++)
            {
                if(i==0)
                {
                    BranchPipeX.Add(400);
                }
                else
                {
                    if(PipeOffsetX[i]!=lastPipeOffsetX)
                    {
                        BranchPipeX.Add(BranchPipeX.Last()+600);
                    }
                    else
                    {
                        BranchPipeX.Add(BranchPipeX.Last());
                    }
                }
                lastPipeOffsetX = PipeOffsetX[i];
            }
        }
    }
}
