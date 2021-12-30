﻿using AcHelper;
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
            var database = acadDatabase.Database;
            var selectArea = SelectAreas();//生成候选区域
            var outerBrder = new OuterBrder();
            outerBrder.Extract(database, selectArea);//提取多段线
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
                int index = layoutPara.AreaNumber[j];
                layoutPara.SegLineDic.TryGetValue(index, out List<Line> lanes);
                layoutPara.AreaDic.TryGetValue(index, out Polyline boundary);
                layoutPara.ObstaclesList.TryGetValue(index, out List<List<Polyline>> obstacleList);
                //layoutPara.BuildingBoxes.TryGetValue(index, out List<Polyline> buildingBoxes);
                layoutPara.AreaWalls.TryGetValue(index, out List<Polyline> walls);
                layoutPara.AreaSegs.TryGetValue(index, out List<Line> inilanes);

                List<Polyline> buildingBoxes = new List<Polyline>();
                var bound = GeoUtilities.JoinCurves(walls, inilanes)[0];
                var obstacleListNew = new List<List<Polyline>>();
                for (int i = 0; i < obstacleList.Count; i++)
                {
                    var pls = obstacleList[i];
                    obstacleListNew.Add(new List<Polyline>());
                    foreach (var pl in pls)
                    {
                        obstacleListNew[obstacleListNew.Count - 1].AddRange((GeoUtilities.SplitCurve(pl, bound).Where(e => bound.IsPointIn(e.GetCenter())).Select(e =>
                          {
                              if (e is Polyline) return (Polyline)e;
                              else return GeoUtilities.PolyFromLine((Line)e);
                          })));
                    }
                }
                foreach (var obs in obstacleListNew)
                {
                    if (obs.Count == 0) continue;
                    Extents3d ext = new Extents3d();
                    foreach (var pl in obs) ext.AddExtents(pl.GeometricExtents);
                    buildingBoxes.Add(ext.ToRectangle());
                }

                var obstacles = new List<Polyline>();
                obstacleList.ForEach(e => obstacles.AddRange(e));
                var Cutters = new DBObjectCollection();
                obstacles.ForEach(e => Cutters.Add(e));
                var ObstaclesSpatialIndex = new ThCADCoreNTSSpatialIndex(Cutters);
                var CuttersM = new DBObjectCollection();
                obstacles.ForEach(e => CuttersM.Add(e.ToNTSPolygon().ToDbMPolygon()));
                var ObstaclesMpolygonSpatialIndex = new ThCADCoreNTSSpatialIndex(CuttersM);

                string w = "";
                string l = "";
                foreach (var e in walls)
                {
                    foreach (var pt in e.Vertices().Cast<Point3d>().ToList())
                        w += pt.X.ToString() + "," + pt.Y.ToString() + ",";
                }
                foreach (var e in inilanes)
                {
                    l += e.StartPoint.X.ToString() + "," + e.StartPoint.Y.ToString() + ","
                        + e.EndPoint.X.ToString() + "," + e.EndPoint.Y.ToString() + ",";
                }
#if DEBUG
                FileStream fs1 = new FileStream("D:\\GALog.txt", FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs1);
                sw.WriteLine(w);
                sw.WriteLine(l);
                sw.Close();
                fs1.Close();
#endif
                inilanes = inilanes.Distinct().ToList();
                PartitionV3 partition = new PartitionV3(walls, inilanes, obstacles, bound, buildingBoxes);
                partition.ObstaclesSpatialIndex = ObstaclesSpatialIndex;
                partition.ObstaclesMPolygonSpatialIndex = ObstaclesMpolygonSpatialIndex;
                try
                {
                    count += partition.ProcessAndDisplay(layerNames, 30);
                }
                catch (Exception ex)
                {
                    ;
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
