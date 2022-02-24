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
using Accord.Statistics;

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
        public int SpecialFlag { get; set; }// 特殊基因编号，-1：非特殊基因，0：位于lowerbound的特殊基因，1：位于ub的特殊基因，2：位于lb + 车位长的特殊基因，3：位于ub-车位长的特殊基因 
        public Gene(double value, bool direction, double minValue, double maxValue, double startValue, double endValue,int specialFlag = -1)
        {
            Value = value;
            VerticalDirection = direction;
            MinValue = minValue;//绝对的最小值
            MaxValue = maxValue;//绝对的最大值
            StartValue = startValue;
            EndValue = endValue;
            SpecialFlag = specialFlag;
        }
        public Gene Clone()
        {
            var gene = new Gene(Value, VerticalDirection, MinValue, MaxValue, StartValue, EndValue, SpecialFlag);
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

        public int ParkingStallCount { get; set; }

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
            clone.ParkingStallCount = ParkingStallCount;

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
            var rst2 = layoutPara.Set(Genome);
            if (!rst2) return 0;

            Random rand = new Random();
            int rst = rand.Next(200);
            ParkingStallCount = rst;
            return rst;
        }

        //Fitness method
        public int GetMaximumNumber(LayoutParameter layoutPara, GaParameter gaPara, ParkingStallArrangementViewModel parameterViewModel)
        {
            var rst = layoutPara.Set(Genome);
            if (!rst) return 0;

            GeoUtilities.LogMomery("SolutionStart: ");
            if (!IsValidatedSolutions(layoutPara)) return -1;
            int result = GetParkingNums(layoutPara, parameterViewModel);
            GeoUtilities.LogMomery("SolutionEnd: ");
            //Thread.Sleep(3);
            //int result = Convert.ToInt32(Regex.Match(Guid.NewGuid().ToString(), @"\d+").Value);
            ParkingStallCount = result;
            //System.Diagnostics.Debug.WriteLine(Count);

            return result;
        }

        public static bool IsValidatedSolutions(LayoutParameter layoutPara)
        {
            var lanes = new List<Line>();
            var boundary = layoutPara.OuterBoundary;
            for (int k = 0; k < layoutPara.AreaNumber.Count; k++)
            {
                layoutPara.SubAreaId2SegsDic.TryGetValue(k, out List<Line> iniLanes);
                lanes.AddRange(iniLanes);
            }
            var tmplanes = new List<Line>();
            //与边界邻近的无效车道线剔除
            for (int i = 0; i < lanes.Count; i++)
            {
                var buffer = lanes[i].Buffer(2750 - 1);
                var splits = GeoUtilities.SplitCurve(boundary, buffer);
                if (splits.Count() == 1) continue;
                splits = splits.Where(e => buffer.Contains(e.GetPointAtParam(e.EndParam / 2))).Where(e => e.GetLength() > 1).ToArray();
                if (splits.Count() == 0) continue;
                var split = splits.First();
                var ps = lanes[i].GetClosestPointTo(split.StartPoint, false);
                var pe = lanes[i].GetClosestPointTo(split.EndPoint, false);
                var splitline = new Line(ps, pe);
                var splitedlines = GeoUtilities.SplitLine(lanes[i], new List<Point3d>() { ps, pe });
                splitedlines = splitedlines.Where(e => e.GetCenter().DistanceTo(splitline.GetClosestPointTo(e.GetCenter(), false)) > 1).ToList();
                lanes.RemoveAt(i);
                tmplanes.AddRange(splitedlines);
                i--;
            }
            lanes.AddRange(tmplanes);
            GeoUtilities.RemoveDuplicatedLines(lanes);
            //连接碎车道线
            int count = 0;
            while (true)
            {
                count++;
                if (count > 10) break;
                if (lanes.Count < 2) break;
                for (int i = 0; i < lanes.Count - 1; i++)
                {
                    var joined = false;
                    for (int j = i + 1; j < lanes.Count; j++)
                    {
                        if (GeoUtilities.IsParallelLine(lanes[i], lanes[j]) && (lanes[i].StartPoint.DistanceTo(lanes[j].StartPoint) == 0
                            || lanes[i].StartPoint.DistanceTo(lanes[j].EndPoint) == 0
                            || lanes[i].EndPoint.DistanceTo(lanes[j].StartPoint) == 0
                            || lanes[i].EndPoint.DistanceTo(lanes[j].EndPoint) == 0))
                        {
                            var pl = GeoUtilities.JoinCurves(new List<Polyline>(), new List<Line>() { lanes[i], lanes[j] }).Cast<Polyline>().First();
                            var line = new Line(pl.StartPoint, pl.EndPoint);
                            if (Math.Abs(line.Length - lanes[i].Length - lanes[j].Length) < 1)
                            {
                                lanes.RemoveAt(j);
                                lanes.RemoveAt(i);
                                lanes.Add(line);
                                joined = true;
                                break;
                            }
                        }
                    }
                    if (joined) break;
                }
            }
            //判断是否有孤立的车道线
            if (lanes.Count == 1) return true;
            for (int i = 0; i < lanes.Count; i++)
            {
                bool connected = false;
                for (int j = 0; j < lanes.Count; j++)
                {
                    if (i != j)
                    {
                        if (GeoUtilities.IsConnectedLines(lanes[i], lanes[j]) || lanes[i].Intersect(lanes[j], Intersect.OnBothOperands).Count > 0)
                        {
                            connected = true;
                            break;
                        }
                    }
                }
                if (!connected) return false;
            }
            return true;
        }

        public bool IsVaild(LayoutParameter layoutPara, ParkingStallArrangementViewModel ParameterViewModel)
        {
            return layoutPara.IsVaildGenome(Genome, ParameterViewModel);
        }
        public void Clear()
        {
            Genome.Clear();
        }

        public int GetMaximumNumberFast(LayoutParameter layoutPara, GaParameter gaPara)
        {
            var rst = layoutPara.Set(Genome);
            if (!rst) return 0;
            int result = GetParkingNumsFast(layoutPara);
            ParkingStallCount = result;
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
                GeoUtilities.LogMomery("UnitStart: ");
                var use_partition_pro = true;
                if (use_partition_pro)
                {
                    var partitionpro = new ParkingPartitionPro();
                    ConvertParametersToPartitionPro(layoutPara, j, ref partitionpro, ParameterViewModel);
                    if (!partitionpro.Validate()) continue;
                    try
                    {
                        var partitionBoundary = new PartitionBoundary(partitionpro.Boundary.Vertices());
                        if (CachedPartitionCnt.ContainsKey(partitionBoundary))
                        {
                            count += CachedPartitionCnt[partitionBoundary];
                        }
                        else
                        {
                            var subCnt = partitionpro.CalNumOfParkingSpaces();
                            CachedPartitionCnt.Add(partitionBoundary, subCnt);
                            System.Diagnostics.Debug.WriteLine($"Sub area count: {CachedPartitionCnt.Count}");
                            count += subCnt;
                        }
                    }
                    catch (Exception ex)
                    {
                        ;
                    }
                    continue;
                }

                ParkingPartition partition = new ParkingPartition();
                if (ConvertParametersToPartition(layoutPara, j, ref partition, ParameterViewModel, Logger))
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
        //Genetic Algorithm parameters
        readonly double MaxTime;
        readonly int IterationCount = 10;
        int PopulationSize;
        int GeneCount;
        int FirstPopulationSize;
        double SelectionRate;
        int FirstPopulationSizeMultiplyFactor = 2;
        int SelectionSize;
        int MaxCount = 10;//出现相同车位数的最大次数
        double MutationRate;
        double GeneMutationRate;
        double MaxSMutationRate;
        int Elite_popsize;
        int Max_SelectionSize;
        double EliminateRate;
        double GoldenRatio;
        private bool SpecialOnly;
        private Dictionary<int, Tuple<double, double>> LowerUpperBound;
        //-1：非特殊基因，0：位于lowerbound的特殊基因，1：位于ub的特殊基因，2：位于lb + 车位长的特殊基因，3：位于ub-车位长的特殊基因 
        private List<double[]> SpecialGene;// 特殊基因的值
        private List<double?[]> MovingAvgPN;// 特殊基因的车位数量,PN parkingnumber,MovingAvgPN[i][j]代表第i个基因的第j个特殊基因的值，可以为空
        private List<double[]> SpecialGeneScore;//每个特殊基因的分数
        private List<double[]> SpecialGeneProb;// 特殊基因对应的随机概率
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
            GeneCount = gaPara.LineCount;//基因总数
            //大部分参数采取黄金分割比例，保持选择与变异过程中种群与基因相对稳定
            GoldenRatio = (Math.Sqrt(5) - 1) / 2;//0.618
            IterationCount = parameterViewModel == null ? 10 : parameterViewModel.IterationCount;

            PopulationSize = parameterViewModel == null ? 10 : parameterViewModel.PopulationCount;//种群数量
            if (PopulationSize < 3) throw (new ArgumentOutOfRangeException("种群数量至少为3"));
            MaxTime =  parameterViewModel == null ? 180 : parameterViewModel.MaxTimespan;//最大迭代时间

            InitGenomes = initgenomes;// 输入初始基因，生成初代时使用
            // 更改迭代最大时间以及种群数量
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
            FirstPopulationSizeMultiplyFactor = 2;
            FirstPopulationSize = PopulationSize * FirstPopulationSizeMultiplyFactor;
            MutationRate = 1 - GoldenRatio;//变异因子,0.382
            GeneMutationRate = 1 - GoldenRatio;//基因变异因子0.382,保持迭代过程中变异基因的比例
            MaxSMutationRate = GoldenRatio;// 最大小变异几率，随代数递减，0.618
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
            for (int i = 0; i < GeneCount; ++i)
            {
                GetBoundary(i, out double LowerBound, out double UpperBound);
                //UpperLowerBound[i] = new Tuple<double, double>(LowerBound, UpperBound);
                var tempT = new Tuple<double, double>(LowerBound, UpperBound);
                LowerUpperBound.Add(i, tempT);
            }
            InitSpecialGene();
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
        #region
        // 特殊基因处理部分
        private double RandomSpecialNumber(int i, out int idx)
        {
            //随机的特殊解，基于当前基因的每个特殊解的概率。用于卡车位
            // idx 特殊基因的flag
            idx = General.Utils.RandChoiceOne(SpecialGeneProb[i]);// 基于概率随机选一个
            return SpecialGene[i][idx];// 随机选一个
        }
        private void InitSpecialGene()// 初始化特殊基因数据
        {
            SpecialGene = new List<double[]>();// 特殊基因的值
            MovingAvgPN = new List<double?[]>();// 特殊基因的车位数量,PN parkingnumber,MovingAvgPN[i][j]代表第i个基因的第j个特殊基因的值，可以为空
            SpecialGeneScore = new List<double[]>();//每个特殊基因的分数
            SpecialGeneProb = new List<double[]>();// 特殊基因对应的随机概率
            var parkingLength = ParameterViewModel.VerticalSpotLength;
            for (int i = 0;i< GeneCount; ++i)
            {
                double LowerBound = LowerUpperBound[i].Item1;
                double UpperBound = LowerUpperBound[i].Item2;
                var SolutionLis = new List<double>() { LowerBound, UpperBound };
                var s1 = LowerBound + parkingLength;
                var s2 = UpperBound - parkingLength;
                if (s1 < UpperBound) SolutionLis.Add(s1);
                if (s2 > LowerBound) SolutionLis.Add(s2);//这俩条件满足一个则都满足
                SpecialGene.Add(SolutionLis.ToArray());

                var initArr = new double?[SolutionLis.Count];
                var initScore = new double[SolutionLis.Count];
                var initProb = new double[SolutionLis.Count];
                for (int j = 0; j < SolutionLis.Count; ++j)
                {
                    initArr[j] = null;
                    initScore[j] = 1;
                    initProb[j] = 1 / SolutionLis.Count;//初始每个特殊基因的几率相等
                }
                MovingAvgPN.Add(initArr);
                SpecialGeneScore.Add(initScore);
                SpecialGeneProb.Add(initProb);
            }
            ;
        }
        private void UpdateSpecialGene(List<Chromosome> solutions)
        {
            // 输入最新的solutions，更新特殊基因概率
            UpdateMovingAvgPNs(solutions);
            UpdateGeneScore();
            UpdateGeneProb();
        }
        private void UpdateMovingAvgPNs(List<Chromosome> solutions)//更新全部的MovingAvg
        {
            var SpecialGenePNs = new List<List<List<int>>>();//SpecialGenePNs[i][j][k]代表第i个基因的第j个特殊基因的第k个元素
            for (int i = 0; i < GeneCount; i++)
            {
                var lis = new List<List<int>>();
                for (int j = 0; j < MovingAvgPN[i].Length; ++j)
                {
                    lis.Add(new List<int>());// 添加特殊基因个list
                }
                SpecialGenePNs.Add(lis);//创建SpecialGenePNs
            }
            foreach (var solution in solutions)
            {
                var parkingStallCount = solution.ParkingStallCount;
                for (int i = 0; i < GeneCount; i++)
                {
                    if (solution.Genome[i].SpecialFlag != -1)// 特殊基因
                    {
                        int j = solution.Genome[i].SpecialFlag;
                        SpecialGenePNs[i][j].Add(parkingStallCount);//向SpecialGenePNs中添加数据
                    }
                }
            }
            for (int i = 0; i < GeneCount; i++)
            {
                for (int j = 0; j < MovingAvgPN[i].Length; ++j)
                {
                    MovingAvgPN[i][j] = GetNewMovingAvg(MovingAvgPN[i][j], SpecialGenePNs[i][j]);//更新moving avg
                }
            }
        }
        private double? GetNewMovingAvg(double? preMA, List<int> PSCounts)// 获取某一个特殊基因更新后的movingAvg
        {
            if (PSCounts.Count == 0) return preMA;// 无更新元素，返回上一次元素
            var PS_Avg = PSCounts.Average();
            if (preMA == null) return PS_Avg;//上一次元素为null，返回当前平均值
            else//加权平均
            {
                var val = Math.PI / 2;
                var lam = 0.2 * PSCounts.Count;
                var alpha = Math.Atan(lam) / val;// alpha 范围从0.126~0.9999999
                return (double)preMA * (1 - alpha) + alpha * PS_Avg;//加权平均
            }
        }

        private void UpdateGeneScore()//更新基因分数
        {
            for (int i = 0; i < GeneCount; i++)//对于所有的基因
            {
                //获取MovingAvgPN[i] 不为null的index
                var index = new List<int>();
                var slice = new List<double>();
                for (int j = 0; j < MovingAvgPN[i].Length; ++j)
                {
                    if (MovingAvgPN[i][j] != null)
                    {
                        index.Add(j);
                        slice.Add((double) MovingAvgPN[i][j]);
                    }
                }
                if (slice.Count != 0)
                {
                    var SPMedian = Measures.Median(slice.ToArray());//取中位数
                    var SPAbs = slice.Max() - slice.Min();
                    if (SPAbs > 0)
                    {
                        for (int n = 0; n < index.Count; ++n)
                        {
                            var idx = index[n];
                            SpecialGeneScore[i][idx] = 2*(slice[n] - SPMedian) / SPAbs;// score 绝对值长度为2，最大可能值+2（取值范围2~0），最小可能值-2（0~-2）
                        }
                    }
                    else// 最大最小值相同则设置为0
                    {
                        foreach (int idx in index)SpecialGeneScore[i][idx] = 0;
                    }
                }
                // 对于所有为null的
                for (int k = 0; k < SpecialGeneScore[i].Length; ++k)
                {
                    if (!index.Contains(k)) SpecialGeneScore[i][k] = 2;
                }
            }
        }
        private void UpdateGeneProb()//更新每个特殊基因的概率
        {
            for (int i = 0; i < GeneCount; i++)//对于所有的基因
            {
                double sumExp = 0;
                foreach (var score in SpecialGeneScore[i]) sumExp += Math.Exp(score);
                for (int j = 0; j < SpecialGeneProb[i].Length; ++j)
                {
                    var score = SpecialGeneScore[i][j];
                    SpecialGeneProb[i][j] = (0.1 / SpecialGeneProb[i].Length) + 0.9 * (Math.Exp(score)/ sumExp);
                }
            }
        }
        #endregion

        #region
        //第一代初始化
        private List<Gene> ConvertLineToGene(int index)
        {
            var genome = new List<Gene>();
            for (int i = 0; i < GeneCount; i++)
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
            for (int i = 0; i < GeneCount; i++)
            {
                var line = GaPara.SegLine[i];
                var dir = line.GetValue(out double value, out double startVal, out double endVal);
                var valueWithIndex = value;
                Gene gene = new Gene(valueWithIndex, dir, GaPara.MinValues[i], GaPara.MaxValues[i], startVal, endVal);
                genome.Add(gene);
            }
            return genome;
        }

        private bool RandomCreateChromosome(out Chromosome solution, int N = 100)
        {
            // Try N times
            solution = new Chromosome();
            for (int j = 0; j < N; j++)
            {
                var genome = new List<Gene>();
                for (int i = 0; i < GeneCount; i++)
                {
                    var line = GaPara.SegLine[i];
                    var dir = line.GetValue(out double value, out double startVal, out double endVal);
                    double LowerBound = LowerUpperBound[i].Item1;
                    double UpperBound = LowerUpperBound[i].Item2;
                    double RandValue;
                    Gene gene;
                    if (RandDouble() > GoldenRatio)
                    {
                        //RandValue = RandomSpecialNumber(LowerBound, UpperBound);//随机特殊解
                        RandValue = RandomSpecialNumber(i,out int specialflag);
                        gene = new Gene(RandValue, dir, GaPara.MinValues[i], GaPara.MaxValues[i], startVal, endVal, specialflag);
                    }
                    else
                    {
                        RandValue = RandDoubleInRange(LowerBound, UpperBound);//纯随机数
                        gene = new Gene(RandValue, dir, GaPara.MinValues[i], GaPara.MaxValues[i], startVal, endVal);
                    }
                    //Gene gene = new Gene(RandValue, dir, GaPara.MinValues[i], GaPara.MaxValues[i], startVal, endVal);
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
        private List<Chromosome> CreateFirstPopulation()
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
            while (solutions.Count < FirstPopulationSize)
            {
                // 随机生成 其余的解
                var FoundVaild = RandomCreateChromosome(out Chromosome solution);
                if (FoundVaild)
                {
                    solutions.Add(solution);
                }
                else
                {
                    // 没找到则在之前解随机挑选一个
                    var idx = RandInt(solutions.Count);
                    solutions.Add(solutions[idx].Clone());
                }
            }
            return solutions;
        }


        #endregion
        #region
        // run代码部分
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
        #region
        //随机函数
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
                var parkingLength = ParameterViewModel.VerticalSpotLength;
                var SolutionLis = new List<double>() { LowerBound, UpperBound};
                var s1 = LowerBound + parkingLength;
                var s2 = UpperBound - parkingLength;
                if (s1 < UpperBound) SolutionLis.Add(s1);
                if (s2 > LowerBound) SolutionLis.Add(s2);
                return SolutionLis[RandInt(SolutionLis.Count)];// 随机选一个
            }
        }

        #endregion
        #region
        // run2代码部分
        // 选择逻辑增强，除了选择一部分优秀解之外，对其余解随即保留
        // 后代生成逻辑增强，保留之前最优解直接保留，不做变异的逻辑。新增精英种群逻辑，保留精英种群，并且参与小变异。
        // 变异逻辑增强，增加小变异（用于局部最优化搜索），保留之前的变异逻辑（目前称之为大变异）。
        // 对精英种群和一部分交叉产生的后代使用小变异，对一部分后代使用大变异，对剩下的后代不做变异。
        public List<Chromosome> Run2(List<Chromosome> histories, bool recordprevious,bool specialOnly = false)
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
        private List<Chromosome> Selection2(List<Chromosome> inputSolution, out int maxNums)
        {
            Logger?.Information("进行选择");
            inputSolution.ForEach(s =>
            {
                s.GetMaximumNumber(LayoutPara, GaPara, ParameterViewModel);
                ReclaimMemory();
            });
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
            UpdateSpecialGene(sorted.Take(Max_SelectionSize).ToList());

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
            int SMsize = PopulationSize - SelectionSize;// small mutation size,0.618of total population size
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
            double cur_MR = MaxSMutationRate / Math.Sqrt(lamda);// 当前最大变异几率
            int MaxgeneCnt = Math.Min((int)(s[0].GenomeCount() * GeneMutationRate), 1);//需要变异的基因数目，最小为1
            for (int i = 1; i < s.Count; ++i)
            {
                var geneCnt = RandInt(MaxgeneCnt) + 1;
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
                        //s[i].Genome[j].Value = RandomSpecialNumber(minVal, maxVal);
                        s[i].Genome[j].Value = RandomSpecialNumber(j, out int specialflag);
                        s[i].Genome[j].SpecialFlag = specialflag;
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
