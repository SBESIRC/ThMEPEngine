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
    public class Gene : IEquatable<Gene>
    {
        public double Value { get; set; }//线的值
        public bool Direction { get; set; }//点的方向，true是y方向
        public double MinValue { get; set; }//线的最小值
        public double MaxValue { get; set; }//线的最大值
        public double StartValue { get; set; }//线的起始点另一维
        public double EndValue { get; set; }//线的终止点另一维
        public Gene(double value, bool direction, double minValue, double maxValue, double startValue, double endValue)
        {
            double diswidthlane = 5500;
            Value = value;
            Direction = direction;
            MinValue = minValue /*+ diswidthlane / 2*/;
            MaxValue = maxValue /*- diswidthlane / 2*/;
            StartValue = startValue;
            EndValue = endValue;
        }

        public Gene()
        {
            Value = 0;
            Direction = false;
            MinValue = 0;
            MaxValue = 0;
            StartValue = 0;
            EndValue = 0;
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ Direction.GetHashCode();
        }
        public bool Equals(Gene other)
        {
            return Value.Equals(other.Value) && Direction.Equals(other.Direction);
        }

        public Line ToLine()
        {
            Point3d spt, ept;
            if(Direction)
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

        public int Count { get; set; }
        public int GenomeCount()
        {
            return Genome.Count;
        }
        //Fitness method
        public int GetMaximumNumber(LayoutParameter layoutPara, GaParameter gaPara)
        {
            layoutPara.Set(Genome);
            int result = GetParkingNums(layoutPara);
            Count = result;
            return result;
        }
        
        private int GetParkingNums(LayoutParameter layoutPara)
        {

            //这个函数是用于统计车位数，由余工完成
            //var guid = Guid.NewGuid();
            //var rand = new Random(guid.GetHashCode());
            //int num = rand.Next(10);
            //return num;

            int count = 0;
            for (int j = 0; j < layoutPara.AreaNumber.Count; j++)
            {
                int index = layoutPara.AreaNumber[j];
                layoutPara.SegLineDic.TryGetValue(index, out List<Line> lanes);
                layoutPara.AreaDic.TryGetValue(index, out Polyline boundary);
                layoutPara.ObstacleDic.TryGetValue(index, out List<Polyline> obstacles);
                layoutPara.AreaWalls.TryGetValue(index, out List<Polyline> walls);
                layoutPara.AreaSegs.TryGetValue(index, out List<Line> inilanes);

                //log
                List<Polyline> pls = walls;
                string w = "";
                string l = "";
                foreach (var e in pls)
                {
                    foreach (var pt in e.Vertices().Cast<Point3d>().ToList())
                        w += pt.X.ToString() + "," + pt.Y.ToString() + ",";
                }
                foreach (var e in inilanes)
                {
                    l += e.StartPoint.X.ToString() + "," + e.StartPoint.Y.ToString() + ","
                        + e.EndPoint.X.ToString() + "," + e.EndPoint.Y.ToString() + ",";
                }

                FileStream fs1 = new FileStream("D:\\GALog.txt", FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs1);
                sw.WriteLine(w);
                sw.WriteLine(l);
                sw.Close();
                fs1.Close();

                ParkingPartition p = new ParkingPartition(walls, inilanes, obstacles, boundary);
                //bool valid = p.Validate();
                if (true)
                {
                    //p.Log();
                    p.Initialize();

                    try
                    {
                        count += p.CalNumOfParkingSpaces();
                    }
                    catch(Exception ex)
                    {
                        ;
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
        int MaxTime;
        int IterationCount = 10;
        int PopulationSize;
        int SelectionSize = 6;
        int ChromoLen = 2;
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

        public GA(GaParameter gaPara, LayoutParameter layoutPara, int popSize = 10, int iterationCnt = 10)
        {
            IterationCount = iterationCnt;
            Rand = new Random(DateTime.Now.Millisecond);//随机数
            PopulationSize = popSize;//种群数量
            MaxTime = 300;
            CrossRate = 0.8;//交叉因子
            MutationRate = 0.2;//变异因子
            GeneMutationRate = 0.3;//基因变异因子
            //InputsF
            GaPara = gaPara;
            LayoutPara = layoutPara;
        }

        private List<Gene> ConvertLineToGene(int index)
        {
            var genome = new List<Gene>();
            for(int i = 0; i < GaPara.LineCount; i++)
            {
                var line = GaPara.SegLine[i]; 
                var dir = line.GetValue(out double value, out double startVal, out double endVal);
                var valueWithIndex = value + (GaPara.MaxValues[i] - GaPara.MinValues[i]) / PopulationSize * index + GaPara.MinValues[i];
                Gene gene = new Gene(valueWithIndex, dir, GaPara.MaxValues[i], GaPara.MinValues[i], startVal, endVal);
                genome.Add(gene);
            }
            return genome;
        }
        public List<Chromosome> Run()
        {
            List<Chromosome> selected = new List<Chromosome>();

            var pop = CreateFirstPopulation();//创建第一代
            var strFirstPopCnt = $"\n init pop cnt {pop.Count}";
            Active.Editor.WriteMessage(strFirstPopCnt);
            Logger.Information(strFirstPopCnt);
            var curIteration = 0;
            int maxCount = 0;
            int maxNums = 0;

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            while (curIteration++ < IterationCount && maxCount < 5 && stopWatch.Elapsed.Minutes < MaxTime)
            {
                var strCurIterIndex = $"\n iteration index：     {curIteration}";
                Active.Editor.WriteMessage(strCurIterIndex);
                Logger.Information(strCurIterIndex);
                selected = Selection(pop, out int curNums);
                pop = CreateNextGeneration(selected);
                if(maxNums == curNums)
                {
                    maxCount++;
                }
                else
                {
                    maxNums = curNums;
                }
                Mutation(pop);
            }
            stopWatch.Stop();
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

            foreach(var i in selectedChromosome)
            {
                foreach(var j in selectedGene)
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

            for (int i = 0; i < PopulationSize; ++i)//
            {
                var solution = new Chromosome();
                var genome = ConvertLineToGene(i);//创建初始基因序列
                solution.Genome = genome;
                //Draw.DrawSeg(solution);
                solutions.Add(solution);
            }

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
            var sorted = inputSolution.OrderByDescending(s => s.GetMaximumNumber(LayoutPara, GaPara)).ToList();
            maxNums = sorted.First().GetMaximumNumber(LayoutPara, GaPara);
            var strBestCnt = $"\n Current best： {sorted.First().Count}";
            Active.Editor.WriteMessage(strBestCnt);
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
