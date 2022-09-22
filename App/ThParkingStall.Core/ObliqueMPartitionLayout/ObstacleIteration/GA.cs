using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThParkingStall.Core.MPartitionLayout;


namespace ThParkingStall.Core.ObliqueMPartitionLayout.ObstacleIteration
{
    public class Gene
    {
        public Gene()
        {

        }
        public Gene(double[]values)
        {
            Values=values;
        }
        public Gene Clone()
        {
            return new Gene(Values);
        }
        public double[] Values { get; set; }
        public int ParkingCount = 0;
    }
    public class GA
    {
        public GA(List<LineString> walls, List<LineSegment> vaildLanes, List<Polygon> buildings,
             List<Polygon> buildingBoxes, Polygon region)
        {
            Walls = walls;
            VaildLanes = vaildLanes;
            Buildings = buildings;
            BuildingBoxes = buildingBoxes;
            Region = region;
            Count = buildingBoxes.Count;
        }
        public bool _RunGA { get; set; }
        private List<LineString> Walls { get; set; }
        private List<LineSegment> VaildLanes { get; set; }
        public List<Polygon> Buildings { get; set; }
        public List<Polygon> BuildingBoxes { get; set; }
        private Polygon Region { get; set; }

        public static int PopulationSizeAVG = 100;
        public static int PopulationSize = 10;
        public static int IterationCount = 10;
        public int SelectionSize = Math.Max(2, (int)(0.382 * PopulationSize));
        public int Max_SelectionSize = Math.Max(2, (int)(0.618 * PopulationSize));//最大保留数量0.618
        public double Max = 1000;
        public double Min = -1000;
        public int Count = 0;
        public int CurIteration = 0;
        public int Elite_popsize = Math.Max((int)(PopulationSize * 0.2), 1);//精英种群数量,种群数要大于3
        public int SMsize = Math.Max(1, (int)(0.618 * PopulationSize));//小变异比例
        public void Process()
        {
            Gene solution = new Gene();
            if (_RunGA)
            {
                solution= RunGA().First();
            }
            else
                solution = RunAvg().First();

            UpdateObstacles(GeneToVector(solution));
            //CalParkings calParkings = new CalParkings(solution,Walls,VaildLanes,Buildings,BuildingBoxes,Region);
            //var final = calParkings.Cal();
            //MessageBox.Show("最终车位数" + final);
        }
        List<Vector2D> GeneToVector(Gene gene)
        {
            List<Vector2D> vecs = new List<Vector2D>();
            for (int i = 0; i < gene.Values.Length / 2; i++)
            {
                var a = gene.Values[2 * i];
                var b = gene.Values[2 * i + 1];
                vecs.Add(new Vector2D(a, b));
            }
            return vecs;
        }
        void UpdateObstacles(List<Vector2D> Vecs)
        {

            var obstacles = new List<Polygon>();
            if (BuildingBoxes.Count == 1)
                obstacles = Buildings.Select(e => e.Clone().Translation(Vecs[0])).ToList();
            else
            {
                var obspacialIndex = new MNTSSpatialIndex(Buildings);
                for (int i = 0; i < BuildingBoxes.Count; i++)
                {
                    var crossed = obspacialIndex.SelectCrossingGeometry(BuildingBoxes[i]).Cast<Polygon>();
                    obstacles.AddRange(crossed.Select(e => e.Translation(Vecs[i])));
                    BuildingBoxes[i] = BuildingBoxes[i].Translation(Vecs[i]);
                }
            }
            Buildings=obstacles;
        }
        public List<Gene> RunAvg()
        {
            var pop = CreateAvgPopulation(Count);
            foreach (var solution in pop)
            {
                CalParkings calParkings = new CalParkings(solution, Walls, VaildLanes, Buildings.Select(e => e.Clone()).ToList(), BuildingBoxes.Select(e => e.Clone()).ToList(), Region);
                solution.ParkingCount = calParkings.Cal();
            }
            pop = pop.OrderByDescending(e => e.ParkingCount).ToList();

            var valStr = "";
            var nums = pop.Select(e => e.ParkingCount).OrderByDescending(e => e).ToList();
            nums.ForEach(e => valStr += e + ",");
            if (valStr.Length > 0) valStr.Remove(valStr.Length - 1);
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            FileStream fs = new FileStream(dir + "\\GAMonitor.txt", FileMode.Append);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine($"均质排布车位数{valStr}");
            sw.Close();
            fs.Close();

            return pop;
        }
        public List<Gene> RunGA()
        {
            List<Gene> selected = new List<Gene>();
            var pop = CreateFirstPopulation(Count);
            CurIteration = 0;
            int maxNums = 0;
            int lamda; //变异方差，随代数递减
            while (CurIteration++ < IterationCount)
            {
                selected = Selection(pop, out int CurNums);

                var st = selected.Select(e => e.ParkingCount).ToList();
                
                var valStr = "";
                var nums = pop.Select(e => e.ParkingCount).OrderByDescending(e => e).ToList();
                nums.ForEach(e => valStr += e + ",");
                if (valStr.Length > 0) valStr.Remove(valStr.Length - 1);
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                FileStream fs = new FileStream(dir + "\\GAMonitor.txt", FileMode.Append);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine($"第{CurIteration}代最大值:{CurNums}:{valStr}");
                sw.Close();
                fs.Close();

                var temp_list = CreateNextGeneration(selected);
                // 小变异
                pop = temp_list[0];
                //lamda = CurIteration + 3;// 小变异系数，随时间推移，变异缩小，从4 开始
                //MutationS(pop, lamda);
                // 大变异
                var rstLM = temp_list[1];
                MutationL(ref rstLM);
                pop.AddRange(rstLM);

                var reRandomCount = ((int)(Math.Floor(PopulationSize * 0.1))) > 2 ? ((int)(Math.Floor(PopulationSize * 0.1))) : 2;
                for (int i = 0; i < reRandomCount; i++)
                {
                    pop.RemoveAt(pop.Count - 1);
                }
                for (int i = 0; i < reRandomCount; i++)
                {
                    pop.Add(RandomChromosome(Count));
                }
            }
            return selected;
        }

