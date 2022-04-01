using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ThCADCore.NTS;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.PartitionLayout;
using ThMEPArchitecture.ParkingStallArrangement.General;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore;
using ThMEPEngineCore.Command;
using Draw = ThMEPArchitecture.ParkingStallArrangement.Method.Draw;
using static ThMEPArchitecture.ParkingStallArrangement.ParameterConvert;
using ThMEPArchitecture.ViewModel;
using Serilog;
using System.IO;

namespace ThMEPArchitecture.ParkingStallArrangement
{
    public class WithoutSegLineCmd : ThMEPBaseCommand, IDisposable
    {
        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "AutoSeglineLog.txt");

        public Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
            .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10).CreateLogger();
        public static ParkingStallArrangementViewModel ParameterViewModel { get; set; }
        private CommandMode _CommandMode { get; set; } = CommandMode.WithoutUI;

        public WithoutSegLineCmd()//根据自动生成的分割线得到车位排布结果
        {
            CommandName = "-THWFGXCWBZ";//天华无分割线车位布置
            ActionName = "生成";
            _CommandMode = CommandMode.WithoutUI;
            ParameterViewModel = new ParkingStallArrangementViewModel();
            ParameterViewModel.JustCreateSplittersChecked = false;
        }

        public WithoutSegLineCmd(ParkingStallArrangementViewModel vm)//根据自动生成的分割线得到车位排布结果
        {
            CommandName = "THZDCWBZ";
            ActionName = "自动分割线迭代";//天华无分割线车位布置
            ParameterViewModel = vm;
            _CommandMode = CommandMode.WithUI;
        }

        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            ParameterStock.Set(ParameterViewModel);
            Utils.SetSeed();
            try
            {
                using (var docLock = Active.Document.LockDocument())
                using (AcadDatabase currentDb = AcadDatabase.Active())
                {
                    if (_CommandMode == CommandMode.WithUI)
                    {
                        Logger?.Information($"############################################");
                        Logger?.Information($"自动分割线迭代");
                        Logger?.Information($"Random Seed:{Utils.GetSeed()}");
                        var stopWatch = new Stopwatch();
                        stopWatch.Start();
                        var rstDataExtract = InputData.GetOuterBrder(currentDb, out OuterBrder outerBrder, Logger);
                        if (!rstDataExtract)
                        {
                            return;
                        }
                        for (int i = 0; i < ParameterViewModel.LayoutCount; ++i)
                        {
                            RunWithWindmillSeglineSupported(currentDb, outerBrder, i);
                        }
                        stopWatch.Stop();
                        var strTotalMins = $"总运行时间: {stopWatch.Elapsed.TotalMinutes} 分";
                        Logger?.Information(strTotalMins);

                    }
                    else//生成二分全部方案
                    {
                        var rstDataExtract = InputData.GetOuterBrder(currentDb, out OuterBrder outerBrder, Logger);
                        var autogen = new AutoSegGenerator(outerBrder, Logger);
                        autogen.Run(false);
                    }
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
            Active.Editor.WriteMessage($"seconds: {_stopwatch.Elapsed.TotalSeconds} \n");
            base.AfterExecute();
        }

        public void RunWithWindmillSeglineSupported(AcadDatabase acadDatabase, OuterBrder outerBrder, int index = 0)
        {
            var area = outerBrder.WallLine;
            var areas = new List<Polyline>() { area };

            var maxVals = new List<double>();
            var minVals = new List<double>();
            var segLinesEx = Dfs.GetRandomSeglines(outerBrder);
            if(segLinesEx.Count() < outerBrder.Buildings.Count - 1)
            {
                Active.Editor.WriteMessage("分割线生成失败！");
                return;
            }
            var sortedSegs = segLinesEx.Select(lex => lex.Segline).ToList();

            var sortedSegLines = SeglineTools.SeglinePrecut(sortedSegs, area);
            bool usePline = ParameterViewModel.UsePolylineAsObstacle;           

            var autoSpliterLayerName = $"AI-自动分割线{index}";
            if (!acadDatabase.Layers.Contains(autoSpliterLayerName))
                ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, autoSpliterLayerName, 30);

            var SegLines_C = new List<Line>();
            sortedSegLines.ForEach(l => SegLines_C.Add(l.Clone() as Line));
            foreach (var seg in sortedSegLines)
            {
                seg.Layer = autoSpliterLayerName;
                acadDatabase.CurrentSpace.Add(seg);
            }

            if (ParameterViewModel.JustCreateSplittersChecked) return;

            outerBrder.SegLines = sortedSegLines;
            var dataPreprocessingFlag = Preprocessing.DataPreprocessing(outerBrder, out GaParameter gaPara, out LayoutParameter layoutPara, Logger, false, usePline);
            if (!dataPreprocessingFlag)
            {
                return;
            }

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
                BreakFlag = Msg.StringResult.Equals("是");
                //输入
                var iterationCnt = Active.Editor.GetInteger("\n 请输入迭代次数:");
                if (iterationCnt.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;

                var popSize = Active.Editor.GetInteger("\n 请输入种群数量:");
                if (popSize.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;

                ParameterViewModel.IterationCount = iterationCnt.Value;
                ParameterViewModel.PopulationCount = popSize.Value;
                ParameterViewModel.MaxTimespan = 180;
                geneAlgorithm = new ParkingStallGAGenerator(gaPara, layoutPara, ParameterViewModel, null, BreakFlag);
            }
            else
            {
                ParkingPartitionPro.LayoutMode = (int)ParameterViewModel.RunMode;
                BreakFlag = ParameterViewModel.OptmizeThenBreakSeg;
                geneAlgorithm = new ParkingStallGAGenerator(gaPara, layoutPara, ParameterViewModel,null, BreakFlag);
            }

            geneAlgorithm.Logger = Logger;

            var rst = new List<Chromosome>();
            var histories = new List<Chromosome>();
            try
            {
                rst = geneAlgorithm.Run2(histories, false);
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }

            string autoCarSpotLayer = $"AI-停车位{index}";
            string autoColumnLayer = $"AI-柱子{index}";

            //var solution = rst.First();
            //histories.Add(rst.First());

            Chromosome solution = rst.First();

            if (BreakFlag)
            {
                Logger?.Information($"############################################");
                Logger?.Information($"垂直打断迭代");
                var layoutParaVB = Functions.BreakAndOptimize(SegLines_C, outerBrder, ParameterViewModel, Logger, out Chromosome solutionVB, true, rst);// 垂直打断
                Logger?.Information($"############################################");
                Logger?.Information($"水平打断迭代");
                var layoutParaHB = Functions.BreakAndOptimize(SegLines_C, outerBrder, ParameterViewModel, Logger, out Chromosome solutionHB, false, rst);// 横向打断

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
            var strBest = $"最大车位数{solution.ParkingStallCount}";
            Logger?.Information(strBest);
            Active.Editor.WriteMessage(strBest);
            histories.Add(solution);

            for (int k = 0; k < histories.Count; k++)
            {
                layoutPara.Set(histories[k].Genome);
                if (!Chromosome.IsValidatedSolutions(layoutPara)) continue;
                var Cars = new List<InfoCar>();
                var Pillars = new List<Polyline>();
                var Walls = new List<Polyline>();
                var Lanes = new List<Line>();
                var Boundary = layoutPara.OuterBoundary;
                var ObstaclesSpacialIndex = layoutPara.AllShearwallsMPolygonSpatialIndex;
                for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
                {
                    var partitionpro = new ParkingPartitionPro();
                    ConvertParametersToPartitionPro(layoutPara, j, ref partitionpro, ParameterViewModel);
                    Walls.AddRange(partitionpro.Walls);
                    if (!partitionpro.Validate()) continue;
                    try
                    {
                        partitionpro.Process(Cars, Pillars, Lanes, autoCarSpotLayer, autoColumnLayer);
                    }
                    catch (Exception ex)
                    {
                        Active.Editor.WriteMessage(ex.Message);
                    }
                }
                LayoutPostProcessing.DealWithCarsOntheEndofLanes(ref Cars, ref Pillars,ref Lanes, Walls, ObstaclesSpacialIndex, Boundary, ParameterViewModel);
                var partitionpro_final = new ParkingPartitionPro();
                partitionpro_final.Cars = Cars;
                partitionpro_final.Pillars = Pillars;
                partitionpro_final.OutputLanes = Lanes;
                partitionpro_final.Display();
            }
            layoutPara.Set(solution.Genome);

            string finalSplitterLayerName = $"AI-最终分割线{index}";
            Draw.DrawSeg(solution, finalSplitterLayerName);
        }
    }
}
