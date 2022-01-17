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
using Autodesk.AutoCAD.EditorInput;
using ThMEPArchitecture.ViewModel;
using ThMEPArchitecture.ParkingStallArrangement.General;
using NFox.Cad;

namespace ThMEPArchitecture.ParkingStallArrangement
{
    public class GenerateParkingStallDirectlyCmd : ThMEPBaseCommand, IDisposable
    {
        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "DirectLayoutLog.txt");

        public Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
            .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day,retainedFileCountLimit:10).CreateLogger();
        public static ParkingStallArrangementViewModel ParameterViewModel { get; set; }

        private CommandMode _CommandMode { get; set; } = CommandMode.WithoutUI;
        public GenerateParkingStallDirectlyCmd()
        {
            CommandName = "-THDXQYFG2";
            ActionName = "生成";
            _CommandMode = CommandMode.WithoutUI;
            ParameterViewModel = new ParkingStallArrangementViewModel();
        }

        public GenerateParkingStallDirectlyCmd(ParkingStallArrangementViewModel vm)
        {
            CommandName = "THDXCW";
            ActionName = "直接生成";
            ParameterViewModel = vm;
            _CommandMode = CommandMode.WithUI;
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
                Logger?.Information(ex.Message);
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
            var dataprocessingFlag = Preprocessing.DataPreprocessing(acadDatabase, out GaParameter gaPara, out LayoutParameter layoutPara);
            if (!dataprocessingFlag) return;
            var geneAlgorithm = new ParkingStallDirectGenerator(gaPara);

            var rst = geneAlgorithm.Run();

            layoutPara.Set(rst);      
            int count = 0;
            for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
            {
                var use_partition_pro = true;
                if (use_partition_pro)
                {
                    var partitionpro = new ParkingPartitionPro();
                    ConvertParametersToPartitionPro(layoutPara, j, ref partitionpro, ParameterViewModel);
                    try
                    {
                        count += partitionpro.ProcessAndDisplay();
                    }
                    catch (Exception ex)
                    {
                        ;
                    }
                    continue;
                }
                else
                {
                    ParkingPartition partition = new ParkingPartition();
                    if (ConvertParametersToPartition(layoutPara, j, ref partition, ParameterViewModel, Logger))
                    {
                        try
                        {
                            count += partition.ProcessAndDisplay();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex.Message);
                            partition.Dispose();
                        }
                    }
                }          
            }
            ParkingSpace.GetSingleParkingSpace(Logger, layoutPara, count);
            layoutPara.Dispose();
            Active.Editor.WriteMessage("Count of car spots: " + count.ToString() + "\n");
        }
    }
}
