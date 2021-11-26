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
            Value = value;
            Direction = direction;
            MinValue = minValue;
            MaxValue = maxValue;
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

        //Fitness method
        public int GetMaximumNumber(LayoutParameter layoutPara, GaParameter gaPara)
        {
            layoutPara.Set(Genome);
            int result = GetParkingNums(layoutPara);
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

                //log
                List<Polyline> pls = new List<Polyline>() { boundary };
                string w = "";
                string l = "";
                foreach (var e in pls)
                {
                    foreach (var pt in e.Vertices().Cast<Point3d>().ToList())
                        w += pt.X.ToString() + "," + pt.Y.ToString() + ",";
                }
                foreach (var e in lanes)
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


                ParkingPartition p = new ParkingPartition(new List<Polyline>(), lanes, obstacles, boundary);
                bool valid = p.Validate();
                if (valid)
                {
                    //p.Log();
                    p.Initialize();
                    count += p.CalNumOfParkingSpaces();
                }
            }
            return count;
        }

        public void AddChromos(Gene c)
        {
            Genome.Add(c);
        }
    }

    public class GA
    {
        Random Rand = new Random();

        //Genetic Algorithm parameters
        int MaxTime;
        int PopulationSize;
        int SelectionSize = 6;
        int ChromoLen = 2;
        double CrossRate;
        double MutationRate;

        //Inputs
        GaParameter GaPara;
        LayoutParameter LayoutPara;

        //Range
        double Low, High;

        public GA(GaParameter gaPara, LayoutParameter layoutPara, int popSize = 10)
        {
            Rand = new Random(DateTime.Now.Millisecond);//随机数
            PopulationSize = popSize;//种群数量
            MaxTime = 100;//遗传代数
            CrossRate = 0.8;//交叉因子
            MutationRate = 0.2;//变异因子

            //Inputs
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

            Active.Editor.WriteMessage($"init pop cnt {pop.Count}");
            var cnt = 1;

            while (cnt-- > 0)
            {
                //Active.Editor.WriteMessage($"iteration cnt： {cnt}");
                selected = Selection(pop);
                pop = CreateNextGeneration(selected);
                //Mutation(pop);
            }

            return selected;
        }

        public void Mutation(List<Chromosome> s)
        {
            //变异代码，有待完善
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

            for (int i = 0; i < PopulationSize; ++i)
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

        public List<Chromosome> Selection(List<Chromosome> inputSolution)
        {
            var sorted = inputSolution.OrderBy(s => s.GetMaximumNumber(LayoutPara, GaPara)).ToList();
            var rst = new List<Chromosome>();
            for (int i = 0; i < SelectionSize; ++i)
            {
                rst.Add(sorted[i]);
            }
            return rst;
        }
    }
}
