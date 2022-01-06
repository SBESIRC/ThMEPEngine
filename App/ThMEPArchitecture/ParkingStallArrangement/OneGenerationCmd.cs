using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    public class OneGenerationCmd : ThMEPBaseCommand, IDisposable
    {
        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "GaLog.txt");

        public Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
            .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Hour).CreateLogger();

        public OneGenerationCmd()
        {
            CommandName = "-THDXQYFG2";
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

            var usedLines = new HashSet<int>();
            var maxVals = new List<double>();
            var minVals = new List<double>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            double threshSecond = 20;

            var splitRst = Dfs.dfsSplit(ref usedLines, ref areas, ref sortSegLines, buildLinesSpatialIndex, gaPara, ref maxVals, ref minVals, stopwatch, threshSecond);
            if (!splitRst)
            {
                Logger?.Information("分割线不合理，分区失败！");
                return;
            }
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
                var flag = General.Utils.RandDouble() < 0.5;
                directionList.Add(num, flag);//默认给全横向
            }

            var layoutPara = new LayoutParameter(area, outerBrder.BuildingLines, sortSegLines, ptDic, directionList, linePtDic);
            var geneAlgorithm = new GA2(gaPara);

            var rst = geneAlgorithm.Run();

            layoutPara.Set(rst);
            var layerNames = "solutions0";
            using (AcadDatabase adb = AcadDatabase.Active())
            {
                try
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(adb.Database, layerNames, 30);
                }
                catch { }
            }
            int count = 0;
            for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
            {
                ParkingPartition partition = new ParkingPartition();
                if (ConvertParametersToCalculateCarSpots(layoutPara, j, ref partition, Logger))
                {
                    try
                    {
                        count += partition.ProcessAndDisplay(layerNames, 30);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex.Message);
                        partition.Dispose();
                    }
                }
            }
            layoutPara.Dispose();
            Active.Editor.WriteMessage("Count of car spots: " + count.ToString() + "\n");
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
