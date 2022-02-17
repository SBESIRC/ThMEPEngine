using AcHelper;
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

namespace ThMEPArchitecture.ParkingStallArrangement
{
    public class ThBreakSegLinesCmd : ThMEPBaseCommand, IDisposable
    {
        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "GaLog.txt");

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
            Utils.SetSeed();
            try
            {
                //Logger?.Information($"Random Seed:{Utils.GetRandomSeed()}");
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
            catch(Exception ex)
            {
                ;
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

            var segLinesEx = Dfs.GetRandomSeglines(outerBrder);
            var GenSegLines = segLinesEx.Select(lex => lex.Segline).ToList();
            outerBrder.SegLines = GenSegLines;
            var gaPara = new GaParameter(GenSegLines);

            var geneAlgorithm = new ParkingStallDirectGenerator(gaPara);

            var rst = geneAlgorithm.Run();
            try
            {
                var segbkparam = new SegBreak(outerBrder, gaPara, false, false);
                Draw.DrawSeg(segbkparam.NewSegLines, 0);
                outerBrder.SegLines = segbkparam.NewSegLines;

            }
            catch (Exception ex)
            {
                ;
            }
        }

        public void RunBrSeg(AcadDatabase acadDatabase)// 生成二分生成分割线，然后打断，然后迭代
        {
            var rstDataExtract = InputData.GetOuterBrder(acadDatabase, out OuterBrder outerBrder);
            if (!rstDataExtract)
            {
                return;
            }

            var segLinesEx = Dfs.GetRandomSeglines(outerBrder);
            var GenSegLines = segLinesEx.Select(lex => lex.Segline).ToList();
            outerBrder.SegLines = GenSegLines;
            var GaPara = new GaParameter(GenSegLines);

            //var geneAlgorithm = new ParkingStallDirectGenerator(gaPara);

            var segbkparam = new SegBreak(outerBrder, GaPara, true, true);// 纵向且正方向
            outerBrder.SegLines = segbkparam.NewSegLines;

            bool usePline = ParameterViewModel.UsePolylineAsObstacle;
            Preprocessing.DataPreprocessing(outerBrder, out GaParameter gaPara, out LayoutParameter layoutPara, Logger, false, usePline);

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

                ParameterViewModel.IterationCount = iterationCnt.Value;
                ParameterViewModel.PopulationCount = popSize.Value;
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
            bool recordprevious = false;
            try
            {
                rst = geneAlgorithm.Run2(histories, recordprevious);
            }
            catch (Exception ex)
            {
                ;
            }
            var solution = rst.First();
            histories.Add(rst.First());
            var parkingStallCount = solution.ParkingStallCount;
            ParkingSpace.GetSingleParkingSpace(Logger, layoutPara, parkingStallCount);

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
                        partitionpro.Process(Cars, Pillars, Lanes);
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
            Draw.DrawSeg(solution);
            Draw.DrawSeg(GenSegLines, "自动分割线");
        }


    }
}
