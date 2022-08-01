using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPArchitecture.ViewModel;
using ThMEPEngineCore.Command;
using ThParkingStall.Core.IO;
using Autodesk.AutoCAD.ApplicationServices;
using AcHelper;
using ThMEPArchitecture.ParkingStallArrangement.General;
using Linq2Acad;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThParkingStall.Core.OInterProcess;
using Serilog;
using System.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPArchitecture.ParkingStallArrangement.PreProcess;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.PartitionLayout;
using Autodesk.AutoCAD.Geometry;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.PostProcess;
using ThMEPEngineCore;
using ThParkingStall.Core.InterProcess;

namespace ThMEPArchitecture.MultiProcess
{
    public class ThOArrangementCmd : ThMEPBaseCommand, IDisposable
    {
        public static string LogFileName = Path.Combine(GetPath.GetAppDataPath(), "MPLog.txt");

        public static string DisplayLogFileName = Path.Combine(System.IO.Path.GetTempPath(), "DisplayLog.txt");
        public static string DisplayLogFileName2 = Path.Combine(System.IO.Path.GetTempPath(), "DisplayLog2.txt");
        public Serilog.Core.Logger Logger = null;
        DisplayInfo displayInfo;
        public static Serilog.Core.Logger DisplayLogger = null;//用于记录信息日志
        public Serilog.Core.Logger DisplayLogger2 = null;//用于记录信息日志

        public string DrawingName;
        public static ParkingStallArrangementViewModel ParameterViewModel { get; set; }
        private CommandMode _CommandMode { get; set; } = CommandMode.WithoutUI;
        //public ThMPArrangementCmd()//debug 读取基因直排
        //{
        //    CommandName = "-THDJCCWBZ";
        //    ActionName = "生成";
        //    _CommandMode = CommandMode.WithoutUI;
        //    ParameterViewModel = new ParkingStallArrangementViewModel();
        //}
        public ThOArrangementCmd(ParkingStallArrangementViewModel vm)
        {
            CommandName = "THZDCWBZ";
            ActionName = "手动分区线迭代生成";
            ParameterViewModel = vm;
            _CommandMode = CommandMode.WithUI;
        }
        public override void SubExecute()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            DrawingName = Path.GetFileName(doc.Name);

