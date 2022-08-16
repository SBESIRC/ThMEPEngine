using AcHelper;
using Linq2Acad;
using System.Collections.Generic;
using System.Windows.Forms;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.WaterSupplyPipeSystem.model;
using ThMEPWSS.WaterSupplyPipeSystem.Method;
using ThMEPWSS.WaterSupplyPipeSystem.Data;
using ThMEPWSS.WaterSupplyPipeSystem.HalfFloorCase;
using ThMEPWSS.UndergroundSpraySystem.Service;

namespace ThMEPWSS.WaterSupplyPipeSystem.Command
{
    public class WaterSupplyCmd
    {
        public static void Execute(WaterSupplyVM uiConfigs, Dictionary<string, List<string>> blockConfig)
        {
            BlocksImport.ImportElementsFromStdDwg();

            var isHalfFloor = uiConfigs.SetViewModel.MeterType.Equals(WaterMeterLocation.HalfFloor);//布置方式
            var prValveStyle = uiConfigs.SetViewModel.PRValveStyleDynamicRadios[1].IsChecked;//true表示一户一阀
            using (Active.Document.LockDocument())//非模态框不能直接操作CAD，需要加锁
            using (var acadDatabase = AcadDatabase.Active())
            {
                var halfFloor = new HalfFloor(uiConfigs.SetViewModel);
                if (!halfFloor.IsValidCode() && isHalfFloor)
                {
                    MessageBox.Show("所选模板未在数据库");
                    return;
                }
                var sysIn = new SysIn();
                sysIn.Set(acadDatabase, (WaterSupplyVM)uiConfigs, blockConfig);
                var sysProcess = new SysProcess();
                sysProcess.Set(sysIn);

                if (isHalfFloor)//半平台绘制
                {
                    // 创建竖管系统列表
                    var PipeSystem = ThWCompute.CreatePipeSystem(sysIn, sysProcess);

                    //竖管对象绘制
                    var pt = new List<double>();
                    for (int i = 0; i < PipeSystem.Count; i++)
                    {
                        var highestFlag = isHalfFloor && i == PipeSystem.Count - 1;
                        PipeSystem[i].DrawPipeLine(i, sysIn.InsertPt, sysIn.FloorHeight, PipeSystem.Count, highestFlag);
                        pt.Add(PipeSystem[i].GetPipeX());
                    }
                    //楼层线绘制
                    for (int i = 0; i < sysIn.FloorNumbers + 1; i++)
                    {
                        sysProcess.StoreyList[i].DrawHalfFloorStorey(i, sysIn, sysProcess);
                    }

                    halfFloor.Draw(sysIn, sysProcess, prValveStyle);
                }
                else//绘制同层布置
                {
                    // 创建竖管系统列表
                    var PipeSystem = ThWCompute.CreatePipeSystem(sysIn, sysProcess);

                    //竖管对象绘制
                    var pt = new List<double>();
                    for (int i = 0; i < PipeSystem.Count; i++)
                    {
                        PipeSystem[i].DrawPipeLine(i, sysIn.InsertPt, sysIn.FloorHeight, PipeSystem.Count);
                        pt.Add(PipeSystem[i].GetPipeX());
                    }

                    //楼层线绘制
                    for (int i = 0; i < sysIn.FloorNumbers + 1; i++)
                    {
                        sysProcess.StoreyList[i].DrawStorey(i, sysIn);
                    }

                    //创建支管对象
                    var BranchPipe = new List<ThWSSDBranchPipe>();

                    for (int i = 0; i < sysIn.FloorNumbers; i++)
                    {
                        var HouseholdNum = Tool.GetHouseholdNum(i, sysIn, sysProcess);
                        double Ngi = ThWCompute.InnerProduct(sysProcess.FloorCleanToolList[i][sysIn.AreaIndex].GetCleaningTools(), sysIn.WaterEquivalent) / HouseholdNum;
                        double U0i = Ngi.ComputeU0i(sysIn);

                        //var DN = Tool.GetDN(U0i, Ngi, HouseholdNum);

                        BranchPipe.Add(new ThWSSDBranchPipe(i, "", sysIn, sysProcess, prValveStyle));
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
                            Details.Add(i, acadDatabase, BranchPipe, ref layFlag, ref elevateFlag, sysIn, sysProcess, prValveStyle);
                        }
                    }
                }
            }
        }
    }
}
