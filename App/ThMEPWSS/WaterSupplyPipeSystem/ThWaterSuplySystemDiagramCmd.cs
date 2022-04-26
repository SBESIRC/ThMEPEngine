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
        
        public void ExecuteTest(WaterSupplyVM uiConfigs, Dictionary<string, List<string>> blockConfig)
        {
            /*
            var tmpUiConfigs = uiConfigs;
            if (tmpUiConfigs.SelectRadionButton.Content is null)
            {
                MessageBox.Show("不存在有效分组，请重新读取");
                return;
            }
            var insertPt = tmpUiConfigs.InsertPt;
            int areaIndex = 0;
            if (tmpUiConfigs.SelectRadionButton.Content.Contains("组"))
            {
                areaIndex = Convert.ToInt32(tmpUiConfigs.SelectRadionButton.Content.Split('组')[1]) + tmpUiConfigs.StartNum - 1;//读取分区
            }
             
            var setViewModel = tmpUiConfigs.SetViewModel;
            var layingMethod = (int)LayingMethod.Piercing;//敷设方式默认为穿梁
            if (setViewModel.LayingDynamicRadios[1].IsChecked)
            {
                layingMethod = (int)LayingMethod.Buried;//敷设方式为埋地
            }
            var FloorHeight = setViewModel.FloorLineSpace;  //楼层线间距 mm
            var FlushFaucet = ThWCompute.ExtractData(setViewModel.FaucetFloor, "冲洗龙头");//冲洗龙头层
            if (setViewModel.FaucetFloor != "")
            {
                var dataName = "冲洗龙头";
                FlushFaucet = ThWCompute.ExtractData(setViewModel.FaucetFloor, dataName);
                if (FlushFaucet.Count == 0)
                {
                    return;
                }
            }

            var NoPRValve = ThWCompute.ExtractData(setViewModel.NoCheckValve, "无减压阀");
            if (setViewModel.NoCheckValve != "")
            {
                var dataName = "无减压阀";
                NoPRValve = ThWCompute.ExtractData(setViewModel.NoCheckValve, dataName);
                if (NoPRValve.Count == 0)
                {
                    return;
                }
            }

            var selectedArea = tmpUiConfigs.SelectedArea;
            var floorAreaList = tmpUiConfigs.FloorAreaList;
            var floorNumList = tmpUiConfigs.FloorNumList;

            var floorNumbers = 1;//楼层数统计
            foreach (var fn in floorNumList)
            {
                if (fn.Max() > floorNumbers)
                {
                    floorNumbers = fn.Max();
                }
            }
            
            var QL = setViewModel.MaxDayQuota;  //最高日用水定额 QL            
            var Kh = Convert.ToDouble(setViewModel.MaxDayHourCoefficient.ToString("0.0"));  //最高日小时变化系数  Kh
            var m = Convert.ToDouble(setViewModel.NumberOfHouseholds.ToString("0.0"));   //每户人数  m

            //立管编号及对应的最低、最高层
            var pipeNumber = new List<String>();
            var lowestStorey = new List<int>();
            var highestStorey = new List<int>();
                       
            for (int i = 0; i < setViewModel.PartitionDatas.Count; i++)
            {
                if(Convert.ToInt32(setViewModel.PartitionDatas[i].MinimumFloorNumber) <= 0)
                {
                    continue;
                }
                pipeNumber.Add(setViewModel.PartitionDatas[i].RiserNumber);
                lowestStorey.Add(Convert.ToInt32(setViewModel.PartitionDatas[i].MinimumFloorNumber));
                highestStorey.Add(Convert.ToInt32(setViewModel.PartitionDatas[i].HighestFloorNumber));
                if(lowestStorey[i] > highestStorey[i])
                {
                    MessageBox.Show("当前行最底层的值大于最高层");
                    return;
                }
                if(lowestStorey[i] > floorNumbers || highestStorey[i] > floorNumbers)
                {
                    MessageBox.Show("水管楼层超过最高楼层");
                    return;
                }
            }
            var floorExist = new List<int>();
            var pipeFloorList = new List<int>();
            for(int i = 0; i < pipeNumber.Count; i++)
            {
                for(int j = lowestStorey[i]; j <= highestStorey[i]; j++)
                {
                    pipeFloorList.Add(j);
                    if (j == 0)
                    {
                        break;
                    }
                    if(floorExist.Contains(j))
                    {
                        MessageBox.Show("水管楼层重复");
                        return;
                    }
                    else
                    {
                        floorExist.Add(j);
                    }
                }
            }
            
            double floorLength = 20000;//楼板线长度
            var WaterEquivalent = new double[] { 0.5, 0.75, 1, 0.75, 1, 0.5, 1, 1.2 };//用水当量数

            using (Active.Document.LockDocument())//非模态框不能直接操作CAD，需要加锁
            using (var acadDatabase = AcadDatabase.Active()) 
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

                //统计厨房数
                var households = ThWCompute.CountKitchenNums(floorAreaList, selectedArea, floorNumList, floorNumbers);
                
                var maxHouseholds = ThWCompute.GetMaxHouseholds(households, FlushFaucet);

                bool cleanToolFlag = setViewModel.CleanToolDynamicRadios[0].IsChecked;
                //统计卫生洁具数
                var floorCleanToolList = ThWCompute.CountCleanToolNums(floorAreaList, households, floorNumList, selectedArea, 
                    notExistFloor, blockConfig, cleanToolFlag);

                WaterSuplyUtils.ImportNecessaryBlocks();//导入需要的模块
                var bt = acadDatabase.Element<BlockTable>(acadDatabase.Database.BlockTableId);//创建BlockTable
                var BlockSize = ThWCompute.CreateBlockSizeList(bt);//获取并添加 block 尺寸

                var maxHouseholdNums = 0;
                for (int i = 0; i < floorNumbers; i++)
                {
                    if (floorCleanToolList[i][areaIndex].GetHouseholdNums() > maxHouseholdNums)
                    {
                        maxHouseholdNums = floorCleanToolList[i][areaIndex].GetHouseholdNums();
                    }
                }

                var startPt = new Point3d(insertPt.X, insertPt.Y, 0);
                var floorheight = new int[] { 1800,2000,2200,2500,3000};
                //var floorheight = new int[] {3000 };
                int houseNum = 0;
                for(maxHouseholds = 7; maxHouseholds > 1; maxHouseholds--)
                {
                    int indxx = 0;
                    for (var flush = 0; flush < 2; flush++)
                    {
                        for(int fh = 0; fh < floorheight.Count(); fh++)
                        {
                            for(layingMethod = 0; layingMethod < 2; layingMethod ++)
                            {
                                if(maxHouseholds == 7 && flush == 1)
                                {
                                    continue;
                                }
                                if(flush == 1)
                                {
                                    FlushFaucet.Clear();
                                    for(int i = 1; i <=18;i++)
                                    {
                                        FlushFaucet.Add(i);
                                    }
                                }
                                try
                                {
                                    insertPt = startPt.OffsetXY(21000 * indxx, houseNum * 70000);
                                    indxx++;
                                    bool hasFlush = flush > 0;
                                    FloorHeight = floorheight[fh];
                                    //楼板线生成
                                    var StoreyList = ThWCompute.CreateStoreysList(floorNumbers, FloorHeight, FlushFaucet, NoPRValve, households);


                                    double T = 24;
                                    double PipeOffset_X = 10000; //第一根竖管相对于楼板起始 X 的偏移量
                                    double PipeGap = -600;  //竖管间的偏移量

                                    // 创建竖管系统列表
                                    var NGLIST = new List<double[]>();
                                    var U0LIST = new List<double[]>();
                                    var PipeSystem = ThWCompute.CreatePipeSystem(ref NGLIST, ref U0LIST, lowestStorey, highestStorey, PipeOffset_X,
                                        floorCleanToolList, areaIndex, PipeGap, WaterEquivalent, setViewModel, T, maxHouseholdNums, pipeNumber);


                                    //竖管对象绘制
                                    var pt = new List<double>();
                                    for (int i = 0; i < PipeSystem.Count; i++)
                                    {
                                        PipeSystem[i].DrawPipeLine(i, insertPt, FloorHeight, PipeSystem.Count);
                                        pt.Add(PipeSystem[i].GetPipeX());
                                    }

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

                                    //楼层线绘制
                                    for (int i = 0; i < floorNumbers + 1; i++)
                                    {
                                        StoreyList[i].DrawStorey(i, floorNumbers, insertPt, floorLength, highestStorey, PipeOffsetX);
                                    }

                                    //创建支管对象
                                    var BranchPipe = new List<ThWSSDBranchPipe>();

                                    for (int i = 0; i < floorNumbers; i++)
                                    {
                                        var HouseholdNum = floorCleanToolList[i][areaIndex].GetHouseholdNums();
                                        if (HouseholdNum == 0)
                                        {
                                            HouseholdNum = maxHouseholdNums;
                                        }
                                        double Ngi = ThWCompute.InnerProduct(floorCleanToolList[i][areaIndex].GetCleaningTools(), WaterEquivalent) / HouseholdNum;
                                        double U0i = 0;
                                        if (Math.Abs(Ngi) > 1e-6)
                                        {
                                            U0i = 100 * QL * m * Kh / (0.2 * Ngi * T * 3600);
                                        }

                                        var pipeCompute = new PipeCompute(U0i, Ngi * HouseholdNum);
                                        var DN = "";
                                        if (Math.Abs(Ngi) > 1e-6)
                                        {
                                            DN = pipeCompute.PipeDiameterCompute();
                                        }

                                        BranchPipe.Add(new ThWSSDBranchPipe(DN, StoreyList[i], insertPt.Y,
                                            PipeOffsetX[i], BlockSize, layingMethod, areaIndex, maxHouseholds,
                                            hasFlush));
                                    }
                                    //支管绘制
                                    for (int i = 0; i < BranchPipe.Count; i++)
                                    {
                                        if (pipeFloorList.Contains(i + 1))
                                        {
                                            BranchPipe[i].DrawBranchPipe();
                                        }
                                    }

                                    var layFlag = true;
                                    var elevateFlag = true;
                                    for (int i = 0; i < floorNumbers; i++)
                                    {
                                        if (pipeFloorList.Contains(i + 1))
                                        {
                                            Details.Add(i, acadDatabase, BranchPipe, ref layFlag, ref elevateFlag, floorCleanToolList, areaIndex,
                                                layingMethod, NoPRValve, highestStorey, floorNumbers, FlushFaucet, FloorHeight, insertPt);
                                        }
                                    }
                                }
                                catch
                                {
                                    ;
                                }

                            }
                        }
                    }
                    houseNum++;
                }



                
            }  
            */
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
            Active.Editor.WriteMessage($"seconds: {_stopwatch.Elapsed.TotalSeconds} \n");
        }
    }
}