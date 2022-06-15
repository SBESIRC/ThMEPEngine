using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.PartitionLayout;
using ThMEPEngineCore.Command;
using Draw = ThMEPArchitecture.ParkingStallArrangement.Method.Draw;
using static ThMEPArchitecture.ParkingStallArrangement.ParameterConvert;
using Autodesk.AutoCAD.EditorInput;
using ThMEPArchitecture.ViewModel;
using ThMEPArchitecture.ParkingStallArrangement.General;
using Linq2Acad;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using ThParkingStall.Core.InterProcess;
using static ThParkingStall.Core.MPartitionLayout.MCompute;
using ThCADCore.NTS;
using ThParkingStall.Core.MPartitionLayout;
using Dreambuild.AutoCAD;
using Utils = ThMEPArchitecture.ParkingStallArrangement.General.Utils;
using ThMEPEngineCore;
using Autodesk.AutoCAD.Geometry;
using ThMEPArchitecture.ParkingStallArrangement;
using DotNetARX;
using System.IO.MemoryMappedFiles;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NetTopologySuite.Geometries;
using ThParkingStall.Core.Tools;
using MPChromosome = ThParkingStall.Core.InterProcess.Chromosome;
using MPGene = ThParkingStall.Core.InterProcess.Gene;
using ThMEPArchitecture.ParkingStallArrangement.PostProcess;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.PreProcess;
using Autodesk.AutoCAD.ApplicationServices;

namespace ThMEPArchitecture.MultiProcess
{
    public class ThMPArrangementCmd : ThMEPBaseCommand, IDisposable
    {
        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "MPLog.txt");

        public static string DisplayLogFileName = Path.Combine(System.IO.Path.GetTempPath(), "DisplayLog.txt");
        public static string DisplayLogFileName2 = Path.Combine(System.IO.Path.GetTempPath(), "DisplayLog2.txt");
        public Serilog.Core.Logger Logger = null;

        public Serilog.Core.Logger DisplayLogger = null;//用于记录信息日志
        public Serilog.Core.Logger DisplayLogger2 = null;//用于记录信息日志
        public static ParkingStallArrangementViewModel ParameterViewModel { get; set; }

        private CommandMode _CommandMode { get; set; } = CommandMode.WithoutUI;
        public ThMPArrangementCmd()//debug 读取基因直排
        {
            CommandName = "-THDJCCWBZ";
            ActionName = "生成";
            _CommandMode = CommandMode.WithoutUI;
            ParameterViewModel = new ParkingStallArrangementViewModel();
        }

