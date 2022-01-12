using AcHelper;
using Linq2Acad;
using System;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPEngineCore.Command;
using ThMEPArchitecture.ViewModel;
using System.IO;
using Serilog;

namespace ThMEPArchitecture
{
    public class CreateAllSeglinesCmd : ThMEPBaseCommand, IDisposable
    {
        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "GaLog.txt");

        public Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
            .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day).CreateLogger();
        public static ParkingStallArrangementViewModel ParameterViewModel { get; set; }

        public CreateAllSeglinesCmd()//自动生成全部分割线
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
                    Run(acadDatabase);
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

            //TODO
            var allSeglines = Dfs.GetDichotomySegline(outerBrder);//生成所有的分割线方案
        }
    }
}
