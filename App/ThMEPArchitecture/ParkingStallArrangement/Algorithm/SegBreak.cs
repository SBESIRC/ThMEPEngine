using System;
using AcHelper;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Model;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using Autodesk.AutoCAD.Geometry;
using System.IO;
using ThCADExtension;
using Serilog;
using System.Diagnostics;
using ThCADCore.NTS;
using ThMEPArchitecture.PartitionLayout;
using Dreambuild.AutoCAD;
using DotNetARX;
using ThMEPArchitecture.ParkingStallArrangement.General;
using ThMEPArchitecture.ViewModel;
using Draw = ThMEPArchitecture.ParkingStallArrangement.Method.Draw;

namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    class SegBreak : IDisposable
    {
        public List<Line> NewSegLines;// 新的初始分割线
        public List<int> FixedIdx;// 不迭代的分割线index（对应NewSegLines)
        public List<int> VarIdx;//迭代的分割线index（对应NewSegLines)
        public List<int> NewToOrgMap;// NewSegLines 对应初始SegLines的索引
        public bool VerticalDirection;//是否纵向
        public bool GoPositive; // 是否坐标增加顺序打断

        private List<Line> BreakedLines;//所有打断后的分割线，要导入迭代之后的值
        private List<Line> OtherLines;//其余所有线 （纵向打断则为横线，横向打断则为纵线）,要迭代之后的

        private List<int> BreakedLinesIdxs;// 打断后的线对应原始线的索引
        private List<List<int>> CorssIdxs;// 与BreakedLines 交叉的的所有横线索引

        private List<Line> SegLines;// 初始的分割线，会被重新赋值

        private List<int> VertLines_index ;// 垂直线索引

        private List<int> HorzLines_index;// 横向线索引

        private GaParameter GaPara;
        
        private Polyline WallLine;

        private int ImpossibleIdx = int.MaxValue;
        //输入，初始分割线，以及打断的方向。输出，分割线与其交点
        public SegBreak(OuterBrder outerbrder,GaParameter gaPara, bool verticaldirection, bool gopositive = true)
        {
            SegLines = new List<Line>();
            outerbrder.SegLines.ForEach(l => SegLines.Add(new Line(l.StartPoint, l.EndPoint)));
            WallLine = outerbrder.WallLine;
            //预切割
            precut();
            
            VerticalDirection = verticaldirection;// true 则纵向，false横向
            GoPositive = gopositive;
            //GoPositive true则坐标增加顺序打断，false则坐标减少顺序打断
            // 获取分割线上下边界，目前不做判断
            GaPara = gaPara;
            VertLines_index = new List<int>();

            HorzLines_index = new List<int>();
            for (int i = 0; i < SegLines.Count; ++i)
            {
                var line = SegLines[i];
                if (Math.Abs(line.StartPoint.X - line.EndPoint.X) < 1e-4)
                {
                    //横坐标相等，垂直线
                    VertLines_index.Add(i);
                }
                else
                {
                    HorzLines_index.Add(i);
                }
            }
            premove();// 预平移，避免起于同一点

            BreakedLines = new List<Line>();
            BreakedLinesIdxs = new List<int>();
            OtherLines = new List<Line>();// 记录与打断线垂直方向的线
            List<int> CurLinesIdx;//需要打断的线的index
            List<int> OtherLinesIdx;//不需要打断的index（与打断线垂直方向的线）
            
            CorssIdxs = new List<List<int>>();// 交叉线的idx

            if (VerticalDirection)
            {
                OtherLinesIdx = HorzLines_index;
                CurLinesIdx = VertLines_index;
            }
            else
            {
                OtherLinesIdx = VertLines_index;
                CurLinesIdx = HorzLines_index;
            }

            //OtherLinesIdx.ForEach(idx => OtherLines.Add(new Line(SegLines[idx].StartPoint, SegLines[idx].EndPoint)));
            for (int k = 0; k < CurLinesIdx.Count; ++k)
            {
                var line1 = SegLines[CurLinesIdx[k]];//需打断的线

                GetSptEpt(line1, out Point3d spt, out Point3d ept);//确定该线起点以及终点

                GetAllIntSecPoints(line1, OtherLinesIdx, out List<Point3d> IntSecPoints, out List<int> IntSecIndex);//确定该线的断点，以及对应断点的交叉点index

                List<int> InnerIndex = new List<int>();// 记录在当前断线上所有交叉线的绝对索引

                // 边界断点
                if (IntSecIndex[0] != ImpossibleIdx)//起始如果不贯穿边界
                {
                    var AbsIdx = IntSecIndex[0];//交叉线绝对索引
                    InnerIndex.Add(AbsIdx);
                    //spt = IntSecPoints[0];// 且起点在分割线上
                }

                if (IntSecPoints.Count > 2)// 至少有一个中间断点可打断 
                {
                    List<Line> cur_breakedlines = new List<Line>(); // line1 打断后的线list
                    List<List<int>> cur_CrossIdx = new List<List<int>>(); //与每根断线相交的线的绝对索引

                    int CurCount = 0;// 记录经过的断点数量，隔一个断点打断

                    Line BreakLine;
                    for (int i = 1; i < IntSecPoints.Count - 1; ++i)// 遍历中间的所有可能断点，不包含边界上的断点
                    {
                        var AbsIdx = IntSecIndex[i];//交叉线绝对索引
                        InnerIndex.Add(AbsIdx);
                        // 双指针确定断线
                        if (CurCount > 0 || IntSecPoints.Count == 3)// 3个断点则中间需要打断,已经跳过一个点，也做打断。
                        {
                            // 新断线
                            BreakLine = new Line(spt, IntSecPoints[i]);

                            cur_breakedlines.Add(BreakLine);
                            cur_CrossIdx.Add(InnerIndex);

                            spt = IntSecPoints[i];//更新起始点
                            CurCount = 0;
                            InnerIndex = new List<int>();
                            InnerIndex.Add(AbsIdx);
                        }

                        else//count 为0 跳过一个
                        {
                            CurCount += 1;
                        }
                    }
                    // 添加最后的断线,连接终点与上一个断点
                    
                    if (IntSecIndex.Last() != ImpossibleIdx)//终点如果不贯穿边界，记录边界交叉线
                    {
                        var AbsIdx = IntSecIndex.Last();
                        InnerIndex.Add(AbsIdx);
                        //ept = IntSecPoints.Last();// 且终点在分割线上
                    }

                    BreakLine = new Line(spt, ept);
                    cur_breakedlines.Add(BreakLine);
                    cur_CrossIdx.Add(InnerIndex);

                    BreakedLines.AddRange(cur_breakedlines);
                    for (int m = 0; m < cur_breakedlines.Count; ++m) BreakedLinesIdxs.Add(CurLinesIdx[k]);//添加初始线索引
                    

                    CorssIdxs.AddRange(cur_CrossIdx);
                }
                else// 虽然不能打断，但是也要迭代
                {
                    BreakedLines.Add(line1);
                    BreakedLinesIdxs.Add(CurLinesIdx[k]);//添加初始线索引
                    // 因为不做移动，所以横线不需要更新,添加占位index
                    CorssIdxs.Add(new List<int> { ImpossibleIdx });
                }
            }
            // 调整breakline 位置，拉伸交叉线
            ModifyLines();
            OtherLinesIdx.ForEach(idx => OtherLines.Add(new Line(SegLines[idx].StartPoint, SegLines[idx].EndPoint)));
            UpdateNewSegLines(OtherLinesIdx);
        }
        
        public List<Chromosome> TransPreSols(ref GaParameter gaPara, List<Chromosome> solutions)
        {
            // gaPara 调整最大最小值
            foreach (var idx in FixedIdx)// 调整上下边界
            {
                gaPara.MaxValues[idx] = 0;
                gaPara.MinValues[idx] = 0;
            }
            // 根据solutions 创建多个基因组
            var results = new List<Chromosome>();
            foreach(var solution in solutions)
            {
                results.Add(TransPreSol(gaPara, solution));
            }
            return results;
        }
        public Chromosome TransPreSol(GaParameter gaPara,Chromosome solution)
        {
            // solution 之前的解,获取value
            // 生成对应的新的Chromosome

            var res = new Chromosome();
            var NewGenome = new List<Gene>();
            for (int i = 0; i < NewSegLines.Count; ++i)
            {
                var GAidx = NewToOrgMap[i];
                var value = solution.Genome[GAidx].Value;
                var line = NewSegLines[i];
                // 获取打断后分割线start val/end val
                var dir = line.GetValue(out double _value, out double startVal, out double endVal);
                Gene gene = new Gene(value, dir, gaPara.MinValues[i], gaPara.MaxValues[i], startVal, endVal);
                NewGenome.Add(gene);
            }
            res.Genome = NewGenome;
            return res;
        }

        private void premove(double dist = 1)
        {
            var dist1 = dist * 1.1;
            // 先出头
            foreach (var index in VertLines_index)
            {
                var line = SegLines[index];
                var MaxY = Math.Max(line.StartPoint.Y, line.EndPoint.Y);
                var MinY = Math.Min(line.StartPoint.Y, line.EndPoint.Y);
                var spt = new Point3d(line.StartPoint.X, MinY - dist1, 0);
                var ept = new Point3d(line.StartPoint.X, MaxY + dist1, 0);
                SegLines[index] = new Line(spt, ept);
            }
            foreach (var index in HorzLines_index)
            {
                var line = SegLines[index];
                var MaxX = Math.Max(line.StartPoint.X, line.EndPoint.X);
                var MinX = Math.Min(line.StartPoint.X, line.EndPoint.X);
                var spt = new Point3d(MinX - dist1, line.StartPoint.Y, 0);
                var ept = new Point3d(MaxX + dist1, line.StartPoint.Y, 0);
                SegLines[index] = new Line(spt, ept);
            }

            ////预位移，避免同向线相交
            //double curdist;
            //if (VerticalDirection)//垂直打断，平移横向线
            //{
            //    var stepsize = (2 * dist) / (HorzLines_index.Count - 1);//均匀步长
            //    for (int i = 0;i< HorzLines_index.Count; i++)
            //    {
            //        var index = HorzLines_index[i];
            //        var line = SegLines[index];
            //        curdist = stepsize * i - dist;//当前位置
            //        var spt = new Point3d(line.StartPoint.X, line.StartPoint.Y + curdist, 0);
            //        var ept = new Point3d(line.EndPoint.X, line.EndPoint.Y + curdist, 0);
            //        SegLines[index] = new Line(spt, ept);
            //    }
            //}
            //else//横向打断，纵向平移
            //{
            //    var stepsize = (2 * dist) / (VertLines_index.Count - 1);
            //    for (int i = 0; i < VertLines_index.Count; i++)
            //    {
            //        var index = VertLines_index[i];
            //        var line = SegLines[index];
            //        curdist = stepsize * i - dist;
            //        var spt = new Point3d(line.StartPoint.X+ curdist, line.StartPoint.Y , 0);
            //        var ept = new Point3d(line.EndPoint.X+ curdist, line.EndPoint.Y , 0);
            //        SegLines[index] = new Line(spt, ept);
            //    }
            //}
        }
        private void precut(double prop = 0.8)
        {
            ;// 留下的出头比例
            for(int i = 0; i < SegLines.Count; ++i)
            {
                var line1 = SegLines[i];
                for (int j =i;j< SegLines.Count; ++j)
                {
                    if (i == j) continue;
                    var line2 = SegLines[j];
                    //找交点
                    var templ = line1.Intersect(line2, Intersect.OnBothOperands);
                    if (templ.Count!= 0)
                    {
                        var pt = templ.First();//交点
                        if (!WallLine.Contains(pt))//交点在边界外，需要切割
                        {
                            //点不在区域内部，需要切割
                            //1.找到在边界上距离pt最近的点

                            //line1在边界上距离pt最近的点
                            var dis1 = GetNearestOnWall(line1, pt, out Point3d wpt1);

                            //line2在边界上距离pt最近的点
                            var dis2 = GetNearestOnWall(line2, pt, out Point3d wpt2);

                            ////选伸出边界较长的一根切割
                            //if(dis1 > dis2)
                            //{
                            //    CutLine(ref line1, wpt1, pt, prop);
                            //    SegLines[i] = line1;
                            //}
                            //else
                            //{
                            //    CutLine(ref line2, wpt2, pt, prop);
                            //    SegLines[j] = line2;
                            //}

                            //两根都切一下
                            CutLine(ref line1, wpt1, pt, prop);
                            SegLines[i] = line1;
                            CutLine(ref line2, wpt2, pt, prop);
                            SegLines[j] = line2;
                        }
                    }
                }
            }
        }
        private void CutLine(ref Line line, Point3d wpt, Point3d pt,double prop)
        {
            // 把line切割,在边界外的线只保留交点和墙线之前部分的prop比例的线
            // wpt 在墙线上的点
            // pt 在外部的交点
            // prop 切断后留的比例
            var spt = line.StartPoint;
            var ept = line.EndPoint;
            double dis = pt.DistanceTo(wpt);
            Point3d tempPT;// 切割后的线的一个端点
            if (IsVertical(line))
            {
                if(wpt.Y < pt.Y)
                {
                    tempPT = new Point3d(wpt.X,wpt.Y+ dis * prop,0);
                }
                else
                {
                    tempPT = new Point3d(wpt.X, wpt.Y - dis * prop, 0);
                }
            }
            else
            {
                if (wpt.X < pt.X)
                {
                    tempPT = new Point3d(wpt.X + dis * prop, wpt.Y, 0);
                }
                else
                {
                    tempPT = new Point3d(wpt.X - dis * prop, wpt.Y, 0);
                }
            }
            if (spt.DistanceTo(wpt) < spt.DistanceTo(pt))
            {
                // 起始点在保留的一侧
                line.EndPoint = tempPT;
            }
            else
            {
                // 终点在保留的一侧
                line.StartPoint = tempPT;
            }
        }
        private double GetNearestOnWall(Line line,Point3d IntPt,out Point3d wpt)
        {
            var templ = line.Intersect(WallLine, Intersect.OnBothOperands);
            if (templ.Count == 0) throw new ArgumentException("线不与边界相交");
            if (templ.Count == 1)
            {
                wpt = templ.First();
                return IntPt.DistanceTo(wpt);
            }
                
            else
            {
                if (IsVertical(line))
                {
                    templ = templ.OrderBy(pt => pt.Y).ToList();
                    if (IntPt.DistanceTo(templ.First()) < IntPt.DistanceTo(templ.Last()))
                    {
                        wpt = templ.First();
                    }
                    else
                    {
                        wpt = templ.Last();
                    }
                    return IntPt.DistanceTo(wpt);
                }
                else
                {
                    templ = templ.OrderBy(pt => pt.X).ToList();
                    if (IntPt.DistanceTo(templ.First()) < IntPt.DistanceTo(templ.Last()))
                    {
                        wpt = templ.First();
                    }
                    else
                    {
                        wpt = templ.Last();
                    }
                    return IntPt.DistanceTo(wpt);
                }
            }
        }
        private void UpdateNewSegLines(List<int> OtherLinesIdx)
        {
            NewSegLines = new List<Line>();
            VarIdx = new List<int>();
            FixedIdx = new List<int>();
            NewToOrgMap = new List<int>();
            for (int i = 0; i < BreakedLines.Count; ++i)
            {
                NewSegLines.Add(BreakedLines[i]);
                VarIdx.Add(i);
                NewToOrgMap.Add(BreakedLinesIdxs[i]);
            }
            for(int j=0; j < OtherLines.Count; ++j)
            {
                NewSegLines.Add(OtherLines[j]);
                FixedIdx.Add(j + BreakedLines.Count);
                NewToOrgMap.Add(OtherLinesIdx[j]);
            }
        }
        private bool IsVertical(Line line)
        {
            if (Math.Abs(line.StartPoint.X - line.EndPoint.X) < 1e-4) return true;
            else return false;
        }
        private void ModifyLines(double size = 1)
        {
            //1.获取所有断线的最大最小范围
            int direction = 1;
            double dist;
            for(int i = 0; i < BreakedLines.Count; ++i)
            {
                //var BreakedLine = BreakedLines[i];
                var spt = BreakedLines[i].StartPoint;
                var ept = BreakedLines[i].EndPoint;
                //var OrgIdx = BreakedLinesIdxs[i];
                var CorssLineIdxs = CorssIdxs[i];// 需要延展的线
                if (CorssLineIdxs.First() == ImpossibleIdx) continue;//该线不需要操作
                //GetBoundary(OrgIdx, out double LowerBound, out double UpperBound);//当前断线的上下限,目前不做判断
                // 假设在 +/- size 范围内平移线都可以
                dist = direction * size;
                direction *= -1;
                // 平移+ 拉伸
                if (VerticalDirection)
                {
                    BreakedLines[i].StartPoint =new Point3d(spt.X + dist, spt.Y, 0);
                    BreakedLines[i].EndPoint = new Point3d(ept.X + dist, ept.Y, 0);
                }
                else
                {
                    BreakedLines[i].StartPoint = new Point3d(spt.X, spt.Y + dist, 0);
                    BreakedLines[i].EndPoint = new Point3d(ept.X, ept.Y + dist, 0);
                }

                foreach(int idx in CorssLineIdxs)
                {
                    var crossline = SegLines[idx];
                    var templ = BreakedLines[i].Intersect(crossline, Intersect.OnBothOperands);
                    if (templ.Count == 0)
                    {
                        // 平移后未相交
                        var pt = crossline.Intersect(BreakedLines[i], Intersect.ExtendThis).First();//延长crossline,得到交点
                                                                                                    // 将crossline线延长
                        if (pt.DistanceTo(crossline.StartPoint) > pt.DistanceTo(crossline.EndPoint)) SegLines[idx].EndPoint = pt;
                        else SegLines[idx].StartPoint = pt;
                    }
                }
            }
        }
        private bool GetBoundary(int i, out double LowerBound, out double UpperBound)
        {
            // 判断分割线的边界
            // get absolute coordinate of segline
            var line = GaPara.SegLine[i];
            var dir = line.GetValue(out double value, out double startVal, out double endVal);
            LowerBound = GaPara.MinValues[i] + value;
            UpperBound = GaPara.MaxValues[i] + value;
            if (GaPara.MaxValues[i] > GaPara.MinValues[i]) return true;
            else return false;
        }

        private void GetSptEpt(Line Line1, out Point3d spt, out Point3d ept)
        {
            //确定起点以及终点
            //起始点spt
            //终结点ept
            // 纵向逻辑
            if (GoPositive)//从小到大排序
            {
                if( (Line1.StartPoint.Y > Line1.EndPoint.Y && VerticalDirection) || (Line1.StartPoint.X > Line1.EndPoint.X && ! VerticalDirection))
                {
                    spt = Line1.EndPoint;
                    ept = Line1.StartPoint;
                }
                else
                {
                    spt = Line1.StartPoint;
                    ept = Line1.EndPoint;
                }
            }
            else//从大到小排序
            {
                if ((Line1.StartPoint.Y > Line1.EndPoint.Y && VerticalDirection ) || (Line1.StartPoint.X > Line1.EndPoint.X && !VerticalDirection) )
                {
                    spt = Line1.StartPoint;
                    ept = Line1.EndPoint;
                }
                else
                {
                    spt = Line1.EndPoint;
                    ept = Line1.StartPoint;
                }
            }
        }
        private void GetAllIntSecPoints(Line line1, List<int> OtherLinesIdx, out List<Point3d> IntSecPoints, out List<int> IntSecIndex)
        {
            IntSecPoints = new List<Point3d>();//断点列表
            var newIntSecIndex = new List<int>();//当前纵线的所有断点索引
            var templ = line1.Intersect(WallLine, Intersect.OnBothOperands);
            if (templ.Count != 0)//初始线和外包框有交点
            {
                if (templ.Count < 2)
                {
                    foreach (var pt in templ)
                    {
                        IntSecPoints.Add(pt);
                        newIntSecIndex.Add(ImpossibleIdx);//添加不可能值，填充作用
                    }
                }
                else
                {
                    if (VerticalDirection) templ = templ.OrderBy(i => i.Y).ToList();// 垂直order by Y
                    else templ = templ.OrderBy(i => i.X).ToList();//水平orderby X
                    IntSecPoints.Add(templ.First());
                    newIntSecIndex.Add(ImpossibleIdx);
                    IntSecPoints.Add(templ.Last());
                    newIntSecIndex.Add(ImpossibleIdx);
                }
            }
            for (int j = 0; j < OtherLinesIdx.Count; ++j)
            {
                var line2 = SegLines[OtherLinesIdx[j]];//初始交叉分割线
                templ = line1.Intersect(line2, Intersect.OnBothOperands);
                if (templ.Count != 0)//初始线之间有交点
                {
                    newIntSecIndex.Add(OtherLinesIdx[j]);// 交叉线绝对索引
                    IntSecPoints.Add(templ.First());
                }
            }
            // sort ptlist and IntersectLines(base on pt list)按照纵坐标排序，两种排序可以都试一下，保留最合理排序
            List<KeyValuePair<Point3d, int>> sorted;//获得排序，以及index，存在key value pair中
            if (GoPositive)//从小到大排序
            {
                if (VerticalDirection)
                {
                    sorted = IntSecPoints.Select((x, i) => new KeyValuePair<Point3d, int>(x, i)).OrderBy(x => x.Key.Y).ToList();
                }
                else
                {
                    sorted = IntSecPoints.Select((x, i) => new KeyValuePair<Point3d, int>(x, i)).OrderBy(x => x.Key.X).ToList();
                }
                
            }
            else//从大到小排序
            {
                if (VerticalDirection)
                {
                    sorted = IntSecPoints.Select((x, i) => new KeyValuePair<Point3d, int>(x, i)).OrderByDescending(x => x.Key.Y).ToList();
                }
                else
                {
                    sorted = IntSecPoints.Select((x, i) => new KeyValuePair<Point3d, int>(x, i)).OrderByDescending(x => x.Key.X).ToList();
                }

            }
            IntSecPoints = sorted.Select(x => x.Key).ToList();
            var idx = sorted.Select(x => x.Value).ToList();// 索引值

            IntSecIndex = idx.Select(x => newIntSecIndex[x]).ToList();
        }
        private void Clear()//清空非托管资源
        {
        }
        public void Dispose()
        {
            Clear();
        }
    }


    static class Functions
    {   // Note： 分割线打断排布会使用之前的参数（种群数和代数）,对种群数做调整

        // solution: 当前发现的最优解
        // verticaldirection: 是否纵向打断
        // Orgsolutions: 之前发现的优解
        // specialOnly:是否只使用特殊基因
        // gopositive: 是否按坐标增加顺序打断
        // layoutPara: 返回值，当前打断方案对应的LayoutParameter
        public static LayoutParameter BreakAndOptimize(List<Line> sortedSegLines, OuterBrder outerBrder, ParkingStallArrangementViewModel ParameterViewModel, Serilog.Core.Logger Logger,
            out Chromosome solution,bool verticaldirection = true, List<Chromosome> Orgsolutions = null,bool specialOnly = false, bool gopositive = true)// 打断，赋值，再迭代,默认正方向打断
        {
            outerBrder.SegLines = sortedSegLines;// 之前的分割线
            var GaPara = new GaParameter(sortedSegLines);

            var segbkparam = new SegBreak(outerBrder, GaPara, verticaldirection, gopositive);// 打断操作
            outerBrder.SegLines = new List<Line>();
            segbkparam.NewSegLines.ForEach(l => outerBrder.SegLines.Add(l.Clone() as Line));// 复制打断后的分割线
            //bool usePline = ParameterViewModel.UsePolylineAsObstacle;
            Preprocessing.DataPreprocessing(outerBrder, out GaParameter gaPara, out LayoutParameter layoutPara, Logger, false);

            ParkingStallGAGenerator geneAlgorithm;
            if (Orgsolutions != null)
            {
                // gaparam 赋值
                var initgenomes = segbkparam.TransPreSols(ref gaPara, Orgsolutions);// orgsolutions 之前迭代结果
                geneAlgorithm = new ParkingStallGAGenerator(gaPara, layoutPara, ParameterViewModel, initgenomes);
            }
            else
            {
                geneAlgorithm = new ParkingStallGAGenerator(gaPara, layoutPara, ParameterViewModel);
            }
            geneAlgorithm.Logger = Logger;
            var rst = new List<Chromosome>();
            var histories = new List<Chromosome>();
            bool recordprevious = false;
            try
            {
                rst = geneAlgorithm.Run2(histories, recordprevious, specialOnly);
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
            }
            solution = rst.First();
            return layoutPara;
#if (DEBUG)
            string layer;
            if (verticaldirection) layer = "AI-垂直打断后初始分割线-Debug";
            else layer = "AI-水平打断后初始分割线-Debug";
            Draw.DrawSeg(segbkparam.NewSegLines, layer);
            Draw.DrawSeg(sortedSegLines, "AI-打断前分割线-Debug");
#endif
        }

        public static bool IsVertical(this Line line)
        {
            if (Math.Abs(line.StartPoint.X - line.EndPoint.X) < 1e-4) return true;
            else return false;
        }
    }
}
