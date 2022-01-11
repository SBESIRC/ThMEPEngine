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

namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    public class SegBreakParam
    {
        List<List<Line>> ListOfBreakedLines;
        List<Line> OtherLines;
        List<List<int>> ListOfLowerBounds;
        List<List<int>> ListOfUpperBounds;
        int LineCount;
        //输入，初始分割线，以及打断的方向。输出，分割线与其交点
        public void BreakLines(List<Line> SegLines,bool VerticalDirection,bool GoPositive)
        {
            // GoPositive 从下至上打断，从左至右打断（坐标增加顺序）
            List<Line> VertLines = new List<Line>();//垂直线
            List<Line> HorzLines = new List<Line>();//水平线
            foreach (Line line in SegLines)
            {
                if (line.StartPoint.X== line.EndPoint.X)
                {
                    //横坐标相等，平行线
                    HorzLines.Add(new Line(line.StartPoint, line.EndPoint));
                }
                else
                {
                    VertLines.Add(new Line(line.StartPoint, line.EndPoint));
                }
            }
            ListOfBreakedLines = new List<List<Line>>();
            ListOfLowerBounds = new List<List<int>>();// 打断线的下边界
            ListOfUpperBounds = new List<List<int>>();// 打断线的上边界

            if (VerticalDirection)
            {
                // otherlines 添加横向线
                OtherLines = HorzLines;
                LineCount = VertLines.Count;
                //打断纵向线
                foreach (Line line1 in VertLines)
                {
                    List<Point3d> ptlist = new List<Point3d>();//断点列表
                    List<Line> IntersectLines = new List<Line>();//交叉线列表
                    foreach (Line line2 in HorzLines)
                    {
                        var templ = line1.Intersect(line2, Intersect.OnBothOperands);
                        if (templ.Count != 0)
                        {
                            ptlist.Add(templ.First());// 添加打断点
                            IntersectLines.Add(new Line(line2.StartPoint, line2.EndPoint));// 添加线的复制
                        }
                    }
                    if (ptlist.Count > 2) 
                    {
                        List<Line> BreakedLines = new List<Line>();// 打断后的纵线list
                        List<int> LowerBounds = new List<int>();// 打断线的下边界
                        List<int> UpperBounds = new List<int>();// 打断线的上边界
                        // 该纵线打断
                        if (GoPositive)
                        {
                            // 1.TODO:sort ptlist and IntersectLines(base on pt list)按照纵坐标排序
                            // 2. 确定打断后的纵线
                                
                            Point3d spt;
                            if (line1.StartPoint.Y > line1.EndPoint.Y)
                            {
                                spt = line1.StartPoint;
                            }
                            else
                            {
                                spt = line1.EndPoint;
                            }
                            int CurCount = 0;
                            List < int > InnerIndex = new List<int>();// 记录在当前断线上所有横线的索引
                            Line BreakLine ;
                            for (int i = 0; i < ptlist.Count; ++i)
                            {
                                // 双指针确定断线

                                if (CurCount < 1)
                                {
                                    CurCount += 1;
                                    InnerIndex.Add(i);
                                }
                                else
                                {
                                    // 新断线
                                    InnerIndex.Add(i);
                                    if (i != ptlist.Count - 2)
                                    {
                                        BreakLine = new Line(spt, ptlist[i]);
                                        //不为倒数第二个点，则直接添加
                                        BreakedLines.Add(BreakLine);
                                        spt = ptlist[i];
                                            
                                        // TO DO: 确定断线范围，buffer，取建筑或者buffer值（添加 Lower & Upper Bound to Lower & upper Bounds)
                                        // TO DO: 更新断线上所有横向线（拉伸，覆盖断线的最大范围）

                                        //重新计数
                                        InnerIndex = new List<int>();
                                        InnerIndex.Add(i);
                                        CurCount = 0;
                                    }

                                }
                            }
                        }
                        else
                        {
                            ;//对称逻辑
                        }
                        ListOfBreakedLines.Add(BreakedLines);
                        ListOfLowerBounds.Add(LowerBounds);
                        ListOfUpperBounds.Add(UpperBounds);
                    }
                    
                }
            }
            else
            {
                ;//与上面对称
            }
        }
    }

    public class SegGA : IDisposable
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
   

        SegBreakParam SegParam;
        LayoutParameter LayoutPara;

        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "GaLog.txt");

        public Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
            .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Hour).CreateLogger();

        //public SegGA(SegParameter SegParam, LayoutParameter layoutPara, int popSize = 10, int iterationCnt = 10)
        public SegGA(SegBreakParam segparam, LayoutParameter layoutPara, int popSize = 10, int iterationCnt = 10)
        {
            //大部分参数采取黄金分割比例，保持选择与变异过程中种群与基因相对稳定
            GoldenRatio = (Math.Sqrt(5) - 1) / 2;//0.618
            IterationCount = iterationCnt;
            Rand = new Random(DateTime.Now.Millisecond);//随机数
            PopulationSize = popSize;//种群数量
            FirstPopulationSizeMultiplyFactor = 2;
            FirstPopulationSize = PopulationSize * FirstPopulationSizeMultiplyFactor;
            MaxTime = 180;
            MutationRate = 1 - GoldenRatio;//变异因子,0.382
            GeneMutationRate = 1 - GoldenRatio;//基因变异因子0.382,保持迭代过程中变异基因的比例

            SelectionRate = 1 - GoldenRatio;//保留因子0.382
            SelectionSize = Math.Max(2, (int)(SelectionRate * popSize));

            //InputsF
            SegParam = segparam;
            LayoutPara = layoutPara;
            // Run2 添加参数
            Elite_popsize = Math.Max((int)(popSize * 0.2), 1);//精英种群数量,种群数要大于3
            EliminateRate = GoldenRatio;//除保留部分随机淘汰概率0.618
            Max_SelectionSize = Math.Max(2, (int)(GoldenRatio * popSize));//最大保留数量0.618
            MutationUpperBound = 15700.0;// 最大变异范围，两排车道宽
            LowerUpperBound = new Dictionary<int, Tuple<double, double>>();//储存每条基因可变动范围，方便后续变异
            for (int i = 0; i < SegParam.LineCount; ++i)
            {
                GetBoundary(i, out double LowerBound, out double UpperBound);
                //UpperLowerBound[i] = new Tuple<double, double>(LowerBound, UpperBound);
                var tempT = new Tuple<double, double>(LowerBound, UpperBound);
                LowerUpperBound.Add(i, tempT);
            }
        }
        //private void GetBoundary(int i, out double LowerBound, out double UpperBound)
        //{
        //    // get absolute coordinate of segline
        //    var line = SegParam.SegLine[i];
        //    var dir = line.GetValue(out double value, out double startVal, out double endVal);
        //    LowerBound = SegParam.MinValues[i] + value;
        //    UpperBound = SegParam.MaxValues[i] + value;
        //}
        private void GetBoundary(int i, out double LowerBound, out double UpperBound)
        {
            // get absolute coordinate of segline
            LowerBound = SegParam.MinValues[i] + value;
            UpperBound = SegParam.MaxValues[i] + value;
        }
        #region
        //第一代初始化
        private List<Gene> ConvertLineToGene(int index)
        {
            var genome = new List<Gene>();
            for (int i = 0; i < SegParam.LineCount; i++)
            {
                if (index == 0)
                {
                    var line = SegParam.SegLine[i];
                    var dir = line.GetValue(out double value, out double startVal, out double endVal);
                    var valueWithIndex = value + SegParam.MaxValues[i];
                    Gene gene = new Gene(valueWithIndex, dir, SegParam.MinValues[i], SegParam.MaxValues[i], startVal, endVal);
                    genome.Add(gene);
                }
                else
                {
                    var line = SegParam.SegLine[i];
                    var dir = line.GetValue(out double value, out double startVal, out double endVal);

                    var valueWithIndex = value + (SegParam.MaxValues[i] - SegParam.MinValues[i]) / FirstPopulationSize * index + SegParam.MinValues[i];
                    Gene gene = new Gene(valueWithIndex, dir, SegParam.MinValues[i], SegParam.MaxValues[i], startVal, endVal);
                    genome.Add(gene);
                }
            }
            return genome;
        }
        private List<Gene> ConvertLineToGene()//仅根据分割线生成第一代
        {
            var genome = new List<Gene>();
            for (int i = 0; i < SegParam.LineCount; i++)
            {
                var line = SegParam.SegLine[i];
                var dir = line.GetValue(out double value, out double startVal, out double endVal);
                var valueWithIndex = value;
                Gene gene = new Gene(valueWithIndex, dir, SegParam.MinValues[i], SegParam.MaxValues[i], startVal, endVal);
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
            inputSolution.ForEach(s => s.GetMaximumNumberWithLines(LayoutPara, SegParam));
            //inputSolution.ForEach(s => s.GetMaximumNumberFast(LayoutPara, SegParam));
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