        private void MutationL(ref List<Gene> s)
        {
            for (int i = 0; i < s.Count; i++)
            {
                var values = s[i].Values.ToList();
                int valuesCount=values.Count;
                for (int j = 0; j < valuesCount / 2; j++)
                {
                    if (i % 3 == 0)
                    {
                        values[2 * j] = GenerateRandom(Max, Min, 1)[0];
                        values[2 * j + 1] = GenerateRandom(Max, Min, 1)[0];
                    }
                    else if (i % 3 == 1)
                    {
                        values[2 * j] = GenerateRandom(Max, Min, 1)[0];
                    }
                    else
                    {
                        values[2 * j + 1] = GenerateRandom(Max, Min, 1)[0];
                    }
                }
                s[i]=new Gene(values.ToArray());
            }

        }
        List<int> RandChoice(int UpperBound, int n = -1, int LowerBound = 0)
        {
            // random choose n integers from n to UpperBound without replacement
            // if n < 0,return a shuffled list from lower to upper bound
            List<int> index = Enumerable.Range(LowerBound, UpperBound).ToList();
            Shuffle(index);
            if (n > UpperBound || n < 0)
            {
                return index;
            }
            else
            {
                return index.Take(n).ToList();
            }
        }
        public void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ((int)GenerateRandom(n+1,0,1).First());
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        private Gene Crossover(Gene s1, Gene s2)
        {
            var values = new List<double>();
            for (int i = 0; i < s1.Values.Length; i++)
            {
                if (i % 2 == 0)
                {
                    values.Add(s1.Values[i]);
                }
                else
                    values.Add(s2.Values[i]);
            }
            var gene=new Gene(values.ToArray());
            return gene;
        }
        List<List<Gene>> CreateNextGeneration(List<Gene> solutions)
        {
            List<Gene> rstSM = new List<Gene>();
            List<Gene> rstLM = new List<Gene>();
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
                index = RandChoice(solutions.Count, -1, 0); 
                for (int i = 0; i < index.Count / 2; ++i)
                {
                    var s = Crossover(solutions[index[2 * i]].Clone(), solutions[index[2 * i + 1]].Clone());
                    rstLM.Add(s);
                    //if (j < SMsize)//添加小变异
                    //{
                    //    rstSM.Add(s);
                    //}
                    //else//其余大变异
                    //{
                    //    rstLM.Add(s);
                    //}
                    j++;
                    if (j == PopulationSize)
                    {
                        return new List<List<Gene>> { rstSM, rstLM };
                    }
                }
            }
        }
        List<Gene> Selection(List<Gene> inputSolutions, out int maxNums)
        {
            List<Gene> sorted = new List<Gene>();
            maxNums = 0;
            foreach (var solution in inputSolutions)
            {
                CalParkings calParkings=new CalParkings(solution, Walls, VaildLanes, Buildings.Select(e => e.Clone()).ToList(), BuildingBoxes.Select(e => e.Clone()).ToList(), Region);
                solution.ParkingCount = calParkings.Cal();
            }
            sorted=inputSolutions.OrderByDescending(e => e.ParkingCount).ToList();
            maxNums=sorted.First().ParkingCount;
            var rst = new List<Gene>();
            // SelectionSize 直接保留
            for (int i = 0; i < SelectionSize; ++i)
            {
                rst.Add(sorted[i]);
            }
            //除了SelectionSize 随机淘汰;
            for (int i = SelectionSize; i < sorted.Count; ++i)
            {
                var Rand_d = GenerateRandom(1,0,1).First();
                if (Rand_d > 0.618)
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
        public List<Gene> CreateAvgPopulation(int count)
        {
            List<Gene> solutions = new List<Gene>();
            int vectorPairCount = count;
            if (vectorPairCount == 1)
            {             
                var stepCount = Math.Sqrt(PopulationSizeAVG);
                var steps = new List<double>();
                for (int i = 0; i < stepCount; i++)
                {
                    var value = Min + (Max-Min)/stepCount * i;
                    steps.Add(value);
                }
                for (int i = 0; i < steps.Count; i++)
                {
                    for (int j = 0; j < steps.Count; j++)
                    {
                        double[] values = new double[] { steps[i],steps[j] };
                        Gene gene = new Gene(values);
                        solutions.Add(gene);
                    }
                }
            }
            else if(vectorPairCount == 2)
            {
                var stepCount = Math.Sqrt(PopulationSizeAVG);
                stepCount=Math.Sqrt(stepCount);
                var steps = new List<double>();
                for (int i = 0; i < stepCount; i++)
                {
                    var value = Min + stepCount * i;
                    steps.Add(value);
                }
                for (int i = 0; i < steps.Count; i++)
                {
                    for (int j = 0; j < steps.Count; j++)
                    {
                        for (int k = 0; k < steps.Count; k++)
                        {
                            for (int u = 0; u < steps.Count; u++)
                            {
                                double[] values = new double[] { steps[i], steps[j], steps[k], steps[u] };
                                Gene gene = new Gene(values);
                                solutions.Add(gene);
                            }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < PopulationSizeAVG; i++)
                {
                    solutions.Add(RandomChromosome(vectorPairCount));
                }
            }
            return solutions;
        }
        public List<Gene> CreateFirstPopulation(int count)
        {
            List<Gene> solutions = new List<Gene>();
            for (int i = 0; i < PopulationSize; i++)
                solutions.Add(RandomChromosome(count));
            return solutions;
        }
        Gene RandomChromosome(int count)
        {
            var res = new List<double>();
            for (int i = 0; i < count * 2; i++)
            {
                RNGCryptoServiceProvider csp = new RNGCryptoServiceProvider();
                byte[] byteCsp = new byte[10];
                csp.GetBytes(byteCsp);
                var random = BitConverter.ToInt64(byteCsp, 0);
                var str = random.ToString().Substring(random.ToString().Length - 3);
                var d = double.Parse(str) * 2 - 1000;
                res.Add(d);
            }
            return new Gene(res.ToArray());
        }

        List<double> GenerateRandom(double max, double min, int count)
        {
            var res = new List<double>();
            for (int i = 0; i < count; i++)
            {
                RNGCryptoServiceProvider csp = new RNGCryptoServiceProvider();
                byte[] byteCsp = new byte[10];
                csp.GetBytes(byteCsp);
                var random = BitConverter.ToInt64(byteCsp, 0);
                var str = random.ToString().Substring(random.ToString().Length - 3);
                var d = double.Parse(str) / 1000;
                d = (max - min) * d + min;
                res.Add(d);
            }
            return res;
        }
    }

    public class CalParkings
    {
        public CalParkings(Gene gene, List<LineString> walls, List<LineSegment> vaildLanes, List<Polygon> buildings,
             List<Polygon> buildingBoxes, Polygon region)
        {
            Vecs = GeneToVector(gene);
            Walls = walls;
            VaildLanes = vaildLanes;
            Buildings = buildings;
            BuildingBoxes = buildingBoxes;
            Region = region;
        }
        private List<LineString> Walls { get; set; }
        private List<LineSegment> VaildLanes { get; set; }
        private List<Polygon> Buildings { get; set; }
        private List<Polygon> BuildingBoxes { get; set; }
        private Polygon Region { get; set; }
        private List<Vector2D> Vecs { get; set; }
        List<Vector2D> GeneToVector(Gene gene)
        {
            List<Vector2D> vecs = new List<Vector2D>();
            for (int i = 0; i < gene.Values.Length / 2; i++)
            {
                var a = gene.Values[2 * i];
                var b = gene.Values[2 * i + 1];
                vecs.Add(new Vector2D(a, b));
            }
            return vecs;
        }
        public int Cal()
        {
            if (Vecs.Count != BuildingBoxes.Count) return -1;
            var bound = Region.Clone();
            var obstacles = new List<Polygon>();
            if (BuildingBoxes.Count == 1)
                obstacles = Buildings.Select(e => e.Clone().Translation(Vecs[0])).ToList();
            else
            {
                var obspacialIndex = new MNTSSpatialIndex(Buildings);
                for (int i = 0; i < BuildingBoxes.Count; i++)
                {
                    var crossed = obspacialIndex.SelectCrossingGeometry(BuildingBoxes[i]).Cast<Polygon>();
                    obstacles.AddRange(crossed.Select(e => e.Translation(Vecs[i])));
                    BuildingBoxes[i] = BuildingBoxes[i].Translation(Vecs[i]);
                }
            }
            var obliqueMPartition = new ObliqueMPartition(Walls.Select(e => new LineString(e.Coordinates)).ToList(), VaildLanes.Select(e => new LineSegment(e.P0, e.P1)).ToList(), obstacles, bound);
            obliqueMPartition.OutputLanes = new List<LineSegment>();
            obliqueMPartition.OutBoundary = bound;
            obliqueMPartition.BuildingBoxes = BuildingBoxes.Select(e => e.Clone()).ToList();
            obliqueMPartition.ObstaclesSpatialIndex = new MNTSSpatialIndex(obstacles);
            obliqueMPartition.GenerateParkingSpaces();
            return obliqueMPartition.Cars.Count;

        }
    }
}
