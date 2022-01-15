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
}
