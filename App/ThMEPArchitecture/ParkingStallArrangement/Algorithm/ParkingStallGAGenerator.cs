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
using ThCADCore.NTS;
using ThMEPArchitecture.PartitionLayout;
using Dreambuild.AutoCAD;
using static ThMEPArchitecture.ParkingStallArrangement.ParameterConvert;
using System.Text.RegularExpressions;
using ThMEPArchitecture.ViewModel;

namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    public class Gene : IEquatable<Gene>
    {
        public double Value { get; set; }//线的值
        public bool VerticalDirection { get; set; }//点的方向，true是y方向
        public double MinValue { get; set; }//线的最小值
        public double MaxValue { get; set; }//线的最大值
        public double StartValue { get; set; }//线的起始点另一维
        public double EndValue { get; set; }//线的终止点另一维
        public Gene(double value, bool direction, double minValue, double maxValue, double startValue, double endValue)
        {
            Value = value;
            VerticalDirection = direction;
            MinValue = minValue;//绝对的最小值
            MaxValue = maxValue;//绝对的最大值
            StartValue = startValue;
            EndValue = endValue;
        }
        public Gene Clone()
        {
            var gene = new Gene(Value, VerticalDirection, MinValue, MaxValue, StartValue, EndValue);
            return gene;
        }
        public Gene()
        {
            Value = 0;
            VerticalDirection = false;
            MinValue = 0;
            MaxValue = 0;
            StartValue = 0;
            EndValue = 0;
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ VerticalDirection.GetHashCode();
        }
        public bool Equals(Gene other)
        {
            return Value.Equals(other.Value) && VerticalDirection.Equals(other.VerticalDirection);
        }

        public Line ToLine()
        {
            Point3d spt, ept;
            if (VerticalDirection)
            {
                spt = new Point3d(Value, StartValue, 0);
                ept = new Point3d(Value, EndValue, 0);
            }
            else
            {
                spt = new Point3d(StartValue, Value, 0);
                ept = new Point3d(EndValue, Value, 0);
            }
            return new Line(spt, ept);
        }
    }

    public class Chromosome
    {
        //Group of genes
        public List<Gene> Genome = new List<Gene>();

        public Serilog.Core.Logger Logger = null;

        public int Count { get; set; }

        static private Dictionary<PartitionBoundary,int> _cachedPartitionCnt = new Dictionary<PartitionBoundary, int>();

        static public Dictionary<PartitionBoundary,int> CachedPartitionCnt
        {
            get { return _cachedPartitionCnt; }
            set { _cachedPartitionCnt = value; }
        }
        public Chromosome Clone()
        {
            var clone = new Chromosome();
            clone.Logger = Logger;
            clone.Genome = new List<Gene>();
            clone.Count = Count;

            foreach (var gene in Genome)
            {
                clone.Genome.Add(gene.Clone());
            }
            return clone;
        }
        public int GenomeCount()
        {
            return Genome.Count;
        }

        public int GetMaximumNumber_(LayoutParameter layoutPara, GaParameter gaPara)
        {
            layoutPara.Set(Genome);

            Random rand = new Random();
            int rst = rand.Next(200);
            Count = rst;
            return rst;
        }

        //Fitness method
        public int GetMaximumNumber(LayoutParameter layoutPara, GaParameter gaPara, ParkingStallArrangementViewModel parameterViewModel)
        {
            layoutPara.Set(Genome);
            int result = GetParkingNums(layoutPara, parameterViewModel);
            //Thread.Sleep(3);
            //int result = Convert.ToInt32(Regex.Match(Guid.NewGuid().ToString(), @"\d+").Value);
            Count = result;
            //System.Diagnostics.Debug.WriteLine(Count);

            return result;
        }

        public void Clear()
        {
            Genome.Clear();
        }

        public int GetMaximumNumberFast(LayoutParameter layoutPara, GaParameter gaPara)
        {
            layoutPara.Set(Genome);
            int result = GetParkingNumsFast(layoutPara);
            Count = result;
            return result;
        }

        private int GetParkingNumsFast(LayoutParameter layoutPara)
        {
            int count = 0;
            for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
            {
                int index = layoutPara.AreaNumber[j];
                layoutPara.Id2AllSegLineDic.TryGetValue(index, out List<Line> lanes);
                layoutPara.Id2AllSubAreaDic.TryGetValue(index, out Polyline boundary);
                layoutPara.SubAreaId2ShearWallsDic.TryGetValue(index, out List<List<Polyline>> obstaclesList);
                layoutPara.BuildingBoxes.TryGetValue(index, out List<Polyline> buildingBoxes);
                layoutPara.SubAreaId2OuterWallsDic.TryGetValue(index, out List<Polyline> walls);
                layoutPara.SubAreaId2SegsDic.TryGetValue(index, out List<Line> inilanes);
                var obstacles = new List<Polyline>();
                obstaclesList.ForEach(e => obstacles.AddRange(e));
                var bound = GeoUtilities.JoinCurves(walls, inilanes)[0];
                PartitionFast partition = new PartitionFast(walls, inilanes, obstacles, bound, buildingBoxes);
                try
                {
                    count += partition.CalCarSpotsFastly();
                }
                catch (Exception ex)
                {
                    ;
                }
            }
            return count;
        }

        
        private int GetParkingNums(LayoutParameter layoutPara, ParkingStallArrangementViewModel ParameterViewModel)
        {
            int count = 0;
            for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
            {
                ParkingPartition partition = new ParkingPartition();
                if (ConvertParametersToCalculateCarSpots(layoutPara, j, ref partition, ParameterViewModel, Logger))
                {
                    try
                    {
                        var partitionBoundary = new PartitionBoundary(partition.Boundary.Vertices());
                        if (CachedPartitionCnt.ContainsKey(partitionBoundary))
                        {
                            count += CachedPartitionCnt[partitionBoundary];
                        }
                        else
                        {
                            var subCnt = partition.CalNumOfParkingSpaces();
                            CachedPartitionCnt.Add(partitionBoundary, subCnt);
                            System.Diagnostics.Debug.WriteLine($"Sub area count: {CachedPartitionCnt.Count}");
                            count += subCnt;
                        }
                    }
                    catch (Exception ex)
                    {
                        //partition.Dispose();
                        Logger.Error(ex.Message);
                    }
                    finally
                    {
                       // partition.Dispose();
                    }
                }
            }
            return count;
        }

        public void AddChromos(Gene c)
        {
            Genome.Add(c);
        }
    }

    public class ParkingStallGAGenerator : IDisposable
    {
        Random Rand = new Random();

        //Genetic Algorithm parameters
        readonly int MaxTime;
        readonly int IterationCount = 10;
        int PopulationSize;

        int FirstPopulationSize;
        double SelectionRate;
        int FirstPopulationSizeMultiplyFactor = 2;
        int SelectionSize;
        int MaxCount = 10;//出现相同车位数的最大次数
        double MutationRate;
        double GeneMutationRate;

        int Elite_popsize;
        int Max_SelectionSize;
        double EliminateRate;
        double MutationUpperBound;
        double GoldenRatio;
        private Dictionary<int, Tuple<double, double>> LowerUpperBound;
        //Inputs
        GaParameter GaPara;
        LayoutParameter LayoutPara;
        ParkingStallArrangementViewModel ParameterViewModel;

        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "GaLog.txt");

        public Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
            .File(LogFileName, flushToDiskInterval:new TimeSpan(0,0,5), rollingInterval: RollingInterval.Hour).CreateLogger();

        public ParkingStallGAGenerator(GaParameter gaPara, LayoutParameter layoutPara, ParkingStallArrangementViewModel parameterViewModel)
        {
            //大部分参数采取黄金分割比例，保持选择与变异过程中种群与基因相对稳定
            GoldenRatio = (Math.Sqrt(5) - 1) / 2;//0.618
            IterationCount = parameterViewModel.IterationCount;
            Rand = new Random(DateTime.Now.Millisecond);//随机数
            PopulationSize = parameterViewModel.PopulationCount;//种群数量
            FirstPopulationSizeMultiplyFactor = 2;
            FirstPopulationSize = PopulationSize * FirstPopulationSizeMultiplyFactor;
            MaxTime = 180;
            MutationRate = 1 - GoldenRatio;//变异因子,0.382
            GeneMutationRate = 1- GoldenRatio;//基因变异因子0.382,保持迭代过程中变异基因的比例

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
            MutationUpperBound = 15700.0;// 最大变异范围，两排车道宽
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
            // get absolute coordinate of segline
            var line = GaPara.SegLine[i];
            var dir = line.GetValue(out double value, out double startVal, out double endVal);
            LowerBound = GaPara.MinValues[i] + value;
            UpperBound = GaPara.MaxValues[i] + value;
        }
        #region
        //第一代初始化
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
        private List<Chromosome> CreateFirstPopulation(bool accordingSegline)
        {
            List<Chromosome> solutions = new List<Chromosome>();
            if (accordingSegline)
            {
                var solution = new Chromosome();
                solution.Logger = this.Logger;
                var genome = ConvertLineToGene();//创建初始基因序列
                solution.Genome = genome;
                //Draw.DrawSeg(solution);
                solutions.Add(solution);
            }
            else
            {
                for (int i = 0; i < FirstPopulationSize; ++i)//
                {
                    var solution = new Chromosome();
                    solution.Logger = this.Logger;
                    var genome = ConvertLineToGene(i);//创建初始基因序列
                    solution.Genome = genome;
                    //Draw.DrawSeg(solution);
                    solutions.Add(solution);
                }
            }
            return solutions;
        }
        #endregion
        #region
        // run代码部分
        public List<Chromosome> Run(List<Chromosome> histories, bool recordprevious)
        {
            Logger?.Information($"迭代次数: {IterationCount}");
            Logger?.Information($"种群数量: {PopulationSize}");
            Logger?.Information($"最大迭代时间: {MaxTime} 分");

            List<Chromosome> selected = new List<Chromosome>();
            try
            {
                var pop = CreateFirstPopulation(IterationCount == 1);//创建第一代
                if (IterationCount == 1)
                {
                    return pop;
                }

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
                var strBest = $"最大车位数: {pop.First().Count}";
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
                System.Diagnostics.Debug.WriteLine($"{iterationIndex}.{index++}: { s.Count}");
            }
            );
            //inputSolution.ForEach(s => s.GetMaximumNumberFast(LayoutPara, GaPara));

            var sorted = inputSolution.OrderByDescending(s => s.Count).ToList();
            maxNums = sorted.First().Count;
            var strBestCnt = $"当前最大车位数： {sorted.First().Count}\n";
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
        #region
        //随机函数
        private List<int> RandChoice(int UpperBound, int n = -1, int LowerBound = 0)
        {
            return General.Utils.RandChoice(UpperBound, n, LowerBound);
        }
        private double RandNormalInRange(double loc, double scale, double LowerBound, double UpperBound, int MaxIter = 1000)
        {
            return General.Utils.RandNormalInRange(loc, scale, LowerBound, UpperBound, MaxIter);
        }
        private int RandInt(int range)
        {
            return General.Utils.RandInt(range);
        }
        private double RandDouble()
        {
            return General.Utils.RandDouble();
        }
        #endregion
        #region
        // run2代码部分
        // 选择逻辑增强，除了选择一部分优秀解之外，对其余解随即保留
        // 后代生成逻辑增强，保留之前最优解直接保留，不做变异的逻辑。新增精英种群逻辑，保留精英种群，并且参与小变异。
        // 变异逻辑增强，增加小变异（用于局部最优化搜索），保留之前的变异逻辑（目前称之为大变异）。
        // 对精英种群和一部分交叉产生的后代使用小变异，对一部分后代使用大变异，对剩下的后代不做变异。
        public List<Chromosome> Run2(List<Chromosome> histories, bool recordprevious)
        {
            Logger?.Information($"迭代次数: {IterationCount}");
            Logger?.Information($"种群数量: {PopulationSize}");
            Logger?.Information($"最大迭代时间: {MaxTime} 分");

            List<Chromosome> selected = new List<Chromosome>();

            var pop = CreateFirstPopulation(IterationCount == 1);//创建第一代
            if (IterationCount == 1)
            {
                return pop;
            }
            var strFirstPopCnt = $"第一代种群数量: {pop.Count}\n";
            Active.Editor.WriteMessage(strFirstPopCnt);
            Logger?.Information(strFirstPopCnt);
            var curIteration = 0;
            int maxCount = 0;
            int maxNums = 0;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            int lamda;

            while (curIteration++ < IterationCount && maxCount < MaxCount && stopWatch.Elapsed.TotalMinutes < MaxTime)
            {
                var strCurIterIndex = $"迭代次数：{curIteration}";
                //Active.Editor.WriteMessage(strCurIterIndex);
                Logger?.Information(strCurIterIndex);
                Logger?.Information($"Total seconds: {stopWatch.Elapsed.TotalSeconds}");
                System.Diagnostics.Debug.WriteLine(strCurIterIndex);
                System.Diagnostics.Debug.WriteLine($"Total seconds: {stopWatch.Elapsed.TotalSeconds}");
                selected = Selection2(pop, out int CurNums);
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
            var strBest = $"最大车位数: {maxNums}";
            Active.Editor.WriteMessage(strBest);
            Logger?.Information(strBest);
            stopWatch.Stop();
            var strTotalMins = $"运行总时间: {stopWatch.Elapsed.TotalMinutes} 分";
            Logger?.Information(strTotalMins);
            return selected;
        }
        private List<Chromosome> Selection2(List<Chromosome> inputSolution, out int maxNums)
        {
            Logger?.Information("进行选择");
            inputSolution.ForEach(s => s.GetMaximumNumber(LayoutPara, GaPara, ParameterViewModel));
            //inputSolution.ForEach(s => s.GetMaximumNumberFast(LayoutPara, GaPara));
            var sorted = inputSolution.OrderByDescending(s => s.Count).ToList();
            maxNums = sorted.First().Count;
            //var strBestCnt = $"当前最大车位数： {sorted.First().Count}\n";
            //Logger?.Information(strBestCnt);
            var strCnt = $"当前车位数：";
            for (int k = 0; k < sorted.Count; ++k)
            {
                strCnt += sorted[k].Count.ToString();
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
                    s[i].Genome[j].Value = RandDouble() * dist + minVal;
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

                    //if (maxVal - minVal > MutationUpperBound)
                    //{
                    //    maxVal = minVal + MutationUpperBound;
                    //}
                    var loc = s[i].Genome[j].Value;

                    var std = (maxVal - minVal) / lamda;//2sigma 原则，从mean到边界概率为95.45%

                    s[i].Genome[j].Value = RandNormalInRange(loc, std, minVal, maxVal);

                }
            }
        }
        #endregion
        public void Dispose()
        {

        }
    }
}
