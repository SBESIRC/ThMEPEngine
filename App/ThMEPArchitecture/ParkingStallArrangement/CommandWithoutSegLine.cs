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
            CommandName = "THZDCWPZ";
            ActionName = "自动分割线迭代";//天华无分割线车位布置
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
                    var rstDataExtract = InputData.GetOuterBrder(currentDb, out OuterBrder outerBrder, Logger);
                    if (!rstDataExtract)
                    {
                        return;
                    }
                    for (int i = 0; i < ParameterViewModel.LayoutCount; ++i)
                    {
                        Run(currentDb, outerBrder, i);
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
            if (_CommandMode == CommandMode.WithoutUI)
            {
                var dirSetted = General.Utils.SetLayoutMainDirection();
                if (!dirSetted)
                    return;

                //输入
                var iterationCnt = Active.Editor.GetInteger("\n 请输入迭代次数:");
                if (iterationCnt.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;

                var popSize = Active.Editor.GetInteger("\n 请输入种群数量:");
                if (popSize.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;

                ParameterViewModel.IterationCount = iterationCnt.Value;
                ParameterViewModel.PopulationCount = popSize.Value;
                ParameterViewModel.MaxTimespan = 180;
                geneAlgorithm = new ParkingStallGAGenerator(gaPara, layoutPara, ParameterViewModel);
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

            var solution = rst.First();
            histories.Add(rst.First());
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
                            partitionpro.ProcessAndDisplay(autoCarSpotLayer, autoColumnLayer);
                        }
                        catch (Exception ex)
                        {
                            ;
                        }
                    }
                }
            }

            layoutPara.Set(solution.Genome);

            string finalSplitterLayerName = $"AI-最终分割线{index}";
            Draw.DrawSeg(solution, finalSplitterLayerName);
            //layoutPara.Dispose();
        }
    }
}
