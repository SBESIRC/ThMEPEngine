using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
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

namespace ThMEPArchitecture.ParkingStallArrangement
{
    public class ThParkingStallArrangementCmd : ThMEPBaseCommand, IDisposable
    {
        public ThParkingStallArrangementCmd()
        {
            CommandName = "-THDXQYFG";
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
                    CreateAreaSegment(currentDb);
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

        public void CreateAreaSegment(AcadDatabase acadDatabase)
        {
            var database = acadDatabase.Database;
            var selectArea = SelectAreas();//生成候选区域
            var outerBrder = new OuterBrder();
            outerBrder.Extract(database, selectArea);//提取多段线
            var area = outerBrder.OuterLines[0] as Polyline;
            var areas = new List<Polyline>() { area };
            var sortSegLines = new List<Line>();
            var buildLinesSpatialIndex = new ThCADCoreNTSSpatialIndex(outerBrder.BuildingLines);
            var gaPara = new GaParameter(outerBrder.SegLines);
            
            var usedLines = new HashSet<int>();
            Dfs.dfsSplit(ref usedLines, ref areas, ref sortSegLines, buildLinesSpatialIndex, gaPara);
            gaPara.Set(sortSegLines);
            var layoutPara = new LayoutParameter(area, outerBrder.BuildingLines, sortSegLines);

            var iterationCnt = Active.Editor.GetInteger("\n Input GA iteration count:");
            if (iterationCnt.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;

            var popSize = Active.Editor.GetInteger("\n Input population size:");
            if (popSize.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;

            var geneAlgorithm = new GA(gaPara, layoutPara, popSize.Value, iterationCnt.Value);
            var rst = new List<Chromosome>();
            var histories = new List<Chromosome>();
            try
            {
                rst = geneAlgorithm.Run(histories);
            }
            catch (Exception ex)
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
                    layoutPara.SegLineDic.TryGetValue(index, out List<Line> lanes);
                    layoutPara.AreaDic.TryGetValue(index, out Polyline boundary);
                    layoutPara.ObstaclesList.TryGetValue(index, out List<List<Polyline>> obstaclesList);
                    layoutPara.BuildingBoxes.TryGetValue(index, out List<Polyline> buildingBoxes);
                    layoutPara.AreaWalls.TryGetValue(index, out List<Polyline> walls);
                    layoutPara.AreaSegs.TryGetValue(index, out List<Line> inilanes);
                    var obstacles = new List<Polyline>();
                    obstaclesList.ForEach(e => obstacles.AddRange(e));


                    var Cutters = new DBObjectCollection();
                    obstacles.ForEach(e => Cutters.Add(e));
                    var ObstaclesSpatialIndex = new ThCADCoreNTSSpatialIndex(Cutters);
                    PartitionV3 partition = new PartitionV3(walls, inilanes, obstacles, GeoUtilities.JoinCurves(walls, inilanes)[0], buildingBoxes);
                    partition.ObstaclesSpatialIndex = ObstaclesSpatialIndex;

                    partition.Print(layerNames, 30);

                }
            }

            layoutPara.Set(solution.Genome);

            Draw.DrawSeg(solution);
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
