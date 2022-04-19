using System;
using AcHelper;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using Autodesk.AutoCAD.Geometry;
using ThCADExtension;
using System.Diagnostics;
using ThCADCore.NTS;
using ThMEPArchitecture.PartitionLayout;
using Dreambuild.AutoCAD;
using static ThMEPArchitecture.ParkingStallArrangement.ParameterConvert;
using ThMEPArchitecture.ViewModel;
using ThMEPArchitecture.MultiProcess;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using static ThParkingStall.Core.MPartitionLayout.MCompute;
namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    public class ParkingStallGAGenerator : IDisposable
    {
        //Genetic Algorithm parameters
        double MaxTime;
        int IterationCount = 10;
        int PopulationSize;
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
        private Dictionary<int, Tuple<double, double>> LowerUpperBound;
        //Inputs
        GaParameter GaPara;
        LayoutParameter LayoutPara;
        ParkingStallArrangementViewModel ParameterViewModel;
        List<Chromosome> InitGenomes;
        private bool BreakFlag;
        //public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "GaLog.txt");

        //public Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
        //    .File(LogFileName, flushToDiskInterval:new TimeSpan(0,0,5), rollingInterval: RollingInterval.Hour).CreateLogger();

        public Serilog.Core.Logger Logger = null;

        public ParkingStallGAGenerator(GaParameter gaPara, LayoutParameter layoutPara, ParkingStallArrangementViewModel parameterViewModel=null, List<Chromosome> initgenomes = null,bool breakFlag = false)
        {
            //大部分参数采取黄金分割比例，保持选择与变异过程中种群与基因相对稳定
            GoldenRatio = (Math.Sqrt(5) - 1) / 2;//0.618
            IterationCount = parameterViewModel == null ? 10 : parameterViewModel.IterationCount;
            var Rand = new Random(DateTime.Now.Millisecond);//随机数
            PopulationSize = parameterViewModel == null ? 10 : parameterViewModel.PopulationCount;//种群数量
            if (PopulationSize < 3) throw (new ArgumentOutOfRangeException("种群数量至少为3"));
            MaxTime =  parameterViewModel == null ? 180 : parameterViewModel.MaxTimespan;//最大迭代时间

            InitGenomes = initgenomes;// 输入初始基因，生成初代时使用
            // TO DO 更改迭代最大时间以及种群数量
            BreakFlag = breakFlag;// true 则为打断模式（打断模式包含早期迭代，纵向打断迭代，以及横向打断迭代）
            if (BreakFlag & InitGenomes == null)
            {
                if (InitGenomes == null) MaxTime = MaxTime * GoldenRatio;// 早期迭代模式，0.618总时长
                else//打断迭代，横纵各进行一次
                {
                    MaxTime = 0.5 * (1 - GoldenRatio) * MaxTime;// 0.191总时长
                    PopulationSize = Math.Max((int)(PopulationSize * GoldenRatio), 3);
                    InitGenomes.ForEach(g => g.Logger = this.Logger);
                }
            }

            FirstPopulationSizeMultiplyFactor = 1;
            FirstPopulationSize = PopulationSize * FirstPopulationSizeMultiplyFactor;
            MutationRate = 1 - GoldenRatio;//变异因子,0.382
            GeneMutationRate = 1 - GoldenRatio;//基因变异因子0.382,保持迭代过程中变异基因的比例

            SelectionRate = 1- GoldenRatio;//保留因子0.382
            SelectionSize = Math.Max(2, (int)(SelectionRate * PopulationSize));

            //InputsF
            GaPara = gaPara;
            LayoutPara = layoutPara;
            ParameterViewModel = parameterViewModel;

            // Run2 添加参数
            Elite_popsize = Math.Max((int)(PopulationSize * 0.2), 1);//精英种群数量,种群数要大于3
            EliminateRate = GoldenRatio;//除保留部分随机淘汰概率0.618
            Max_SelectionSize = Math.Max(2, (int)(GoldenRatio * PopulationSize));//最大保留数量0.618
            LowerUpperBound = new Dictionary<int, Tuple<double, double>>();//储存每条基因可变动范围，方便后续变异
            for (int i = 0; i < GaPara.LineCount; ++i)
            {
                GetBoundary(i, out double LowerBound, out double UpperBound);
                //UpperLowerBound[i] = new Tuple<double, double>(LowerBound, UpperBound);
                var tempT = new Tuple<double, double>(LowerBound, UpperBound);
                LowerUpperBound.Add(i, tempT);
            }
        }

        private void GetBoundary(int i, out double LowerBound, out double UpperBound)
        {
            double tol = 1e-4;
            // get absolute coordinate of segline
            var line = GaPara.SegLine[i];
            var dir = line.GetValue(out double value, out double startVal, out double endVal);
            if (Math.Abs(GaPara.MaxValues[i] - GaPara.MinValues[i])< tol)
            {
                LowerBound = value;
                UpperBound = value;
            }
            else
            {
                var Bound1 = GaPara.MinValues[i] + value;
                var Bound2 = GaPara.MaxValues[i] + value;

                LowerBound = Math.Min(Bound1,Bound2);
                UpperBound = Math.Max(Bound1,Bound2);
            }
        }

        private void ReclaimMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
        }

        #region 第一代初始化
        private List<Gene> ConvertLineToGene(int index)
        {
            var genome = new List<Gene>();
            for (int i = 0; i < GaPara.LineCount; i++)
            {
                if (index == 0)
                {
                    var line = GaPara.SegLine[i];
                    var dir = line.GetValue(out double value, out double startVal, out double endVal);
                    var valueWithIndex = value + GaPara.MaxValues[i];
                    Gene gene = new Gene(valueWithIndex, dir, GaPara.MinValues[i], GaPara.MaxValues[i], startVal, endVal);
                    genome.Add(gene);
                }
                else
                {
                    var line = GaPara.SegLine[i];
                    var dir = line.GetValue(out double value, out double startVal, out double endVal);

                    var valueWithIndex = value + (GaPara.MaxValues[i] - GaPara.MinValues[i]) / FirstPopulationSize * index + GaPara.MinValues[i];
                    Gene gene = new Gene(valueWithIndex, dir, GaPara.MinValues[i], GaPara.MaxValues[i], startVal, endVal);
                    genome.Add(gene);
                }
            }
            return genome;
        }
        private List<Gene> ConvertLineToGene()//仅根据分割线生成第一代
        {
            var genome = new List<Gene>();
            for (int i = 0; i < GaPara.LineCount; i++)
            {
                var line = GaPara.SegLine[i];
                var dir = line.GetValue(out double value, out double startVal, out double endVal);
                var valueWithIndex = value;
                Gene gene = new Gene(valueWithIndex, dir, GaPara.MinValues[i], GaPara.MaxValues[i], startVal, endVal);
                genome.Add(gene);
            }
            return genome;
        }

        private bool RandomCreateChromosome(out Chromosome solution, int N = 20)
        {
            // Try N times
            solution = new Chromosome();
            for (int j = 0; j < N; j++)
            {
                var genome = new List<Gene>();
                for (int i = 0; i < GaPara.LineCount; i++)
                {
                    var line = GaPara.SegLine[i];
                    var dir = line.GetValue(out double value, out double startVal, out double endVal);
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
                    Gene gene = new Gene(RandValue, dir, GaPara.MinValues[i], GaPara.MaxValues[i], startVal, endVal);
                    genome.Add(gene);
                }
                solution.Genome = genome;
                if (solution.IsVaild(LayoutPara, ParameterViewModel))
                {
                    return true;
                }
                ReclaimMemory();
            }
            return false;
        }
        public List<Chromosome> CreateFirstPopulation()
        {
            List<Chromosome> solutions = new List<Chromosome>();
            // 添加初始画的分割线,该解必须是合理解
            var orgSolution = new Chromosome();
            orgSolution.Logger = this.Logger;
            var orgGenome = ConvertLineToGene();//创建初始基因序列
            orgSolution.Genome = orgGenome;
            //Draw.DrawSeg(solution);
            solutions.Add(orgSolution);
            if (InitGenomes != null)
            {
                // 有额外输入的基因，判断是否为合理解，然后添加
                foreach(var initgenome in InitGenomes)
                {
                    // 如果为合理解则添加
                    if (initgenome.IsVaild(LayoutPara, ParameterViewModel)) solutions.Add(initgenome.Clone());
                }
            }
            var RndFlag = RandomCreateChromosome(out Chromosome Rsolution,200);//尝试200次看看有没有合理解
            if(RndFlag) solutions.Add(Rsolution);//找到了合理解
            while (solutions.Count < FirstPopulationSize)
            {
                // 随机生成 其余的解
                var FoundVaild = false;
                if (RndFlag)//之前找到合理解
                {
                    FoundVaild = RandomCreateChromosome(out Chromosome solution);//尝试找一下
                    if(FoundVaild) solutions.Add(solution);
                }
                if(!FoundVaild)//没有合理解
                {
                    // 没找到则在之前解随机挑选一个
                    var idx = RandInt(solutions.Count);
                    solutions.Add(solutions[idx].Clone());
                }
            }
            return solutions;
        }


        #endregion
        #region run代码部分
        public List<Chromosome> Run(List<Chromosome> histories, bool recordprevious)
        {
            Logger?.Information($"\n");
            Logger?.Information($"迭代次数: {IterationCount}");
            Logger?.Information($"种群数量: {PopulationSize}");
            Logger?.Information($"最大迭代时间: {MaxTime} 分");

            List<Chromosome> selected = new List<Chromosome>();
            try
            {
                var pop = CreateFirstPopulation();//创建第一代

                var strFirstPopCnt = $"第一代种群数量: {pop.Count}\n";
                Active.Editor.WriteMessage(strFirstPopCnt);
                Logger?.Information(strFirstPopCnt);
                var curIteration = 0;
                int maxCount = 0;
                int maxNums = 0;
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                while (curIteration++ < IterationCount && maxCount < MaxCount && stopWatch.Elapsed.TotalMinutes < MaxTime)
                {
                    var strCurIterIndex = $"迭代次数：{curIteration}";
                    //Active.Editor.WriteMessage(strCurIterIndex);
                    Logger?.Information(strCurIterIndex);
                    System.Diagnostics.Debug.WriteLine(strCurIterIndex);
                    System.Diagnostics.Debug.WriteLine($"Total seconds: {stopWatch.Elapsed.TotalSeconds}");

                    selected = Selection(curIteration, pop, out int curNums);

                    if (recordprevious)
                    {
                        histories.Add(selected.First());
                    }
                    if (maxNums == curNums)
                    {
                        maxCount++;
                    }
                    else
                    {
                        maxCount = 0;
                        maxNums = curNums;
                    }
                    pop = CreateNextGeneration(selected);
                    Mutation(pop);
                }
                var strBest = $"最大车位数: {pop.First().ParkingStallCount}";
                Active.Editor.WriteMessage(strBest);
                Logger?.Information(strBest);
                var strTotalMins = $"运行总时间: {stopWatch.Elapsed.TotalMinutes} 分";
                stopWatch.Stop();

                Logger?.Information(strTotalMins);
                System.Diagnostics.Debug.WriteLine(strTotalMins);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine($"Total sub partitions: {Chromosome.CachedPartitionCnt.Count}");

                Chromosome.CachedPartitionCnt.Clear();
            }

            return selected;
        }
        private List<Chromosome> Selection(int iterationIndex, List<Chromosome> inputSolution, out int maxNums)
        {
            Logger?.Information("进行选择");
            //System.Diagnostics.Debug.WriteLine("进行选择");

            int index = 0;
            inputSolution.ForEach(s =>
            {
                s.GetMaximumNumber(LayoutPara, GaPara, ParameterViewModel);
                System.Diagnostics.Debug.WriteLine($"{iterationIndex}.{index++}: { s.ParkingStallCount}");
                ReclaimMemory();
            }
            );
            //inputSolution.ForEach(s => s.GetMaximumNumberFast(LayoutPara, GaPara));

            var sorted = inputSolution.OrderByDescending(s => s.ParkingStallCount).ToList();
            maxNums = sorted.First().ParkingStallCount;
            var strBestCnt = $"当前最大车位数： {sorted.First().ParkingStallCount}\n";
            Logger?.Information(strBestCnt);
            System.Diagnostics.Debug.WriteLine(strBestCnt);

            var rst = new List<Chromosome>();
            for (int i = 0; i < SelectionSize; ++i)
            {
                rst.Add(sorted[i]);
            }
            return rst;
        }
        private void Mutation(List<Chromosome> s)
        {
            int cnt = Math.Min((int)(s.Count * MutationRate), 1);//需要变异的染色体数目，最小为1
            int geneCnt = Math.Min((int)(s[0].GenomeCount() * GeneMutationRate), 1);//需要变异的基因数目，最小为1
            int index = 0;
            HashSet<int> selectedChromosome = new HashSet<int>();//被选中的染色体号
            HashSet<int> selectedGene = new HashSet<int>();//被选中的基因号
            while (index >= cnt)//挑选染色体
            {
                int num = RandInt(cnt);//生成随机号
                if (selectedChromosome.Contains(num) || num == 0)
                {
                    continue;//重新摇号
                }
                else
                {
                    selectedChromosome.Add(num);//直接添加
                    index++;
                }
            }
            index = 0;
            while (index >= geneCnt)//挑选基因号
            {
                int num = RandInt(geneCnt);//生成随机号
                if (selectedGene.Contains(num))
                {
                    continue;//重新摇号
                }
                else
                {
                    selectedGene.Add(num);//直接添加
                    index++;
                }
            }

            foreach (var i in selectedChromosome)
            {
                foreach (var j in selectedGene)
                {
                    var maxVal = s[i].Genome[j].MaxValue;
                    var minVal = s[i].Genome[j].MinValue;
                    var dist = Math.Min(maxVal - minVal, 15700);
                    s[i].Genome[j].Value = RandDouble() * dist + minVal;
                }
            }
        }
        private List<Chromosome> CreateNextGeneration(List<Chromosome> solutions)
        {
            List<Chromosome> rst = new List<Chromosome>();
            rst.Add(solutions.First());
            for (int i = 0; i < PopulationSize - 1; ++i)
            {
                int rd1 = RandInt(solutions.Count);
                int rd2 = RandInt(solutions.Count);
                var s = Crossover(solutions[rd1], solutions[rd2]);
                s.Logger = this.Logger;
                rst.Add(s);
            }

            return rst;
        }
        private Chromosome Crossover(Chromosome s1, Chromosome s2)
        {
            Chromosome newS = new Chromosome();
            var chromoLen = s1.Genome.Count;
            int[] covering_code = new int[chromoLen];
            for (int i = 0; i < chromoLen; ++i)
            {
                var cc = RandInt(2);
                if (cc == 0)
                {
                    newS.AddChromos(s1.Genome[i]);
                }
                else
                {
                    newS.AddChromos(s2.Genome[i]);
                }
            }

            return newS;
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
            if (UpperBound- LowerBound <= tol) return loc;

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
            if (UpperBound - LowerBound < tol) return LowerBound ;
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
                var SolutionLis = new List<double>() { LowerBound, UpperBound};
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
        public List<Chromosome> Run2(List<Chromosome> histories, bool recordprevious,bool specialOnly = false,bool MultiProcess = false)
        {
            Logger?.Information($"迭代次数: {IterationCount}");
            Logger?.Information($"种群数量: {PopulationSize}");
            Logger?.Information($"最大迭代时间: {MaxTime} 分");
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            SpecialOnly = specialOnly;
            List<Chromosome> selected = new List<Chromosome>();

            var pop = CreateFirstPopulation();
            var strFirstPopCnt = $"第一代种群数量: {pop.Count}\n";
            Active.Editor.WriteMessage(strFirstPopCnt);
            Logger?.Information(strFirstPopCnt);
            var curIteration = 0;
            int maxCount = 0;
            int maxNums = 0;

            int lamda; //变异方差，随代数递减

            while (curIteration++ < IterationCount && maxCount < MaxCount && stopWatch.Elapsed.TotalMinutes < MaxTime)
            {
                var strCurIterIndex = $"迭代次数：{curIteration}";
                //Active.Editor.WriteMessage(strCurIterIndex);
                Logger?.Information(strCurIterIndex);
                Logger?.Information($"Total seconds: {stopWatch.Elapsed.TotalSeconds}");
                System.Diagnostics.Debug.WriteLine(strCurIterIndex);
                System.Diagnostics.Debug.WriteLine($"Total seconds: {stopWatch.Elapsed.TotalSeconds}");
                selected = Selection2(pop, out int CurNums, MultiProcess);
                if (recordprevious)
                {
                    histories.Add(selected.First());
                }
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
                lamda = curIteration + 3;// 小变异系数，随时间推移，变异缩小，从4 开始
                MutationS(pop, lamda);
                // 大变异
                var rstLM = temp_list[1];
                MutationL(rstLM);
                pop.AddRange(rstLM);
            }
            //string strBest;
            //if (!BreakFlag) strBest = $"最大车位数: {maxNums}";
            //else
            //{
            //    if(InitGenomes != null)
            //    {
            //        strBest = $"打断前最大车位数: {maxNums}";
            //    }
            //    else
            //    {
            //        strBest = $"打断后最大车位数: {maxNums}";
            //    }
            //}
            //Active.Editor.WriteMessage(strBest);
            //Logger?.Information(strBest);
            string strConverged;
            if (maxCount < MaxCount) strConverged = $"未收敛";
            else strConverged = $"已收敛";
            Active.Editor.WriteMessage(strConverged);
            Logger?.Information(strConverged);
            stopWatch.Stop();
            var strTotalMins = $"迭代时间: {stopWatch.Elapsed.TotalMinutes} 分";
            Logger?.Information(strTotalMins);
            // 返回最后一代选择的比例
            return selected.Take(SelectionSize).ToList();
        }
        private List<Chromosome> Selection2(List<Chromosome> inputSolution, out int maxNums,bool MultiProcess)
        {
            Logger?.Information("进行选择");
            if (MultiProcess) CalculateParkingSpaces(inputSolution);
            else
            {
                inputSolution.ForEach(s =>
                {
                    s.GetMaximumNumber(LayoutPara, GaPara, ParameterViewModel);
                    ReclaimMemory();
                });
            }
            //inputSolution.ForEach(s => s.GetMaximumNumberFast(LayoutPara, GaPara));
            var sorted = inputSolution.OrderByDescending(s => s.ParkingStallCount).ToList();
            maxNums = sorted.First().ParkingStallCount;
            //var strBestCnt = $"当前最大车位数： {sorted.First().Count}\n";
            //Logger?.Information(strBestCnt);
            var strCnt = $"当前车位数：";
            for (int k = 0; k < sorted.Count; ++k)
            {
                strCnt += sorted[k].ParkingStallCount.ToString();
                strCnt += " ";
            }
            strCnt += "\n";
            Logger?.Information(strCnt);
            System.Diagnostics.Debug.WriteLine(strCnt);
            var rst = new List<Chromosome>();
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

        private void CalculateParkingSpaces(List<Chromosome> inputSolution)
        {
            var chromosomeCollection = inputSolution.GetChromosomeCollection();
            for(int i = 0; i < inputSolution.Count;i++)
            {
                var chrom = chromosomeCollection.Chromosomes[i];
                var subAreas = InterParameter.GetSubAreas(chrom);
                List<MParkingPartitionPro> mParkingPartitionPros = new List<MParkingPartitionPro>();
                MParkingPartitionPro mParkingPartition=new MParkingPartitionPro();
                inputSolution[i].ParkingStallCount = CalculateTheTotalNumOfParkingSpace(subAreas, ref mParkingPartitionPros,ref mParkingPartition);
            }
        }
        private List<List<Chromosome>> CreateNextGeneration2(List<Chromosome> solutions)
        {
            List<Chromosome> rstSM = new List<Chromosome>();
            List<Chromosome> rstLM = new List<Chromosome>();
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
                    s.Logger = this.Logger;
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
                        return new List<List<Chromosome>> { rstSM, rstLM };
                    }
                }
            }
        }
        private void MutationL(List<Chromosome> s)
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
        private void MutationS(List<Chromosome> s, int lamda)
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
