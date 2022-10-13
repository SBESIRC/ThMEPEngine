using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.Buffer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPArchitecture.MultiProcess;
using ThMEPArchitecture.ViewModel;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.IO;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.OInterProcess;
using ThParkingStall.Core.OTools;
using ThParkingStall.Core.Tools;
using BuildingPosGene = ThParkingStall.Core.OInterProcess.BuildingPosGene;
namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    public class BuildingPosAnalysis
    {
        public List<List<Vector2D>> PotentialMovingVectors = new List<List<Vector2D>>();//i,j 代表第i个建筑的第j个移动方案
        private List<List<int>> ParkingCnts;//i,j代表第i个建筑的第j个车位个数
        private List<Vector2D> BestVectors = new List<Vector2D>();
        private List<OSubArea> InitSubAreas;//初始子区域

        List<BuildingPosGA> GAlist;
        private int ProcessCnt;
        List<Process> ProcList;
        private List<List<Mutex>> MutexLists;//进程锁列表的列表
        private int Iter;//当前代
        private Stopwatch stopWatch = new Stopwatch();
        double t_pre;
        public int BuildingMoveDistance;//建筑横纵偏移最大距离
        private double SampleDistance;//采样间距
        private int SampleCnt;
        private double HalfLaneWidth = -0.1 + VMStock.RoadWidth / 2;
        private Geometry CenterLaneGeo;
        private MNTSSpatialIndex CenterLaneSPIdx;
        private BuildingPosCalculate BPC;

        public ParkingStallArrangementViewModel VM;
        public Serilog.Core.Logger Logger { get; set; }
        //1.获取所有可能的移动方案
        //网格+特殊点 
        //筛选合理解
        //确保一个分区只有一个建筑，且移动不会跨区域
        //2.可移动方案分配到子进程
        //3.每个建筑筛选最多个数的移动方案
        //4.按最多个数进行排布，返回排布方案
        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);
        public BuildingPosAnalysis(ParkingStallArrangementViewModel parameterViewModel)
        {
            BuildingMoveDistance = parameterViewModel.BuildingMoveDistance;
            SampleCnt = (int)(Math.Sqrt( parameterViewModel.PopulationCount * parameterViewModel.IterationCount)/2);
            //SampleDistance = parameterViewModel.SampleDistance;
            SampleDistance =BuildingMoveDistance / SampleCnt;
            VM = parameterViewModel;
            if (parameterViewModel.ProcessCount == -1) ProcessCnt = Environment.ProcessorCount;
            else ProcessCnt = parameterViewModel.ProcessCount;
            //BuildingMoveDistance = 500;
            //SampleDistance = 500;
            UpdateMovingVector();
            //InitSubAreas = OInterParameter.GetOSubAreas(null);
        }

        private void UpdateMovingVector()
        {
            var lanes = OInterParameter.InitSegLines.Select(l =>l.VaildLane.OExtend(0.1)).ToList();
            var centerLanes = new List<LineSegment>();
            for(int i = 0; i < lanes.Count; i++)
            {
                var currentLane = lanes[i];
                if(currentLane == null) continue;
                var IntSecPts = new List<Coordinate>();
                for(int j = 0; j < lanes.Count; j++)
                {
                    if (i == j) continue;
                    var nextLane = lanes[j];
                    if(nextLane == null) continue;
                    var intSecPt = currentLane.Intersection(nextLane);
                    if(intSecPt != null) IntSecPts.Add(intSecPt);
                }
                if(IntSecPts.Count > 1)
                {
                    var ordered = IntSecPts.PositiveOrder();
                    centerLanes.Add(new LineSegment(ordered.First(), ordered.Last()));
                }
            }
            CenterLaneGeo = new MultiLineString(centerLanes.ToLineStrings().ToArray()).Buffer(HalfLaneWidth,MitreParam);
            var bounds = new List<LineString>();
            CenterLaneGeo.Get<Polygon>(true).ForEach(s => bounds.AddRange(s.Shell.ToLineSegments().ToLineStrings()));
            CenterLaneSPIdx = new MNTSSpatialIndex(bounds);
            //CenterLaneGeo.Get<Polygon>(false).ForEach(p => p.ToDbMPolygon().AddToCurrentSpace());
            //var StepCnts = BuildingMoveDistance / SampleDistance;
            AffineTransformation transformation = new AffineTransformation();
            for (int k = 0; k < OInterParameter.MovingBounds.Count; k++)
            {
                var bound = OInterParameter.MovingBounds[k];
                PotentialMovingVectors.Add(new List<Vector2D>());
                if (bound.Intersects(CenterLaneGeo)) throw new Exception("建筑物与核心车道相交!");
                for (int i = -SampleCnt; i < SampleCnt + 1; i++)
                {
                    for (int j = -SampleCnt; j < SampleCnt + 1; j++)
                    {
                        var x = i * SampleDistance;
                        var y = j * SampleDistance;
                        var vector = new Vector2D(x, y);
                        transformation.SetToTranslation(x, y);
                        var newBound = transformation.Transform(bound);
                        if (newBound.Disjoint(CenterLaneGeo))
                        {
                            PotentialMovingVectors[k].Add(vector);
                        }
                    }
                }
            }
        }

        public bool IsVaild(int index,Vector2D vector)
        {
            AffineTransformation transformation = new AffineTransformation();
            var bound = OInterParameter.MovingBounds[index];
            transformation.SetToTranslation(vector.X, vector.Y);
            var newBound = transformation.Transform(bound);
            //if (newBound.Disjoint(CenterLaneGeo)) return true;
            //else return false;
            if (CenterLaneSPIdx.SelectCrossingGeometry(newBound).Count == 0) return true;
            else return false;
        }

        public int CalculateScore(int index,Vector2D vector)
        {
            if (BPC == null) BPC = new BuildingPosCalculate();
            return BPC.CalculateScore(index, vector);
        }
        public int InitScore(int index)//弃用，未计算初始值
        {
            if (BPC == null) BPC = new BuildingPosCalculate();
            return BPC.DynamicSubAreas[index].Sum(s => s.InitParkingCnt);
        }
        public void UpdataGA()
        {
            var bestVectors = new List<Vector2D>();
            Logger?.Information("-----------------");
            Logger?.Information("障碍物迭代");
            for (int i = 0; i < OInterParameter.MovingBounds.Count; i++)
            {
                Logger?.Information($"障碍物迭代:{i}/{OInterParameter.MovingBounds.Count}");
                BuildingPosGAAnalysis buildingPosGA = new BuildingPosGAAnalysis(i, this);
                buildingPosGA.Process();
                Logger?.Information($"第{i}区最佳得分:{buildingPosGA.BestScore}");
                var initScore = InitScore(i);
                Logger?.Information($"第{i}区初始得分:{initScore}");
                if (initScore < buildingPosGA.BestScore)
                    bestVectors.Add(buildingPosGA.Best);
                else
                    bestVectors.Add(Vector2D.Zero);
            }
            Logger?.Information("-----------------");
            OInterParameter.UpdateBuildings(bestVectors);
        }
        public void UpdateGASP()
        {
            //var bestVectors = new List<Vector2D>();
            Logger?.Information("-----------------");
            Logger?.Information("障碍物迭代");
            var iterCnt = VM.IterationCount;
            GAlist = new List<BuildingPosGA>();
            for (int i = 0; i < OInterParameter.MovingBounds.Count; i++)
            {
                var ga = new BuildingPosGA(i,this, VM);
                GAlist.Add(ga);
            }
            for(int iter  = 0; iter < iterCnt; iter++)
            {
                var genome = new List<BuildingPosGene>();
                for (int i = 0; i < GAlist.Count; i++)
                {
                    genome.AddRange(GAlist[i].GetPopToCalculate());
                }
                var scores = genome.Select(g => CalculateScore(g.Index, g.Vector())).ToList();
                int j = 0;
                var msg = "";
                for (int i = 0; i < GAlist.Count; i++)
                {
                    var ga = GAlist[i];
                    var tempScores = new List<int>();
                    for(int k = 0;k < ga.IdxToCalculate.Count; k++)
                    {
                        tempScores.Add(scores[j]);
                        j++;
                    }
                    ga.Update(tempScores);
                    msg += ga.BestScore;
                }
                Logger?.Information($"第{iter}代计算结果:" + msg);
            }
            var bestVectors = GAlist.Select(g => g.OptimalSolution.Vector()).ToList();
            Logger?.Information("-----------------");
            OInterParameter.UpdateBuildings(bestVectors);
        }
        public void SpeedTest(int n = 1000)
        {
            var SW = new Stopwatch();
            var ga = new BuildingPosGA(0, this, VM);
            SW.Start();
            Logger?.Information($"速度测试{n}次");
            for (int i = 0; i < OInterParameter.MovingBounds.Count; i++)
            {
                var stopwatch = new Stopwatch();
                int vaildCnts = 0;
                stopwatch.Start();
                for (int j = 0; j < n; j++)
                {
                    var randVec = ga.RandVec();
                    if (IsVaild(i, randVec)) vaildCnts++;
                }
                stopwatch.Stop();
                var avgTime = stopwatch.Elapsed.TotalMilliseconds / n;
                Logger?.Information("平均用时(ms)" + avgTime);
                Logger?.Information("命中/总数:" + $"{vaildCnts}/{n}");
            }
            SW.Stop();
            Logger?.Information("总用时(s)" + SW.Elapsed.TotalSeconds);
        }
        public void UpdateBest()
        {
            var BPC = new BuildingPosCalculate();
            InitSubAreas = BPC.InitSubAreas;
            //InitSubAreas.ForEach(s => s.Display("初始小分区"));
            var scoresList = BPC.CalculateScore(PotentialMovingVectors);
            var bestVectors = new List<Vector2D>();
            for (int i = 0; i < PotentialMovingVectors.Count; i++)
            {
                bestVectors.Add(new Vector2D());
                var scores = scoresList[i];

                if (scores.Count == 0) continue;//不移动，跳过

                var bestScore = scores.Max();
                var idx = scores.IndexOf(bestScore);
                var initScore = BPC.DynamicSubAreas[i].Sum(s => s.InitParkingCnt);
                if (initScore >= bestScore) continue;//初始分数较好

                var bestVector = PotentialMovingVectors[i][idx];

                bestVectors[bestVectors.Count - 1] = bestVector;

                ////这块不对（多个bound在一个分区的case）
                //var movedSubAreas = BPC.DynamicSubAreas[idx].Select(s =>s.GetMovedArea(bestVector));

                //foreach(var DSubArea in BPC.DynamicSubAreas[idx])
                //{
                //    BPC.InitSubAreas[DSubArea.InitIndex] = DSubArea.GetMovedArea(bestVector);
                //}
            }
            OInterParameter.UpdateBuildings(bestVectors);
            //var subAreas = OInterParameter.GetOSubAreas(null);
        }
        public void UpdateGAMP()
        {
            Logger?.Information($"迭代次数: {VM.IterationCount}");
            Logger?.Information($"种群数量: {VM.PopulationCount}");
            Logger?.Information($"初代倍率: {VM.FirstPopMagnitude}");
            Logger?.Information($"最大迭代时间: {VM.MaxTimespan} 分");
            Logger?.Information($"CPU数量：" + Environment.ProcessorCount.ToString());
            try
            {
                GAMP_init();
                for (int i = 0; i < VM.IterationCount; i++)
                {
                    var finished = GAMP_ProcessOne();
                    if (finished) break;
                    if (stopWatch.Elapsed.TotalMinutes > VM.MaxTimespan) break;
                }
                var bestVectors = GAlist.Select(g => g.OptimalSolution.Vector()).ToList();
                var initScore = "";
                GAlist.ForEach(ga => initScore += $"{ga.Index}:{ga.init_Score},");
                Logger.Information("初始分数" + initScore);
                Logger?.Information($"遗传用时{stopWatch.Elapsed.TotalSeconds}秒");
                Logger?.Information("-----------------");
                OInterParameter.UpdateBuildings(bestVectors);
            }
            catch (Exception e)
            {
                Logger?.Information(e.StackTrace);
            }
            finally
            {
                ProcList.ForEach(x =>
                {
                    if (!x.HasExited)
                    {
                        try
                        {
                            x.Kill();
                        }
                        catch (Exception)
                        {
                        }
                    }
                });
                MutexLists.ForEach(l => l.ForEach(x => x.Dispose()));
                MutexLists.Clear();
            }

        }

        private void GAMP_init()
        {
            stopWatch.Start();
            t_pre = stopWatch.Elapsed.TotalSeconds;
            Logger?.Information("-----------------");
            Logger?.Information("障碍物迭代");
            MutexLists = new List<List<Mutex>>();
            var initSingnals = new List<Mutex>();
            ProcList = new List<Process>();
            var currentMutexList = new List<Mutex>();
            //var inputSingnal = CreateMutex("Input");
            //inputSingnal.ReleaseMutex();
            for (int idx = 0; idx < ProcessCnt; idx++)
            {
                var initSingnal = CreateMutex("Mutex", idx);
                initSingnals.Add(initSingnal);
                initSingnal.ReleaseMutex();
                var proc = CreateSubProcess(idx, ParameterStock.LogSubProcess, ParameterStock.ThreadCount);
                //var proc = CreateSubProcess(idx, true, ParameterStock.ThreadCount);
                ProcList.Add(proc);
                currentMutexList.Add(CreateMutex("Mutex0_", idx));
                proc.Start();
            }
            MutexLists.Add(currentMutexList);
            //ProcList.ForEach(proc => proc.Start());
            GAlist = new List<BuildingPosGA>();
            for (int i = 0; i < OInterParameter.MovingBounds.Count; i++)
            {
                var ga = new BuildingPosGA(i, this, VM);
                GAlist.Add(ga);
            }
            initSingnals.ForEach(s => s.WaitOne());//进程启动完成
            Logger?.Information($"障碍物迭代初始化用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");
        }
        private bool GAMP_ProcessOne()
        {
            Iter += 1;
            var genome = new List<BuildingPosGene>();//所有需要计算的基因列表
            var GAs = GAlist.Where(ga => !ga.Ended).ToList();//尚未退出的遗传
            if (GAs.Count == 0) return true;//全部退出，返回完成
            GAs.ForEach(ga => genome.AddRange(ga.GetPopToCalculate()));
            //var scores = genome.Select(g => CalculateScore(g.Index, g.Vector())).ToList();
            var scores = MPCalculate(genome);
            int j = 0;
            var msg = "";
            var calCnts = "";
            foreach(var ga in GAs)
            {
                var tempScores = new List<int>();
                for (int k = 0; k < ga.IdxToCalculate.Count; k++)
                {
                    tempScores.Add(scores[j]);
                    j++;
                }
                calCnts += $"{ga.Index}:{ga.IdxToCalculate.Count},";
                ga.Update(tempScores);
                msg += $"{ga.Index}:{ga.BestScore},";
            }
            Logger?.Information($"第{Iter}代计算结果:" + msg);
            Logger?.Information($"每个建筑计算次数:" + calCnts);
            return false;
        }

        private int[] MPCalculate(List<BuildingPosGene> genome)//多进程计算分数
        {
            var bpgCollection = new BPGCollection();
            bpgCollection.Genomes = genome;
            var nbytes1 = 2 * 1024 * 1024;
            var nbytes2 = 1024 * 1024;
            t_pre = stopWatch.Elapsed.TotalSeconds;
            var scores = new int[genome.Count];
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("BPGCollection", nbytes1))// 1mb
            {
                //写入：
                using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                {
                    BinaryWriter writer = new BinaryWriter(stream);
                    bpgCollection.WriteToStream(writer);
                }
                var ParkingCntFileList = new List<MemoryMappedFile>();
                var nextMutexList = new List<Mutex>();
                for (int idx = 0; idx < ProcessCnt; idx++)
                {
                    var parkingCntFile = MemoryMappedFile.CreateNew("BResults" + idx.ToString(), nbytes2);
                    ParkingCntFileList.Add(parkingCntFile);
                    nextMutexList.Add(CreateMutex("Mutex" + Iter.ToString() + "_", idx));//下一代进程锁
                }
                MutexLists.Add(nextMutexList);
                var currentMutexList = MutexLists[Iter - 1];
                Logger?.Information($"写入数据用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");
                //进程锁解锁，子进程开始计算
                t_pre = stopWatch.Elapsed.TotalSeconds;
                for (int idx = currentMutexList.Count - 1; idx >= 0; idx--)
                {
                    currentMutexList[idx].ReleaseMutex();
                    Thread.Sleep(2);
                }
                currentMutexList.ForEach(mutex => mutex.WaitOne(-1, true));//等待结束
                Logger?.Information($"子进程全部计算完成用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");

                t_pre = stopWatch.Elapsed.TotalSeconds;
                for (int idx = 0; idx < ProcessCnt; idx++)//更新分数
                {
                    var submmf = ParkingCntFileList[idx];
                    using (MemoryMappedViewStream stream = submmf.CreateViewStream(0L, 0L, MemoryMappedFileAccess.Read))
                    {
                        BinaryReader reader = new BinaryReader(stream);
                        var subSocres = ReadWriteEx.ReadInts(reader);
                        UpdateScore(idx,scores,subSocres);
                    }
                }
                ParkingCntFileList.ForEach(submmf => submmf.Dispose());
                Logger?.Information($"读取用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");
            }

            return scores;
        }
        private void UpdateScore(int idx,int[] scores,List<int> subScores)
        {
            for (int i = 0; i < subScores.Count(); i++)
            {
                scores[i*ProcessCnt + idx] = subScores[i];
            }
        }
        private Process CreateSubProcess(int idx, bool LogAllInfo, int ThreadCnt)
        {
            var proc = new Process();
            var currentDllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            proc.StartInfo.FileName = Path.Combine(currentDllPath, "ThParkingStall.Core.exe");
            string log_subprocess;
            if (LogAllInfo) log_subprocess = "1";
            else log_subprocess = "0";
            proc.StartInfo.Arguments = ProcessCnt.ToString() + ' ' + idx.ToString() + ' ' +
                VM.IterationCount.ToString() + ' ' + log_subprocess + ' ' + ThreadCnt.ToString() + ' ' + "2";
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            //proc.StartInfo.RedirectStandardOutput = true;
            //proc.Start();
            return proc;
        }
        private Mutex CreateMutex(string mutexName, int idx = -1, bool initowned = true)
        {
            bool mutexCreated;
            string Name;
            if(idx!=-1) Name = mutexName + idx.ToString();
            else Name = mutexName;
            Mutex mutex = new Mutex(initowned, Name, out mutexCreated);
            //Logger?.Information("Init mutex status:" + mutexCreated.ToString());
            if (!mutexCreated)
            {
                try
                {
                    mutex = Mutex.OpenExisting(Name, System.Security.AccessControl.MutexRights.FullControl);
                    mutex.Dispose();
                    mutex = new Mutex(initowned, Name, out mutexCreated);
                    Logger?.Information("second mutex status:" + mutexCreated.ToString());
                }
                catch (Exception ex)
                {
                    Logger?.Information("still have problem on mutex status:" + mutexCreated.ToString());
                }
            }
            return mutex;
        }
    }
}
