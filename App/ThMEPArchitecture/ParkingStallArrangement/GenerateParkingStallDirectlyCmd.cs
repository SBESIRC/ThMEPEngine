﻿using AcHelper;
using Linq2Acad;
using Serilog;
using System;
using System.IO;
using ThMEPArchitecture.ParkingStallArrangement.Algorithm;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.PartitionLayout;
using ThMEPEngineCore.Command;
using static ThMEPArchitecture.ParkingStallArrangement.ParameterConvert;
using ThMEPArchitecture.ViewModel;
using ThMEPArchitecture.ParkingStallArrangement.General;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System.Linq;

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
            CommandName = "THZDCWBZ";
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
            var isDirectlyArrange = true;
            bool usePline = true;
            //var dataprocessingFlag = Preprocessing.DataPreprocessing(acadDatabase, out GaParameter gaPara, out LayoutParameter layoutPara, Logger, isDirectlyArrange, usePline);
            //if (!dataprocessingFlag) return;

            var dataprocessingFlag = Preprocessing.GetOuterBorder(acadDatabase, out OuterBrder outerBrder, Logger);
            if (!dataprocessingFlag) return;
            Preprocessing.DataPreprocessing(outerBrder, out GaParameter gaPara, out LayoutParameter layoutPara, Logger, isDirectlyArrange, usePline);

            var geneAlgorithm = new ParkingStallDirectGenerator(gaPara);

            var rst = geneAlgorithm.Run();

            layoutPara.DirectlyArrangementSetParameter(rst);
            int count = 0;
            if (!Chromosome.IsValidatedSolutions(layoutPara))
            {
                count = -1;
            }
            else
            {
                var Cars = new List<Polyline>();
                var Pillars = new List<Polyline>();
                var Lanes = new List<Line>();
                var Boundary = layoutPara.OuterBoundary;
                var ObstaclesSpacialIndex = layoutPara.AllShearwallsMPolygonSpatialIndex;
                for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
                {
                    var partitionpro = new ParkingPartitionPro();
                    ConvertParametersToPartitionPro(layoutPara, j, ref partitionpro, ParameterViewModel);
                    if (!partitionpro.Validate()) continue;
                    try
                    {
                        count += partitionpro.Process(Cars,Pillars,Lanes);
                    }
                    catch (Exception ex)
                    {
                        ;
                    }
                }
                LayoutPostProcessing.DealWithCarsOntheEndofLanes(ref Cars,ref Pillars, Lanes, ObstaclesSpacialIndex, Boundary, ParameterViewModel);
                count = Cars.Count;
                var partitionpro_final = new ParkingPartitionPro();
                partitionpro_final.CarSpots = Cars;
                partitionpro_final.Pillars = Pillars;
                partitionpro_final.Display();
            }
            ParkingSpace.GetSingleParkingSpace(Logger, layoutPara, count);
            layoutPara.Dispose();
            Active.Editor.WriteMessage("Count of car spots: " + count.ToString() + "\n");
        }
    }
}
