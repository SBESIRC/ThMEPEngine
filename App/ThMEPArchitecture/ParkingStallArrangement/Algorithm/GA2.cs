using System;
using AcHelper;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using Autodesk.AutoCAD.Geometry;
using System.IO;
using ThCADExtension;
using Serilog;
using System.Diagnostics;

namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    public class GA2 : IDisposable
    {
        Random Rand = new Random();

        //Genetic Algorithm parameters
        int MaxTime;
        int IterationCount = 1;
        int PopulationSize;

        int FirstPopulationSize;
        double SelectionRate;
        int FirstPopulationSizeMultiplyFactor = 1;
        int SelectionSize = 1;

        int ChromoLen = 1;
        double CrossRate;
        double MutationRate;
        double GeneMutationRate;

        //Inputs
        GaParameter GaPara;
        LayoutParameter LayoutPara;

        //Range
        double Low, High;

        //log file name
        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "GaLog.txt");

        Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
            .File(LogFileName, rollingInterval: RollingInterval.Hour).CreateLogger();

        public GA2(GaParameter gaPara, LayoutParameter layoutPara, int popSize = 10, int iterationCnt = 10)
        {
            IterationCount = iterationCnt;
            Rand = new Random(DateTime.Now.Millisecond);//随机数
            PopulationSize = popSize;//种群数量
            FirstPopulationSizeMultiplyFactor = 1;
            FirstPopulationSize = PopulationSize * FirstPopulationSizeMultiplyFactor;
            MaxTime = 300;
            CrossRate = 0.8;//交叉因子
            MutationRate = 0.2;//变异因子
            GeneMutationRate = 0.3;//基因变异因子

            SelectionRate = 0.6;//保留因子
            SelectionSize = Math.Max(1, (int)(SelectionRate * popSize));

            //InputsF
            GaPara = gaPara;
            LayoutPara = layoutPara;
        }

        private List<Gene> ConvertLineToGene(int index)
        {
            var genome = new List<Gene>();
            for (int i = 0; i < GaPara.LineCount; i++)
            {
                if (index == 0)
                {
                    var line = GaPara.SegLine[i];
                    var dir = line.GetValue(out double value, out double startVal, out double endVal);
                    var valueWithIndex = value + GaPara.MaxValues[i] - 2750;
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
                var valueWithIndex = value + GaPara.MaxValues[i] - 2750; 
                Gene gene = new Gene(valueWithIndex, dir, GaPara.MinValues[i], GaPara.MaxValues[i], startVal, endVal);
                genome.Add(gene);
            }
            return genome;
        }

        public List<Chromosome> Run(List<Chromosome> histories)
        {
            Logger.Information($"Iteration count: {IterationCount}");
            Logger.Information($"Population count: {PopulationSize}");
            Logger.Information($"Max minutes: {MaxTime}");

            List<Chromosome> selected = new List<Chromosome>();
            try
            {
                var pop = CreateFirstPopulation();//创建第一代
                if (IterationCount == 1)
                {
                    return pop;
                }

                var strFirstPopCnt = $"\n  First poplulation size: {pop.Count}";
                Active.Editor.WriteMessage(strFirstPopCnt);
                Logger.Information(strFirstPopCnt);
                var curIteration = 0;
                int maxCount = 0;
                int maxNums = 0;

                var stopWatch = new Stopwatch();
                stopWatch.Start();
                while (curIteration++ < IterationCount && maxCount < 20 && stopWatch.Elapsed.Minutes < MaxTime)
                {
                    var strCurIterIndex = $"\n iteration index：     {curIteration}";
                    //Active.Editor.WriteMessage(strCurIterIndex);
                    Logger.Information(strCurIterIndex);
                    selected = Selection(pop, out int curNums);
                    histories.Add(selected.First());
                    if (maxNums == curNums)
                    {
                        maxCount++;
                    }
                    else
                    {
                        maxNums = curNums;
                    }
                    pop = CreateNextGeneration(selected);
                    //Mutation(pop);

                    stopWatch.Stop();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return selected;
        }

        public void Mutation(List<Chromosome> s)
        {
            //变异代码，有待完善
            int cnt = Math.Min((int)(s.Count * MutationRate), 1);//需要变异的染色体数目，最小为1
            int geneCnt = Math.Min((int)(s[0].GenomeCount() * GeneMutationRate), 1);//需要变异的基因数目，最小为1
            int index = 0;
            HashSet<int> selectedChromosome = new HashSet<int>();//被选中的染色体号
            HashSet<int> selectedGene = new HashSet<int>();//被选中的基因号
            while (index >= cnt)//挑选染色体
            {
                int num = RandInt(cnt);//生成随机号
                if (selectedChromosome.Contains(num))
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
                    s[i].Genome[j].Value = Rand.NextDouble() * (maxVal - minVal) + minVal;
                }
            }
        }

        private int RandInt(int range)
        {
            var guid = Guid.NewGuid();
            var rand = new Random(guid.GetHashCode());
            int i = rand.Next(range);
            return i;
        }

        public List<Chromosome> CreateFirstPopulation()
        {
            List<Chromosome> solutions = new List<Chromosome>();

            var solution = new Chromosome();
            solution.Logger = this.Logger;
            var genome = ConvertLineToGene();//创建初始基因序列
            solution.Genome = genome;
            //Draw.DrawSeg(solution);
            solutions.Add(solution);

            return solutions;
        }

        public List<Chromosome> CreateNextGeneration(List<Chromosome> solutions)
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

        public Chromosome Crossover(Chromosome s1, Chromosome s2)
        {
            Chromosome newS = new Chromosome();
            var chromoLen = s1.Genome.Count;
            int[] covering_code = new int[chromoLen];
            for (int i = 0; i < chromoLen; ++i)
            {
                var cc = RandInt(2);//rand.Next(0, 2);
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

        public List<Chromosome> Selection(List<Chromosome> inputSolution, out int maxNums)
        {
            Logger.Information("Doing Selection");

            inputSolution.ForEach(s => s.GetMaximumNumber(LayoutPara, GaPara));

            var sorted = inputSolution.OrderByDescending(s => s.Count).ToList();
            maxNums = sorted.First().Count;
            var strBestCnt = $"\n Current best： {sorted.First().Count}";
            //Active.Editor.WriteMessage(strBestCnt);
            Logger.Information(strBestCnt);
            var rst = new List<Chromosome>();
            for (int i = 0; i < SelectionSize; ++i)
            {
                rst.Add(sorted[i]);
            }
            return rst;
        }

        public void Dispose()
        {

        }
    }
}
