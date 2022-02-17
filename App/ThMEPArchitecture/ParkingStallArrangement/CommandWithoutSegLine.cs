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
            Utils.SetSeed();
            try
            {
                using (var docLock = Active.Document.LockDocument())
                using (AcadDatabase currentDb = AcadDatabase.Active())
                {
                    Logger?.Information($"############################################");
                    Logger?.Information($"自动分割线迭代");
                    //Logger?.Information($"Random Seed:{Utils.GetRandomSeed()}");
                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    var rstDataExtract = InputData.GetOuterBrder(currentDb, out OuterBrder outerBrder, Logger);
                    if (!rstDataExtract)
                    {
                        return;
                    }
                    for (int i = 0; i < ParameterViewModel.LayoutCount; ++i)
                    {
                        Run(currentDb, outerBrder, i);
                    }
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
            Active.Editor.WriteMessage($"seconds: {_stopwatch.Elapsed.TotalSeconds} \n");
            base.AfterExecute();
        }

        public void Run(AcadDatabase acadDatabase, OuterBrder outerBrder, int index = 0)
        {
            var area = outerBrder.WallLine;
            var areas = new List<Polyline>() { area };
            
            var maxVals = new List<double>();
            var minVals = new List<double>();
            var segLinesEx = Dfs.GetRandomSeglines(outerBrder);
            var sortedSegLines = segLinesEx.Select(lex => lex.Segline).ToList();
            var gaPara = new GaParameter(sortedSegLines);
            var buildLinesSpatialIndex = new ThCADCoreNTSSpatialIndex(outerBrder.BuildingLines);
            var usedLines = new List<int>();
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var splitRst = Dfs.dfsSplitTiny(ref areas, gaPara, ref usedLines, buildLinesSpatialIndex, ref maxVals, ref minVals, stopWatch);
            if (!splitRst) return;
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

            gaPara.Set(sortedSegLines, maxVals, minVals);
            var segLineDic = new Dictionary<int, Line>();
            for (int i = 0; i < sortedSegLines.Count; i++)
            {
                segLineDic.Add(i, sortedSegLines[i]);
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
            bool usePline = ParameterViewModel.UsePolylineAsObstacle;
            var layoutPara = new LayoutParameter(area, outerBrder.BuildingLines, sortedSegLines, ptDic,
                directionList, linePtDic, null, areas.Count, usePline, Logger);

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
                geneAlgorithm = new ParkingStallGAGenerator(gaPara, layoutPara, ParameterViewModel,null , BreakFlag);
            }
            else
            {
                ParkingPartition.LayoutMode = (int)ParameterViewModel.RunMode;
                geneAlgorithm = new ParkingStallGAGenerator(gaPara, layoutPara, ParameterViewModel);
            }

            geneAlgorithm.Logger = Logger;

            var rst = new List<Chromosome>();
            var histories = new List<Chromosome>();
            try
            {
                rst = geneAlgorithm.Run2(histories, false);
            }
            catch
            {
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
            var strBest = $"最大车位数{solution.ParkingStallCount}";
            Logger?.Information(strBest);
            Active.Editor.WriteMessage(strBest);
            histories.Add(solution);

            for (int k = 0; k < histories.Count; k++)
            {
                layoutPara.Set(histories[k].Genome);
                if (!Chromosome.IsValidatedSolutions(layoutPara)) continue;
                var Cars = new List<Polyline>();
                var Pillars = new List<Polyline>();
                var Lanes = new List<Line>();
                var Boundary = layoutPara.OuterBoundary;
                var ObstaclesSpacialIndex = layoutPara.AllShearwallsMPolygonSpatialIndex;
                for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
                {
                    var partitionpro = new ParkingPartitionPro();
                    ConvertParametersToPartitionPro(layoutPara, j, ref partitionpro, ParameterViewModel);
                    if (!partitionpro.Validate()) continue;
                    try
                    {
                        partitionpro.Process(Cars, Pillars, Lanes, autoCarSpotLayer, autoColumnLayer);
                    }
                    catch (Exception ex)
                    {
                        ;
                    }
                }
                LayoutPostProcessing.DealWithCarsOntheEndofLanes(ref Cars, ref Pillars, Lanes, ObstaclesSpacialIndex, Boundary, ParameterViewModel);
                var partitionpro_final = new ParkingPartitionPro();
                partitionpro_final.CarSpots = Cars;
                partitionpro_final.Pillars = Pillars;
                partitionpro_final.Display();
            }
            layoutPara.Set(solution.Genome);

            string finalSplitterLayerName = $"AI-最终分割线{index}";
            Draw.DrawSeg(solution, finalSplitterLayerName);

            //if (_CommandMode == CommandMode.WithoutUI)
            //{
            //    var options = new PromptKeywordOptions("\n是否打断迭代：");

            //    options.Keywords.Add("是", "Y", "是(Y)");
            //    options.Keywords.Add("否", "N", "否(N)");

            //    options.Keywords.Default = "是";
            //    var Msg = Active.Editor.GetKeywords(options);
            //    if (Msg.Status != PromptStatus.OK || Msg.StringResult.Equals("否")) return;
                
                
            //    var options2 = new PromptKeywordOptions("\n打断方向：");

            //    options2.Keywords.Add("纵向", "V", "纵向(V)");
            //    options2.Keywords.Add("横向", "H", "横向(H)");

            //    options2.Keywords.Default = "纵向";
            //    var breakMsg = Active.Editor.GetKeywords(options2);
            //    if (breakMsg.Status != PromptStatus.OK) return;
                    
            //    var breakDir = breakMsg.StringResult.Equals("纵向");

            //    var options3 = new PromptKeywordOptions("\n打断顺序：");

            //    options3.Keywords.Add("坐标增加", "P", "坐标增加(P)");
            //    options3.Keywords.Add("坐标减少", "N", "坐标减少(N)");

            //    options3.Keywords.Default = "坐标增加";
            //    var posMsg = Active.Editor.GetKeywords(options3);
            //    if (posMsg.Status != PromptStatus.OK) return;

            //    var posDir = posMsg.StringResult.Equals("坐标增加");
            //    BreakAndOptimize(sortedSegLines_C, outerBrder, rst, breakDir, posDir);
            //}
                
            //layoutPara.Dispose();
        }
        // Note： 分割线打断排布会使用之前的参数（种群数和代数）
        
        public LayoutParameter BreakAndOptimize(List<Line> sortedSegLines, OuterBrder outerBrder, List<Chromosome> Orgsolutions, bool verticaldirection, out Chromosome solution, bool gopositive = true)// 打断，赋值，再迭代,默认正方向打断
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
