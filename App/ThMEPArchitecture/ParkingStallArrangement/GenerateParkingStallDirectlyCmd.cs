using AcHelper;
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
using Autodesk.AutoCAD.Geometry;
using System.Threading.Tasks;
using ThCADCore.NTS;
using NetTopologySuite.Geometries;
using System.Collections.Concurrent;

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
            ParameterStock.Set(ParameterViewModel);
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

        public void _Run(AcadDatabase acadDatabase)
        {
            var isDirectlyArrange = true;
            bool usePline = true;

            var getOuterBorderFlag = Preprocessing.GetOuterBorder(acadDatabase, out OuterBrder outerBrder, Logger);
            if (!getOuterBorderFlag) return;
            
            var dataprocessingFlag = Preprocessing.DataPreprocessing(outerBrder, out GaParameter gaPara, out LayoutParameter layoutPara, Logger, isDirectlyArrange, usePline);
            if (!dataprocessingFlag) return;

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
                        Active.Editor.WriteMessage(ex.Message);
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

        public void Run(AcadDatabase acadDatabase)
        {
            Active.Editor.WriteMessage($"线求交点\n");
            LineTest2();
        }
        private void ParallelTest()
        {
            var l1 = new List<double>();
            var l2 = new List<double>();
            for (int i = 0; i < 100000; i++)
            {
                l1.Add(General.Utils.RandDouble());
                l2.Add(General.Utils.RandDouble());
            }
            var t0 = _stopwatch.Elapsed.TotalSeconds;
            foreach (var d1 in l1)
            {
                foreach(var d2 in l2)
                {
                    Math.Min(d1, d2);
                }
            }
            var t1 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"单线程seconds: {t1 - t0} \n");
            Parallel.ForEach(l1, d1 => l2.ForEach(d2 => Math.Min(d1, d2)));
            var t2 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"多线程seconds: {t2 - t1} \n");

        }
        private void NTSLineTest()
        {
            var lineLis1 = new List<LineString>();
            for (int i = 0; i < 6; i++)
            {
                lineLis1.Add(RandomLine().ToNTSLineString());
            }
            var t0 = _stopwatch.Elapsed.TotalSeconds;
            foreach (var l1 in lineLis1)
            {
                GetMoreBuffer(l1);
            }
            var t1 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"单线程seconds: {t1 - t0} \n");
            Parallel.ForEach(lineLis1, l => GetMoreBuffer(l));
            var t2 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"多线程seconds: {t2 - t1} \n");
        }
        private void NTSLineTest2()
        {
            var lineLis1 = new List<(LineString, LineString)>();
            for (int i = 0; i < 6; i++)
            {
                lineLis1.Add((RandomLine().ToNTSLineString(), RandomLine().ToNTSLineString()));
            }
            var t0 = _stopwatch.Elapsed.TotalSeconds;
            foreach (var l in lineLis1)
            {
                ToLineMore(l.Item1,l.Item2);
            }
            var t1 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"单线程seconds: {t1 - t0} \n");
            Parallel.ForEach(lineLis1, l => ToLineMore(l.Item1, l.Item2));
            var t2 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"多线程seconds: {t2 - t1} \n");
        }
        private void NTSLineTest3()
        {
            var lineLis1 = new List<(LineSegment, LineSegment)>();
            for (int i = 0; i < 60; i++)
            {
                lineLis1.Add((RandomLine().ToNTSLineSegment(), RandomLine().ToNTSLineSegment()));
            }
            var t0 = _stopwatch.Elapsed.TotalSeconds;
            foreach (var l in lineLis1)
            {
                InsectMore(l.Item1, l.Item2);
            }
            var t1 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"单线程seconds: {t1 - t0} \n");
            Parallel.ForEach(lineLis1, l => InsectMore(l.Item1, l.Item2));
            var t2 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"多线程seconds: {t2 - t1} \n");
        }
        private void NTSLineTest4()
        {
            var lineLis1 = new ConcurrentBag<(LineSegment, LineSegment)>();
            for (int i = 0; i < 6; i++)
            {
                lineLis1.Add((RandomLine().ToNTSLineSegment(), RandomLine().ToNTSLineSegment()));
            }
            var t0 = _stopwatch.Elapsed.TotalSeconds;
            foreach (var l in lineLis1)
            {
                ClosestMore(l.Item1, l.Item2);
            }
            var t1 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"单线程seconds: {t1 - t0} \n");
            Parallel.ForEach(lineLis1, l => ClosestMore(l.Item1, l.Item2));
            var t2 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"多线程seconds: {t2 - t1} \n");
        }
        //基本无增速
        private void LineTest()
        {
            var lineLis1 = new List<Line>();
            var lineLis2 = new List<Line>();
            for (int i = 0;i < 1000; i++)
            {
                lineLis1.Add(RandomLine());
                lineLis2.Add(RandomLine());
            }
            var t0 = _stopwatch.Elapsed.TotalSeconds;
            foreach(var l1 in lineLis1)
            {
                foreach(var l2 in lineLis2)
                {
                    l1.Intersect(l2, Intersect.OnBothOperands);
                }
            }
            var t1 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"单线程seconds: {t1 - t0} \n");
            Parallel.ForEach(lineLis1, l1 => lineLis2.ForEach(l2 => l1.Intersect(l2, Intersect.OnBothOperands)));
            var t2 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"多线程seconds: {t2 - t1} \n");
        }
        private void LineTest1()
        {
            var lineLis1 = new ConcurrentBag<(Line, Line)>();
            for (int i = 0; i < 60; i++)
            {
                lineLis1.Add((RandomLine(), RandomLine()));
            }
            var t0 = _stopwatch.Elapsed.TotalSeconds;
            foreach (var l in lineLis1)
            {
                DistMore(l.Item1, l.Item2);
            }
            var t1 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"单线程seconds: {t1 - t0} \n");
            Parallel.ForEach(lineLis1, l => DistMore(l.Item1, l.Item2));
            var t2 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"多线程seconds: {t2 - t1} \n");
        }
        private void LineTest2()
        {
            var lineLis = new List<Line>();
 
            for (int i = 0; i < 1000; i++)
            {
                lineLis.Add(RandomLine());
            }
            var t0 = _stopwatch.Elapsed.TotalSeconds;
            foreach (var l1 in lineLis)
            {
                NewMantTimes();
            }
            var t1 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"单线程seconds: {t1 - t0} \n");
            Parallel.ForEach(lineLis, l1 => NewMantTimes());
            var t2 = _stopwatch.Elapsed.TotalSeconds;
            Active.Editor.WriteMessage($"多线程seconds: {t2 - t1} \n");
        }
        private Line RandomLine(double Lim = 100)
        {
            var spt = new Point3d(General.Utils.RandDouble() * Lim, General.Utils.RandDouble() * Lim, 0);
            var ept = new Point3d(General.Utils.RandDouble() * Lim, General.Utils.RandDouble() * Lim, 0);
            return new Line(spt, ept);
        }
        private void GetMoreCenter( Line l,int n = 10000)
        {
            for(int i = 0; i < n; ++i)
            {
                l.GetCenter();
            }
        }
        private void GetMoreLength(Line l, int n = 100000)
        {
            for (int i = 0; i < n; ++i)
            {
                l.GetLength();
            }
        }
        private void GetMoreBuffer(LineString lstr, int n = 100000)
        {
            for (int i = 0; i < n; ++i)
            {
                lstr.Buffer(100);
            }
        }
        private void InsectMore(LineString lstr1, LineString lstr2, int n = 1000000)
        {
            for (int i = 0; i < n; ++i)
            {
                lstr1.Intersects(lstr2);
            }
        }
        private void InsectMore(LineSegment l1, LineSegment l2, int n = 10000000)
        {
            for (int i = 0; i < n; ++i)
            {
                l1.CompareTo(l2);
            }
        }
        private void UnionMore(LineString lstr1, LineString lstr2, int n = 100000)
        {
            for (int i = 0; i < n; ++i)
            {
                lstr1.Union(lstr2);
            }
        }
        private void DistMore(LineString lstr1, LineString lstr2, int n = 10000000)
        {
            for (int i = 0; i < n; ++i)
            {
                lstr1.Distance(lstr2);
            }
        }
        private void DistMore(LineSegment lstr1, LineSegment lstr2, int n = 20000000)
        {
            for (int i = 0; i < n; ++i)
            {
                lstr1.Distance(lstr2);
            }
        }
        private void DistMore(Line l1, Line l2,int n = 1000000)
        {
            for (int i = 0; i < n; ++i)
            {
                l1.Distance(l2);
            }
        }
        private void IsDistMore(LineString lstr1, LineString lstr2, int n = 10000000)
        {
            for (int i = 0; i < n; ++i)
            {
                lstr1.IsWithinDistance(lstr2,10);
            }
        }
        private void ToLineMore(LineString lstr1, LineString lstr2, int n = 100000)
        {
            for (int i = 0; i < n; ++i)
            {
                lstr1.ToDbline();
            }
        }
        private void ClosestMore(LineSegment lstr1, LineSegment lstr2, int n = 10000000)
        {
            for (int i = 0; i < n; ++i)
            {
                lstr1.ClosestPoints(lstr2);
            }
        }
        private void NewMantTimes(int n = 10000000)
        {
            for (int i = 0; i < n; ++i)
            {
                var lis = new int();
            }
        }
    }
}