        public ThMPArrangementCmd(ParkingStallArrangementViewModel vm)
        {
            CommandName = "THZDCWBZ";
            ActionName = "手动分割线迭代生成";
            ParameterViewModel = vm;
            _CommandMode = CommandMode.WithUI;
        }

        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            System.IO.FileStream emptyStream = new System.IO.FileStream(DisplayLogFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            emptyStream.Close();
            System.IO.FileStream emptyStream2 = new System.IO.FileStream(DisplayLogFileName2, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            emptyStream2.Close();
            ParameterStock.Set(ParameterViewModel);
            
            if (ParameterStock.LogMainProcess)
            {
                Logger = new Serilog.LoggerConfiguration().WriteTo
                            .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10).CreateLogger();
                DisplayLogger = new Serilog.LoggerConfiguration().WriteTo
                            .File(DisplayLogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Infinite, retainedFileCountLimit: null).CreateLogger();
                DisplayLogger2 = new Serilog.LoggerConfiguration().WriteTo
            .File(DisplayLogFileName2, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Infinite, retainedFileCountLimit: null).CreateLogger();
            }
            Logger?.Information($"############################################");
            Logger?.Information("LayoutScareFactor_Intergral:" + ParameterStock.LayoutScareFactor_Intergral.ToString());
            Logger?.Information("LayoutScareFactor_Adjacent:" + ParameterStock.LayoutScareFactor_Adjacent.ToString());
            Logger?.Information("LayoutScareFactor_betweenBuilds:" + ParameterStock.LayoutScareFactor_betweenBuilds.ToString());
            Logger?.Information("LayoutScareFactor_SingleVert:" + ParameterStock.LayoutScareFactor_SingleVert.ToString());
            Logger?.Information("SingleVertModulePlacementFactor:" + ParameterStock.SingleVertModulePlacementFactor.ToString());
            Logger?.Information("CutTol:" + ParameterStock.CutTol.ToString());
            Utils.SetSeed();
            try
            {
                if(_CommandMode == CommandMode.WithoutUI)
                {
                    Logger?.Information($"DEbug--读取复现");
                    using (var docLock = Active.Document.LockDocument())
                    using (AcadDatabase currentDb = AcadDatabase.Active())
                    {
                        RunDebug();
                    }
                }
                else
                {
                    if (ParameterViewModel.CommandType == CommandTypeEnum.RunWithoutIteration)
                    {
                        Logger?.Information($"无迭代速排");
                        Logger?.Information($"Random Seed:{Utils.GetSeed()}");

                        using (var docLock = Active.Document.LockDocument())
                        using (AcadDatabase currentDb = AcadDatabase.Active())
                        {
                            RunDirect(currentDb);
                        }
                    }
                    else if(ParameterViewModel.CommandType == CommandTypeEnum.RunWithIteration)
                    {

                        using (var docLock = Active.Document.LockDocument())
                        using (AcadDatabase currentDb = AcadDatabase.Active())
                            Run(currentDb);
                    }
                    else 
                    {
                        using (var docLock = Active.Document.LockDocument())
                        using (AcadDatabase currentDb = AcadDatabase.Active())
                            RunWithAutoSegLine(currentDb);
                    }
                }
            }
            catch (Exception ex)
            {
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
        public void RunDebug()
        {
            MPGAData.Load();
            var dataWraper = MPGAData.dataWraper;
            var chromosome = MPGAData.dataWraper.chromosome;
            VMStock.Init(dataWraper);
            InterParameter.Init(dataWraper);
            InterParameter.MultiThread = false;
            ProcessAndDisplay(chromosome);
        }
        public void RunDirect(AcadDatabase acadDatabase)
        {
            var block = InputData.SelectBlock(acadDatabase);//提取地库对象
            var MultiSolutionList = ParameterViewModel.GetMultiSolutionList();
            var layoutData = new LayoutData();
            var inputvaild = layoutData.Init(block, Logger);
            if (!inputvaild) return;
            for (int i = 0; i < MultiSolutionList.Count; i++)
            {
                ParameterStock.RunMode = MultiSolutionList[i];
                Converter.GetDataWraper(layoutData, ParameterViewModel, false);
                InterParameter.MultiThread = true;
#if DEBUG
                InterParameter.MultiThread = false;
#endif
                var orgSolution = new MPChromosome();
                var genome = new List<MPGene>();
                foreach (var lineSeg in InterParameter.InitSegLines)
                {
                    MPGene gene = new MPGene(lineSeg);
                    genome.Add(gene);
                }
                orgSolution.Genome = genome;
                ProcessAndDisplay(orgSolution, block, i);
            }
            
        }

        public void Run(AcadDatabase acadDatabase)
        {

            var blks = InputData.SelectBlocks(acadDatabase);
            if (blks == null) return;
            var displayPro = ProcessForDisplay.CreateSubProcess();
            if (ParameterViewModel.ShowLogs)
            {
                displayPro.Start();
            }
            DisplayLogger?.Information("地库总数量: " + blks.Count().ToString());
            var displayInfos = new List<DisplayInfo>();
            foreach (var blk in blks)
            {
                try
                {
                    var blkName = blk.GetEffectiveName();
                    var displayInfo = new DisplayInfo(blkName);
                    Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
                    var drawingName = Path.GetFileName(doc.Name);
                    var logFileName = Path.Combine(System.IO.Path.GetTempPath(), drawingName + '(' + blkName + ')' + "Log.txt");
                    Logger = new Serilog.LoggerConfiguration().WriteTo
                            .File(logFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10).CreateLogger();
                    DisplayLogger?.Information("块名: " + blkName);
                    Logger?.Information("块名：" + blkName);
                    Logger?.Information("文件名：" + drawingName);
                    Logger?.Information("用户名：" + Environment.UserName);
                    RunABlock(blk, displayInfo,ParameterViewModel.AddBoundSegLines);
                    displayInfos.Add(displayInfo);
                }
                catch(Exception ex)
                {
                    Logger?.Information(ex.Message);
                    Logger?.Information("##################################");
                    Logger?.Information(ex.StackTrace);
                    Active.Editor.WriteMessage(ex.Message);
                }
            }
            DisplayLogger2?.Information("----------------------------------------------");
            DisplayLogger2?.Information("----------------------------------------------");
            DisplayLogger2?.Information("----------------------");
            DisplayLogger2?.Information("地库总数：" + blks.Count);
            DisplayLogger2?.Information($"总用时: {_stopwatch.Elapsed.TotalMinutes} 分");
            DisplayLogger2?.Information("----------------------");
            foreach (var displayInfo in displayInfos)
            {
                DisplayLogger2?.Information(displayInfo.BlockName);
                DisplayLogger2?.Information(displayInfo.FinalIterations);
                DisplayLogger2?.Information(displayInfo.FinalStalls);
                DisplayLogger2?.Information(displayInfo.FinalAveAreas);
                DisplayLogger2?.Information(displayInfo.CostTime);
                DisplayLogger2?.Information("----------------------");
            }
            DisplayLogger2?.Information("----------------------------------------------");
            DisplayLogger2?.Information("----------------------------------------------");
            DisplayLogger2?.Information("地库程序运行结束");
        }

        public void RunWithAutoSegLine(AcadDatabase acadDatabase)
        {
            var blks = InputData.SelectBlocks(acadDatabase);
            if (blks == null) return;
            var displayPro = ProcessForDisplay.CreateSubProcess();
            if (ParameterViewModel.ShowLogs)
            {
                displayPro.Start();
            }
            var cutTol = ParameterStock.CutTol + 5;
            var HorizontalFirst = true;
            DisplayLogger?.Information("地库总数量: " + blks.Count().ToString());
            var displayInfos = new List<DisplayInfo>();
            foreach (var blk in blks)
            {
                try
                {
                    var blkName = blk.GetEffectiveName();
                    var displayInfo = new DisplayInfo(blkName);
                    Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
                    var drawingName = Path.GetFileName(doc.Name);
                    var logFileName = Path.Combine(System.IO.Path.GetTempPath(), drawingName + '(' + blkName + ')' + "Log.txt");
                    Logger = new Serilog.LoggerConfiguration().WriteTo
                            .File(logFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10).CreateLogger();
                    DisplayLogger?.Information("块名: " + blkName);
                    Logger?.Information("块名：" + blkName);
                    Logger?.Information("文件名：" + drawingName);
                    Logger?.Information("用户名：" + Environment.UserName);
                    var autoSegLines = GenerateAutoSegLine(blk,cutTol, HorizontalFirst, out LayoutData layoutData,false);
                    if(! ParameterViewModel.JustCreateSplittersChecked && autoSegLines != null) RunABlock(blk, displayInfo,true, autoSegLines, layoutData);
                    displayInfos.Add(displayInfo);
                }
                catch (Exception ex)
                {
                    Logger?.Information(ex.Message);
                    Logger?.Information("##################################");
                    Logger?.Information(ex.StackTrace);
                    Active.Editor.WriteMessage(ex.Message);
                }
            }
            DisplayLogger2?.Information("----------------------------------------------");
            DisplayLogger2?.Information("----------------------------------------------");
            DisplayLogger2?.Information("----------------------");
            DisplayLogger2?.Information("地库总数：" + blks.Count);
            DisplayLogger2?.Information($"总用时: {_stopwatch.Elapsed.TotalMinutes} 分\n");
            DisplayLogger2?.Information("----------------------");
            foreach (var displayInfo in displayInfos)
            {
                DisplayLogger2?.Information(displayInfo.BlockName);
                DisplayLogger2?.Information(displayInfo.FinalIterations);
                DisplayLogger2?.Information(displayInfo.FinalStalls);
                DisplayLogger2?.Information(displayInfo.FinalAveAreas);
                DisplayLogger2?.Information(displayInfo.CostTime);
                DisplayLogger2?.Information("----------------------");
            }
            DisplayLogger2?.Information("----------------------------------------------");
            DisplayLogger2?.Information("----------------------------------------------");
            DisplayLogger2?.Information("地库程序运行结束");

        }
        public void RunABlock(BlockReference blk, DisplayInfo displayInfo, bool AddBoundSegLines = true, List<LineSegment> AutoSegLines = null, LayoutData layoutData = null)
        {
            Logger?.Information("##################################");
            Logger?.Information("迭代模式：");
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int fileSize = 64; // 64Mb
            var nbytes = fileSize * 1024 * 1024;
            DisplayParkingStall.Add(blk.Clone() as BlockReference);
            if (AutoSegLines == null)
            {
                layoutData = new LayoutData();
                var inputvaild = layoutData.Init(blk, Logger);
                if (!inputvaild) return;
            }
            else
            {
                var inputvaild = layoutData.ProcessSegLines(AutoSegLines);
                if (!inputvaild) return;
            }
            var MultiSolutionList = ParameterViewModel.GetMultiSolutionList();
            for (int i = 0; i < MultiSolutionList.Count; i++)
            {
                ParameterStock.RunMode = MultiSolutionList[i];
                var addBoundSegLines = AddBoundSegLines && i == 0;
                var dataWraper = Converter.GetDataWraper(layoutData, ParameterViewModel, addBoundSegLines);
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("DataWraper", nbytes))
                {
                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                    {
                        IFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, dataWraper);
                    }
                    var GA = new MultiProcessGAGenerator(ParameterViewModel);
                    Logger?.Information($"初始化用时: {stopWatch.Elapsed.TotalSeconds}秒 \n");
                    GA.Logger = Logger;
                    GA.DisplayLogger = DisplayLogger;
                    try
                    {
                        var res = GA.Run2(displayInfo);
                        var best = res.First();
                        ProcessAndDisplay(best, blk, i, stopWatch, displayInfo);
                    }
                    catch (Exception ex)
                    {
                        Logger?.Information(ex.Message);
                        Logger?.Information("##################################");
                        Logger?.Information(ex.StackTrace);
                        Active.Editor.WriteMessage(ex.Message);
                    }
                    finally
                    {

                    }
                }
            }
        }

