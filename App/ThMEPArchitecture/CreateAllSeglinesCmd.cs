using AcHelper;
using Linq2Acad;
using System;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPEngineCore.Command;
using ThMEPArchitecture.ViewModel;
using System.IO;
using Serilog;
using ThMEPArchitecture.ParkingStallArrangement.PreProcess;
using ThParkingStall.Core.OInterProcess;
using ThMEPArchitecture.MultiProcess;
using ThParkingStall.Core.ObliqueMPartitionLayout;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using ThParkingStall.Core.MPartitionLayout;
using ThMEPArchitecture.PartitionLayout;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThParkingStall.Core.IO;
using Autodesk.AutoCAD.ApplicationServices;
using System.Diagnostics;

namespace ThMEPArchitecture
{
    public class CreateAllSeglinesCmd : ThMEPBaseCommand, IDisposable
    {
        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "SeglineLog.txt");

        public Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
            .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day, retainedFileCountLimit:10).CreateLogger();
        public static ParkingStallArrangementViewModel ParameterViewModel { get; set; }
        public string DrawingName;
        public CreateAllSeglinesCmd()
        {
            CommandName = "-THDXFGXSC";//天华地下分割线生成
            ActionName = "生成";
        }
        public CreateAllSeglinesCmd(ParkingStallArrangementViewModel vm)
        {
            CommandName = "THZDCWBZ";
            ActionName = "斜交无迭代速排";
            ParameterViewModel = vm;
        }
        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            try
            {
                
                using (var docLock = Active.Document.LockDocument())
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    ORun(acadDatabase);
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
        public void Run(AcadDatabase acadDatabase, bool randomFlag = true)//false 自动生成全部分割线, ture 生成随机分割线
        {
            var rstDataExtract = InputData.GetOuterBrder(acadDatabase, out OuterBrder outerBrder, Logger);
            if (!rstDataExtract)
            {
                return;
            }
            //TODO
            if(randomFlag)
            {
                var seglines = Dfs.GetRandomSeglines(outerBrder);//生成随机的分割线方案

            }
            else
            {
                var seglinesList = Dfs.GetDichotomySegline(outerBrder);//生成全部的分割线方案
                foreach(var seglines in seglinesList)
                {
                    
                }
            }
        }

        public void ORun(AcadDatabase acadDatabase)
        {
            ParameterStock.Set(new ParkingStallArrangementViewModel());
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            DrawingName = Path.GetFileName(doc.Name);
            var blks = InputData.SelectBlocks(acadDatabase);
            if (blks == null) return;
            foreach(var blk in blks)
            {
                try
                {
                    ProcessABlock(blk);
                }
                catch (Exception ex)
                {
                    Active.Editor.WriteMessage(ex.Message);
                }
            }
            
        }

        public void ProcessABlock(BlockReference block)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var blkName = block.GetEffectiveName();
            UpdateLogger(blkName);
            Logger?.Information("块名：" + blkName);
            Logger?.Information("文件名：" + DrawingName);
            Logger?.Information("用户名：" + Environment.UserName);
            var layoutData = new OLayoutData(block, Logger, out bool succeed);
            if (!succeed) return;
            layoutData.ProcessSegLines();
            layoutData.SetInterParam();
            var subAreas = OInterParameter.GetOSubAreas();
#if DEBUG
            subAreas.ForEach(s => s.Display("MPDebug"));
#endif
            var totalParkingStallCount = 0;
            foreach (var oSubArea in subAreas)
            {
                try
                {
                    ObliqueMPartition mParkingPartitionPro = new ObliqueMPartition(oSubArea.Walls, oSubArea.VaildLanes, oSubArea.Buildings, oSubArea.Area);
                    mParkingPartitionPro.OutputLanes = new List<LineSegment>();
                    mParkingPartitionPro.OutBoundary = oSubArea.Area;
                    mParkingPartitionPro.BuildingBoxes = new List<Polygon>();
                    mParkingPartitionPro.ObstaclesSpatialIndex = new MNTSSpatialIndex(mParkingPartitionPro.Obstacles);
#if DEBUG
                    var s = MDebugTools.AnalysisPolygon(mParkingPartitionPro.Boundary);
                    string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    FileStream fs = new FileStream(dir + "\\bound.txt", FileMode.Create, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs);
                    sw.WriteLine(s);
                    sw.Close();
                    fs.Close();
#endif
                    mParkingPartitionPro.Process(true);
                    MultiProcessTestCommand.DisplayMParkingPartitionPros(mParkingPartitionPro.ConvertToMParkingPartitionPro());
                    mParkingPartitionPro.IniLanes.Select(e => e.Line.ToDbLine()).AddToCurrentSpace();
                    totalParkingStallCount += mParkingPartitionPro.Cars.Count;
                }
                catch (System.Exception ex)
                {
                    Active.Editor.WriteMessage(ex.Message);
                }
            }

            var strBest = $"最大车位数{totalParkingStallCount}\n";
            Logger?.Information(strBest);
            Logger?.Information($"单地库用时: {stopWatch.Elapsed.TotalMinutes} 分\n");
            ReclaimMemory();
        }

        private void UpdateLogger(string blkName)
        {
            string modName = "斜交_";
            var logFileName = Path.Combine(GetPath.GetAppDataPath(), modName + DrawingName.Split('.').First() + '(' + blkName + ')' + ".txt");
            Logger = new Serilog.LoggerConfiguration().WriteTo
                    .File(logFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10).CreateLogger();

        }

        private void ReclaimMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
        }
    }
}