            System.IO.FileStream emptyStream = new System.IO.FileStream(DisplayLogFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            emptyStream.Close();
            System.IO.FileStream emptyStream2 = new System.IO.FileStream(DisplayLogFileName2, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            emptyStream2.Close();
            ParameterStock.Set(ParameterViewModel);
            Utils.SetSeed();
            try
            {
                using (var docLock = Active.Document.LockDocument())
                using (AcadDatabase currentDb = AcadDatabase.Active())
                {
                    var saveDoc = true;
                    if (_CommandMode == CommandMode.WithoutUI)
                    {
                        Logger?.Information($"DEbug--读取复现");
                        //RunDebug();
                    }
                    else
                    {
                        if (ParameterViewModel.CommandType == CommandTypeEnum.RunWithoutIteration)
                        {
                            saveDoc = false;
                            RunDirect(currentDb);
                        }
                        else if (ParameterViewModel.CommandType == CommandTypeEnum.RunWithIteration)
                        {
                            //Run(currentDb);
                        }
                        else
                        {
                            //RunWithAutoSegLine(currentDb);
                        }
                    }
                    //TableTools.EraseOrgTable();
                    //TableTools.hideOrgTable();
                    if (saveDoc)
                        Active.Document.Save();
                }
            }
            catch (Exception ex)
            {
                DisplayLogger2?.Information(ex.Message);
                DisplayLogger2?.Information("程序出错！");
                Logger?.Information(ex.Message);
                Logger?.Information("##################################");
                Logger?.Information(ex.StackTrace);
                Active.Editor.WriteMessage(ex.Message);
            }
            finally
            {
                DisplayLogger?.Information($"总用时: {_stopwatch.Elapsed.TotalMinutes} 分\n");
                DisplayLogger?.Information($"地库程序运行结束 \n");
                DisplayLogger?.Dispose();
                DisplayLogger2?.Dispose();
            }
        }

        public override void AfterExecute()
        {
            base.AfterExecute();
            Active.Editor.WriteMessage($"总运行时间: {_stopwatch.Elapsed.TotalSeconds}秒 \n");
            Logger?.Information($"总运行时间: {_stopwatch.Elapsed.TotalSeconds}秒 \n");
            base.AfterExecute();
        }

        public void RunDirect(AcadDatabase acadDatabase)
        {
            var blks = InputData.SelectBlocks(acadDatabase);
            //var block = InputData.SelectBlock(acadDatabase);//提取地库对象
            //var MultiSolutionList = ParameterViewModel.GetMultiSolutionList();
            var MultiSolutionList = new List<int> { 0 };
            if (blks == null) return;
            foreach (var blk in blks)
            {
                var blkName = blk.GetEffectiveName();
                UpdateLogger(blkName);
                Logger?.Information("块名：" + blkName);
                Logger?.Information("文件名：" + DrawingName);
                Logger?.Information("用户名：" + Environment.UserName);
                var layoutData = new OLayoutData(blk, Logger, out bool succeed);
                if (!succeed) return;
                layoutData.ProcessSegLines();
                layoutData.SetInterParam();
                for (int i = 0; i < MultiSolutionList.Count; i++)
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    ParameterStock.RunMode = MultiSolutionList[i];
                    var dataWraper = new DataWraper();
                    dataWraper.UpdateVMParameter(ParameterViewModel);
                    VMStock.Init(dataWraper);
                    ProcessAndDisplay(null,i,stopWatch);
                }
            }
        }

        private void ProcessAndDisplay(List<double> solution, int SolutionID = 0, Stopwatch stopWatch = null)
        {
            var moveDistance = SolutionID * 2 * (OInterParameter.TotalArea.Coordinates.Max(c => c.X) -
                                                OInterParameter.TotalArea.Coordinates.Min(c => c.X));
            var subAreas = OInterParameter.GetOSubAreas(solution);
            subAreas.ForEach(s =>s.UpdateParkingCnts());
            var ParkingStallCount = subAreas.Where(s =>s.Count>0).Sum(s =>s.Count);
            foreach(var subArea in subAreas)
            {
                MultiProcessTestCommand.DisplayMParkingPartitionPros(subArea.obliqueMPartition.ConvertToMParkingPartitionPro());
                //subArea.obliqueMPartition.IniLanes.Select(e => e.Line.ToDbLine()).AddToCurrentSpace();
            }
#if DEBUG
            for (int i = 0; i < subAreas.Count; i++)
            {
                var subArea = subAreas[i];
                subArea.Display("MPDebug");
            }
#endif
            var strBest = $"最大车位数{ParkingStallCount}\n";
            Logger?.Information(strBest);
            Active.Editor.WriteMessage(strBest);
            //if (ParameterViewModel.ShowSubAreaTitle) subAreas.ForEach(area => area.ShowText());
            if (stopWatch != null)
            {
                Logger?.Information($"单地库用时: {stopWatch.Elapsed.TotalSeconds}秒 \n");
                DisplayLogger?.Information($"最大车位数: {ParkingStallCount}");
                var areaPerStall = ParameterStock.TotalArea / ParkingStallCount;
                DisplayLogger?.Information("车均面积: " + string.Format("{0:N2}", areaPerStall) + "平方米/辆");
                DisplayLogger?.Information($"单地库用时: {stopWatch.Elapsed.TotalMinutes} 分\n");

                if (ParameterViewModel.ShowTitle) ShowTitle(ParkingStallCount, areaPerStall, stopWatch.Elapsed.TotalSeconds);
                //if (ParameterViewModel.ShowTable)
                //{
                //    var minY = OInterParameter.TotalArea.Coordinates.Min(c => c.Y);
                //    var midX = (OInterParameter.TotalArea.Coordinates.Max(c => c.X) +
                //        OInterParameter.TotalArea.Coordinates.Min(c => c.X)) / 2;
                //    TableTools.ShowTables(new Point3d(midX, minY - 20000, 0), ParkingStallCount);
                //}
                if (displayInfo != null)
                {
                    displayInfo.FinalStalls = $"最大车位数: {ParkingStallCount} ";
                    displayInfo.FinalAveAreas = "车均面积: " + string.Format("{0:N2}", areaPerStall) + "平方米/辆";
                    displayInfo.CostTime = $"单地库用时: {stopWatch.Elapsed.TotalMinutes} 分\n";
                }
            }
            DisplayParkingStall.MoveAddedEntities(moveDistance);
            //SubAreaParkingCnt.Clear();
            ReclaimMemory();
        }