        private void ProcessAndDisplay(MPChromosome solution,BlockReference orgBlk=null,int SolutionID = 0 ,Stopwatch stopWatch = null, DisplayInfo displayInfo=null)
        {
            var moveDistance = 0.0;
            if (orgBlk != null && SolutionID!=0)
            {
                var blk_C = orgBlk.Clone() as BlockReference;
                blk_C.AddToCurrentSpace();
                DisplayParkingStall.Add(blk_C);
                moveDistance = SolutionID*2 * (InterParameter.TotalArea.Coordinates.Max(c => c.X) - InterParameter.TotalArea.Coordinates.Min(c => c.X));
            }
            var subAreas = InterParameter.GetSubAreas(solution);
            var finalSegLines = InterParameter.ProcessToSegLines(solution).Item1;
            var layer = "最终分割线";
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(layer))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, layer, 2);
                finalSegLines.Select(l => l.ToDbLine(2, layer)).Cast<Entity>().ToList().ShowBlock(layer, layer);
                MPEX.HideLayer(layer);
            }
#if DEBUG
            for (int i = 0; i < subAreas.Count; i++)
            {
                var subArea = subAreas[i];
                subArea.Display("MPDebug");
            }
#endif
            List<MParkingPartitionPro> mParkingPartitionPros = new List<MParkingPartitionPro>();
            MParkingPartitionPro mParkingPartition = new MParkingPartitionPro();
            var ParkingStallCount = CalculateTheTotalNumOfParkingSpace(subAreas, ref mParkingPartitionPros, ref mParkingPartition, true);
            var strBest = $"最大车位数{ParkingStallCount}\n";
            Logger?.Information(strBest);
            Active.Editor.WriteMessage(strBest);
            MultiProcessTestCommand.DisplayMParkingPartitionPros(mParkingPartition);
            subAreas.ForEach(area => area.ShowText());
            DisplayParkingStall.Display(moveDistance);
            SubAreaParkingCnt.Clear();
            ReclaimMemory();
            if(stopWatch != null)
            {
                Logger?.Information($"单地库用时: {stopWatch.Elapsed.TotalSeconds}秒 \n");
                DisplayLogger?.Information($"最大车位数: {ParkingStallCount}");
                var areaPerStall = ParameterStock.TotalArea  / ParkingStallCount;
                DisplayLogger?.Information("车均面积: " + string.Format("{0:N2}", areaPerStall) + "平方米/辆");
                DisplayLogger?.Information($"单地库用时: {stopWatch.Elapsed.TotalMinutes} 分\n");
                if(displayInfo!=null)
                {
                    displayInfo.FinalStalls = $"最大车位数: {ParkingStallCount} ";
                    displayInfo.FinalAveAreas = "车均面积: " + string.Format("{0:N2}", areaPerStall) + "平方米/辆";
                    displayInfo.CostTime = $"单地库用时: {stopWatch.Elapsed.TotalMinutes} 分\n";
                }
            }
        }
        public List<LineSegment> GenerateAutoSegLine(BlockReference blk, int cutTol, bool HorizontalFirst,out LayoutData layoutData,bool definePriority = true)
        {
            Logger?.Information("##################################");
            var blk_Name = blk.GetEffectiveName();
            Logger?.Information("块名：" + blk_Name);
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var t_pre = 0.0;
            layoutData = new LayoutData();
            var inputvaild = layoutData.Init(blk, Logger, false);
            if (!inputvaild) return null;
            Converter.GetDataWraper(layoutData, ParameterViewModel,false);
            var autogen = new AutoSegGenerator(layoutData, Logger, cutTol);
            Logger?.Information($"初始化用时: {stopWatch.Elapsed.TotalSeconds - t_pre }");
            t_pre = stopWatch.Elapsed.TotalSeconds;
            autogen.Run(false);
            Logger?.Information($"穷举用时: {stopWatch.Elapsed.TotalSeconds - t_pre}");
            t_pre = stopWatch.Elapsed.TotalSeconds;
            var girdLines = autogen.GetGrid().Select(l => l.SegLine.ToNTSLineSegment()).ToList();
            if (girdLines.Count < 2)
            {
                Active.Editor.WriteMessage("块名为：" + blk_Name + "的地库暂不支持自动分割线！\n");
                Logger?.Information("块名为：" + blk_Name + "的地库暂不支持自动分割线！\n");
                return null;
            }
            //girdLines.ForEach(l => l.ToDbLine().AddToCurrentSpace());
            girdLines = girdLines.RemoveDuplicated(5);
            girdLines.SeglinePrecut(layoutData.WallLine);
            //girdLines.ForEach(l => l.ToDbLine().AddToCurrentSpace());
            var grouped = girdLines.GroupSegLines().OrderBy(g => g.Count).Last();
            //grouped.ForEach(l => l.ToDbLine().AddToCurrentSpace());
            var result = grouped;

            result = result.GridLinesRemoveEmptyAreas(HorizontalFirst);
            if(definePriority) result = result.DefineSegLinePriority();

            Logger?.Information($"去重+去空区用时: {stopWatch.Elapsed.TotalSeconds - t_pre}");
            t_pre = stopWatch.Elapsed.TotalSeconds;
            if(ParameterViewModel.JustCreateSplittersChecked)
            {
                var layer = "AI自动分割线";
                using (AcadDatabase acad = AcadDatabase.Active())
                {
                    if (!acad.Layers.Contains(layer))
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, layer, 2);
                }
                result.Select(l => l.ToDbLine(2, layer)).Cast<Entity>().ToList().ShowBlock(layer, layer);
            }
            ReclaimMemory();
            Logger?.Information($"当前图生成分割线总用时: {stopWatch.Elapsed.TotalSeconds }\n");
            return result;
        }
        private void ReclaimMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
        }
    }
    public static class MPEX
    {
        public static void Display(this SubArea subArea,string blockName,string layer = "MPDebug")
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(layer))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, layer, 0);
            }

            var entities = new List<Entity>();
            entities.Add(subArea.Area.ToDbMPolygon());
            entities[0].Layer = layer;
            if(subArea.VaildLanes != null)
                entities.AddRange(subArea.VaildLanes.Select(l => l.ToDbLine(2,layer)));
            if (subArea.SegLines != null)
            entities.AddRange(subArea.SegLines.Select(l => l.ToDbPolyline(2, layer)));
            entities.AddRange(subArea.Walls.Select(wall => wall.ToDbPolyline(1, layer)));
            entities.AddRange(subArea.Buildings.Select(polygon => polygon.ToDbMPolygon(5, layer)));
            entities.AddRange(subArea.Ramps.Select(ramp => ramp.Area.ToDbMPolygon(3, layer)));
            entities.AddRange(subArea.BoundingBoxes.Select(polygon => polygon.ToDbMPolygon(4, layer)));
            entities.ShowBlock(blockName, layer);
        }
        private static Polyline ToDbPolyline(this LineString lstr, int coloridx, string layer)
        {
            var pline = lstr.ToDbPolyline();
            pline.Layer = layer;
            pline.ColorIndex = coloridx;
            return pline;
        }
        public static Line ToDbLine(this LineSegment segment, int coloridx, string layer)
        {
            var line = segment.ToDbLine();
            line.Layer = layer;
            line.ColorIndex = coloridx;
            return line;
        }
        public static MPolygon ToDbMPolygon(this Polygon polygon, int coloridx, string layer)
        {
            var mpolygon = polygon.ToDbMPolygon();
            mpolygon.Layer = layer;
            mpolygon.ColorIndex = coloridx;
            return mpolygon;

        }
        public static void HideLayer(string layerName)
        {
            var id = DbHelper.GetLayerId(layerName);
            id.QOpenForWrite<LayerTableRecord>(layer =>
            {
                layer.IsOff = true;
            });
        }
        public static void ShowLowerUpperBound(this List<LineSegment> SegLines, string layer = "最大最小值")
        {
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(layer))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, layer, 3);
            }

            for (int i = 0; i < SegLines.Count; i++)
            {
                LineSegment SegLine = SegLines[i];
                var lb = InterParameter.LowerUpperBound[i].Item1;
                var ub = InterParameter.LowerUpperBound[i].Item2;
                Line LowerLine;
                Line UpperLine;
                if (SegLine.IsVertical())
                {
                    LowerLine = new Line(new Point3d(lb, SegLine.P0.Y, 0), 
                                            new Point3d(lb, SegLine.P1.Y, 0));
                    UpperLine = new Line(new Point3d(ub, SegLine.P0.Y, 0),
                                            new Point3d(ub, SegLine.P1.Y, 0));
                }
                else
                {
                    LowerLine = new Line(new Point3d(SegLine.P0.X,lb, 0),
                                            new Point3d(SegLine.P1.X,lb, 0));
                    UpperLine = new Line(new Point3d(SegLine.P0.X, ub, 0),
                                            new Point3d(SegLine.P1.X, ub, 0));
                }
                LowerLine.Layer = layer;
                LowerLine.ColorIndex = 3;
                UpperLine.Layer = layer;
                UpperLine.ColorIndex = 3;
                LowerLine.AddToCurrentSpace();
                UpperLine.AddToCurrentSpace();
            }
        }
    }
}
