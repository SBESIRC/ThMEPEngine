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
            }
            
        }
    }
}
