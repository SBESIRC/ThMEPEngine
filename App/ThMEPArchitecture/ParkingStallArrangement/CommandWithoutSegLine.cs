using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.PartitionLayout;
using ThMEPEngineCore;
using ThMEPEngineCore.Command;
using Draw = ThMEPArchitecture.ParkingStallArrangement.Method.Draw;
using static ThMEPArchitecture.ParkingStallArrangement.ParameterConvert;

namespace ThMEPArchitecture.ParkingStallArrangement
{
    public class WithoutSegLineCmd : ThMEPBaseCommand, IDisposable
    {
        public WithoutSegLineCmd()
        {
            CommandName = "-THDXQYFG3";
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
                    Test(currentDb);
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
        }

        public void Run(AcadDatabase acadDatabase)
        {
            var database = acadDatabase.Database;
            var selectArea = SelectAreas();//生成候选区域
            var outerBrder = new OuterBrder();
            var extractRst = outerBrder.Extract(database, selectArea);//提取多段线
            if (!extractRst)
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

            var geneAlgorithm = new GA(gaPara, layoutPara, popSize.Value, iterationCnt.Value);
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
                    PartitionV3 partition = new PartitionV3();
                    if (ConvertParametersToCalculateCarSpots(layoutPara, j, ref partition))
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
        public void Test(AcadDatabase acadDatabase)
        {
            var database = acadDatabase.Database;
            var selectArea = SelectAreas();//生成候选区域
            var outerBrder = new OuterBrder();
            var extractRst = outerBrder.Extract(database, selectArea);//提取多段线
            if (!extractRst)
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
            var buildNums = outerBrder.Building.Count;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            double threshSecond = 20;
            int throughBuildNums = 0;

            var seglineCnt = buildNums - 1;
            var orderCnt = Convert.ToInt32(Math.Pow(2, seglineCnt));
            var probList = new List<string>();
            for(int i =0; i < orderCnt; i++)
            {
                probList.Add(Convert.ToString(i, 2).PadLeft(seglineCnt,'0'));
            }

            var allSeglines = new List<List<Line>>();
            var okDir = new List<string>();
            for( int i =0; i < probList.Count;i++)
            {
                areas = new List<Polyline>() { area };
                sortSegLines.Clear();
                var num = 0;
                var splitRst = Dfs.dfsSplitWithoutSegline2(num,probList[i], area, throughBuildNums, ref areas, ref sortSegLines, buildLinesSpatialIndex, buildNums, ref maxVals, ref minVals, stopwatch, threshSecond);
                if (splitRst)
                {
                    var newlines = new List<Line>();
                    foreach (var lines in allSeglines)
                    {
                        if(lines.EqualsTo(sortSegLines))
                        {
                            continue;//存在当前解
                        }
                    }

                    sortSegLines.ForEach(l => newlines.Add(l));
                    allSeglines.Add(newlines);
                    okDir.Add(probList[i]);
                }
            }
            for (int i = 0; i < allSeglines.Count; i++)
            {
                try
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, okDir[i], 30);
                }
                catch { }

                var segs = allSeglines[i];
                try
                {
                    foreach (var seg in segs)
                    {
                        seg.Layer = okDir[i];
                        acadDatabase.CurrentSpace.Add(seg);
                    }
                }
                catch (Exception ex)
                {
                    ;
                }

            }
            return;
            sortSegLines = allSeglines[0];
            gaPara.Set(sortSegLines, maxVals, minVals);

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

            var geneAlgorithm = new GA(gaPara, layoutPara, popSize.Value, iterationCnt.Value);
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
                    int index = layoutPara.AreaNumber[j];
                    layoutPara.Id2AllSegLineDic.TryGetValue(index, out List<Line> lanes);
                    layoutPara.Id2AllSubAreaDic.TryGetValue(index, out Polyline boundary);
                    layoutPara.SubAreaId2ShearWallsDic.TryGetValue(index, out List<List<Polyline>> obstaclesList);
                    layoutPara.BuildingBoxes.TryGetValue(index, out List<Polyline> buildingBoxes);
                    layoutPara.SubAreaId2OuterWallsDic.TryGetValue(index, out List<Polyline> walls);
                    layoutPara.SubAreaId2SegsDic.TryGetValue(index, out List<Line> inilanes);
                    var obstacles = new List<Polyline>();
                    obstaclesList.ForEach(e => obstacles.AddRange(e));

                    var Cutters = new DBObjectCollection();
                    obstacles.ForEach(e => Cutters.Add(e));
                    var ObstaclesSpatialIndex = new ThCADCoreNTSSpatialIndex(Cutters);
                    PartitionV3 partition = new PartitionV3(walls, inilanes, obstacles, GeoUtilities.JoinCurves(walls, inilanes)[0], buildingBoxes);
                    partition.ObstaclesSpatialIndex = ObstaclesSpatialIndex;
                    partition.ProcessAndDisplay(layerNames, 30);
                }
            }

            layoutPara.Set(solution.Genome);
            Draw.DrawSeg(solution);
            layoutPara.Dispose();
        }
        private static Point3dCollection SelectAreas()
        {
            using (var pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return new Point3dCollection();
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());
                return frame.Vertices();
            }
        }
    }
}
