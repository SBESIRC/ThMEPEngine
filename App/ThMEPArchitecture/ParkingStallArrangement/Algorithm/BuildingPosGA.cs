using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPArchitecture.ParkingStallArrangement.General;
using ThMEPArchitecture.ViewModel;
using ThParkingStall.Core.OInterProcess;
using ThParkingStall.Core.Tools;
namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    public class BuildingPosGA : IDisposable
    {
        //Genetic Algorithm parameters
        double MaxTime = 60;
        int IterationCount = 60;
        int PopulationSize = 32;
        int FirstPopMagnitude = 2;//初代倍率
        int MaxCount = 5;//出现相同车位数的最大次数
        int CurrentCnt = 0;
        public bool Ended = false;
        double GeneMutationRate;

        double GoldenRatio;
        public double Max;
        public double Min;

        public int CompareSize = 3;
        //public int ParentSize = 2;
        public int Index { get; set; }
        BuildingPosAnalysis BPA { get; set; }

        ParkingStallArrangementViewModel ParameterViewModel;
        public Serilog.Core.Logger Logger = null;

        public int CurIteration;
        public int BestScore = -1;
        public BuildingPosGene OptimalSolution;//遗传发现的最优解
        public List<BuildingPosGene> Population;//当前代
        public List<int> IdxToCalculate = new List<int>();//需要计算的基因序号

        private Dictionary<Vector2D, int> VecCached = new Dictionary<Vector2D, int>();//计算过的基因
        public int init_Score
        {
            get { return VecCached[new Vector2D()]; }
        }
        public BuildingPosGA(int buildingIdx, BuildingPosAnalysis bpa,ParkingStallArrangementViewModel parameterViewModel = null)
        {
            BPA = bpa;
            //大部分参数采取黄金分割比例，保持选择与变异过程中种群与基因相对稳定
            GoldenRatio = (Math.Sqrt(5) - 1) / 2;//0.618
            IterationCount = parameterViewModel == null ? 60 : parameterViewModel.IterationCount;
            PopulationSize = parameterViewModel == null ? 32 : parameterViewModel.PopulationCount;//种群数量
            MaxCount = parameterViewModel == null ? 10 : parameterViewModel.MaxEqualCnt;//相同退出次数
            if (PopulationSize < 3) throw (new ArgumentOutOfRangeException("种群数量至少为3"));
            MaxTime = parameterViewModel == null ? 180 : parameterViewModel.MaxTimespan;//最大迭代时间
            GeneMutationRate = parameterViewModel.GeneMutationRate;//基因变异因子0.382,保持迭代过程中变异基因的比例
            //InputsF
            ParameterViewModel = parameterViewModel;
            Min = -parameterViewModel.BuildingMoveDistance;
            Max = parameterViewModel.BuildingMoveDistance;
            Index = buildingIdx;
            FirstPopMagnitude = parameterViewModel == null ? 2 : parameterViewModel.FirstPopMagnitude;
            InitPopulation();//初始化种群
        }
        #region 第一代初始化
        BuildingPosGene RandomGene(int maxCnt = 100)//测试下函数速度
        {
            var vec = Vector2D.Zero;
            for(int i = 0; i < maxCnt; i++)
            {
                var tempvec = RandVec();
                if(BPA.IsVaild(Index,tempvec)) vec = tempvec;
            }
            return new BuildingPosGene(vec,Index);
        }
        public Vector2D RandVec()
        {
            return new Vector2D(RandDoubleInRange(Min, Max), RandDoubleInRange(Min, Max));
        }
        private void InitPopulation()
        {
            Population = new List<BuildingPosGene> { new BuildingPosGene(0,0,Index)};
            IdxToCalculate.Add(0);
            for (int i = 0; i < PopulationSize* FirstPopMagnitude -1; i++)
            {
                Population.Add(RandomGene());
                IdxToCalculate.Add(i+1);
            }
        }

        #endregion
        #region 随机函数
        private double RandDouble()
        {
            return ThParkingStall.Core.Tools.ThParkingStallCoreTools.RandDouble();
        }
        private double RandDoubleInRange(double LowerBound, double UpperBound)
        {
            double tol = 1e-4;
            if (UpperBound - LowerBound < tol) return LowerBound;
            else return RandDouble() * (UpperBound - LowerBound) + LowerBound;
        }
        //直接返回相对值
        
        #endregion
        #region 迭代
        // 更新下一代
        public void Update(List<int> Scores)// 更新当前种群，Scores 对应需要更新的种群
        {
            if(Ended) return;
            UpdateScore(Scores);
            IdxToCalculate.Clear();
            var currentBest = Population.Max(p => p.Score);
            var bestOnes = Population.Where(p => p.Score == currentBest).ToList();
            ThParkingStall.Core.Tools.ShuffleExtensions.Shuffle(bestOnes);
            if (BestScore == -1)
            {
                BestScore = init_Score;
                OptimalSolution = new BuildingPosGene(0, 0, Index);
            }

            if(currentBest > BestScore)
            {
                CurrentCnt = 0;
                BestScore = currentBest;
                OptimalSolution = bestOnes.First();
            }
            else
            {
                CurrentCnt += 1;
                if (CurrentCnt == MaxCount)
                {
                    Ended = true;
                    return;
                }
            }
            var nextPop = new List<BuildingPosGene> { bestOnes.First().Clone() };
            while (nextPop.Count != PopulationSize)
            {
                nextPop.Add(CreateFromPop());
            }
            Population = nextPop;
            UpdateIdxToCalculate();
        }

        public List<BuildingPosGene> GetPopToCalculate()
        {
            return Population.Slice(IdxToCalculate);
        }
        private void UpdateScore(List<int> Scores)//更新种群的分数
        {
            if (Scores.Count != IdxToCalculate.Count) throw new ArgumentException("Invaild Score Length");
            int j = 0;
            for (int i = 0; i < Population.Count; i++)
            {
                var pop = Population[i];
                if (IdxToCalculate.Contains(i))
                {
                    pop.Score = Scores[j];
                    VecCached.Add(pop.Vector(), pop.Score);
                    j++;
                }
                else
                {
                    pop.Score = VecCached[pop.Vector()];
                }
            }
        }
        private void UpdateIdxToCalculate()//更新要计算的idx
        {
            var ToCalculate = new HashSet<Vector2D>();
            for (int i = 0; i < Population.Count; i++)
            {
                var vec = Population[i].Vector();
                if (VecCached.ContainsKey(vec)) continue;
                if (ToCalculate.Contains(vec)) continue;
                IdxToCalculate.Add(i);
                ToCalculate.Add(vec);
            }
        }
        //做成多线程
        private BuildingPosGene CreateFromPop(int MaxCnt = 100)
        {
            for(int i = 0; i < MaxCnt; i++)
            {
                var child = CreateOne();
                if(BPA.IsVaild(Index,child.Vector())) return child;
            }
            //未找到合理解，从上一代随机挑选一个
            Population.Shuffle();
            return Population.First();
        }
        private BuildingPosGene CreateOne()
        {
            //锦标赛选择，生成父代
            var parents = new List<BuildingPosGene>();
            for (int i = 0; i < 2; i++)
            {
                Population.Shuffle();
                var selected = Population.Take(CompareSize);
                parents.Add(selected.OrderByDescending(p => p.Score).First().Clone());
            }
            //交叉
            double X;
            double Y;
            if (RandDouble() > 0.5)
            {
                X = parents.First().X;
                Y = parents.Last().Y;
            }
            else
            {
                X = parents.Last().X;
                Y = parents.First().Y;
            }
            //变异
            if (RandDouble() < GeneMutationRate) X = RandDoubleInRange(Min, Max);
            if (RandDouble() < GeneMutationRate) Y = RandDoubleInRange(Min, Max);
            return new BuildingPosGene(X, Y,Index);
        }
        
        #endregion
        public void Dispose()
        {

        }
    }
}
