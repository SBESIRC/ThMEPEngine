using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ThMEPEngineCore.Command;
using ThMEPWSS.Diagram.ViewModel;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.Uitl.ExtensionsNs;
using ThMEPWSS.WaterSupplyPipeSystem;
using ThMEPWSS.WaterSupplyPipeSystem.model;
using ThMEPWSS.WaterSupplyPipeSystem.tool;
using ThMEPWSS.WaterSupplyPipeSystem.Method;
using ThMEPWSS.WaterSupplyPipeSystem.Data;
using ThMEPWSS.WaterSupplyPipeSystem.HalfFloorCase;

namespace ThMEPWSS.Command
{
    public class ThWaterSuplySystemDiagramCmd : ThMEPBaseCommand, IDisposable
    {
        readonly WaterSupplyVM _UiConfigs;
        Dictionary<string, List<string>> BlockConfig;

        public ThWaterSuplySystemDiagramCmd(WaterSupplyVM uiConfigs, Dictionary<string, List<string>> blockConfig)
        {
            _UiConfigs = uiConfigs;
            BlockConfig = blockConfig;
            CommandName = "THJSXTT";
            ActionName = "生成";
        }
        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            try
            {
                Execute(_UiConfigs, BlockConfig);
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }

        public void Execute(WaterSupplyVM uiConfigs, Dictionary<string, List<string>> blockConfig)
        {
            var tmpUiConfigs = uiConfigs;
            
            if (tmpUiConfigs.SelectRadionButton.Content is null)
            {
                MessageBox.Show("不存在有效分组，请重新读取");
                return;
            }
            var isHalfFloor = tmpUiConfigs.SetViewModel.MeterType.Equals(WaterMeterLocation.HalfFloor);//布置方式
            var prValveStyle = tmpUiConfigs.SetViewModel.PRValveStyleDynamicRadios[1].IsChecked;//true表示一户一阀
            using (Active.Document.LockDocument())//非模态框不能直接操作CAD，需要加锁
            using (var acadDatabase = AcadDatabase.Active())
            {
                var halfFloor = new HalfFloor(tmpUiConfigs.SetViewModel);
                if(!halfFloor.IsValidCode() && isHalfFloor)
                {
                    MessageBox.Show("所选模板未在数据库");
                    return;
                }
                var sysIn = new SysIn();
                sysIn.Set(acadDatabase, tmpUiConfigs, blockConfig);
                var sysProcess = new SysProcess();
                sysProcess.Set(sysIn);

                
                if(isHalfFloor)//半平台绘制
                {
                    // 创建竖管系统列表
                    var PipeSystem = ThWCompute.CreatePipeSystem(sysIn, sysProcess);

                    //竖管对象绘制
                    var pt = new List<double>();
                    for (int i = 0; i < PipeSystem.Count; i++)
                    {
                        var highestFlag = isHalfFloor && i== PipeSystem.Count-1;
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

                        var DN = Tool.GetDN(U0i, Ngi, HouseholdNum);

                        BranchPipe.Add(new ThWSSDBranchPipe(i, DN, sysIn, sysProcess, prValveStyle));
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

        public override void AfterExecute()
        {
            Active.Editor.WriteLine($"seconds: {_stopwatch.Elapsed.TotalSeconds}");
            base.AfterExecute();
        }
    }
}