        private void ShowTitle(int ParkingStallCount, double areaPerStall, double TotalSeconds)
        {
            string layer = "AI-总指标";
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(layer))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, layer, 4);
            }
            var MidX = (OInterParameter.TotalArea.Coordinates.Max(c => c.X) + OInterParameter.TotalArea.Coordinates.Min(c => c.X)) / 2;
            var StartY = (OInterParameter.TotalArea.Coordinates.Max(c => c.Y) + 20000);
            var xshift = 12000.0;
            var textList = new List<Entity>();

            double curr_Y = StartY;
            var T_str = "运算时间： " + string.Format("{0:N2}", TotalSeconds) + "s";
            textList.Add(ArrangementInfo.GetText(T_str, MidX - xshift, curr_Y, 2450, layer));

            string Dir_str;
            if (ParameterStock.RunMode == 0) Dir_str = "排布方向:自动组合";
            else if (ParameterStock.RunMode == 1) Dir_str = "排布方向:优先横向";
            else Dir_str = "排布方向:  优先纵向";
            curr_Y += 2450 + 2000;
            textList.Add(ArrangementInfo.GetText(Dir_str, MidX - xshift, curr_Y, 2450, layer));

            string CommandType_Str;
            if (ParameterViewModel.CommandType == CommandTypeEnum.RunWithoutIteration) CommandType_Str = "模式:手动分区线，无迭代速排";
            else if (ParameterViewModel.CommandType == CommandTypeEnum.RunWithIteration) CommandType_Str = "模式:手动分区线，迭代排布";
            else CommandType_Str = "模式:  自动分区线，迭代排布";
            curr_Y += 2450 + 2000;
            textList.Add(ArrangementInfo.GetText(CommandType_Str, MidX - xshift, curr_Y, 2450, layer));

            string PerStall_Str = "车均面积： " + string.Format("{0:N2}", areaPerStall) + "m" + Convert.ToChar(0x00b2) + "/辆";
            curr_Y += 3000 + 2500;
            textList.Add(ArrangementInfo.GetText(PerStall_Str, MidX - xshift, curr_Y, 3000, layer));

            string StallCnt_Str1 = "车位数:";
            curr_Y += 3000 + 2500;
            textList.Add(ArrangementInfo.GetText(StallCnt_Str1, MidX - xshift, curr_Y, 3000, layer));

            string StallCnt_Str2 = ParkingStallCount.ToString();
            textList.Add(ArrangementInfo.GetText(StallCnt_Str2, MidX, curr_Y, 8000, layer));
            textList.ShowBlock(layer, layer);
        }
        private void UpdateLogger(string blkName)
        {
            string modName = "斜交_";
            var logFileName = Path.Combine(GetPath.GetAppDataPath(), modName + DrawingName.Split('.').First() + '(' + blkName + ')' + ".txt");
            Logger = new Serilog.LoggerConfiguration().WriteTo
                    .File(logFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10).CreateLogger();

        }

        private void ReclaimMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }
    }
}
