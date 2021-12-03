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
            var layoutPara = new LayoutParameter(area, outerBrder.BuildLines, sortSegLines);

            var iterationCnt = Active.Editor.GetInteger("\n Input GA iteration count:");
            if (iterationCnt.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;

            var popSize = Active.Editor.GetInteger("\n Input population size:");
            if (popSize.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK) return;

            var geneAlgorithm = new GA(gaPara, layoutPara, popSize.Value, iterationCnt.Value);
            var rst = new List<Chromosome>();
            try
            {
                rst = geneAlgorithm.Run();
            }
            catch (Exception ex)
            {

            }

            var solution = rst.First();
            layoutPara.Set(solution.Genome);

            
            for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
            {
                int index = layoutPara.AreaNumber[j];
                layoutPara.SegLineDic.TryGetValue(index, out List<Line> lanes);
                layoutPara.AreaDic.TryGetValue(index, out Polyline boundary);
                layoutPara.ObstacleDic.TryGetValue(index, out List<Polyline> obstacles);
                layoutPara.AreaWalls.TryGetValue(index, out List<Polyline> walls);
                layoutPara.AreaSegs.TryGetValue(index, out List<Line> inilanes);
                ParkingPartition p = new ParkingPartition(walls, inilanes, obstacles, boundary);
                bool valid = p.Validate();
                if (valid)
                {
                    p.Initialize();
                    p.Display();
                }
            }
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
