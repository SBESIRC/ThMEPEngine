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
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Utils = ThMEPArchitecture.ParkingStallArrangement.General.Utils;
using ThParkingStall.Core.OTools;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using NetTopologySuite.Geometries;
using ThParkingStall.Core.MPartitionLayout;
using System.IO.MemoryMappedFiles;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using ThParkingStall.Core.ObliqueMPartitionLayout.OPostProcess;
using static ThMEPArchitecture.PartitionLayout.DisplayTools;
using ThParkingStall.Core.Tools;
using ThParkingStall.Core.ObliqueMPartitionLayout;
using ThMEPArchitecture.MultiProcess;
using ThMEPEngineCore.Diagnostics;

using ThParkingStall.Core;
using static ThParkingStall.Core.MPartitionLayout.MCompute;

using System.Reflection;
using System.Net;
using ThMEPIdentity;
using System.Windows;

namespace ThMEPArchitecture.MultiProcess
{
    public class ThOArrangementCmd : ThMEPBaseCommand, IDisposable
    {
        public static string LogFileName = Path.Combine(GetPath.GetAppDataPath(), "MPLog.txt");
        public static string DisplayLogFileName = Path.Combine(System.IO.Path.GetTempPath(), "DisplayLog.txt");
        public Serilog.Core.Logger Logger = null;
        List<DisplayInfo> displayInfos;
        public static Serilog.Core.Logger DisplayLogger = null;//用于记录信息日志

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
            ParameterStock.Set(ParameterViewModel);
            if (ParameterStock.LogMainProcess)
            {
                Logger = new Serilog.LoggerConfiguration().WriteTo
                            .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10).CreateLogger();
                DisplayLogger = new Serilog.LoggerConfiguration().WriteTo
                            .File(DisplayLogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Infinite, retainedFileCountLimit: null).CreateLogger();
            }
            ThParkingStallCoreTools.SetSeed();
            try
            {
                using (var docLock = Active.Document.LockDocument())
                using (AcadDatabase currentDb = AcadDatabase.Active())
                {
                    var saveDoc = true;
#if DEBUG
                    saveDoc = false;
#endif
                    if (_CommandMode == CommandMode.WithoutUI)
                    {
                        Logger?.Information($"DEbug--读取复现");
                        RunDebug();
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
                            Run(currentDb);
                        }

                        else if (ParameterViewModel.CommandType == CommandTypeEnum.RunWithIterationAutomatically)
                        {
                            Run(currentDb, true);
                        }
                        else if (ParameterViewModel.CommandType == CommandTypeEnum.BuildingAnalysis)
                        {
                            BuildingAnalysis(currentDb);
                        }
                    }
                    TableTools.EraseOrgTable();
                    //TableTools.hideOrgTable();
                    if (saveDoc)
                        Active.Document.Save();
                }
            }
            catch (Exception ex)
            {
                DisplayLogger?.Information(ex.Message);
                DisplayLogger?.Information("程序出错！");
                Logger?.Information(ex.Message);
                Logger?.Information("##################################");
                Logger?.Information(ex.StackTrace);
                Active.Editor.WriteMessage(ex.Message);
            }
            finally
            {
                DisplayLogger?.Information($"地库程序运行结束,总用时: {_stopwatch.Elapsed.TotalMinutes} 分\n");
                DisplayLogger?.Dispose();
            }
        }

        public override void AfterExecute()
        {
            base.AfterExecute();
            Active.Editor.WriteMessage($"总运行时间: {_stopwatch.Elapsed.TotalSeconds}秒 \n");
            Logger?.Information($"总运行时间: {_stopwatch.Elapsed.TotalSeconds}秒 \n");
            base.AfterExecute();
        }
        public void RunDebug()
        {
            MPGAData.Load();
            var dataWraper = MPGAData.dataWraper;
            var genome = MPGAData.dataWraper.genome;
            VMStock.Init(dataWraper);
            InterParameter.Init(dataWraper);
            InterParameter.MultiThread = false;
            ProcessAndDisplay(genome);
        }
        public void RunDirect(AcadDatabase acadDatabase)
        {
            var blks = InputData.SelectBlocks(acadDatabase);
            //var block = InputData.SelectBlock(acadDatabase);//提取地库对象
            var MultiSolutionList = ParameterViewModel.GetMultiSolutionList();
            //var MultiSolutionList = new List<int> { 0 };
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
                if(layoutData.SegLines.Count == 0) return;
                //layoutData.SetInterParam();
                Converter.GetDataWraper(layoutData, ParameterViewModel);
                for (int i = 0; i < MultiSolutionList.Count; i++)
                {
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    ParameterStock.RunMode = MultiSolutionList[i];
                    var dataWraper = new DataWraper();
                    dataWraper.UpdateVMParameter(ParameterViewModel);
                    VMStock.Init(dataWraper);
                    ProcessAndDisplay(null, i, stopWatch);
                }
            }
        }
        public void Run(AcadDatabase acadDatabase, bool autoMode = false)
        {
            var blks = InputData.SelectBlocks(acadDatabase);
            var displayPro = ProcessForDisplay.CreateSubProcess();
            if (ParameterViewModel.ShowLogs)
            {
                displayPro.Start();
                displayInfos = new List<DisplayInfo>();
            }
            //var block = InputData.SelectBlock(acadDatabase);//提取地库对象
            if (blks == null) return;
            DisplayLogger?.Information("地库总数量: " + blks.Count().ToString());
            foreach (var blk in blks)
            {
                var msg = "";
                ProcessTheBlock(blk,ref msg, autoMode);
                if (msg != "")
                {
                    if (msg.Contains("服务器繁忙"))
                    {
                        DisplayLogger.Information("服务器繁忙中，请稍后再试");
                        break;
                    }
                }
            }
            ShowDisplayInfo(blks.Count());
        }
        public void BuildingAnalysis(AcadDatabase acadDatabase)
        {
            var blks = InputData.SelectBlocks(acadDatabase);
            if (blks == null) return;
            foreach (var blk in blks)
            {
                var blkName = blk.GetEffectiveName();
                UpdateLogger(blkName);
                //DisplayParkingStall.Add(blk.Clone() as BlockReference);
                Logger?.Information("块名：" + blkName);
                Logger?.Information("文件名：" + DrawingName);
                Logger?.Information("用户名：" + Environment.UserName);
                var layoutData = new OLayoutData(blk, Logger, out bool succeed);
                if (!succeed) return;
                layoutData.ProcessSegLines();
                if(layoutData.SegLines.Count == 0) return;
                if(layoutData.MovingBounds.Count == 0)
                {
                    Active.Editor.WriteMessage("未检测到楼栋微调框线，请确保图层正确且闭合!");
                    return;
                }
                //layoutData.SetInterParam();
                var dataWraper = Converter.GetDataWraper(layoutData, ParameterViewModel);
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                ProcessAndDisplay(null, 0, stopWatch);
                //ParameterStock.RunMode = MultiSolutionList[i];
                //var lanes = OInterParameter.GetBoundLanes();
                var BPA = new BuildingPosAnalysis(ParameterViewModel);
                BPA.Logger = Logger;
                int fileSize = 64; // 64Mb
                var nbytes = fileSize * 1024 * 1024;
                if (ParameterViewModel.UseGA)
                {
                    using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("DataWraper", nbytes))
                    {
                        using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                        {
                            IFormatter formatter = new BinaryFormatter();
                            formatter.Serialize(stream, dataWraper);
                        }
                        BPA.UpdateGAMP();
                    }
                        //BPA.UpdataGA();
                        
                    //BPA.SpeedTest();
                }
                else BPA.UpdateBest();
                var entities = new List<Entity>();
                using (AcadDatabase acad = AcadDatabase.Active())
                {
                    if (!acad.Layers.Contains("障碍物"))
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, "障碍物", 0);
                    if (!acad.Layers.Contains("分区线"))
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, "分区线", 0);
                    if (!acad.Layers.Contains("地库边界"))
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, "地库边界", 0);
                }
                foreach (var b in OInterParameter.Buildings)
                {
                    var pl = b.Shell.ToDbPolyline(5, "障碍物");
                    DisplayParkingStall.Add(pl);
                    entities.Add(pl);
                }
                foreach (var l in OInterParameter.InitSegLines)
                {
                    var line = l.Splitter.ToDbLine(2, "分区线");
                    DisplayParkingStall.Add(line);
                    entities.Add(line);
                }
                var newBound = CaledBound.Shell.ToDbPolyline(1, "地库边界");
                entities.Add(newBound);
                DisplayParkingStall.Add(newBound);
                //DisplayParkingStall.MoveAddedEntities();
                //OInterParameter.Buildings.ForEach(b => entities.Add( b.Shell.ToDbPolyline(5, "障碍物")));
                //OInterParameter.Buildings.ForEach(b => b.ToDbPolylines().ForEach(pl => { pl.AddToCurrentSpace(); DisplayParkingStall.Add(pl); }));
                ProcessAndDisplay(null, 1, stopWatch, false);
                //OInterParameter.BuildingBounds.ForEach(b => b.ToDbMPolygon().AddToCurrentSpace());
                //lanes.Get<LineString>(true).ForEach(l => l.ToDbPolyline().AddToCurrentSpace());
                //ProcessAndDisplay(null, 0, stopWatch);
                entities.ShowBlock("障碍物移位结果", "障碍物移位结果");
            }
        }
        private void ProcessTheBlock(BlockReference block,ref string msg, bool autoMode = false, bool definePriority = true)
        {
            var MultiSolutionList = ParameterViewModel.GetMultiSolutionList();
            //var MultiSolutionList = new List<int> { 0 };
            var blkName = block.GetEffectiveName();
            UpdateLogger(blkName);
            Logger?.Information("块名：" + blkName);
            Logger?.Information("文件名：" + DrawingName);
            Logger?.Information("用户名：" + Environment.UserName);
            DisplayLogger?.Information("块名: " + blkName);
            displayInfos.Add(new DisplayInfo(blkName));
            var layoutData = new OLayoutData(block, Logger, out bool succeed);
            if (!succeed) return;
            if(layoutData.Obstacles.Count == 0) return;
            List<LineSegment> autoSegLines = null;
            InterParameter.Init(layoutData.WallLine, layoutData.Buildings);
            if (autoMode)//生成正交全自动分区线
            {
                var cutTol = 1000;
                var HorizontalFirst = true;
                var autogen = new AutoSegGenerator(layoutData, Logger, cutTol);
                autogen.Run(false);
                var girdLines = autogen.GetGrid().Select(l => l.SegLine.ToNTSLineSegment()).ToList();
                if (girdLines.Count < 2)
                {
                    DisplayLogger.Information("块名为：" + blkName + "的地库暂不支持自动分区线！\n");
                    Active.Editor.WriteMessage("块名为：" + blkName + "的地库暂不支持自动分区线！\n");
                    Logger?.Information("块名为：" + blkName + "的地库暂不支持自动分区线！\n");
                    return;
                }
                girdLines = girdLines.RemoveDuplicated(5);
                girdLines.SeglinePrecut(layoutData.WallLine);
                var grouped = girdLines.GroupSegLines().OrderBy(g => g.Count).Last();
                autoSegLines = grouped;
                autoSegLines = autoSegLines.GridLinesRemoveEmptyAreas(HorizontalFirst);
                if (definePriority) autoSegLines = autoSegLines.DefineSegLinePriority();
                if (ParameterViewModel.JustCreateSplittersChecked)
                {
                    var layer = "AI自动分区线";
                    using (AcadDatabase acad = AcadDatabase.Active())
                    {
                        if (!acad.Layers.Contains(layer))
                            ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, layer, 2);
                    }
                    autoSegLines.Select(l => l.ToDbLine(2, layer)).Cast<Entity>().ToList().ShowBlock(layer, layer);
                    return;
                }
            }
            layoutData.ProcessSegLines(autoSegLines, ParameterViewModel.AddBoundSegLines);
            if(layoutData.SegLines.Count == 0) return;
            //layoutData.SetInterParam();
            for (int i = 0; i < MultiSolutionList.Count; i++)
            {
                var guid = (Guid.NewGuid()).ToString();
                var userName = System.Environment.UserName;
                guid = userName + "_" + guid;
                WriteGuidToMemoryFile(guid);
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                ParameterStock.RunMode = MultiSolutionList[i];
                var dataWraper = Converter.GetDataWraper(layoutData, ParameterViewModel);
                Genome Solution;
                if (ParkingStallArrangementViewModel.DebugLocal)
                {
                    Solution = GetGenomeInitially(dataWraper);
                }
                else
                {
                    DisplayLogger.Information("发送至服务器计算;");
                    Solution = GetGenomeFromServer(dataWraper, guid,ref msg);
                    if (msg != "")
                    {
                        return;
                    }
                    DisplayLogger.Information("接受到服务器计算结果;");
                }
                ProcessAndDisplay(Solution, i, stopWatch);
            }
        }
        void WriteGuidToMemoryFile(string data)
        {
            //byte[] B = Encoding.UTF8.GetBytes(data);
            //MemoryMappedFile memory = MemoryMappedFile.CreateOrOpen("AI-guid", B.Length);    // 创建指定大小的内存文件，会在应用程序退出时自动释放
            //MemoryMappedViewAccessor accessor1 = memory.CreateViewAccessor();               // 访问内存文件对象
            //accessor1.Flush();
            //accessor1.WriteArray<byte>(0, B, 0, B.Length);
            //accessor1.Dispose();
            //return;
            string file = Path.Combine(System.IO.Path.GetTempPath(), "AICal_File_id.txt");
            FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(data);
            sw.Close();
            fs.Close();
            return;
        }
        Genome GetGenomeInitially(DataWraper dataWraper)
        {
            int fileSize = 64; // 64Mb
            var nbytes = fileSize * 1024 * 1024;
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("DataWraper", nbytes))
            {
                using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, dataWraper);
                }
                var GA_Engine = new OGAGenerator(ParameterViewModel);
                GA_Engine.Logger = Logger;
                GA_Engine.DisplayLogger = DisplayLogger;
                GA_Engine.displayInfo = displayInfos.Last();
                var Solution = GA_Engine.Run().First();
                return Solution;
            }
        }
        Genome GetGenomeFromServer(DataWraper dataWraper,string guid,ref string msg)
        {
            ServerGenerationService serverGenerationService = new ServerGenerationService();
            var gene = serverGenerationService.GetGenome(dataWraper,guid,ref msg);
            return gene;
        }

        private Polygon CaledBound;
        private void ProcessAndDisplay(Genome solution, int SolutionID = 0, Stopwatch stopWatch = null, bool disPlayBound = true)
        {
            var moveDistance = SolutionID * 2 * (OInterParameter.TotalArea.Coordinates.Max(c => c.X) -
                                                OInterParameter.TotalArea.Coordinates.Min(c => c.X));
            var subAreas = OInterParameter.GetOSubAreas(solution);
            subAreas.ForEach(s => s.UpdateParkingCnts(true));

            var ParkingStallCount = subAreas.Where(s => s.Count > 0).Sum(s => s.Count);

            CaledBound = ProcessPartitionGlobally(subAreas, disPlayBound);
            if (solution != null)
            {
                var finalLayer = "最终分区线";
                using (AcadDatabase acad = AcadDatabase.Active())
                {
                    if (!acad.Layers.Contains(finalLayer))
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, finalLayer, 2);
                    var outSegLines = OInterParameter.CurrentSegs.Where(l =>l.Splitter!= null).Select(l => l.Splitter.ToDbLine(2, finalLayer)).Cast<Entity>().ToList();
                    foreach (var subarea in subAreas)
                        outSegLines.AddRange(subarea.obliqueMPartition.OutEnsuredLanes.Select(e => e.ToDbLine()));
                    outSegLines.ShowBlock(finalLayer, finalLayer);
                    MPEX.HideLayer(finalLayer);
                }
            }
            if(SolutionID != 0)
            {
                using (AcadDatabase acad = AcadDatabase.Active())
                {
                    if (!acad.Layers.Contains("障碍物"))
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, "障碍物", 0);
                }
                foreach (var b in OInterParameter.Buildings)
                {
                    var pl = b.Shell.ToDbPolyline(5, "障碍物");
                    pl.AddToCurrentSpace();
                    DisplayParkingStall.Add(pl);
                }
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
            if (ParameterViewModel.ShowSubAreaTitle) subAreas.ForEach(area => area.ShowText());
            if (stopWatch != null)
            {
                Logger?.Information($"单地库用时: {stopWatch.Elapsed.TotalSeconds}秒 \n");
                DisplayLogger?.Information($"最大车位数: {ParkingStallCount}");
                var areaPerStall = CaledBound.Area * 0.001 * 0.001 / ParkingStallCount;
                DisplayLogger?.Information("车均面积: " + string.Format("{0:N2}", areaPerStall) + "平方米/辆");
                DisplayLogger?.Information($"单地库用时: {stopWatch.Elapsed.TotalMinutes} 分\n");

                if (ParameterViewModel.ShowTitle) ShowTitle(ParkingStallCount, areaPerStall, stopWatch.Elapsed.TotalSeconds);
                if (ParameterViewModel.ShowTable)
                {
                    var minY = OInterParameter.TotalArea.Coordinates.Min(c => c.Y);
                    var midX = (OInterParameter.TotalArea.Coordinates.Max(c => c.X) +
                        OInterParameter.TotalArea.Coordinates.Min(c => c.X)) / 2;
                    TableTools.ShowTables(new Point3d(midX, minY - 20000, 0), ParkingStallCount);
                }
                if (displayInfos != null)
                {
                    displayInfos.Last().FinalStalls = $"最大车位数: {ParkingStallCount} ";
                    displayInfos.Last().FinalAveAreas = "车均面积: " + string.Format("{0:N2}", areaPerStall) + "平方米/辆";
                    displayInfos.Last().CostTime = $"单地库用时: {stopWatch.Elapsed.TotalMinutes} 分\n";
                }
            }
            DisplayParkingStall.MoveAddedEntities(moveDistance);
            //SubAreaParkingCnt.Clear();
            ReclaimMemory();
        }
        Polygon ProcessPartitionGlobally(List<OSubArea> subAreas, bool disPlayBound = true)
        {
            DataToDeformationService dataToDeformationService = new DataToDeformationService(subAreas);
            var parkingPlaceBlocks = dataToDeformationService.GetParkingPlaceBlocks();
            //
            string Boundlayer = "AI-参考地库轮廓";
            var laneLayer = "AI-车道中心线";
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(Boundlayer))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, Boundlayer, 141);
                if (!acad.Layers.Contains(laneLayer))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, laneLayer, 2);
            }
            GlobalBusiness globalBusiness = new GlobalBusiness(subAreas);
            ////车道微动
            //globalBusiness.DeformLanes();
            ////打印变量
            //PrintTmpOutPut(globalBusiness.drawTmpOutPut0);


            var caledBound = globalBusiness.CalBound();
            if (disPlayBound) Display(caledBound, 141, Boundlayer);
            if (ObliqueMPartition.AllowProcessEndLanes)
            {
                globalBusiness.ProcessEndLanes();             
            }
            var integralObliqueMPartition = new ObliqueMPartition()
            { Cars = globalBusiness.cars, OutputLanes = globalBusiness.lanes, Pillars = globalBusiness.pillars };
            MultiProcessTestCommand.DisplayMParkingPartitionPros(integralObliqueMPartition.ConvertToMParkingPartitionPro());


            return caledBound;
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
            else if (ParameterViewModel.CommandType == CommandTypeEnum.RunWithIterationAutomatically) CommandType_Str = "模式:  自动分区线，迭代排布";
            else if (ParameterViewModel.CommandType == CommandTypeEnum.BuildingAnalysis) CommandType_Str = "模式:楼栋微调模式";
            else throw new NotImplementedException();
            Logger?.Information(CommandType_Str);
            Logger?.Information(Dir_str);
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
        private void ShowDisplayInfo(int blkCnt)
        {
            DisplayLogger?.Information("----------------------------------------------");
            DisplayLogger?.Information("----------------------------------------------");
            DisplayLogger?.Information("----------------------");
            DisplayLogger?.Information("地库总数：" + blkCnt);
            DisplayLogger?.Information($"总用时: {_stopwatch.Elapsed.TotalMinutes} 分");
            DisplayLogger?.Information("----------------------");
            foreach (var displayInfo in displayInfos)
            {
                DisplayLogger?.Information(displayInfo.BlockName);
                DisplayLogger?.Information(displayInfo.FinalIterations);
                DisplayLogger?.Information(displayInfo.FinalStalls);
                DisplayLogger?.Information(displayInfo.FinalAveAreas);
                DisplayLogger?.Information(displayInfo.CostTime);
                DisplayLogger?.Information("----------------------");
            }
            DisplayLogger?.Information("----------------------------------------------");
            DisplayLogger?.Information("----------------------------------------------");
            DisplayLogger?.Information("地库程序运行结束");
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

        public void PrintTmpOutPut(DrawTmpOutPut drawTmpOutPut) 
        {
            //打印到CAD
            foreach (Polygon a in drawTmpOutPut.OriginalFreeAreaList) 
            {
                MPolygon ma = a.ToDbMPolygon();
                DrawUtils.ShowGeometry(ma, "l0OriginalFreeAreaList", 0);
            }

            foreach (Polygon a in drawTmpOutPut.FreeAreaRecs)
            {
                MPolygon ma = a.ToDbMPolygon();
                DrawUtils.ShowGeometry(ma, "l0FreeAreaRecs", 1);
            }

            //foreach (Polygon a in drawTmpOutPut.LaneNodes)
            //{
            //    MPolygon ma = a.ToDbMPolygon();
            //    DrawUtils.ShowGeometry(ma, "l0LaneRecs", 2);
            //}

            //foreach (Polygon a in drawTmpOutPut.SpotNodes)
            //{
            //    MPolygon ma = a.ToDbMPolygon();
            //    DrawUtils.ShowGeometry(ma, "l0SpotRecs", 3);
            //}


        }
    }
}
