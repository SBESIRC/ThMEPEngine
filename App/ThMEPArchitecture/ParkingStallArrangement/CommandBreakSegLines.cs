using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.PartitionLayout;
using ThMEPEngineCore.Command;
using Draw = ThMEPArchitecture.ParkingStallArrangement.Method.Draw;
using static ThMEPArchitecture.ParkingStallArrangement.ParameterConvert;
using Autodesk.AutoCAD.EditorInput;
using ThMEPArchitecture.ViewModel;
using ThMEPArchitecture.ParkingStallArrangement.General;
using Autodesk.AutoCAD.Geometry;
using ThParkingStall.Core.Tools;

namespace ThMEPArchitecture.ParkingStallArrangement
{
    public class ThBreakSegLinesCmd : ThMEPBaseCommand, IDisposable
    {
        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "SegBreakLog.txt");

        public Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
            .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day).CreateLogger();

        public static ParkingStallArrangementViewModel ParameterViewModel { get; set; }

        private CommandMode _CommandMode { get; set; } = CommandMode.WithoutUI;
        public ThBreakSegLinesCmd()
        {
            CommandName = "-THFGXDD";//天华分割线打断
            ActionName = "生成";
            _CommandMode = CommandMode.WithoutUI;
            ParameterViewModel = new ParkingStallArrangementViewModel();
        }

        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            ParameterStock.Set(ParameterViewModel);
            ThParkingStallCoreTools.SetSeed();
            Logger?.Information($"Random Seed:{ThParkingStallCoreTools.GetSeed()}");
            try
            {
                using (var docLock = Active.Document.LockDocument())
                using (AcadDatabase currentDb = AcadDatabase.Active())
                {
                    //Run(currentDb);
                    //DrawBrWithoutSeg(currentDb);
                    RunBrSeg(currentDb);
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }

        public override void AfterExecute()
        {
            base.AfterExecute();
            Active.Editor.WriteMessage($"seconds: {_stopwatch.Elapsed.TotalSeconds} \n");
            base.AfterExecute();
        }
        public void DrawBreakedSeg(AcadDatabase acadDatabase)
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
            var gaPara = new GaParameter(outerBrder.SegLines);


            var geneAlgorithm = new ParkingStallDirectGenerator(gaPara);

            var rst = geneAlgorithm.Run();
            try
            {
                var segbkparam = new SegBreak(outerBrder, gaPara, false, false);
                Draw.DrawSeg(segbkparam.NewSegLines, 0);

            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }

            //foreach(Polyline pline in segbkparam.BufferTanks)
            //{
            //    acadDatabase.CurrentSpace.Add(pline);
            //}
        }

        public void DrawBrWithoutSeg(AcadDatabase acadDatabase)// 画出由二分生成的，打断后的分割线
        {
            var rstDataExtract = InputData.GetOuterBrder(acadDatabase, out OuterBrder outerBrder);
            if (!rstDataExtract)
            {
                return;
            }

            var segLinesEx = Dfs.GetRandomSeglines(outerBrder,1);
            var GenSegLines = segLinesEx.Select(lex => lex.Segline).ToList();
            outerBrder.SegLines = GenSegLines;
            var gaPara = new GaParameter(GenSegLines);

            var geneAlgorithm = new ParkingStallDirectGenerator(gaPara);

            var rst = geneAlgorithm.Run();
            try
            {
                var segbkparam = new SegBreak(outerBrder, gaPara, true);
                Draw.DrawSeg(segbkparam.NewSegLines, 0);
                outerBrder.SegLines = segbkparam.NewSegLines;

            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }
        public void RunBrSeg(AcadDatabase acadDatabase)// 生成二分生成分割线，然后打断，然后迭代
        {
            var rstDataExtract = InputData.GetOuterBrder(acadDatabase, out OuterBrder outerBrder);
            if (!rstDataExtract)
            {
                return;
            }
            if (_CommandMode == CommandMode.WithoutUI)
            {
                var dirSetted = General.Utils.SetLayoutMainDirection();
                if (!dirSetted)
                    return;
                var iterationCnt = Active.Editor.GetInteger("\n 请输入迭代次数:");
                if (iterationCnt.Status != PromptStatus.OK) return;

                var popSize = Active.Editor.GetInteger("\n 请输入种群数量:");
                if (popSize.Status != PromptStatus.OK) return;

                ParameterViewModel.IterationCount = iterationCnt.Value;
                ParameterViewModel.PopulationCount = popSize.Value;
            }
            else
            {
                ParkingPartitionPro.LayoutMode = ParameterStock.RunMode;
            }

            LayoutParameter layoutPara;// 最优的分割方案（横向或者纵向优先
            Chromosome solution;// 最优解
            List<Line> GenSegLines;
            Logger?.Information($"############################################");
            Logger?.Information($"垂直打断迭代");
            var segLinesEx = Dfs.GetRandomSeglines(outerBrder,1);
            var GenSegLinesV = segLinesEx.Select(lex => lex.Segline).ToList();

            var layoutParaVB = Functions.BreakAndOptimize(GenSegLinesV, outerBrder, ParameterViewModel, Logger, out Chromosome solutionVB, true, null,true);// 垂直打断,只用特殊解
            Logger?.Information($"############################################");
            Logger?.Information($"水平打断迭代");
            segLinesEx = Dfs.GetRandomSeglines(outerBrder, -1);
            var GenSegLinesH = segLinesEx.Select(lex => lex.Segline).ToList();

            var layoutParaHB = Functions.BreakAndOptimize(GenSegLinesH, outerBrder, ParameterViewModel, Logger, out Chromosome solutionHB, false, null, true);// 横向打断,只用特殊解
            
            if (solutionVB.ParkingStallCount > solutionHB.ParkingStallCount)// 垂直打断比横向优
            {
                solution = solutionVB;
                layoutPara = layoutParaVB;
                GenSegLines = GenSegLinesV;
            }
            else//横向打断最优
            {
                solution = solutionHB;
                layoutPara = layoutParaHB;
                GenSegLines = GenSegLinesH;
            }
            
            var strBest = $"最大车位数{solution.ParkingStallCount}";
            Logger?.Information(strBest);
            Active.Editor.WriteMessage(strBest);

            var histories = new List<Chromosome>();
            histories.Add(solution);

            var parkingStallCount = solution.ParkingStallCount;
            ParkingSpace.GetSingleParkingSpace(Logger, layoutPara, parkingStallCount);

            for (int k = 0; k < histories.Count; k++)
            {
                layoutPara.Set(histories[k].Genome);
                if (!Chromosome.IsValidatedSolutions(layoutPara)) continue;
                var Walls = new List<Polyline>();
                var Cars = new List<InfoCar>();
                var Pillars = new List<Polyline>();
                var ObsVertices = new List<Point3d>();
                var IniPillars = new List<Polyline>();
                var Lanes = new List<Line>();
                var Boundary = layoutPara.OuterBoundary;
                var ObstaclesSpacialIndex = layoutPara.AllShearwallsMPolygonSpatialIndex;
                for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
                {
                    var partitionpro = new ParkingPartitionPro();
                    ConvertParametersToPartitionPro(layoutPara, j, ref partitionpro, ParameterViewModel);
                    Walls.AddRange(partitionpro.Walls);
                    if (!partitionpro.Validate()) continue;
                    ObsVertices.AddRange(partitionpro.ObstacleVertexes);
                    try
                    {
                        partitionpro.Process(Cars, Pillars, Lanes, IniPillars);
                    }
                    catch (Exception ex)
                    {
                        Active.Editor.WriteMessage(ex.Message);
                    }
                }
                LayoutPostProcessing.DealWithCarsOntheEndofLanes(ref Cars, ref Pillars,ref Lanes, Walls, ObstaclesSpacialIndex, Boundary, ParameterViewModel);
                LayoutPostProcessing.PostProcessLanes(ref Lanes, Cars.Select(e => e.Polyline).ToList(), IniPillars, ObsVertices);
                var partitionpro_final = new ParkingPartitionPro();
                partitionpro_final.Cars = Cars;
                partitionpro_final.Pillars = Pillars;
                partitionpro_final.OutputLanes = Lanes;
                partitionpro_final.Display();
            }
            layoutPara.Set(solution.Genome);
            Draw.DrawSeg(solution);
            Draw.DrawSeg(GenSegLines, "自动分割线");
        }
    }
}