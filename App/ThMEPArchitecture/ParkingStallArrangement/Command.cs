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
            ParameterViewModel = new ParkingStallArrangementViewModel();
        }

        public ThParkingStallArrangementCmd(ParkingStallArrangementViewModel vm)
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
            try
            {
                using (var docLock = Active.Document.LockDocument())
                using (AcadDatabase currentDb = AcadDatabase.Active())
                {
                    Logger?.Information($"############################################");
                    Logger?.Information($"手动分割线迭代");
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();

                    RunWithWindmillSeglineSupported(currentDb);

                    stopWatch.Stop();
                    var strTotalMins = $"总运行时间: {stopWatch.Elapsed.TotalMinutes} 分";
                    Logger?.Information(strTotalMins);
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
            var rstDataExtract = InputData.GetOuterBrder(acadDatabase, out OuterBrder outerBrder, Logger);
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
                parameterViewModel.MaxTimespan = 180;
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
                if (!Chromosome.IsValidatedSolutions(layoutPara)) continue;

                for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
                {
                    var use_partition_pro = true;
                    if (use_partition_pro)
                    {
                        var partitionpro = new ParkingPartitionPro();
                        ConvertParametersToPartitionPro(layoutPara, j, ref partitionpro, ParameterViewModel);
                        if (!partitionpro.Validate()) continue;
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
            bool usePline = ParameterViewModel.UsePolylineAsObstacle;

            var dataprocessingFlag = Preprocessing.GetOuterBrder(acadDatabase, out OuterBrder outerBrder, Logger);
            if (!dataprocessingFlag) return;
            Preprocessing.DataPreprocessing(outerBrder, out GaParameter gaPara, out LayoutParameter layoutPara, Logger, false, usePline);

            var SegLines_C = new List<Line>();
            outerBrder.SegLines.ForEach(l => SegLines_C.Add(l.Clone() as Line));

            ParkingStallGAGenerator geneAlgorithm = null;

            bool BreakFlag = false;// 是否进行打断
            if (_CommandMode == CommandMode.WithoutUI)
            {
                var dirSetted = General.Utils.SetLayoutMainDirection();
                if (!dirSetted)
                    return;
                var options = new PromptKeywordOptions("\n是否打断迭代：");
                options.Keywords.Add("是", "Y", "是(Y)");
                options.Keywords.Add("否", "N", "否(N)");

                options.Keywords.Default = "是";
                var Msg = Active.Editor.GetKeywords(options);
                if (Msg.Status != PromptStatus.OK) return;
                BreakFlag =  Msg.StringResult.Equals("是");

                var iterationCnt = Active.Editor.GetInteger("\n 请输入迭代次数:");
                if (iterationCnt.Status != PromptStatus.OK) return;

                var popSize = Active.Editor.GetInteger("\n 请输入种群数量:");
                if (popSize.Status != PromptStatus.OK) return;

                ParameterViewModel.IterationCount = iterationCnt.Value;
                ParameterViewModel.PopulationCount = popSize.Value;
                geneAlgorithm = new ParkingStallGAGenerator(gaPara, layoutPara, ParameterViewModel, null ,BreakFlag );
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
            Chromosome solution = rst.First();

            if (BreakFlag)
            {
                Logger?.Information($"############################################");
                Logger?.Information($"垂直打断迭代");
                var layoutParaVB = BreakAndOptimize(SegLines_C, outerBrder, rst, true, out Chromosome solutionVB);// 垂直打断
                Logger?.Information($"############################################");
                Logger?.Information($"水平打断迭代");
                var layoutParaHB = BreakAndOptimize(SegLines_C, outerBrder, rst, false, out Chromosome solutionHB);// 横向打断

                if (solutionVB.ParkingStallCount > solution.ParkingStallCount)// 垂直打断比初始优
                {
                    solution = solutionVB;
                    layoutPara = layoutParaVB;
                }
                if (solutionHB.ParkingStallCount > solution.ParkingStallCount)//横向打断最优
                {
                    solution = solutionHB;
                    layoutPara = layoutParaHB;
                }
            }
            Logger?.Information($"最大车位数{solution.ParkingStallCount}");
            histories.Add(solution);
            var parkingStallCount = solution.ParkingStallCount;
            ParkingSpace.GetSingleParkingSpace(Logger,  layoutPara, parkingStallCount);

            for (int k = 0; k < histories.Count; k++)
            {
                layoutPara.Set(histories[k].Genome);
                if (!Chromosome.IsValidatedSolutions(layoutPara)) continue;
                for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
                {
                    var use_partition_pro = true;
                    if (use_partition_pro)
                    {
                        var partitionpro = new ParkingPartitionPro();
                        ConvertParametersToPartitionPro(layoutPara, j, ref partitionpro, ParameterViewModel);
                        if (!partitionpro.Validate()) continue;
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
        
        public LayoutParameter BreakAndOptimize(List<Line> sortedSegLines, OuterBrder outerBrder, List<Chromosome> Orgsolutions,bool verticaldirection ,out Chromosome solution, bool gopositive = true)// 打断，赋值，再迭代,默认正方向打断
        {
            outerBrder.SegLines = sortedSegLines;// 之前的分割线
            var GaPara = new GaParameter(sortedSegLines);
            //var geneAlgorithm = new ParkingStallDirectGenerator(gaPara);


            var segbkparam = new SegBreak(outerBrder, GaPara, verticaldirection, gopositive);
            outerBrder.SegLines = new List<Line>();
            segbkparam.NewSegLines.ForEach(l => outerBrder.SegLines.Add(l.Clone() as Line));// 复制打断后的分割线
            bool usePline = ParameterViewModel.UsePolylineAsObstacle;
            Preprocessing.DataPreprocessing(outerBrder, out GaParameter gaPara, out LayoutParameter layoutPara, Logger, false, usePline);

            // gaparam 赋值
            var initgenomes = segbkparam.TransPreSols(ref gaPara, Orgsolutions);

            ParkingStallGAGenerator geneAlgorithm = null;

            geneAlgorithm = new ParkingStallGAGenerator(gaPara, layoutPara, ParameterViewModel, initgenomes);
            geneAlgorithm.Logger = Logger;

            var rst = new List<Chromosome>();
            var histories = new List<Chromosome>();
            bool recordprevious = false;
            try
            {
                rst = geneAlgorithm.Run2(histories, recordprevious);
            }
            catch (Exception ex)
            {
                ;
            }

            solution = rst.First();
            return layoutPara;
#if (DEBUG)
            string layer;
            if (verticaldirection) layer = "AI-垂直打断后初始分割线-Debug";
            else layer = "AI-水平打断后初始分割线-Debug";
            Draw.DrawSeg(segbkparam.NewSegLines, layer);
            Draw.DrawSeg(sortedSegLines, "AI-打断前分割线-Debug");
#endif
        }
    }
}
