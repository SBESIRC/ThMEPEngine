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
using static ThMEPArchitecture.ParkingStallArrangement.ParameterConvert;
using System.Text.RegularExpressions;
using DotNetARX;
using ThMEPArchitecture.ParkingStallArrangement.General;
using ThMEPArchitecture.ViewModel;
namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{

    public class SegBreakParam : IDisposable
    {
        public List<Line> BreakedLines;//所有打断后的分割线，要导入迭代之后的值
        public List<Line> OtherLines;//其余所有线 （纵向打断则为横线，横向打断则为纵线）,要迭代之后的
        public List<double> MinValues;//打断线的最大值
        public List<double> MaxValues;//最小值
        public int LineCount;
        double BufferSize;// 寻找最大，最小值时候的范围,默认无限大，不然有逻辑问题
        private double MaxBufferSize;
        bool VerticalDirection;
        public List<Polyline> BufferTanks;// 记录当前已做的buffer，需要判断是否相互重合
        private List<Line> OtherGALine1s;// 其他同向线 ，对应GAline1
        private int RoadWidth;
        private ThCADCoreNTSSpatialIndex buildLinesSpatialIndex;// 障碍物
        private Polyline WallLine;
        private List<Line> SegLines;
        private List<Line> GALines;
        private int ImpossibleIdx = int.MaxValue;
        private void Clear()//清空非托管资源
        { 
        }
        //输入，初始分割线，以及打断的方向。输出，分割线与其交点
        public SegBreakParam( List<Gene> Genome, OuterBrder outerbrder,bool verticaldirection, bool GoPositive, ParkingStallArrangementViewModel parameterViewModel = null, double? buffersize = null)
        {
            buildLinesSpatialIndex = new ThCADCoreNTSSpatialIndex(outerbrder.BuildingLines);
            BufferTanks = new List<Polyline>();
            GALines = new List<Line>();// 所有GA 转换来的分割线
            Genome.ForEach(gene => GALines.Add(gene.ToLine()));
            WallLine = outerbrder.WallLine;

            VerticalDirection = verticaldirection;//垂直方向为true，水平为false
            if (parameterViewModel is null) RoadWidth = 5500;
            else RoadWidth = parameterViewModel.RoadWidth.Copy();
            // SegLine 初始分割线，必须严格符合相交关系
            // WallLine 地库框线
            // GASolution GA的结果，任意相交关系
            // GoPositive 从下至上打断，从左至右打断（坐标增加顺序）
            //List<Line> VertLines = new List<Line>();//垂直线
            List<int> VertLines_index = new List<int>();// 垂直线索引
            //List<Line> HorzLines = new List<Line>();//水平线
            List<int> HorzLines_index = new List<int>();// 垂直线索引
            SegLines = outerbrder.SegLines;//所有初始分割线

            InitBufferSizes(buffersize);//更新BufferSize 和MaxBufferSize
            for (int i = 0; i < SegLines.Count; ++i)
            {
                var line = SegLines[i];
                if (Math.Abs(line.StartPoint.X - line.EndPoint.X) < 1)
                {
                    //横坐标相等，平行线
                    //VertLines.Add(Genome[i].ToLine());
                    VertLines_index.Add(i);
                }
                else if (Math.Abs(line.StartPoint.Y - line.EndPoint.Y) < 1)
                {
                    //HorzLines.Add(Genome[i].ToLine());
                    HorzLines_index.Add(i);
                }
                else throw new ArgumentException("Invaild Segline" +i.ToString() + "detected: SegLines must be Vertical or Horizontal!");
            }    
            //var sortedH = HorzLines.Select((x, i) => new KeyValuePair<Line, int>(x, i)).OrderBy(x => x.Key.StartPoint.Y).ToList();
            //HorzLines = sortedH.Select(x => x.Key).ToList();
            //var idxH = sortedH.Select(x => x.Value).ToList();// 索引值
            //                                               // 把IntSecIndex 按照IntersectLines 排序
            //var newHidx = new List<int>();
            //idxH.ForEach(i => newHidx.Add(HorzLines_index[i]));
            //HorzLines_index = newHidx;

            //var sortedV = VertLines.Select((x, i) => new KeyValuePair<Line, int>(x, i)).OrderBy(x => x.Key.StartPoint.Y).ToList();
            //VertLines = sortedV.Select(x => x.Key).ToList();
            //var idxV = sortedV.Select(x => x.Value).ToList();// 索引值
            //                                               // 把IntSecIndex 按照IntersectLines 排序
            //var newVidx = new List<int>();
            //idxV.ForEach(i => newVidx.Add(VertLines_index[i]));
            //VertLines_index = newVidx;

            BreakedLines = new List<Line>();

            MinValues = new List<double>();// 打断线最小值，相对值
            MaxValues = new List<double>();// 打断线最大值，相对值

            OtherLines = new List<Line>();// 记录与打断线垂直方向的线
            if (VerticalDirection)
            {
                HorzLines_index.ForEach(Hidx => OtherLines.Add(Genome[Hidx].ToLine()));// update otherlines
                // otherlines 添加横向线
                //打断纵向线
                //对于所有纵线
                //预切割，如果起始或终点在建筑框线之内，只保留分割线之间的部分
                for (int n = 0; n < VertLines_index.Count; ++n)
                {
                    var line1 = SegLines[VertLines_index[n]];//初始纵向分割线
                    var GALine1 = GALines[VertLines_index[n]];//迭代后的纵分割线
                    GetAllIntSecPoints(line1, GALine1, HorzLines_index, true, out List<Point3d> IntSecPoints, out List<int> IntSecIndex);//获取全部交点
                    GetSptEpt(GALine1, true, out Point3d spt, out Point3d ept);//确定起点以及终点
                    GALine1.Dispose();
                    if (IntSecIndex.First() != ImpossibleIdx) spt = IntSecPoints.First();// 起点不在建筑框外 
                    if (IntSecIndex.Last() != ImpossibleIdx) ept = IntSecPoints.Last();// 终点不在建筑框外 
                    GALines[VertLines_index[n]] = new Line(spt, ept);
                }
                for (int k = 0; k < VertLines_index.Count; ++k)
                {
                    var line1 = SegLines[VertLines_index[k]];//初始纵向分割线
                    var GALine1 = GALines[VertLines_index[k]];//迭代后的纵分割线
                    
                    GetSptEpt(GALine1, GoPositive, out Point3d spt, out Point3d ept);//确定起点以及终点

                    OtherGALine1s?.ForEach(l => l.Dispose());
                    OtherGALine1s = new List<Line>();// 其他的纵分割线 TO DO otherlines 需要处理，切掉伸出去的部分
                    for (int g = 0;g< VertLines_index.Count; ++g)
                    {
                        //其他纵分割线
                        if (g != k)
                        {
                            OtherGALine1s.Add(new Line(GALines[VertLines_index[g]].StartPoint, GALines[VertLines_index[g]].EndPoint));
                        }
                    }
                    // IntSecPoints 所有交点，IntSecIndex交点对应的横向线索引
                    GetAllIntSecPoints(line1, GALine1, HorzLines_index, GoPositive, out List<Point3d> IntSecPoints, out List<int> IntSecIndex);
                    if(IntSecPoints.Count < 2) throw new ArgumentException("Invaild Segline" + VertLines_index[k].ToString() + "detected: SegLine must have at lest two intersections!");

                    List<int> InnerIndex = new List<int>();// 记录在当前断线上所有横线的索引

                    //if (IntersectLines[0].Length > 1e-5)//起始如果不贯穿边界，需要添加当前断线上横线的索引,后续更新横线范围
                    if(IntSecIndex[0] != ImpossibleIdx)//起始如果不贯穿边界，需要添加当前断线上横线的索引,后续更新横线范围
                    {
                        InnerIndex.Add(0);
                        spt = IntSecPoints[0];// 且起点在分割线上
                    }
                    if (IntSecPoints.Count > 2)// 至少有一个中间断点可打断 
                    {
                        List<Line> cur_breakedlines = new List<Line>();// 打断后的纵线list
                        List<double> cur_minvalues = new List<double>();// 打断线的下边界
                        List<double> cur_maxvalues = new List<double>();// 打断线的上边界
                        // 该纵线打断
                        // 2. 确定打断后的纵线
                        int CurCount = 0;
                        
                        Line BreakLine;
                        Line BufferLine;
                        for (int i = 1; i < IntSecPoints.Count-1; ++i)// 遍历中间的所有可能断点，不包含边界上的断点
                        {
                            // 双指针确定断线
                            if (CurCount > 0 || IntSecPoints.Count == 3)// 3个断点则中间需要打断,已经跳过一个点，也做打断。
                            {
                                // 新断线
                                InnerIndex.Add(i);
                                BreakLine = new Line(spt, IntSecPoints[i]);
                                    
                                cur_breakedlines.Add(BreakLine);

                                BufferLine = new Line(spt, IntSecPoints[i]);

                                // 确定断线范围，buffer，取建筑或者buffer值（添加 Lower & Upper Bound to Lower & upper Bounds)
                                GetMaxMinValue(BufferLine, outerbrder, out double MinValue, out double MaxValue);// TO DO,不应该用breakline 求最大最小值，判断breakline是否过边线
                                cur_minvalues.Add(MinValue);
                                cur_maxvalues.Add(MaxValue);
                                // 更新断线上所有横向线（拉伸，覆盖断线的最大范围）
                                var Value = BreakLine.StartPoint.X;
                                ModifyOtherLines(IntSecIndex,InnerIndex, MinValue, MaxValue, Value);

                                spt = IntSecPoints[i];//更新起始点
                                //重新计数
                                InnerIndex = new List<int>();
                                InnerIndex.Add(i);
                                CurCount = 0;

                            }
                            else 
                            {
                                CurCount += 1;
                                InnerIndex.Add(i);
                            }
                        }
                        // 添加最后的断线,连接终点与上一个断点
                        //终点如果不贯穿边界，需要添加当前断线上横线的索引,后续更新横线范围
                        if(IntSecIndex.Last() != ImpossibleIdx)
                        {
                            InnerIndex.Add(IntSecPoints.Count - 1);
                            ept = IntSecPoints.Last();// 且终点在分割线上
                        }
                        BreakLine = new Line(spt,ept);
                        cur_breakedlines.Add(BreakLine);

                        BufferLine = new Line(spt, ept);//最后断线，BufferLine以第最后断点作为终点

                        GetMaxMinValue(BufferLine, outerbrder, out double MinValue2, out double MaxValue2);// TO DO
                        cur_minvalues.Add(MinValue2);
                        cur_maxvalues.Add(MaxValue2);
                        // 更新断线上所有横向线（拉伸，覆盖断线的最大范围）
                        var Value2 = BreakLine.StartPoint.X;
                        ModifyOtherLines(IntSecIndex, InnerIndex, MinValue2, MaxValue2, Value2);

                        //将新的断线添加到所有断线
                        BreakedLines.AddRange(cur_breakedlines);
                        MinValues.AddRange(cur_minvalues);
                        MaxValues.AddRange(cur_maxvalues);
                    }
                    else// 虽然不能打断，但是也要迭代
                    {
                        if( IntSecIndex[1] != ImpossibleIdx)//终点如果不贯穿边界，需要添加当前断线上横线的索引,后续更新横线范围
                        {
                            InnerIndex.Add(1);
                        }
                        BreakedLines.Add(GALine1);
                        if (IntSecIndex[0] != ImpossibleIdx) spt = IntSecPoints[0];
                        if (IntSecIndex.Last() != ImpossibleIdx) ept = IntSecPoints.Last();
                        var BufferLine = new Line(spt, ept);
                        GetMaxMinValue(BufferLine, outerbrder, out double MinValue, out double MaxValue);
                        
                        // 更新断线上所有横向线（拉伸，覆盖断线的最大范围）
                        var Value2 = GALine1.StartPoint.X;
                        ModifyOtherLines(IntSecIndex, InnerIndex, MinValue, MaxValue, Value2);
                    }
                    
                }
            }
            else
            {
                ;//与上面对称
            }

            LineCount = BreakedLines.Count;
            ;
        }

        private void InitBufferSizes(double? buffersize = null)
        {
            var pts = WallLine.GetPoints().ToList();
            if (VerticalDirection) //横向buffer
            {
                pts = pts.OrderBy(e => e.X).ToList();
                MaxBufferSize = pts.Last().X - pts.First().X + (RoadWidth / 2);
            }
            else
            {
                pts = pts.OrderBy(e => e.Y).ToList();
                MaxBufferSize = pts.Last().Y - pts.First().Y + (RoadWidth / 2);
            }
            BufferSize = MaxBufferSize.Copy();
            if (!(buffersize is null))
            {
                if (MaxBufferSize > (double)buffersize)
                {
                    BufferSize = (double)buffersize;
                }
            }
        }

        private void GetSptEpt(Line GALine1,bool GoPositive,out Point3d spt,out Point3d ept)
        {
            //确定起点以及终点
            //起始点spt
            //终结点ept
            // 纵向逻辑
            if (GoPositive)//从小到大排序
            {
                if (GALine1.StartPoint.Y > GALine1.EndPoint.Y)
                {
                    spt = GALine1.EndPoint;
                    ept = GALine1.StartPoint;
                }
                else
                {
                    spt = GALine1.StartPoint;
                    ept = GALine1.EndPoint;
                }
            }
            else//从大到小排序
            {
                if (GALine1.StartPoint.Y > GALine1.EndPoint.Y)
                {
                    spt = GALine1.StartPoint;
                    ept = GALine1.EndPoint;
                }
                else
                {
                    spt = GALine1.EndPoint;
                    ept = GALine1.StartPoint;
                }
            }
        }
        // 获取GALine1的与边框或者其他分割线的交点。交点保持初始分割线的相交关系。
        private void GetAllIntSecPoints(Line line1, Line GALine1,List<int> HorzLines_index,bool GoPositive,out List<Point3d> IntSecPoints,out List<int> IntSecIndex)
        {
            IntSecPoints = new List<Point3d>();//断点列表
            //List<Line> IntersectLines = new List<Line>();//交叉线列表，需要动态更新
            var newIntSecIndex = new List<int>();//当前纵线的所有断点索引
            var templ = line1.Intersect(WallLine, Intersect.OnBothOperands);
            if (templ.Count != 0)//初始线和外包框有交点
            {
                var ptl = GALine1.Intersect(WallLine, Intersect.OnBothOperands);// GA 结果求交点,如果多于2个点，取最外两个点
                if (ptl.Count < 2)
                {
                    foreach (var pt in ptl) 
                    {
                        IntSecPoints.Add(pt);
                        //IntersectLines.Add(new Line(pt, pt));// 创建长度为0的线，保证两个list结构一样
                        newIntSecIndex.Add(ImpossibleIdx);//添加不可能值，填充作用
                    }
                }
                else
                {
                    if (VerticalDirection) ptl = ptl.OrderBy(i => i.Y).ToList();// 垂直order by Y
                    else ptl = ptl.OrderBy(i => i.X).ToList();//水平orderby X
                    IntSecPoints.Add(ptl.First());
                    newIntSecIndex.Add(ImpossibleIdx);
                    IntSecPoints.Add(ptl.Last());
                    newIntSecIndex.Add(ImpossibleIdx);
                }
            }
            // 纵向逻辑:
            for (int j = 0; j < HorzLines_index.Count; ++j)
            {
                var line2 = SegLines[HorzLines_index[j]];//初始横向分割线
                templ = line1.Intersect(line2, Intersect.OnBothOperands);
                if (templ.Count != 0)//初始线之间有交点
                {
                    var GALine2 = GALines[HorzLines_index[j]];//迭代后的横分割线
                    //IntersectLines.Add(GALine2);// 记录产生相交的线，后续需要更新
                    newIntSecIndex.Add(j);// 添加索引，方便更新OtherLines
                                       // 说明这俩线有交点，用GA的结果求交点
                    var pt = GALine1.Intersect(GALine2, Intersect.ExtendBoth);
                    IntSecPoints.Add(pt.First());
                }
            }
            // sort ptlist and IntersectLines(base on pt list)按照纵坐标排序，两种排序可以都试一下，保留最合理排序
            List<KeyValuePair<Point3d, int>> sorted;//获得排序，以及index，存在key value pair中
            if (GoPositive)//从小到大排序
            {
                sorted = IntSecPoints.Select((x, i) => new KeyValuePair<Point3d, int>(x, i)).OrderBy(x => x.Key.Y).ToList();
            }
            else//从大到小排序
            {
                sorted = IntSecPoints.Select((x, i) => new KeyValuePair<Point3d, int>(x, i)).OrderByDescending(x => x.Key.Y).ToList();
            }
            IntSecPoints = sorted.Select(x => x.Key).ToList();
            var idx = sorted.Select(x => x.Value).ToList();// 索引值
            // 把IntSecIndex 按照 IntSecPoints 排序
            //var newIndex = new List<int>();
            //idx.ForEach(i => newIndex.Add(IntSecIndex[i]));
            //IntSecIndex = newIndex;
            IntSecIndex = idx.Select(x => newIntSecIndex[x]).ToList();
        }
        private void ModifyOtherLines(List<int> IntSecIndex, List<int> InnerIndex,double MinValue, double MaxValue,double Value)
        {
            // 更新横向线，覆盖最大最小值
            // Value 是纵向线的横坐标
            foreach(int i in InnerIndex)
            {
                if (VerticalDirection)
                {
                    var idx = IntSecIndex[i];// 需要改动的横线idx
                    
                    var line = OtherLines[idx];//交叉的线是横向线
                    Point3d spt;
                    Point3d ept;
                    if (line.StartPoint.X< line.EndPoint.X)
                    {
                        spt = line.StartPoint;
                        ept = line.EndPoint;
                    }
                    else
                    {
                        spt = line.EndPoint;
                        ept = line.StartPoint;
                    }

                    if(spt.X > Value + MinValue)
                    {
                        spt = new Point3d(Value + MinValue, spt.Y,0);
                    }
                    if (ept.X < Value + MaxValue)
                    {
                        ept = new Point3d(Value + MaxValue, ept.Y, 0);
                    }
                    OtherLines[idx] = new Line(spt,ept);
                }
                else
                {
                    //对称逻辑
                    //TO DO
                }
                
            }
        }
        private void GetMaxMinValue(Line BufferLine, OuterBrder outerbrder, out double MinValue, out double MaxValue)
        {
            Polyline Buffer;
            //TO DO 用BreakLine，outerbrder 以及BufferSize 求最大最小值（相对值）
            // 然后判断和其他buffer是否有交集，如果有可能减少buffersize，再做运算
            //算法一：(如果指定buffersize）（必须确保与其他buffer不会相交）（不考虑分割线）
            //1.用起始线，以及向坐标减少方向做矩形，如果框选到障碍物，则以建筑最近点 - 初始线点 - 半个车道宽作为下边界（minvalue)。
            //未框选到则以-buffersize 作为边界
            //2. 上边界同理

            //算法二：若未指定buffersize，使用无穷buffer
            //1.用起始线，以及向坐标减少方向做矩形，如果“先”框选到障碍物，则以建筑最近点 - 初始线点 - 半个车道宽作为下边界（minvalue)。
            //2.如果先框选到其他初始分割线(OtherGAlines，则以两个初始分割线的终点 - 半个车道宽作为边界
            //3.另一个方向同理
            //2.框选建筑

            // 高级算法三：
            // 不论是否指定buffersize，先执行算法二，计算分割线的最大上下边界，做记录
            // 如果边界大于buffersize,取buffersize作为边界
            // 否则取分割线的最大上下边界

            MaxValue = 0;
            MinValue = 0;
            if (VerticalDirection)
            {
                //1.正方向拿buffer
                var BuildingDis = GetDisToBuilding(BufferLine, MaxBufferSize, true);// 距离建筑的最短距离
                var LineDis = MinBufferDisToRestLines(BufferLine, true);
                var TempBufferSize = Math.Min(BuildingDis, LineDis);//与其他建筑和分割线的最短距离

                MaxValue = Math.Min(TempBufferSize, BufferSize) - (RoadWidth / 2);
                //1.反方向拿buffer
                BuildingDis = GetDisToBuilding(BufferLine, MaxBufferSize, false);// 距离建筑的最短距离
                LineDis = MinBufferDisToRestLines(BufferLine, false);
                TempBufferSize = Math.Min(BuildingDis, LineDis);//与其他建筑和分割线的最短距离

                MinValue = -Math.Min(TempBufferSize, BufferSize) + (RoadWidth / 2);

                if (MaxValue - MinValue <= 0) 
                {
                    throw new ArgumentException("Invaild Initial Buffer Line");
                }
            }
            AddBufferTank(BufferLine, MinValue, MaxValue);
        }
        private void AddBufferTank(Line BufferLine,double MinValue,double MaxValue)
        {
            // 当前buffer
            if (VerticalDirection)//纵向线
            {
                var points = new List<Point2d> {new Point2d(BufferLine.StartPoint.X + MinValue, BufferLine.StartPoint.Y) ,
                                                new Point2d(BufferLine.StartPoint.X + MaxValue, BufferLine.StartPoint.Y) ,
                                                new Point2d(BufferLine.EndPoint.X + MaxValue, BufferLine.EndPoint.Y),
                                                new Point2d(BufferLine.EndPoint.X + MinValue, BufferLine.EndPoint.Y)};
                var pline = new Polyline();
                pline.CreatePolyline(points.ToArray());
                pline.Closed = true;

                BufferTanks.Add(pline);
            }
            else
            {
                //对称逻辑
            }
        }

        private Polyline GetBuffer(Line BufferLine,double buffersize, bool PositiveDirection)
        {
            // 获取bufferline往正或负方向的buffer
            var pline = new Polyline();
            if (VerticalDirection)
            {
                double distance;
                if (PositiveDirection) distance = buffersize;
                else distance = -buffersize;
                var points = new List<Point2d> {new Point2d(BufferLine.StartPoint.X , BufferLine.StartPoint.Y) ,
                                                new Point2d(BufferLine.StartPoint.X + distance, BufferLine.StartPoint.Y) ,
                                                new Point2d(BufferLine.EndPoint.X + distance, BufferLine.EndPoint.Y),
                                                new Point2d(BufferLine.EndPoint.X , BufferLine.EndPoint.Y)};
                pline.CreatePolyline(points.ToArray());
            }
            pline.Closed = true;
            return pline;
        }
        private double GetMinDist(Line line, Point3d pt)
        {
            var targetPt = line.GetClosestPointTo(pt, true);
            return pt.DistanceTo(targetPt);
        }
        private double GetDisToBuilding(Line BufferLine, double buffersize, bool PositiveDirection)
        {
            var buffer = GetBuffer(BufferLine, buffersize, PositiveDirection);
            var buildLines = buildLinesSpatialIndex.SelectCrossingPolygon(buffer);
            if (buildLines.Count == 0)
            {
                return buffersize;
            }
            var boundPt = BufferLine.GetBoundPt(buildLines);
            return GetMinDist(BufferLine, boundPt);
        }

        private bool BufferDisToLine(Line line1, Line line2, bool PositiveDirection,out double distance)
        {
            //distance of this line to another line
            // two line must be in same direction (vertical or horizontal) if not return null
            // 如果从this line到line two的矩形框交不到，也retrn null
            // 如果可以交到，return 矩形框伸出的长度
            double factor;
            distance = double.MaxValue;
            if (PositiveDirection) factor = 1;
            else factor = -1;
            if (VerticalDirection)
            {
                var X1 = line1.StartPoint.X;
                var X2 = line2.StartPoint.X;

                var SP1Y = line1.StartPoint.Y;
                var EP1Y = line1.EndPoint.Y;

                var SP2Y = line2.StartPoint.Y;
                var EP2Y = line2.EndPoint.Y;

                bool SP1in = (SP1Y > SP2Y && SP1Y < EP2Y) || (SP1Y < SP2Y && SP1Y > EP2Y);
                bool EP1in = (EP1Y > SP2Y && EP1Y < EP2Y) || (EP1Y < SP2Y && EP1Y > EP2Y);

                if (SP1in || EP1in)// 至少有一个点在范围内
                {
                    distance = factor * (X2 - X1);
                    if (distance > 0)
                    {
                        if (Math.Abs(distance) < 1e-5)
                        {
                            throw new ArgumentException("distance between lines is 0");
                        }
                        else return true;
                    }
                }
            }
            else
            {
                // 对称逻辑TO DO
            }
            return false;
        }

        private double MinBufferDisToRestLines(Line BufferLine,bool PositiveDirection)
        {
            //Buffer distance from Line1 to all other lines 
            double Mindis = double.MaxValue;
            bool FoundDistance;
            foreach(Line line in OtherGALine1s)
            {
                FoundDistance = BufferDisToLine(BufferLine,line,PositiveDirection,out double distance);
                if (FoundDistance && distance< Mindis)
                {
                    Mindis = distance;
                }
            }

            return Mindis/2;// 返回到其他线的中点
        }
        public void Dispose()
        {
            Clear();
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
                var tempT = new Tuple<double, double>(LowerBound, UpperBound);
                LowerUpperBound.Add(i, tempT);
            }
        }
        private void GetBoundary(int i, out double LowerBound, out double UpperBound)
        {
            // get absolute coordinate of segline
            var line = SegParam.BreakedLines[i];
            var dir = line.GetValue(out double value, out double startVal, out double endVal);
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
                    var line = SegParam.BreakedLines[i];
                    var dir = line.GetValue(out double value, out double startVal, out double endVal);
                    var valueWithIndex = value + SegParam.MaxValues[i];
                    Gene gene = new Gene(valueWithIndex, dir, SegParam.MinValues[i], SegParam.MaxValues[i], startVal, endVal);
                    genome.Add(gene);
                }
                else
                {
                    var line = SegParam.BreakedLines[i];
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
                var line = SegParam.BreakedLines[i];
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
