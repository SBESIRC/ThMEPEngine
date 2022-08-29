using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
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
using ThMEPArchitecture.MultiProcess;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.ViewModel;
using ThMEPEngineCore;
using ThParkingStall.Core.IO;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.OInterProcess;
using ThParkingStall.Core.OTools;

namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    public class OGAGenerator : IDisposable
    {
        //Genetic Algorithm parameters
        double MaxTime = 60;
        int IterationCount = 60;
        int PopulationSize = 80;
        double SelectionRate;
        int SelectionSize;
        int MaxCount = 10;//出现相同车位数的最大次数
        double MutationRate;
        double GeneMutationRate;
        double SpecialGeneProp;//特殊基因比例

        double EliteProp;//精英比例
        int Elite_popsize;

        double SMProp;//小变异比例
        int SMsize;//小变异数量
        int Max_SelectionSize;
        double EliminateRate;
        double GoldenRatio;

        int TargetParkingCntMin;
        int TargetParkingCntMax;

        double AreaMax;
        //private List<(double, double)> LowerUpperBound;
        //Inputs

        ParkingStallArrangementViewModel ParameterViewModel;
        public Serilog.Core.Logger Logger = null;
        public Serilog.Core.Logger DisplayLogger = null;
        public DisplayInfo displayInfo = null;
        public int ProcessCount;
        //public List<Process> ProcList;//进程列表
        public List<List<Mutex>> MutexLists;//进程锁列表的列表

        public int CurIteration;
        public OGAGenerator(ParkingStallArrangementViewModel parameterViewModel = null)
        {
            //大部分参数采取黄金分割比例，保持选择与变异过程中种群与基因相对稳定
            GoldenRatio = (Math.Sqrt(5) - 1) / 2;//0.618
            IterationCount = parameterViewModel == null ? 60 : parameterViewModel.IterationCount;

            PopulationSize = parameterViewModel == null ? 80 : parameterViewModel.PopulationCount;//种群数量
            if (PopulationSize < 3) throw (new ArgumentOutOfRangeException("种群数量至少为3"));
            //默认值 核心数 -1,最多为种群数
            int max_process;
            if (ParameterStock.ProcessCount == -1)
            {
                if (Environment.ProcessorCount <= 32)
                {
                    max_process = Environment.ProcessorCount;
                }
                else max_process = PopulationSize;
            }
            else max_process = ParameterStock.ProcessCount;
            ProcessCount = Math.Min(max_process, PopulationSize);
            MutexLists = new List<List<Mutex>>();
            MaxTime = parameterViewModel == null ? 180 : parameterViewModel.MaxTimespan;//最大迭代时间

            MutationRate = parameterViewModel.MutationRate;//变异因子,0.382
            SpecialGeneProp = parameterViewModel.SpecialGeneProp;

            GeneMutationRate = parameterViewModel.GeneMutationRate;//基因变异因子0.382,保持迭代过程中变异基因的比例

            SelectionRate = parameterViewModel.SelectionRate;//保留因子0.382
            SelectionSize = Math.Max(2, (int)(SelectionRate * PopulationSize));

            //InputsF
            ParameterViewModel = parameterViewModel;

            // Run2 添加参数
            EliteProp = parameterViewModel.EliteProp;
            Elite_popsize = Math.Max((int)(PopulationSize * EliteProp), 1);//精英种群数量,种群数要大于3
            EliminateRate = GoldenRatio;//除保留部分随机淘汰概率0.618
            Max_SelectionSize = Math.Max(2, (int)(GoldenRatio * PopulationSize));//最大保留数量0.618
            SMProp = parameterViewModel.SMProp;
            SMsize = Math.Max(1, (int)(SMProp * PopulationSize));//小变异比例

            TargetParkingCntMin = parameterViewModel.TargetParkingCntMin;
            TargetParkingCntMax = parameterViewModel.TargetParkingCntMax;
            AreaMax = ParameterStock.AreaMax;
        }

        private void ReclaimMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
        }
        public double GetScore(Genome genome,int flag)
        {
            var parkingCnt = genome.ParkingStallCount;
            double score;
            switch (flag)
            {
                case 0:
                    score = parkingCnt;
                    break;
                case 1:
                    if(parkingCnt < 2) { score = 99999; }
                    else if(parkingCnt < ParameterViewModel.TargetParkingCntMin)
                    {
                        score = AreaMax / parkingCnt;
                    }
                    else if(parkingCnt <= ParameterViewModel.TargetParkingCntMax)
                    {
                        if(genome.Area >0) score = genome.Area / parkingCnt;
                        else score = ParameterStock.TotalArea / parkingCnt;
                    }
                    else
                    {
                        if (genome.Area > 0) score = genome.Area / ParameterViewModel.TargetParkingCntMax;
                        else score = ParameterStock.TotalArea / ParameterViewModel.TargetParkingCntMax;
                    }
                    break;
                default:
                    throw new NotImplementedException("Do Not Have This Case Now!");
            }
            genome.score = score;
            return score;
        }
        #region 第一代初始化

        private Genome RandomCreateChromosome()
        {
            var center = OInterParameter.TotalArea.Centroid;
            var solution = new Genome();
            foreach(var segLine in OInterParameter.InitSegLines)
            {
                double relativeValue;
                var maxDist = segLine.MaxValue - segLine.MinValue;
                if (RandDouble() < SpecialGeneProp)
                {
                    relativeValue = RandomSpecialNumber(maxDist);//随机特殊解
                }
                else
                {
                    relativeValue = ToRelativeValue(RandDoubleInRange(0, maxDist),maxDist);//纯随机数
                }
                solution.Add(new OGene(0,relativeValue));
            }
            if(OInterParameter.BorderLines != null)
            {
                foreach(var l in OInterParameter.BorderLines)
                {
                    var std = ParameterViewModel.BorderlineMoveRange / 4;//2sigma 原则，从mean到边界概率为95.45%
                    double relativeValue;
                    if(new Vector2D(center.Coordinate,l.MidPoint).Dot(l.NormalVector()) > 0)
                    {
                        relativeValue = RandNormalInRange(ParameterViewModel.BorderlineMoveRange, std, 0, ParameterViewModel.BorderlineMoveRange);
                    }
                    else
                    {
                        relativeValue = RandNormalInRange(-ParameterViewModel.BorderlineMoveRange, std, -ParameterViewModel.BorderlineMoveRange,0);
                    }
                    solution.Add(new OGene(1, relativeValue));
                }
            }
            return solution;
        }
        public List<Genome> CreateFirstPopulation()
        {
            List<Genome> solutions = new List<Genome>();
            var orgSolution = new Genome();
            OInterParameter.InitSegLines.ForEach(l => orgSolution.Add(l.ToGene()));
            if (OInterParameter.BorderLines != null)
            {
                foreach (var l in OInterParameter.BorderLines)
                {
                    orgSolution.Add(new OGene(1, 0));
                }
            }
            solutions.Add(orgSolution);
            while (solutions.Count < PopulationSize)
            {
                solutions.Add(RandomCreateChromosome());
            }
            return solutions;
        }

        #endregion
        #region 随机函数
        //输入0~maxDist的数，返回正（相对于最小值）或负数（相对于最大值）
        public double ToRelativeValue(double RandomNumber,double maxDist)
        {
            if (RandomNumber < maxDist / 2)
            {
                return RandomNumber;
            }
            else return RandomNumber - maxDist;
        }

        private List<int> RandChoice(int UpperBound, int n = -1, int LowerBound = 0)
        {
            return General.Utils.RandChoice(UpperBound, n, LowerBound);
        }
        private double RandNormalInRange(double loc, double scale, double LowerBound, double UpperBound)
        {
            double tol = 1e-4;
            if (UpperBound - LowerBound <= tol) return loc;

            else return General.Utils.Truncnormal(loc, scale, LowerBound, UpperBound);
        }
        private int RandInt(int range)
        {
            return General.Utils.RandInt(range);
        }
        private double RandDouble()
        {
            return General.Utils.RandDouble();
        }
        private double RandDoubleInRange(double LowerBound, double UpperBound)
        {
            double tol = 1e-4;
            if (UpperBound - LowerBound < tol) return LowerBound;
            else return RandDouble() * (UpperBound - LowerBound) + LowerBound;
        }
        //直接返回相对值
        private double RandomSpecialNumber(double maxDist)
        {
            //随机的特殊解，用于卡车位
            // 输出的之保持在最大最小值之间
            var dist = ParameterStock.VerticalSpotLength + ParameterStock.D2;
            var SolutionLis = new List<double>() { 0.1, -0.1 };
            if(dist < maxDist)
            {
                SolutionLis.Add(dist);
                SolutionLis.Add(-dist);
            }
            return SolutionLis[RandInt(SolutionLis.Count)];
        }
        #endregion
        #region run2代码部分
        // 选择逻辑增强，除了选择一部分优秀解之外，对其余解随即保留
        // 后代生成逻辑增强，保留之前最优解直接保留，不做变异的逻辑。新增精英种群逻辑，保留精英种群，并且参与小变异。
        // 变异逻辑增强，增加小变异（用于局部最优化搜索），保留之前的变异逻辑（目前称之为大变异）。
        // 对精英种群和一部分交叉产生的后代使用小变异，对一部分后代使用大变异，对剩下的后代不做变异。
        public List<Genome> Run()
        {
            Logger?.Information($"迭代次数: {IterationCount}");
            Logger?.Information($"种群数量: {PopulationSize}");
            Logger?.Information($"最大迭代时间: {MaxTime} 分");
            Logger?.Information($"CPU数量：" + Environment.ProcessorCount.ToString());
            DisplayLogger?.Information($"预计代数: {IterationCount}\t");
            //DisplayLogger?.Information($"种群数量: {PopulationSize}\t");
            MCompute.Logger = Logger;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            List<Genome> selected = new List<Genome>();
            var currentMutexList = new List<Mutex>();
            var ProcList = new List<Process>();
            MutexLists = new List<List<Mutex>>();
            var initSingnals = new List<Mutex>();
            for (int idx = 0; idx < ProcessCount; idx++)
            {
                initSingnals.Add(CreateMutex("Mutex", idx, false));
                var proc = CreateSubProcess(idx, ParameterStock.LogSubProcess, ParameterStock.ThreadCount);
                //var proc = CreateSubProcess(idx, true, ParameterStock.ThreadCount);
                ProcList.Add(proc);
                currentMutexList.Add(CreateMutex("Mutex0_", idx));
                //NextMutexList.Add(CreateMutex("CalculationFinished", idx));
            }
            MutexLists.Add(currentMutexList);
            ProcList.ForEach(proc => proc.Start());

            try
            {
                //MutexEndList.ForEach(mutex => mutex.ReleaseMutex());
                Logger?.Information($"进程数: {ProcessCount }");
                Logger?.Information($"进程启动用时: {stopWatch.Elapsed.TotalSeconds }");
                var t_pre = stopWatch.Elapsed.TotalSeconds;
                var pop = CreateFirstPopulation();
                Logger?.Information($"初代生成时间: {stopWatch.Elapsed.TotalSeconds - t_pre}");
                t_pre = stopWatch.Elapsed.TotalSeconds;
                var strFirstPopCnt = $"第一代种群数量: {pop.Count}\n";
                Logger?.Information(strFirstPopCnt);
                CurIteration = 0;
                int maxCount = 0;
                int maxNums = 0;
                int lamda; //变异方差，随代数递减
                initSingnals.ForEach(s =>s.WaitOne());  
                while (CurIteration++ < IterationCount && maxCount < MaxCount && stopWatch.Elapsed.TotalMinutes < MaxTime)
                {
                    var strCurIterIndex = $"迭代次数：{CurIteration}";
                    Logger?.Information(strCurIterIndex);
                    DisplayLogger?.Information(strCurIterIndex + "\t");
                    System.Diagnostics.Debug.WriteLine(strCurIterIndex);
                    System.Diagnostics.Debug.WriteLine($"Total seconds: {stopWatch.Elapsed.TotalSeconds}");
                    selected = Selection(pop, out int CurNums);
                    //Logger?.Information($"选择总用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");
                    if (maxNums == CurNums)
                    {
                        maxCount++;
                    }
                    else
                    {
                        maxCount = 0;
                        maxNums = CurNums;
                    }
                    var temp_list = CreateNextGeneration(selected);
                    // 小变异
                    pop = temp_list[0];
                    lamda = CurIteration + 3;// 小变异系数，随时间推移，变异缩小，从4 开始
                    MutationS(pop, lamda);
                    // 大变异
                    var rstLM = temp_list[1];
                    MutationL(rstLM);
                    pop.AddRange(rstLM);
                    Logger?.Information($"当前代用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒\n");
                    t_pre = stopWatch.Elapsed.TotalSeconds;
                    if (CurIteration % 3 == 0)
                        ReclaimMemory();
                }

                string strConverged;
                if (maxCount < MaxCount) strConverged = $"未收敛";
                else strConverged = $"已收敛";
                Active.Editor.WriteMessage(strConverged);
                Logger?.Information(strConverged);
                if (displayInfo != null) displayInfo.FinalIterations = "最终代数: " + (CurIteration - 1).ToString() + "(" + strConverged + ")";
                DisplayLogger?.Information("最终代数: " + (CurIteration - 1).ToString() + "\t");
                DisplayLogger?.Information("收敛情况: " + strConverged + "\t");
                stopWatch.Stop();
                var strTotalMins = $"迭代时间: {stopWatch.Elapsed.TotalMinutes} 分";
                Logger?.Information(strTotalMins);
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
                    //x.Dispose();
                });
                MutexLists.ForEach(l => l.ForEach(x => x.Dispose()));
                MutexLists.Clear();
            }
            // 返回最后一代选择的比例
            return selected.Take(SelectionSize).ToList();
        }
        private List<Genome> Selection(List<Genome> inputSolution, out int maxNums)
        {
            Logger?.Information("进行选择");
            //Logger?.Information("已计算SubArea个数：" + SubAreaParkingCnt.CachedPartitionCnt.Count.ToString());
#if DEBUG
            CalculateSP(inputSolution);
#else
            CalculateMP(inputSolution);
#endif
            List<Genome> sorted;
            if (ParameterStock.BorderlineMoveRange == 0)
            {
                sorted = inputSolution.OrderByDescending(s => s.ParkingStallCount).ToList();
                maxNums = sorted.First().ParkingStallCount;
            }
            else
            {
                inputSolution.ForEach(s => GetScore(s, 1));
                sorted = inputSolution.OrderBy(s => s.score).ToList();
                var scores = inputSolution.Select(s =>s.score).OrderBy(l =>l).ToList();
                maxNums = sorted.First().ParkingStallCount;
                var strScore = $"当前分数：";
                for (int k = 0; k < sorted.Count; ++k)
                {
                    strScore += string.Format("{0:N2}",(scores[k]));
                    strScore += " ";
                }
                Logger?.Information(strScore);

                var strArea = $"当前面积：";
                for (int k = 0; k < sorted.Count; ++k)
                {
                    strArea += string.Format("{0:N2}", (sorted[k].Area));
                    strArea += " ";
                }
                Logger?.Information(strArea);
            }
            var strCnt = $"当前车位数：";
            for (int k = 0; k < sorted.Count; ++k)
            {
                strCnt += sorted[k].ParkingStallCount.ToString();
                strCnt += " ";
            }
            Logger?.Information(strCnt);
            var maxCnt = sorted[0].ParkingStallCount;
            DisplayLogger?.Information("当前车位: " + maxCnt.ToString() + "\t");
            var areaPerStall = ParameterStock.TotalArea / maxCnt;
            DisplayLogger?.Information("车均面积: " + string.Format("{0:N2}", areaPerStall) + "平方米/辆\t");
            //System.Diagnostics.Debug.WriteLine(strCnt);
            var rst = new List<Genome>();
            // SelectionSize 直接保留
            for (int i = 0; i < SelectionSize; ++i)
            {
                rst.Add(sorted[i]);
            }
            //除了SelectionSize 随机淘汰;
            for (int i = SelectionSize; i < sorted.Count; ++i)
            {
                var Rand_d = RandDouble();
                if (Rand_d > EliminateRate)
                {
                    rst.Add(sorted[i]);
                }
                if (rst.Count == Max_SelectionSize)
                {
                    break;
                }
            }
            if (rst.Count % 2 != 0)
            {
                rst.RemoveAt(rst.Count - 1);
            }
            return rst;
        }
        private void CalculateSP(List<Genome> inputSolution)
        {
            foreach (var solution in inputSolution)
            {
                try
                {
                    var subAreas = OInterParameter.GetOSubAreas(solution);
                    subAreas.ForEach(s => s.UpdateParkingCnts());
                    var ParkingStallCount = subAreas.Where(s => s.Count > 0).Sum(s => s.Count);
                    solution.ParkingStallCount = ParkingStallCount;
                    //var newSegs = OInterParameter.ProcessToSegLines(solution);
                    //showSegLines(newSegs);
                }
                catch (Exception ex)
                {
                    Logger?.Information(ex.Message);
                    Logger?.Information("##################################");
                    Logger?.Information(ex.StackTrace);
                }
            }
            Logger?.Information("cached:" + MCompute.CatchedTimes.ToString());
        }
        private void CalculateMP(List<Genome> inputSolution)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var t_pre = 0.0;
            var chromosomeCollection = new GenomeColection();
            chromosomeCollection.Genomes = inputSolution;
            chromosomeCollection.NewCachedPartitionCnt = OCached.NewCachedPartitionCnt;
            var nbytes1 = 4 * 1024 * 1024;
            var nbytes2 = 2 * 1024 * 1024;
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("GenomeColection", nbytes1))// 1mb
            {
                using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                {
                    BinaryWriter writer = new BinaryWriter(stream);
                    chromosomeCollection.WriteToStream(writer);
                    OCached.ClearNewAdded();//清空上一轮记录
                }
                var ParkingCntFileList = new List<MemoryMappedFile>();
                var nextMutexList = new List<Mutex>();
                for (int idx = 0; idx < ProcessCount; idx++)
                {
                    var parkingCntFile = MemoryMappedFile.CreateNew("OResults" + idx.ToString(), nbytes2);
                    ParkingCntFileList.Add(parkingCntFile);
                    nextMutexList.Add(CreateMutex("Mutex" + CurIteration.ToString() + "_", idx));
                }
                MutexLists.Add(nextMutexList);
                var currentMutexList = MutexLists[CurIteration - 1];
                Logger?.Information($"写入数据用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");
                t_pre = stopWatch.Elapsed.TotalSeconds;
                for (int idx = currentMutexList.Count - 1; idx >= 0; idx--)
                {
                    currentMutexList[idx].ReleaseMutex();//进程锁解锁，子进程开始计算
                    Thread.Sleep(2);
                }
                currentMutexList.ForEach(mutex => mutex.WaitOne(-1, true));//等待结束
                Logger?.Information($"子进程全部计算完成用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");
                t_pre = stopWatch.Elapsed.TotalSeconds;
                for (int idx = 0; idx < ProcessCount; idx++)//更新车位记录
                {
                    var submmf = ParkingCntFileList[idx];
                    using (MemoryMappedViewStream stream = submmf.CreateViewStream(0L, 0L, MemoryMappedFileAccess.Read))
                    {
                        BinaryReader reader = new BinaryReader(stream);
                        var layoutResults = ReadWriteEx.ReadLayoutResults(reader);
                        var subProcCached = ReadWriteEx.ReadOCached(reader);
                        UpdateParkingNumber(idx, inputSolution, layoutResults, subProcCached);
                    }
                }
                ParkingCntFileList.ForEach(submmf => submmf.Dispose());
                Logger?.Information($"读取用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");
            }
        }
        private void UpdateParkingNumber(int idx, List<Genome> inputSolution, List<LayoutResult> layoutResults, Dictionary<OSubAreaKey, LayoutResult> subProcResult)
        {

            for (int i = 0; i < layoutResults.Count; i++)
            {
                inputSolution[i * ProcessCount + idx].ParkingStallCount = layoutResults[i].ParkingCnt;
                inputSolution[i * ProcessCount + idx].Area = layoutResults[i].Area;
            }
            OCached.Update(subProcResult);//更新子进程记录
        }
        private Genome Crossover(Genome s1, Genome s2)
        {
            var newS = new Genome();
            
            foreach(var k in s1.OGenes.Keys)
            {
                for(int i = 0;i < s1.OGenes[k].Count; i++)
                {
                    if(RandDouble() < 0.5)
                    {
                        newS.Add(s1.OGenes[k][i]);
                    }
                    else
                    {
                        newS.Add(s2.OGenes[k][i]);
                    }
                }
            }
            return newS;
        }
        private Mutex CreateMutex(string mutexName, int idx, bool initowned = true)
        {
            bool mutexCreated;
            Mutex mutex = new Mutex(initowned, mutexName + idx.ToString(), out mutexCreated);
            //Logger?.Information("Init mutex status:" + mutexCreated.ToString());
            if (!mutexCreated)
            {
                try
                {
                    mutex = Mutex.OpenExisting(mutexName + idx.ToString(), System.Security.AccessControl.MutexRights.FullControl);
                    mutex.Dispose();
                    mutex = new Mutex(initowned, mutexName + idx.ToString(), out mutexCreated);
                    Logger?.Information("second mutex status:" + mutexCreated.ToString());
                }
                catch (Exception ex)
                {
                    Logger?.Information("still have problem on mutex status:" + mutexCreated.ToString());
                }
            }

            return mutex;
        }
        private Process CreateSubProcess(int idx, bool LogAllInfo, int ThreadCnt)
        {
            var proc = new Process();
            var currentDllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            proc.StartInfo.FileName = Path.Combine(currentDllPath, "ThParkingStall.Core.exe");
            string log_subprocess;
            if (LogAllInfo) log_subprocess = "1";
            else log_subprocess = "0";
            proc.StartInfo.Arguments = ProcessCount.ToString() + ' ' + idx.ToString() + ' ' +
                IterationCount.ToString() + ' ' + log_subprocess + ' ' + ThreadCnt.ToString() + ' ' + "1";
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            //proc.StartInfo.RedirectStandardOutput = true;
            //proc.Start();
            return proc;
        }
        private List<List<Genome>> CreateNextGeneration(List<Genome> solutions)
        {
            List<Genome> rstSM = new List<Genome>();
            List<Genome> rstLM = new List<Genome>();
            for (int i = 0; i < Elite_popsize; ++i)
            {
                //添加精英，后续参与小变异
                rstSM.Add(solutions[i].Clone());
            }
            List<int> index;
            int j = Elite_popsize;
            //int SMsize = SelectionSize;// small mutation size,0.382 of total population size
            while (true)
            {
                // 随机两两生成后代
                //index.Shuffle();
                index = RandChoice(solutions.Count);
                for (int i = 0; i < index.Count / 2; ++i)
                {
                    var s = Crossover(solutions[index[2 * i]].Clone(), solutions[index[2 * i + 1]].Clone());
                    if (j < SMsize)//添加小变异
                    {
                        rstSM.Add(s);
                    }
                    else//其余大变异
                    {
                        rstLM.Add(s);
                    }
                    j++;
                    if (j == PopulationSize)
                    {
                        return new List<List<Genome>> { rstSM, rstLM };
                    }
                }
            }
        }
        private void MutationL(List<Genome> s)
        {
            // large mutation
            int cnt = Math.Min((int)(s.Count * MutationRate), 1);//需要变异的染色体数目，最小为1
           
            //需要变异的染色体list：
            var selectedGenomeIdx = RandChoice(s.Count, cnt);
            foreach (int i in selectedGenomeIdx)
            {
                foreach (var geneType in s[i].OGenes.Keys)
                {
                    var totalGeneCnt = s[i].OGenes[geneType].Count;
                    var mutationCnt = Math.Min((int)(totalGeneCnt * GeneMutationRate), 1);//需要变异的基因数目，最小为1
                    var selectedIdx = RandChoice(totalGeneCnt, mutationCnt);
                    foreach (int j in selectedIdx)
                    {
                        switch (geneType)
                        {
                            case 0:
                                double minVal = OInterParameter.InitSegLines[j].MinValue;
                                double maxVal = OInterParameter.InitSegLines[j].MaxValue;
                                var maxDist = maxVal - minVal;
                                s[i].OGenes[geneType][j].dDNAs.First().Value = ToRelativeValue(RandDoubleInRange(0, maxDist), maxDist);//纯随机数
                                break;
                            case 1:
                                var orgValue = s[i].OGenes[geneType][j].dDNAs.First().Value;
                                var std = ParameterViewModel.BorderlineMoveRange / 4;//2sigma 原则，从mean到边界概率为95.45%
                                double relativeValue = RandNormalInRange(orgValue, std, -ParameterViewModel.BorderlineMoveRange, ParameterViewModel.BorderlineMoveRange);
                                s[i].OGenes[geneType][j].dDNAs.First().Value = relativeValue;
                                break;
                            default:
                                throw new NotImplementedException("Do not have this type now");
                        }
                    }

                }
            }
        }
        // small mutation
        private void MutationS(List<Genome> s, int lamda)
        {
            // 除第一个染色体变异
            for (int i = 1; i < s.Count; i++)
            {
                foreach(var geneType in s[i].OGenes.Keys)
                {
                    var totalGeneCnt = s[i].OGenes[geneType].Count;
                    var mutationCnt = Math.Min((int)(totalGeneCnt * GeneMutationRate), 1);//需要变异的基因数目，最小为1
                    var selectedIdx = RandChoice(totalGeneCnt, mutationCnt);
                    foreach (int j in selectedIdx)
                    {
                        switch (geneType)
                        {
                            case 0:
                                double minVal = OInterParameter.InitSegLines[j].MinValue;
                                double maxVal = OInterParameter.InitSegLines[j].MaxValue;
                                var maxDist = maxVal - minVal;
                                var orgValue = s[i].OGenes[geneType][j].dDNAs.First().Value;
                                double newValue;
                                var std = (maxVal - minVal) / lamda;//2sigma 原则，从mean到边界概率为95.45%
                                if (RandDouble() > SpecialGeneProp)
                                {
                                    if(orgValue > 0)//变异出的值也大于0
                                    {
                                        newValue = RandNormalInRange(orgValue, std, 0, maxDist);
                                    }
                                    else
                                    {
                                        newValue = RandNormalInRange(orgValue, std, -maxDist, 0);
                                    }
                                }
                                else
                                {
                                    newValue = RandomSpecialNumber(maxDist);
                                }
                                s[i].OGenes[geneType][j].dDNAs.First().Value = newValue;
                                break;
                            case 1:
                                orgValue = s[i].OGenes[geneType][j].dDNAs.First().Value;
                                std = ParameterViewModel.BorderlineMoveRange / lamda;//2sigma 原则，从mean到边界概率为95.45%
                                double relativeValue = RandNormalInRange(orgValue, std, -ParameterViewModel.BorderlineMoveRange, ParameterViewModel.BorderlineMoveRange);
                                s[i].OGenes[geneType][j].dDNAs.First().Value = relativeValue;
                                break;
                            default:
                                throw new NotImplementedException("Do not have this type now");
                        }
                    }
                }
            }
        }
#endregion

        private void showSegLines(List<SegLine> segLines,bool showSplitters = true)
        {
            var layer = "基因线，代数：" + CurIteration + "_";
            List<LineSegment> LineToShow;
            if (showSplitters)
            {
                LineToShow = segLines.Where(l => l.Splitter != null).Select(l => l.Splitter).ToList();
            }
            else
            {
                LineToShow = segLines.Where(l => l.VaildLane != null).Select(l => l.VaildLane).ToList();
            }
            using (AcadDatabase acad = AcadDatabase.Active())
            {
                if (!acad.Layers.Contains(layer))
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acad.Database, layer, 2);
                var outSegLines = LineToShow.Select(l => l.ToDbLine(2, layer)).Cast<Entity>().ToList();
                outSegLines.ShowBlock(layer, layer);
                //finalSegLines.Select(l => l.ToDbLine(2, layer)).Cast<Entity>().ToList().ShowBlock(layer, layer);
                //MPEX.HideLayer(layer);
            }
        }
        public void Dispose()
        {

        }
    }
}
