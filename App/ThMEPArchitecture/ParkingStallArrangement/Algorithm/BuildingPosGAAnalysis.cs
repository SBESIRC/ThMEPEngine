using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPArchitecture.MultiProcess;
using ThMEPArchitecture.ViewModel;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.OInterProcess;
using ThParkingStall.Core.OTools;
using ThParkingStall.Core.Tools;

namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    public class BuildingPosGene
    {
        public BuildingPosGene(Vector2D vector)
        {
            Vector = vector;
        }
        public BuildingPosGene Clone()
        {
            return new BuildingPosGene(Vector);
        }
        public Vector2D Vector { get; set; }
        public int Score = 0;
    }
    public class BuildingPosGAAnalysis
    {
        public BuildingPosGAAnalysis(int index, BuildingPosAnalysis buildingPosAnalysis)
        {
            Index = index;
            _BuildingPosAnalysis = buildingPosAnalysis;
            Max = buildingPosAnalysis.BuildingMoveDistance;
            Min = -Max;
            PopulationSize = /*buildingPosAnalysis.VM.PopulationCount;*/5;
            IterationCount = /*buildingPosAnalysis.VM.IterationCount;*/5;
            MaxCount = buildingPosAnalysis.VM.MaxEqualCnt;
        }
        private int Index { get; set; }
        private BuildingPosAnalysis _BuildingPosAnalysis { get; set; }
        public Vector2D Best { get; set; }
        public int BestScore { get; set; }

        #region parameters
        public static int PopulationSize = 5;
        public static int IterationCount = 5;
        public static int MaxCount = 5;
        public int SelectionSize = Math.Max(2, (int)(0.382 * PopulationSize));
        public int Max_SelectionSize = Math.Max(2, (int)(0.618 * PopulationSize));//最大保留数量0.618
        public double Max;
        public double Min;
        public int CurIteration = 0;
        public int Elite_popsize = Math.Max((int)(PopulationSize * 0.2), 1);//精英种群数量,种群数要大于3
        public int SMsize = Math.Max(1, (int)(0.618 * PopulationSize));//小变异比例
        #endregion
        public void Process()
        {
            var gene = Run().First();
            Best = gene.Vector;
            BestScore = gene.Score;
        }
        List<BuildingPosGene> Run()
        {
            var selected = new List<BuildingPosGene>();
            var pop = CreateFirstPopulation();
            CurIteration = 0;
            int maxNums = 0;
            int maxCount = 0;
            while (CurIteration++ < IterationCount && maxCount < MaxCount)
            {
                selected = Selection(pop, out int CurNums);
                if (maxNums == CurNums)
                    maxCount++;
                else
                {
                    maxCount = 0;
                    maxNums = CurNums;
                }
                var temp_list = CreateNextGeneration(selected);
                // 小变异
                pop = temp_list[0];
                // 大变异
                var rstLM = temp_list[1];
                MutationL(ref rstLM);
                pop.AddRange(rstLM);

                var reRandomCount = ((int)(Math.Floor(PopulationSize * 0.1))) > 2 ? ((int)(Math.Floor(PopulationSize * 0.1))) : 2;
                for (int i = 0; i < reRandomCount; i++)
                    pop.RemoveAt(pop.Count - 1);
                for (int i = 0; i < reRandomCount; i++)
                    pop.Add(RandomChromosome());
            }
            return selected;
        }
        List<BuildingPosGene> CreateFirstPopulation()
        {
            List<BuildingPosGene> solutions = new List<BuildingPosGene>();
            for (int i = 0; i < PopulationSize; i++)
                solutions.Add(RandomChromosome());
            return solutions;
        }
        BuildingPosGene RandomChromosome()
        {
            Vector2D vec = Vector2D.Zero;
            int count = 0;
            while (true)
            {
                count++;
                vec = new Vector2D(GenerateRandom(Min,Max), GenerateRandom(Min, Max));
                if (_BuildingPosAnalysis.IsVaild(Index, vec))
                    break;
                if (count > 20) break;
            }
            return new BuildingPosGene(vec);
        }
        List<BuildingPosGene> Selection(List<BuildingPosGene> inputSolutions, out int maxNums)
        {
            List<BuildingPosGene> sorted = new List<BuildingPosGene>();
            foreach (var solution in inputSolutions)
            {
                solution.Score = _BuildingPosAnalysis.CalculateScore(Index, solution.Vector);
            }
            sorted = inputSolutions.OrderByDescending(e => e.Score).ToList();
            var numstr = "";
            sorted.Select(e => e.Score).ToList().ForEach(s => numstr += s + ",");
            if (numstr.Length > 0) numstr.Remove(numstr.Length - 1);
            _BuildingPosAnalysis.Logger.Information($"第{CurIteration}代计算结果: {numstr}");
            maxNums = sorted.First().Score;
            var rst = new List<BuildingPosGene>();
            // SelectionSize 直接保留
            for (int i = 0; i < SelectionSize; ++i)
            {
                rst.Add(sorted[i]);
            }
            //除了SelectionSize 随机淘汰;
            for (int i = SelectionSize; i < sorted.Count; ++i)
            {
                var Rand_d = GenerateRandom(0, 1);
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
        double GenerateRandom(double min, double max)
        {
            RNGCryptoServiceProvider csp = new RNGCryptoServiceProvider();
            byte[] byteCsp = new byte[10];
            csp.GetBytes(byteCsp);
            var random = BitConverter.ToInt64(byteCsp, 0);
            var str = random.ToString().Substring(random.ToString().Length - 3);
            var d = double.Parse(str) / 1000;
            d = (max - min) * d + min;
            return d;
        }
        List<List<BuildingPosGene>> CreateNextGeneration(List<BuildingPosGene> solutions)
        {
            List<BuildingPosGene> rstSM = new List<BuildingPosGene>();
            List<BuildingPosGene> rstLM = new List<BuildingPosGene>();
            for (int i = 0; i < Elite_popsize; ++i)
            {
                //添加精英，后续参与小变异
                rstSM.Add(solutions[i].Clone());
            }
            List<int> index;
            int j = Elite_popsize;
            while (true)
            {
                index = RandChoice(solutions.Count, -1, 0);
                for (int i = 0; i < index.Count / 2; ++i)
                {
                    var s = Crossover(solutions[index[2 * i]].Clone(), solutions[index[2 * i + 1]].Clone());
                    rstLM.Add(s);
                    j++;
                    if (j == PopulationSize)
                    {
                        return new List<List<BuildingPosGene>> { rstSM, rstLM };
                    }
                }
            }
        }
        List<int> RandChoice(int UpperBound, int n = -1, int LowerBound = 0)
        {
            List<int> index = Enumerable.Range(LowerBound, UpperBound).ToList();
            Shuffle(index);
            if (n > UpperBound || n < 0)
                return index;
            else
                return index.Take(n).ToList();
        }
        void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = (int)GenerateRandom(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        BuildingPosGene Crossover(BuildingPosGene s1, BuildingPosGene s2)
        {
            var vec = Vector2D.Zero;
            if (GenerateRandom(0, 1) > 0.5)
            {
                var t_vec = new Vector2D(s1.Vector.X, s2.Vector.Y);
                if (_BuildingPosAnalysis.IsVaild(Index, t_vec))
                    vec = t_vec;
            }
            else
            {
                var t_vec = new Vector2D(s2.Vector.X, s1.Vector.Y);
                if (_BuildingPosAnalysis.IsVaild(Index, t_vec))
                    vec = t_vec;
            }
            var gene = new BuildingPosGene(vec);
            return gene;
        }
        void MutationL(ref List<BuildingPosGene> s)
        {
            for (int i = 0; i < s.Count; i++)
            {
                var vec = s[i].Vector;
                if (GenerateRandom(0, 1) > 0.5)
                    vec = new Vector2D(GenerateRandom(Min, Max), vec.Y);
                if (GenerateRandom(0, 1) > 0.5)
                    vec = new Vector2D(vec.X, GenerateRandom(Min, Max));
                if (_BuildingPosAnalysis.IsVaild(Index, vec))
                    s[i].Vector = vec;
            }
        }
    }
}
