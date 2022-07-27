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

namespace ThMEPArchitecture
{
    public class CreateAllSeglinesCmd : ThMEPBaseCommand, IDisposable
    {
        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "SeglineLog.txt");

        public Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
            .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day, retainedFileCountLimit:10).CreateLogger();
        public static ParkingStallArrangementViewModel ParameterViewModel { get; set; }

        public CreateAllSeglinesCmd()
        {
            CommandName = "-THDXFGXSC";//天华地下分割线生成
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
            var blks = InputData.SelectBlocks(acadDatabase);
            if (blks == null) return;
            foreach(var blk in blks)
            {
                var layoutData = new OLayoutData(blk, Logger, out bool succeed);
                if (!succeed) continue;
                layoutData.ProcessSegLines();
                layoutData.SetInterParam();
                var subAreas = OInterParameter.GetOSubAreas();
#if DEBUG
                subAreas.ForEach(s => s.Display("MPDebug"));
#endif
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
                    }
                    catch (System.Exception ex)
                    {
                        Active.Editor.WriteMessage(ex.Message);
                    }
                }
            }
            
        }
    }
}
