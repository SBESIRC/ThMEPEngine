﻿using System;
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
using System.Threading;

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
        public int GetMaximumNumber(LayoutParameter layoutPara, GaParameter gaPara)
        {
            layoutPara.Set(Genome);
            int result = GetParkingNums(layoutPara);
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

        
        private int GetParkingNums(LayoutParameter layoutPara)
        {
            int count = 0;
            for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
            {
                PartitionV3 partition = new PartitionV3();
                if (ConvertParametersToCalculateCarSpots(layoutPara, j, ref partition, Logger))
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

    public class GA : IDisposable
    {
        Random Rand = new Random();

        //Genetic Algorithm parameters
        readonly int MaxTime;
        readonly int IterationCount = 10;
        int PopulationSize;

        int FirstPopulationSize;
        double SelectionRate;
        int FirstPopulationSizeMultiplyFactor = 2;
        int SelectionSize = 6;

        double MutationRate;
        double GeneMutationRate;

        //Inputs
        GaParameter GaPara;
        LayoutParameter LayoutPara;

        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "GaLog.txt");

        public Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
            .File(LogFileName, flushToDiskInterval:new TimeSpan(0,0,5), rollingInterval: RollingInterval.Hour).CreateLogger();

        public GA(GaParameter gaPara, LayoutParameter layoutPara, int popSize = 10, int iterationCnt = 10)
        {
            IterationCount = iterationCnt;
            Rand = new Random(DateTime.Now.Millisecond);//随机数
            PopulationSize = popSize;//种群数量
            FirstPopulationSizeMultiplyFactor = 2;
            FirstPopulationSize = PopulationSize * FirstPopulationSizeMultiplyFactor;
            MaxTime = 180;
            MutationRate = 0.5;//变异因子
            GeneMutationRate = 0.5;//基因变异因子

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
                while (curIteration++ < IterationCount && maxCount < 5 && stopWatch.Elapsed.TotalMinutes < MaxTime)
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

        private int RandInt(int range)
        {
            return General.Utils.RandInt(range);
        }
        private double RandDouble()
        {
            return General.Utils.RandDouble();
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

        private List<Chromosome> Selection(int iterationIndex, List<Chromosome> inputSolution, out int maxNums)
        {
            Logger?.Information("进行选择");
            //System.Diagnostics.Debug.WriteLine("进行选择");

            int index = 0;
            inputSolution.ForEach(s =>
                {
                    s.GetMaximumNumber(LayoutPara, GaPara);
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

        public void Dispose()
        {

        }
    }
}
