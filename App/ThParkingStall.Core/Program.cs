using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using static ThParkingStall.Core.MPartitionLayout.MCompute;
using Serilog;
using System.Threading;
using ThParkingStall.Core.IO;
namespace ThParkingStall.Core
{
    internal class Program
    {
        static void Main(string[] ProcessInfo)
        {
            try
            {
                Run(ProcessInfo);
            }
            catch (Exception ex)
            {
                MCompute.Logger?.Information(ex.Message);
                MCompute.Logger?.Information("----------------------------------");
                MCompute.Logger?.Information(ex.StackTrace);
                MCompute.Logger?.Information("##################################");
                MPGAData.Save();
            }
        }
        static void Run(string[] ProcessInfo)
        {
            var ProcessCount = Int32.Parse(ProcessInfo[0]);
            var ProcessIndex = Int32.Parse(ProcessInfo[1]);
            var IterationCount = Int32.Parse(ProcessInfo[2]);
            var LogAllInfo = ProcessInfo[3] == "1";//是否Log 所有信息
            var ThreadCnt = Int32.Parse(ProcessInfo[4]);// 使用的线程数量
            if (ThreadCnt > 2) InterParameter.MultiThread = true;
            else InterParameter.MultiThread = false;
            string LogFileName;
            if (LogAllInfo) LogFileName = Path.Combine(GetPath.GetAppDataPath(), "SubProcessLog" + ProcessIndex.ToString() + "_.txt");
            else LogFileName = Path.Combine(GetPath.GetDebugPath(), "SubProcessDebug" + ProcessIndex.ToString() + "_.txt");
            var Logger = new Serilog.LoggerConfiguration().WriteTo
                                .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10).CreateLogger();
            MCompute.Logger = Logger;
            MCompute.LogInfo = LogAllInfo;
            MCompute.ThreadCnt = ThreadCnt;
            MPGAData.ProcIndex = ProcessIndex;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var t_pre = 0.0;
            if (LogAllInfo)
            {
                Logger?.Information("#####################################");
                Logger?.Information("子进程启动");
                Logger?.Information("线程数量：" + ThreadCnt.ToString());
            }
            using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("DataWraper"))
            {
                using (MemoryMappedViewStream stream = mmf.CreateViewStream(0L, 0L, MemoryMappedFileAccess.Read))
                {
                    IFormatter formatter = new BinaryFormatter();
                    var dataWraper = (DataWraper)formatter.Deserialize(stream);
                    VMStock.Init(dataWraper);
                    InterParameter.Init(dataWraper);
                    MPGAData.dataWraper = dataWraper;
                }
            }
            if (LogAllInfo)
            {
                Logger?.Information($"初始化用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒\n");
                t_pre = stopWatch.Elapsed.TotalSeconds;
            }
            for (int iter = 0; iter < IterationCount; iter++)
            {
                
                var StartSignal = Mutex.OpenExisting("Mutex" + iter.ToString() + "_" + ProcessIndex.ToString());
                StartSignal.WaitOne();

                try
                {
                    if (LogAllInfo)
                    {
                        Logger?.Information("第" + (iter + 1).ToString() + "代开始：");
                        t_pre = stopWatch.Elapsed.TotalSeconds;
                    }
                    ChromosomeCollection chromosomeCollection;
                    using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("ChromosomeCollection", MemoryMappedFileRights.Read))//读取
                    {
                        using (MemoryMappedViewStream stream = mmf.CreateViewStream(0L, 0L, MemoryMappedFileAccess.Read))
                        {
                            //IFormatter formatter = new BinaryFormatter();
                            //chromosomeCollection = (ChromosomeCollection)formatter.Deserialize(stream);
                            chromosomeCollection = ChromosomeCollection.ReadFromStream(stream);
                            SubAreaParkingCnt.Update(chromosomeCollection);
                        }
                    }
                    if (LogAllInfo)
                    {
                        Logger?.Information($"读取用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");
                        t_pre = stopWatch.Elapsed.TotalSeconds;
                    }
                    var ParkingCnts = new List<int>();
                    var Chromosomes = chromosomeCollection.Chromosomes;
                    for (int i = 0; i <= Chromosomes.Count / ProcessCount; i++)//计算
                    {
                        MCompute.CatchedTimes = 0;
                        int j = i * ProcessCount + ProcessIndex;
                        if (j >= Chromosomes.Count) break;
                        var chromosome = Chromosomes[j];
                        MPGAData.Set(chromosome);
                        var subAreas = InterParameter.GetSubAreas(chromosome);
                        if (LogAllInfo)
                        {
                            Logger?.Information($"区域分割用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");
                            t_pre = stopWatch.Elapsed.TotalSeconds;
                        }
                        List<MParkingPartitionPro> mParkingPartitionPros = new List<MParkingPartitionPro>();
                        MParkingPartitionPro mParkingPartition = new MParkingPartitionPro();
                        var ParkingCount = CalculateTheTotalNumOfParkingSpace(subAreas, ref mParkingPartitionPros, ref mParkingPartition);
                        if (LogAllInfo)
                        {
                            Logger?.Information($"区域计算用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");
                            Logger?.Information("Catched:" + MCompute.CatchedTimes.ToString() + "/" + subAreas.Count.ToString());
                            t_pre = stopWatch.Elapsed.TotalSeconds;
                        }
                        ParkingCnts.Add(ParkingCount);
                    }
                    //if (LogAllInfo) Logger?.Information("计算完成");

                    using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("CachedPartitionCnt" + ProcessIndex.ToString()))
                    {
                        using (MemoryMappedViewStream stream = mmf.CreateViewStream())//结果输出
                        {
                            //IFormatter formatter = new BinaryFormatter();
                            //var newCatched = SubAreaParkingCnt.GetNewUpdated();
                            //formatter.Serialize(stream, (ParkingCnts, newCatched.Item1, newCatched.Item2));
                            BinaryWriter writer = new BinaryWriter(stream);
                            ParkingCnts.WriteToStream(writer);
                            SubAreaParkingCnt.NewCachedPartitionCnt.WriteToStream(writer);
                        }
                    }
                }
                finally
                {
                    StartSignal.ReleaseMutex();//发出信号确认完成
                }
                if (LogAllInfo)
                {
                    Logger?.Information("输出完成");
                    Logger?.Information($"输出用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒\n");
                    t_pre = stopWatch.Elapsed.TotalSeconds;
                }
                SubAreaParkingCnt.ClearNewAdded();
                if (IterationCount % 3 == 0)
                    ReclaimMemory();

            }
            if (LogAllInfo)
            {
                Logger?.Information("子进程退出");
                Logger?.Information($"总用时用时: {stopWatch.Elapsed.TotalSeconds}秒\n");
            }
            stopWatch.Stop();
        }
        private static void ReclaimMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
        }
        static int YMain(string[] parameter)
        {
            string[] parameters = parameter[0].Split('|').Where(e => e.Length > 0).ToArray();
            List<LineString> twalls = new List<LineString>();
            List<LineSegment> tinilanes = new List<LineSegment>();
            List<Polygon> tobs = new List<Polygon>();
            List<Polygon> tbox = new List<Polygon>();
            Polygon tboundary = new Polygon(new LinearRing(new Coordinate[0]));
            read_from_string(parameters, ref twalls, ref tinilanes, ref tobs, ref tboundary, ref tbox);
            MParkingPartitionPro mParkingPartitionPro = ConvertToMParkingPartitionPro(twalls, tinilanes, tobs, tboundary, tbox);
            mParkingPartitionPro.GenerateParkingSpaces();
            Console.WriteLine(mParkingPartitionPro.IniLanes.Count.ToString());
            return mParkingPartitionPro.IniLanes.Count;
        }
        private static MParkingPartitionPro ConvertToMParkingPartitionPro(List<LineString> walls,
    List<LineSegment> inilanes, List<Polygon> obs, Polygon boundary, List<Polygon> box)
        {
            MParkingPartitionPro mParkingPartitionPro = new MParkingPartitionPro(
              walls, inilanes, obs, boundary);
            mParkingPartitionPro.OutBoundary = boundary;
            mParkingPartitionPro.BuildingBoxes = box;
            mParkingPartitionPro.ObstaclesSpatialIndex = new MNTSSpatialIndex(obs);
            return mParkingPartitionPro;
        }
        private static void read_from_string(string[] parameters, ref List<LineString> walls,
       ref List<LineSegment> inilanes, ref List<Polygon> obs, ref Polygon boundary, ref List<Polygon> boxes)
        {
            foreach (var content in parameters.ToList())
            {
                var list = content.Split(':', ';').Where(e => e.Length > 0).ToList();
                list.RemoveAt(0);
                if (content.Contains("walls"))
                {
                    foreach (var wall in list)
                    {
                        var ss = wall.Split(',').Where(e => e.Length > 0).ToList();
                        List<Coordinate> coords = new List<Coordinate>();
                        for (int i = 0; i < ss.Count - 1; i += 2)
                        {
                            var co = new Coordinate(double.Parse(ss[i]), double.Parse(ss[i + 1]));
                            coords.Add(co);
                        }
                        walls.Add(new LineString(coords.ToArray()));
                    }
                }
                else if (content.Contains("lanes"))
                {
                    foreach (var lane in list)
                    {
                        var ss = lane.Split(',').Where(e => e.Length > 0).ToList();
                        List<Coordinate> coords = new List<Coordinate>();
                        for (int i = 0; i < ss.Count - 3; i += 4)
                        {
                            var c0 = new Coordinate(double.Parse(ss[i]), double.Parse(ss[i + 1]));
                            var c1 = new Coordinate(double.Parse(ss[i + 2]), double.Parse(ss[i + 3]));
                            inilanes.Add(new LineSegment(c0, c1));
                        }
                    }
                }
                else if (content.Contains("obs"))
                {
                    foreach (var ob in list)
                    {
                        var ss = ob.Split(',').Where(e => e.Length > 0).ToList();
                        List<Coordinate> coords = new List<Coordinate>();
                        for (int i = 0; i < ss.Count - 1; i += 2)
                        {
                            var co = new Coordinate(double.Parse(ss[i]), double.Parse(ss[i + 1]));
                            coords.Add(co);
                        }
                        obs.Add(new Polygon(new LinearRing(coords.ToArray())));
                    }
                }
                else if (content.Contains("box"))
                {
                    foreach (var bo in list)
                    {
                        var ss = bo.Split(',').Where(e => e.Length > 0).ToList();
                        List<Coordinate> coords = new List<Coordinate>();
                        for (int i = 0; i < ss.Count - 1; i += 2)
                        {
                            var co = new Coordinate(double.Parse(ss[i]), double.Parse(ss[i + 1]));
                            coords.Add(co);
                        }
                        boxes.Add(new Polygon(new LinearRing(coords.ToArray())));
                    }
                }
                else if (content.Contains("bound"))
                {
                    foreach (var bd in list)
                    {
                        var ss = bd.Split(',').Where(e => e.Length > 0).ToList();
                        List<Coordinate> coords = new List<Coordinate>();
                        for (int i = 0; i < ss.Count - 1; i += 2)
                        {
                            var co = new Coordinate(double.Parse(ss[i]), double.Parse(ss[i + 1]));
                            coords.Add(co);
                        }
                        boundary = new Polygon(new LinearRing(coords.ToArray()));
                    }
                }
            }
        }

    }

}
