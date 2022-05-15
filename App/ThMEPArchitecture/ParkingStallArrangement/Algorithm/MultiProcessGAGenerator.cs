using AcHelper;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThMEPArchitecture.MultiProcess;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.ViewModel;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.IO;
using ThParkingStall.Core.MPartitionLayout;
using static ThParkingStall.Core.MPartitionLayout.MCompute;
using MPChromosome = ThParkingStall.Core.InterProcess.Chromosome;
using MPGene = ThParkingStall.Core.InterProcess.Gene;
namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{

    public class MultiProcessGAGenerator : IDisposable
    {
        //Genetic Algorithm parameters
        double MaxTime = 60;
        int IterationCount = 60;
        int PopulationSize = 80;
        int FirstPopulationSize;
        double SelectionRate;
        int FirstPopulationSizeMultiplyFactor = 1;
        int SelectionSize;
        int MaxCount = 10;//出现相同车位数的最大次数
        double MutationRate;
        double GeneMutationRate;

        int Elite_popsize;
        int Max_SelectionSize;
        double EliminateRate;
        double GoldenRatio;
        private bool SpecialOnly;
        private List<(double, double)> LowerUpperBound;
        //Inputs

        ParkingStallArrangementViewModel ParameterViewModel;
        public Serilog.Core.Logger Logger = null;

        public int ProcessCount;
        //public List<Process> ProcList;//进程列表
        public List<List<Mutex>> MutexLists;//进程锁列表的列表

        public int CurIteration;
        public MultiProcessGAGenerator(ParkingStallArrangementViewModel parameterViewModel = null)
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
                max_process = Environment.ProcessorCount;
            }
            else max_process = ParameterStock.ProcessCount;
            //ProcessCount = Math.Min(Environment.ProcessorCount - 1, PopulationSize);
            ProcessCount = Math.Min(max_process, PopulationSize);
            //var ProcList = new List<Process>();
            MutexLists = new List<List<Mutex>>();
            MaxTime = parameterViewModel == null ? 180 : parameterViewModel.MaxTimespan;//最大迭代时间

            FirstPopulationSizeMultiplyFactor = 1;
            FirstPopulationSize = PopulationSize * FirstPopulationSizeMultiplyFactor;
            MutationRate = 1 - GoldenRatio;//变异因子,0.382
            GeneMutationRate = 1 - GoldenRatio;//基因变异因子0.382,保持迭代过程中变异基因的比例

            SelectionRate = 1 - GoldenRatio;//保留因子0.382
            SelectionSize = Math.Max(2, (int)(SelectionRate * PopulationSize));

            //InputsF
            ParameterViewModel = parameterViewModel;

            // Run2 添加参数
            Elite_popsize = Math.Max((int)(PopulationSize * 0.2), 1);//精英种群数量,种群数要大于3
            EliminateRate = GoldenRatio;//除保留部分随机淘汰概率0.618
            Max_SelectionSize = Math.Max(2, (int)(GoldenRatio * PopulationSize));//最大保留数量0.618
            LowerUpperBound = InterParameter.LowerUpperBound;//储存每条基因可变动范围，方便后续变异
        }

        private void ReclaimMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
        }

        #region 第一代初始化
        private List<MPGene> ConvertLineToGene()//仅根据分割线生成第一代
        {
            var genome = new List<MPGene>();
            foreach (var lineSeg in InterParameter.InitSegLines)
            {
                MPGene gene = new MPGene(lineSeg);
                genome.Add(gene);
            }
            return genome;
        }

        private bool RandomCreateChromosome(out MPChromosome solution, int N = 20)
        {
            // Try N times
            solution = new MPChromosome();
            for (int j = 0; j < N; j++)
            {
                var genome = new List<MPGene>();
                for (int i = 0; i < InterParameter.InitSegLines.Count; i++)
                {
                    var line = InterParameter.InitSegLines[i];
                    double LowerBound = LowerUpperBound[i].Item1;
                    double UpperBound = LowerUpperBound[i].Item2;
                    double RandValue;
                    if (RandDouble() > GoldenRatio)
                    {
                        RandValue = RandomSpecialNumber(LowerBound, UpperBound);//随机特殊解
                    }
                    else
                    {
                        RandValue = RandDoubleInRange(LowerBound, UpperBound);//纯随机数
                    }
                    MPGene gene = new MPGene(line, RandValue);
                    genome.Add(gene);
                }
                solution.Genome = genome;
                if (InterParameter.IsValid(solution))
                {
                    return true;
                }
            }
            return false;
        }
        public List<MPChromosome> CreateFirstPopulation()
        {
            List<MPChromosome> solutions = new List<MPChromosome>();
            // 添加初始画的分割线,该解必须是合理解
            var orgSolution = new MPChromosome();

            var orgGenome = ConvertLineToGene();//创建初始基因序列
            orgSolution.Genome = orgGenome;
            solutions.Add(orgSolution);

            var RndFlag = RandomCreateChromosome(out MPChromosome Rsolution, 200);//尝试200次看看有没有合理解
            if (RndFlag) solutions.Add(Rsolution);//找到了合理解
            while (solutions.Count < FirstPopulationSize)
            {
                // 随机生成 其余的解
                var FoundVaild = false;
                if (RndFlag)//之前找到合理解
                {
                    FoundVaild = RandomCreateChromosome(out MPChromosome solution);//尝试找一下
                    //ReclaimMemory();
                    if (FoundVaild) solutions.Add(solution);
                }
                if (!FoundVaild)//没有合理解
                {
                    // 没找到则在之前解随机挑选一个
                    var idx = RandInt(solutions.Count);
                    solutions.Add(solutions[idx].Clone());
                }
            }
            return solutions;
        }

        #endregion
        #region 随机函数
        private List<int> RandChoice(int UpperBound, int n = -1, int LowerBound = 0)
        {
            return General.Utils.RandChoice(UpperBound, n, LowerBound);
        }
        private double RandNormalInRange(double loc, double scale, double LowerBound, double UpperBound)
        {
            if (SpecialOnly)
            {
                return RandomSpecialNumber(LowerBound, UpperBound);
            }
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
            if (SpecialOnly)
            {
                return RandomSpecialNumber(LowerBound, UpperBound);
            }
            double tol = 1e-4;
            if (UpperBound - LowerBound < tol) return LowerBound;
            else return RandDouble() * (UpperBound - LowerBound) + LowerBound;
        }
        private double RandomSpecialNumber(double LowerBound, double UpperBound)
        {
            //随机的特殊解，用于卡车位
            // 输出的之保持在最大最小值之间
            double tol = 1e-4;
            if (UpperBound - LowerBound < tol) return LowerBound;
            else
            {
                var dist = ParameterStock.VerticalSpotLength + ParameterStock.D2;
                var SolutionLis = new List<double>() { LowerBound, UpperBound };
                var s1 = LowerBound + dist;
                var s2 = UpperBound - dist;
                if (s1 < UpperBound) SolutionLis.Add(s1);
                if (s2 > LowerBound) SolutionLis.Add(s2);
                return SolutionLis[RandInt(SolutionLis.Count)];// 随机选一个
            }
        }
        #endregion
        #region run2代码部分
        // 选择逻辑增强，除了选择一部分优秀解之外，对其余解随即保留
        // 后代生成逻辑增强，保留之前最优解直接保留，不做变异的逻辑。新增精英种群逻辑，保留精英种群，并且参与小变异。
        // 变异逻辑增强，增加小变异（用于局部最优化搜索），保留之前的变异逻辑（目前称之为大变异）。
        // 对精英种群和一部分交叉产生的后代使用小变异，对一部分后代使用大变异，对剩下的后代不做变异。
        public List<MPChromosome> Run2()
        {
            Logger?.Information($"迭代次数: {IterationCount}");
            Logger?.Information($"种群数量: {PopulationSize}");
            Logger?.Information($"最大迭代时间: {MaxTime} 分");
            Logger?.Information($"CPU数量：" + Environment.ProcessorCount.ToString());
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            List<MPChromosome> selected = new List<MPChromosome>();
            var currentMutexList = new List<Mutex>();
            var ProcList = new List<Process>();
            //var MutexLists = new List<List<Mutex>>();

            for (int idx = 0; idx < ProcessCount; idx++)
            {
                var proc = CreateSubProcess(idx, ParameterStock.LogSubProcess, ParameterStock.ThreadCount);
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
                Active.Editor.WriteMessage(strFirstPopCnt);
                Logger?.Information(strFirstPopCnt);
                CurIteration = 0;
                int maxCount = 0;
                int maxNums = 0;

                int lamda; //变异方差，随代数递减

                while (CurIteration++ < IterationCount && maxCount < MaxCount && stopWatch.Elapsed.TotalMinutes < MaxTime)
                {
                    var strCurIterIndex = $"迭代次数：{CurIteration}";
                    Logger?.Information(strCurIterIndex);
                    System.Diagnostics.Debug.WriteLine(strCurIterIndex);
                    System.Diagnostics.Debug.WriteLine($"Total seconds: {stopWatch.Elapsed.TotalSeconds}");
                    selected = Selection2(pop, out int CurNums);
                    //Logger?.Information($"选择总用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");
                    if (maxNums >= CurNums)
                    {
                        maxCount++;
                    }
                    else
                    {
                        maxCount = 0;
                        maxNums = CurNums;
                    }
                    var temp_list = CreateNextGeneration2(selected);
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
        private List<MPChromosome> Selection2(List<MPChromosome> inputSolution, out int maxNums)
        {
            Logger?.Information("进行选择");
            Logger?.Information("已计算SubArea个数：" + SubAreaParkingCnt.CachedPartitionCnt.Count.ToString());
#if DEBUG
            CalculateParkingSpacesSP(inputSolution);
#else
            CalculateParkingSpacesMP(inputSolution);
#endif
            //CalculateParkingSpacesSP(inputSolution);
            //CalculateParkingSpacesMP(inputSolution);
            //ReclaimMemory();

            var sorted = inputSolution.OrderByDescending(s => s.ParkingStallCount).ToList();
            maxNums = sorted.First().ParkingStallCount;
            var strCnt = $"当前车位数：";
            for (int k = 0; k < sorted.Count; ++k)
            {
                strCnt += sorted[k].ParkingStallCount.ToString();
                strCnt += " ";
            }
            Logger?.Information(strCnt);
            System.Diagnostics.Debug.WriteLine(strCnt);
            var rst = new List<MPChromosome>();
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
        private MPChromosome Crossover(MPChromosome s1, MPChromosome s2)
        {
            MPChromosome newS = new MPChromosome();
            var chromoLen = s1.Genome.Count;
            int[] covering_code = new int[chromoLen];
            for (int i = 0; i < chromoLen; ++i)
            {
                var cc = RandInt(2);
                if (cc == 0)
                {
                    newS.Append(s1.Genome[i]);
                }
                else
                {
                    newS.Append(s2.Genome[i]);
                }
            }

            return newS;
        }
        private void CalculateParkingSpacesSP(List<MPChromosome> inputSolution)
        {
            var chromosomeCollection = new ChromosomeCollection();
            chromosomeCollection.Chromosomes = inputSolution;

            for (int idx = 0; idx < ProcessCount; idx++)
            {
                var info = new string[2];
                info[0] = ProcessCount.ToString();
                info[1] = idx.ToString();

                var Result = ProgramDebug.TestMain(info, chromosomeCollection);
                int j = 0;
                for (int i = 0; i <= inputSolution.Count / ProcessCount; i++)
                {
                    inputSolution[i * ProcessCount + idx].ParkingStallCount = Result[j];
                    j++;
                    if (j >= Result.Count) break;
                }
            }


        }
        private void CalculateParkingSpacesMP(List<MPChromosome> inputSolution, int fileSize1 = 32, int fileSize2 = 2)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var t_pre = 0.0;
            var chromosomeCollection = new ChromosomeCollection();
            chromosomeCollection.Chromosomes = inputSolution;
            chromosomeCollection.NewCachedPartitionCnt = SubAreaParkingCnt.NewCachedPartitionCnt;
            var nbytes1 = fileSize1 * 1024 * 1024;
            var nbytes2 = fileSize2 * 1024 * 1024;
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("ChromosomeCollection", nbytes1))// 1mb
            {
                using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                {
                    //IFormatter formatter = new BinaryFormatter();
                    //formatter.Serialize(stream, chromosomeCollection);
                    chromosomeCollection.WriteToStream(stream);
                    SubAreaParkingCnt.ClearNewAdded();//清空上一轮记录
                }
                var ParkingCntFileList = new List<MemoryMappedFile>();
                var nextMutexList = new List<Mutex>();
                for (int idx = 0; idx < ProcessCount; idx++)
                {
                    var parkingCntFile = MemoryMappedFile.CreateNew("CachedPartitionCnt" + idx.ToString(), nbytes2);
                    ParkingCntFileList.Add(parkingCntFile);
                    nextMutexList.Add(CreateMutex("Mutex" + CurIteration.ToString() + "_", idx));
                }
                MutexLists.Add(nextMutexList);
                var currentMutexList = MutexLists[CurIteration - 1];
                Logger?.Information($"写入数据用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");
                t_pre = stopWatch.Elapsed.TotalSeconds;

                for (int idx = currentMutexList.Count - 1; idx >= 0; idx--)
                {
                    currentMutexList[idx].ReleaseMutex();
                    if (idx % 5 == 0) Thread.Sleep(1);
                }
                //currentMutexList.Reverse();

                //currentMutexList.ForEach(mutex => { mutex.ReleaseMutex(); Thread.Sleep(1); });//起始锁解锁，子进程开始计算
                Thread.Sleep(100);
                currentMutexList.ForEach(mutex => mutex.WaitOne(-1, true));//等待结束
                //currentMutexList.ForEach(mutex => mutex.Dispose());
                //currentMutexList.Clear();

                Logger?.Information($"子进程全部计算完成用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");
                t_pre = stopWatch.Elapsed.TotalSeconds;

                for (int idx = 0; idx < ProcessCount; idx++)//更新车位记录
                {
                    var submmf = ParkingCntFileList[idx];
                    using (MemoryMappedViewStream stream = submmf.CreateViewStream(0L, 0L, MemoryMappedFileAccess.Read))
                    {
                        //IFormatter formatter = new BinaryFormatter();
                        //var subProcResult = ((List<int>,List<List<(double, double)>>, List<int>))formatter.Deserialize(stream);
                        BinaryReader reader = new BinaryReader(stream);
                        var ParkingCnts = ReadWriteEx.ReadInts(reader);
                        var subProcCached = ReadWriteEx.ReadCached(reader);
                        UpdateParkingNumber(idx, inputSolution, ParkingCnts, subProcCached);
                    }
                }
                ParkingCntFileList.ForEach(submmf => submmf.Dispose());
                Logger?.Information($"读取用时: {stopWatch.Elapsed.TotalSeconds - t_pre}秒");
            }
        }
        private Process CreateSubProcess(int idx, bool LogAllInfo, int ThreadCnt)
        {
            var proc = new Process();
            var currentDllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            proc.StartInfo.FileName = System.IO.Path.Combine(currentDllPath, "ThParkingStall.Core.exe");
            string log_subprocess;
            if (LogAllInfo) log_subprocess = "1";
            else log_subprocess = "0";
            proc.StartInfo.Arguments = ProcessCount.ToString() + ' ' + idx.ToString() + ' ' +
                IterationCount.ToString() + ' ' + log_subprocess + ' ' + ThreadCnt.ToString();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            //proc.StartInfo.RedirectStandardOutput = true;
            //proc.Start();
            return proc;
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
        //private void UpdateParkingNumber(int idx,List<MPChromosome> inputSolution, (List<int>, List<List<(double, double)>>, List<int>) subProcResult)
        //{

        //    var parkingCnts = subProcResult.Item1;
        //    for(int i = 0; i < parkingCnts.Count; i++)
        //    {
        //        inputSolution[i * ProcessCount + idx].ParkingStallCount = parkingCnts[i];
        //    }
        //    SubAreaParkingCnt.Update(subProcResult.Item2, subProcResult.Item3);//更新子进程记录

        //}

        //private void UpdateParkingNumber(int idx, List<MPChromosome> inputSolution, List<int> parkingCnts, Dictionary<LinearRing, int> subProcResult)
        //{

        //    for (int i = 0; i < parkingCnts.Count; i++)
        //    {
        //        inputSolution[i * ProcessCount + idx].ParkingStallCount = parkingCnts[i];
        //    }
        //    SubAreaParkingCnt.Update(subProcResult);//更新子进程记录

        //}
        private void UpdateParkingNumber(int idx, List<MPChromosome> inputSolution, List<int> parkingCnts, Dictionary<SubAreaKey, int> subProcResult)
        {

            for (int i = 0; i < parkingCnts.Count; i++)
            {
                inputSolution[i * ProcessCount + idx].ParkingStallCount = parkingCnts[i];
            }
            SubAreaParkingCnt.Update(subProcResult);//更新子进程记录

        }
        private List<List<MPChromosome>> CreateNextGeneration2(List<MPChromosome> solutions)
        {
            List<MPChromosome> rstSM = new List<MPChromosome>();
            List<MPChromosome> rstLM = new List<MPChromosome>();
            for (int i = 0; i < Elite_popsize; ++i)
            {
                //添加精英，后续参与小变异
                rstSM.Add(solutions[i].Clone());
            }
            List<int> index;
            //List<int> index = Enumerable.Range(0, solutions.Count).ToList();
            int j = Elite_popsize;
            int SMsize = SelectionSize;// small mutation size,0.382 of total population size
            int LMsize = PopulationSize - SMsize;//large mutation size
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
                        return new List<List<MPChromosome>> { rstSM, rstLM };
                    }
                }
            }
        }
        private void MutationL(List<MPChromosome> s)
        {
            // large mutation
            int cnt = Math.Min((int)(s.Count * MutationRate), 1);//需要变异的染色体数目，最小为1
            int geneCnt = Math.Min((int)(s[0].GenomeCount() * GeneMutationRate), 1);//需要变异的基因数目，最小为1

            //需要变异的染色体list：
            var selectedChromosome = RandChoice(s.Count, cnt);
            foreach (int i in selectedChromosome)
            {
                //挑选需要变异的基因
                var selectedGene = RandChoice(s[0].GenomeCount(), geneCnt);
                foreach (int j in selectedGene)
                {
                    double minVal = LowerUpperBound[j].Item1;
                    double maxVal = LowerUpperBound[j].Item2;
                    //var dist = Math.Min(maxVal - minVal, MutationUpperBound);
                    var dist = maxVal - minVal;
                    s[i].Genome[j].Value = RandDoubleInRange(minVal, maxVal);
                }
            }
        }
        private void MutationS(List<MPChromosome> s, int lamda)
        {
            // small mutation
            // 除第一个染色体变异
            int geneCnt = Math.Min((int)(s[0].GenomeCount() * GeneMutationRate), 1);//需要变异的基因数目，最小为1
            for (int i = 1; i < s.Count; ++i)
            {
                //挑选需要变异的基因
                var selectedGene = RandChoice(s[0].GenomeCount(), geneCnt);
                //var cur_lam = (lamda * s.Count) / i;
                foreach (int j in selectedGene)
                {
                    // 对每个选中基因进行变异
                    double minVal = LowerUpperBound[j].Item1;
                    double maxVal = LowerUpperBound[j].Item2;

                    var loc = s[i].Genome[j].Value;

                    var std = (maxVal - minVal) / lamda;//2sigma 原则，从mean到边界概率为95.45%
                    if (RandDouble() < GoldenRatio)
                    {
                        s[i].Genome[j].Value = RandNormalInRange(loc, std, minVal, maxVal);
                    }
                    else
                    {
                        s[i].Genome[j].Value = RandomSpecialNumber(minVal, maxVal);
                    }

                }
            }
        }
        #endregion
        public void Dispose()
        {

        }
    }
}
