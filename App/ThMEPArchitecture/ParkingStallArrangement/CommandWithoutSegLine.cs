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

namespace ThMEPArchitecture.ParkingStallArrangement
{
    public class WithoutSegLineCmd : ThMEPBaseCommand, IDisposable
    {
        public WithoutSegLineCmd()//根据自动生成的分割线得到车位排布结果
        {
            CommandName = "-THWFGXCWBZ";//天华无分割线车位布置
            ActionName = "生成";
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
                    Run(currentDb);
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
        }

        public override void AfterExecute()
        {
            Active.Editor.WriteMessage($"seconds: {_stopwatch.Elapsed.TotalSeconds} \n");
            base.AfterExecute();
        }

        public void Run(AcadDatabase acadDatabase)
        {
            var rstDataExtract = InputData.GetOuterBrder(acadDatabase, out OuterBrder outerBrder);
            if (!rstDataExtract)
            {
                return;
            }
            var area = outerBrder.WallLine;
            var areas = new List<Polyline>() { area };
            var sortSegLines = new List<Line>();
            var buildLinesSpatialIndex = new ThCADCoreNTSSpatialIndex(outerBrder.BuildingLines);
            var gaPara = new GaParameter(outerBrder.SegLines);

            var maxVals = new List<double>();
            var minVals = new List<double>();
            var buildNums = outerBrder.Building.Count;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            double threshSecond = 20;
            int throughBuildNums = 0;

            var splitRst = Dfs.dfsSplitWithoutSegline(area, throughBuildNums, ref areas, ref sortSegLines, buildLinesSpatialIndex, buildNums, ref maxVals, ref minVals, stopwatch, threshSecond);
            if(!splitRst)
            {
                return;
            }

            gaPara.Set(sortSegLines, maxVals, minVals);
            foreach (var seg in sortSegLines)
            {
                acadDatabase.CurrentSpace.Add(seg);
            }
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

            var layoutPara = new LayoutParameter(area, outerBrder.BuildingLines, sortSegLines, ptDic, directionList, linePtDic);

            var iterationCnt = Active.Editor.GetInteger("\n 请输入迭代次数:");
            if (iterationCnt.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;

            var popSize = Active.Editor.GetInteger("\n 请输入种群数量:");
            if (popSize.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;

            //输入对象
            ParkingStallArrangementViewModel parameterViewModel = new ParkingStallArrangementViewModel();
            parameterViewModel.IterationCount = iterationCnt.Value;
            parameterViewModel.PopulationCount = popSize.Value;
            var geneAlgorithm = new ParkingStallGAGenerator(gaPara, layoutPara, parameterViewModel);

            var rst = new List<Chromosome>();
            var histories = new List<Chromosome>();
            try
            {
                rst = geneAlgorithm.Run(histories, false);
            }
            catch
            {
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
                        ThMEPEngineCoreLayerUtils.CreateAILayer(adb.Database, layerNames, 30);
                    }
                    catch { }
                }

                for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
                {
                    ParkingPartition partition = new ParkingPartition();
                    if (ConvertParametersToCalculateCarSpots(layoutPara, j, ref partition, parameterViewModel))
                    {
                        try
                        {
                            partition.ProcessAndDisplay(layerNames, 30);
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
            layoutPara.Dispose();
        }
    }
}
