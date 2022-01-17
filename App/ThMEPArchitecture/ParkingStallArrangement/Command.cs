using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.PartitionLayout;
using ThMEPEngineCore;
using ThMEPEngineCore.Command;
using Draw = ThMEPArchitecture.ParkingStallArrangement.Method.Draw;
using static ThMEPArchitecture.ParkingStallArrangement.ParameterConvert;
using Autodesk.AutoCAD.EditorInput;
using ThMEPArchitecture.ViewModel;
using ThMEPArchitecture.ParkingStallArrangement.General;

namespace ThMEPArchitecture.ParkingStallArrangement
{
    public class ThParkingStallArrangementCmd : ThMEPBaseCommand, IDisposable
    {
        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "GaLog.txt");

        public Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
            .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day, retainedFileCountLimit:10).CreateLogger();

        public static ParkingStallArrangementViewModel ParameterViewModel { get; set; }

        private CommandMode _CommandMode { get; set; } = CommandMode.WithoutUI;
        public ThParkingStallArrangementCmd()
        {
            CommandName = "-THDXQYFG";
            ActionName = "生成";
            _CommandMode = CommandMode.WithoutUI;
        }

        public ThParkingStallArrangementCmd(ParkingStallArrangementViewModel vm)
        {
            CommandName = "THDXCW";
            ActionName = "手动分割线迭代生成";
            ParameterViewModel = vm;
            _CommandMode = CommandMode.WithUI;
        }

        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            try
            {
                using (var docLock = Active.Document.LockDocument())
                using (AcadDatabase currentDb = AcadDatabase.Active())
                {
                    RunWithWindmillSeglineSupported(currentDb);
                }
            }
            catch (Exception ex)
            {
                Logger?.Information(ex.Message);
                Active.Editor.WriteMessage(ex.Message);
            }
        }

        public override void AfterExecute()
        {
            base.AfterExecute();
            Active.Editor.WriteMessage($"seconds: {_stopwatch.Elapsed.TotalSeconds} \n");
            base.AfterExecute();
        }

        public void Run(AcadDatabase acadDatabase)
        {
            var rstDataExtract = InputData.GetOuterBrder(acadDatabase, out OuterBrder outerBrder);
            if (outerBrder.SegLines.Count == 0)//分割线数目为0
            {
                Active.Editor.WriteMessage("分割线不存在！");
                return;
            }
            if (!rstDataExtract)
            {
                return;
            }
            var area = outerBrder.WallLine;
            var areas = new List<Polyline>() { area };
            var sortSegLines = new List<Line>();
            var buildLinesSpatialIndex = new ThCADCoreNTSSpatialIndex(outerBrder.BuildingLines);
            var gaPara = new GaParameter(outerBrder.SegLines);
            
            var usedLines = new HashSet<int>();
            var maxVals = new List<double>();
            var minVals = new List<double>();
            var maxIterations = outerBrder.SegLines.Count;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            double threshSecond = 20;
            var correctSeg = Dfs.dfsSplit(ref usedLines, ref areas, ref sortSegLines, buildLinesSpatialIndex, gaPara, ref maxVals, ref minVals, stopwatch, threshSecond);
            stopwatch.Stop();
            if(!correctSeg)
            {
                Logger?.Information("分割线不合理，分区失败！");
                return;
            }
            gaPara.Set(sortSegLines, maxVals, minVals );

            var segLineDic = new Dictionary<int, Line>();
            for (int i = 0; i < sortSegLines.Count; i++)
            {
                segLineDic.Add(i, sortSegLines[i]);
            }

            var ptDic = Intersection.GetIntersection(segLineDic);//获取分割线的交点
            var linePtDic = Intersection.GetLinePtDic(ptDic);
            var intersectPtCnt = ptDic.Count;//交叉点数目
            var directionList = new Dictionary<int, bool>();//true表示纵向，false表示横向
            foreach (var num in ptDic.Keys)
            {
                var random = new Random();
                var flag = random.NextDouble() < 0.5;
                directionList.Add(num, flag);//默认给全横向
            }
            ParkingStallGAGenerator geneAlgorithm = null;
            var layoutPara = new LayoutParameter(area, outerBrder.BuildingLines, sortSegLines, ptDic, directionList, linePtDic);

            if (_CommandMode == CommandMode.WithoutUI)
            {
                var dirSetted = General.Utils.SetLayoutMainDirection();
                if (!dirSetted)
                    return;

                var iterationCnt = Active.Editor.GetInteger("\n 请输入迭代次数:");
                if (iterationCnt.Status != PromptStatus.OK) return;

                var popSize = Active.Editor.GetInteger("\n 请输入种群数量:");
                if (popSize.Status != PromptStatus.OK) return;

                ParkingStallArrangementViewModel parameterViewModel = new ParkingStallArrangementViewModel();
                parameterViewModel.IterationCount = iterationCnt.Value;
                parameterViewModel.PopulationCount = popSize.Value;
                geneAlgorithm = new ParkingStallGAGenerator(gaPara, layoutPara, parameterViewModel);
            }
            else
            {
                ParkingPartition.LayoutMode = (int)ParameterViewModel.RunMode;
                geneAlgorithm = new ParkingStallGAGenerator(gaPara, layoutPara,  ParameterViewModel);
            }
            geneAlgorithm.Logger = Logger;

            var rst = new List<Chromosome>();
            var histories = new List<Chromosome>();
            bool recordprevious = false;
            try
            {
                //rst = geneAlgorithm.Run(histories, recordprevious);
                rst = geneAlgorithm.Run2(histories, recordprevious);
            }
            catch
            {
                ;
            }
            var solution = rst.First();
            histories.Add(rst.First());
            for (int k = 0; k < histories.Count; k++)
            {
                layoutPara.Set(histories[k].Genome);
                var layerNames = "solutions" + k.ToString();
                using (AcadDatabase adb = AcadDatabase.Active())
                {
                    try
                    {
                        if(!adb.Layers.Contains(layerNames))
                            ThMEPEngineCoreLayerUtils.CreateAILayer(adb.Database, layerNames, 30);
                    }
                    catch { }
                }

                for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
                {
                    var use_partition_pro = true;
                    if (use_partition_pro)
                    {
                        var partitionpro = new ParkingPartitionPro();
                        ConvertParametersToPartitionPro(layoutPara, j, ref partitionpro, ParameterViewModel);
                        try
                        {
                            partitionpro.ProcessAndDisplay();
                        }
                        catch (Exception ex)
                        {
                            ;
                        }
                        continue;
                    }
                    ParkingPartition partition = new ParkingPartition();
                    if (ConvertParametersToPartition(layoutPara, j, ref partition, ParameterViewModel))
                    {
                        try
                        {
                            partition.ProcessAndDisplay();
                        }
                        catch (Exception ex)
                        {
                            partition.Dispose();
                        }
                    }
                }
            }
            layoutPara.Set(solution.Genome);
            Draw.DrawSeg(solution);
        }

        public void RunWithWindmillSeglineSupported(AcadDatabase acadDatabase)
        {
            var dataprocessingFlag = Preprocessing.DataPreprocessing(acadDatabase, out GaParameter gaPara, out LayoutParameter layoutPara);
            if (!dataprocessingFlag) return;
            ParkingStallGAGenerator geneAlgorithm = null;

            if (_CommandMode == CommandMode.WithoutUI)
            {
                var dirSetted = General.Utils.SetLayoutMainDirection();
                if (!dirSetted)
                    return;

                var iterationCnt = Active.Editor.GetInteger("\n 请输入迭代次数:");
                if (iterationCnt.Status != PromptStatus.OK) return;

                var popSize = Active.Editor.GetInteger("\n 请输入种群数量:");
                if (popSize.Status != PromptStatus.OK) return;

                ParkingStallArrangementViewModel parameterViewModel = new ParkingStallArrangementViewModel();
                parameterViewModel.IterationCount = iterationCnt.Value;
                parameterViewModel.PopulationCount = popSize.Value;
                geneAlgorithm = new ParkingStallGAGenerator(gaPara, layoutPara, parameterViewModel);
            }
            else
            {
                ParkingPartition.LayoutMode = (int)ParameterViewModel.RunMode;
                geneAlgorithm = new ParkingStallGAGenerator(gaPara, layoutPara, ParameterViewModel);
            }
            geneAlgorithm.Logger = Logger;

            var rst = new List<Chromosome>();
            var histories = new List<Chromosome>();
            bool recordprevious = false;
            try
            {
                //rst = geneAlgorithm.Run(histories, recordprevious);
                rst = geneAlgorithm.Run2(histories, recordprevious);
            }
            catch(Exception ex)
            {
                ;
            }
            var solution = rst.First();
            histories.Add(rst.First());
            var parkingStallCount = solution.ParkingStallCount;
            ParkingSpace.GetSingleParkingSpace(Logger,  layoutPara, parkingStallCount);

            for (int k = 0; k < histories.Count; k++)
            {
                layoutPara.Set(histories[k].Genome);
                for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
                {
                    var use_partition_pro = true;
                    if (use_partition_pro)
                    {
                        var partitionpro = new ParkingPartitionPro();
                        ConvertParametersToPartitionPro(layoutPara, j, ref partitionpro, ParameterViewModel);
                        try
                        {
                            partitionpro.ProcessAndDisplay();
                        }
                        catch (Exception ex)
                        {
                            ;
                        }
                        continue;
                    }
                    else
                    {
                        ParkingPartition partition = new ParkingPartition();
                        if (ConvertParametersToPartition(layoutPara, j, ref partition, ParameterViewModel, Logger))
                        {
                            try
                            {
                                partition.ProcessAndDisplay();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex.Message);
                                partition.Dispose();
                            }
                        }
                    }
                }
            }
            layoutPara.Set(solution.Genome);
            Draw.DrawSeg(solution);
            //layoutPara.Dispose();
        }
    }
}
