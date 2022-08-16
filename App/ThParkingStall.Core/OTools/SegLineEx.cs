using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.OInterProcess;
using ThParkingStall.Core.Tools;
using static ThParkingStall.Core.Tools.ListEx;
namespace ThParkingStall.Core.OTools
{
    public static class SegLineEx
    {
        public static readonly double MaxDistance = 100000000;//最大距离，10公里
        public static readonly double SegTol = 0.1;//分割线容差，距离大于半车道宽 -0.1则判断满足车道宽
        public static readonly double ExtendTol = 1.0;//延长容差，分割线使用的延长或回缩容差
        public static readonly double OutSideTol = 10.0;//迭代范围出边界的范围
        public static readonly double LengthTol = 100.0;//分割线过滤容差，小于该值自动丢弃

        #region 获取分割线关系
        public static List<(List<int>, List<int>)> GetSegLineIndex(this List<LineSegment>segLines,Polygon wallLine )
        {
            var seglineIndex = new List<(List<int>, List<int>)>();
            for(int i = 0; i < segLines.Count; i++)
            {
                List<int> item1 = null;
                List<int> item2 = null;
                var segLine = segLines[i];
                if(segLine.Distance(new Coordinate(3338684.1858759141, 10522765.62606502)) < 100)
                {
                    ;
                }
                //筛选有效范围内的交点
                var coors = segLines.GetIntersections(i).Where(pt => wallLine.Contains(pt.ToPoint())).ToList();
                //var coors = segLines.GetIntersections(i).ToList();//获取关系即可，不需要管是否在边界内
                if (coors.Count == 0)
                {
                    seglineIndex.Add((null, null));//不与其他线相交，移除
                    continue;
                }
                var shellIntSecs = segLine.ToLineString().Intersection(wallLine.Shell).Coordinates.PositiveOrder();
                if (shellIntSecs.Count() > 0 && !shellIntSecs.First().PositiveTo(coors.First()))
                {
                    //无操作，负向连到边界
                    item1 = new List<int>();
                }
                else
                {
                    //负向不连边界
                    //筛选与第一个交点最近的线
                    var spt = coors.First();
                    item1 = Enumerable.Range(0, segLines.Count).
                        Where(j => segLines[j].Distance(spt) < ExtendTol&& !segLines[j].ParallelTo(segLine)).ToList();
                }
                if (shellIntSecs.Count() > 0 && shellIntSecs.Last().PositiveTo(coors.Last()))
                {
                    //无操作，正向连到边界
                    item2 = new List<int>();
                }
                else
                {
                    var ept = coors.Last();
                    item2 = Enumerable.Range(0, segLines.Count).
                        Where(j => segLines[j].Distance(ept) < ExtendTol && segLines[j].DirVector().Distance(segLine.DirVector()) > 0.01).ToList();
                }
                seglineIndex.Add((item1,item2));
            }
            return seglineIndex;
        }
        public static List<(List<int>, List<int>)> GetSegLineIndex(this List<SegLine> segLines, Polygon wallLine)
        {
            return segLines.Select(seg => seg.Splitter).ToList().GetSegLineIndex(wallLine);
        }
        public static List<Coordinate> GetIntersections(this List<LineSegment> seglines, int idx)
        {
            var coors = new List<Coordinate>();
            var segLine = seglines[idx];
            if (segLine == null) return null;
            for (int i = 0; i < seglines.Count; i++)
            {
                if (i == idx) continue;
                if (seglines[i] == null) continue;
                var intSecPt = segLine.Intersection(seglines[i]);
                if (intSecPt != null) coors.Add(intSecPt);
            }
            var ordered = coors.PositiveOrder();
            return ordered.ToList();
        }

        public static List<Coordinate> GetIntersections(this List<LineSegment> seglines,Polygon shell, int idx)
        {
            var coors = new List<Coordinate>();
            var segLine = seglines[idx];
            if (segLine == null) return null;
            for (int i = 0; i < seglines.Count; i++)
            {
                if (i == idx) continue;
                if (seglines[i] == null) continue;
                var intSecPt = segLine.Intersection(seglines[i]);
                if (intSecPt != null) coors.Add(intSecPt);
            }
            var intSecPts = segLine.GetBaseLine(shell).OExtend(1).ToLineString().Intersection(shell.Shell).Coordinates;
            coors.AddRange(intSecPts);
            var ordered = coors.PositiveOrder();
            if (ordered.Count() == 0) return ordered.ToList();
            if (ordered.Last().Distance(ordered.First())< ExtendTol) return new List<Coordinate> {  ordered.Last() };
            return ordered.ToList();
        }
        #endregion
        #region 获取基线(分割线在可布置区域内最长的部分）
        //切掉几何体外部的，保留边界内最长的线
        //或者切掉外部的（IncludeInner = false)
        public static LineSegment GetBaseLine(this LineString segLine, Geometry Geo, bool IncludeInner = true)
        {
            if (segLine == null) return null;
            IOrderedEnumerable<LineString> baseIntSection;
            if(IncludeInner)baseIntSection = segLine.Intersection(Geo).Get<LineString>().OrderBy(lstr => lstr.Length);
            else baseIntSection = segLine.Difference(Geo).Get<LineString>().OrderBy(lstr => lstr.Length);
            if (baseIntSection.Count() == 0) return null;//线在边界外，无基线
            var orderedBase = baseIntSection.Last().Coordinates.PositiveOrder();
            return new LineSegment(orderedBase.First(), orderedBase.Last());
        }
        public static LineSegment GetBaseLine(this LineSegment segLine, Geometry Geo, bool IncludeInner = true)
        {
            if(segLine == null) return null;
            return segLine.ToLineString().GetBaseLine(Geo, IncludeInner);
        }
        public static SegLine GetBaseLine(this SegLine segLine,Polygon shell)
        {
            var baseLine = segLine.Splitter.GetBaseLine(shell);
            var clone = segLine.CreateNew();
            clone.Splitter = baseLine;
            return clone;
        }
        #endregion
        #region 连接到其他分割线以及边界（迭代后处理为分割线）
        public static LineSegment Connect(this List<LineSegment> seglines, int idx, List<(List<int>, List<int>)> SeglineIndex,Polygon shell)
        {
            Coordinate spt = null;
            Coordinate ept = null;
            var segLine = seglines[idx];
            List<Coordinate> pts;
            var boundPts = new List<Coordinate>();
            if (SeglineIndex[idx].Item1.Count == 0 || SeglineIndex[idx].Item2.Count == 0)//需要连到边界
            {
                var baseLine = segLine.GetBaseLine(shell);
                if (baseLine == null) return null;
                var extended = baseLine.OExtend(MaxDistance).ToLineString();//无限延长+相交
                var basePt = new Point(baseLine.MidPoint);//基线中点
                var intersection = extended.Intersection(shell).Get<LineString>().OrderBy(lstr => basePt.Distance(lstr)).First();//筛选延长后与地库交集
                boundPts = intersection.Coordinates.PositiveOrder();
            }
            if (SeglineIndex[idx].Item1.Count > 0)//需连到其他分割线
            {
                pts = seglines.Slice(SeglineIndex[idx].Item1).Select(l => l.LineIntersection(segLine)).
                    Where(c =>c!= null).PositiveOrder();
                spt = pts.First();
            }
            else//需连到边界
            {
                spt = boundPts.First();
            }
            if (SeglineIndex[idx].Item2.Count > 0)
            {
                pts = seglines.Slice(SeglineIndex[idx].Item2).Select(l => l.LineIntersection(segLine)).
                    Where(c => c != null).PositiveOrder();
                ept = pts.Last();
            }
            else
            {
                ept = boundPts.Last();
            }
            return new LineSegment(spt, ept).GetBaseLine(shell).OExtend(ExtendTol);
        }
        //函数有问题，连到某一个线要确保该线也延长过去
        public static List<LineSegment> Connect(this List<LineSegment> seglines, List<(List<int>, List<int>)> SeglineIndex, Polygon shell)
        {
            var connected = new List<LineSegment>();
            for(int i = 0; i < seglines.Count; i++)
            {
                connected.Add(seglines.Connect(i, SeglineIndex, shell));
            }
            return connected;
        }

        public static void _Connect(this List<LineSegment> seglines, List<(List<int>, List<int>)> SeglineIndex, Polygon shell)
        {
            var nullIndex = new HashSet<int>();//为null的index
            var PtsToExtend = new Dictionary<int, List<Coordinate>>();//其他线相交该线于该点，需要后续将该线延长
            for (int i = 0; i < seglines.Count; i++)
            {
                Coordinate spt = null;
                Coordinate ept = null;
                var segLine = seglines[i];
                var boundPts = new List<Coordinate>();
                if (SeglineIndex[i].Item1.Count == 0 || SeglineIndex[i].Item2.Count == 0)//需要连到边界
                {
                    var baseLine = segLine.GetBaseLine(shell);
                    if (baseLine == null)
                    {
                        nullIndex.Add(i);
                        continue;
                    }
                    var extended = baseLine.OExtend(MaxDistance).ToLineString();//无限延长+相交
                    var basePt = new Point(baseLine.MidPoint);//基线中点
                    var intersection = extended.Intersection(shell).Get<LineString>().OrderBy(lstr => basePt.Distance(lstr)).First();//筛选延长后与地库交集
                    boundPts = intersection.Coordinates.PositiveOrder();
                }
                if (SeglineIndex[i].Item1.Count > 0)//需连到其他分割线
                {
                    int idx;
                    (spt, idx) = seglines.LineIntSecAndIdxByMax(i, SeglineIndex[i].Item1, true);
                    if(PtsToExtend.ContainsKey(idx)) PtsToExtend[idx].Add(spt.Copy());
                    else PtsToExtend.Add(idx,new List<Coordinate> { spt.Copy() });
                }
                else//需连到边界
                {
                    spt = boundPts.First();
                }
                if (SeglineIndex[i].Item2.Count > 0)
                {
                    int idx;
                    (ept, idx) = seglines.LineIntSecAndIdxByMax(i, SeglineIndex[i].Item2, false);
                    if (PtsToExtend.ContainsKey(idx)) PtsToExtend[idx].Add(ept.Copy());
                    else PtsToExtend.Add(idx, new List<Coordinate> { ept.Copy() });
                }
                else
                {
                    ept = boundPts.Last();
                }
                seglines[i] = new LineSegment(spt, ept);
                //return new LineSegment(spt, ept).GetBaseLine(shell).OExtend(ExtendTol);
            }
            foreach(var key in PtsToExtend.Keys)
            {
                if (nullIndex.Contains(key)) continue;//splitter为null，无需延长
                var coors = PtsToExtend[key];
                coors.Add(seglines[key].P0);
                coors.Add(seglines[key].P1);
                coors = coors.PositiveOrder();
                seglines[key] = new LineSegment(coors.First(), coors.Last());
            }
            for (int i = 0; i < seglines.Count; i++)
            {
                if(nullIndex.Contains(i)) seglines[i] = null;
                else seglines[i] = seglines[i].GetBaseLine(shell).OExtend(ExtendTol);
            }
        }
        public static void Connect2(this List<LineSegment> seglines, List<(List<int>, List<int>)> SeglineIndex, Polygon shell)
        {
            var baseLines = seglines.Select(l => l.GetBaseLine(shell)).ToList();//为null的index
            var negIdxs = new HashSet<int>();
            var PtsToExtend = new Dictionary<int, List<Coordinate>>();//其他线相交该线于该点，需要后续将该线延长
            for (int i = 0; i < seglines.Count; i++)
            {
                
                var baseLine = baseLines[i];
                if (baseLine == null)//基线为null，跳过
                {
                    continue;
                }
                Coordinate spt = null;
                Coordinate ept = null;
                var segLine = seglines[i];
                var boundPts = new List<Coordinate>();
                bool negConnect = SeglineIndex[i].Item1.Count == 0 /*|| baseLines.Slice(SeglineIndex[i].Item1).Contains(null)*/;
                bool posConnect = SeglineIndex[i].Item2.Count == 0 /*|| baseLines.Slice(SeglineIndex[i].Item2).Contains(null)*/;
                var extended = baseLine.OExtend(MaxDistance).ToLineString();//无限延长+相交
                var basePt = new Point(baseLine.MidPoint);//基线中点
                var intersection = extended.Intersection(shell).Get<LineString>().OrderBy(lstr => basePt.Distance(lstr)).First();//筛选延长后与地库交集
                boundPts = intersection.Coordinates.PositiveOrder();
                if (!negConnect)//需连到其他分割线
                {
                    int idx;
                    (spt, idx) = seglines.LineIntSecAndIdxByMax(i, SeglineIndex[i].Item1, true);
                    if (PtsToExtend.ContainsKey(idx)) PtsToExtend[idx].Add(spt.Copy());
                    else PtsToExtend.Add(idx, new List<Coordinate> { spt.Copy() });
                    if (baseLines.Slice(SeglineIndex[i].Item1).Contains(null))
                    {
                        if(shell.Contains(spt.ToPoint()))negConnect = true;
                    }
                }
                else//需连到边界
                {
                    spt = boundPts.First();
                }
                if (!posConnect)
                {
                    int idx;
                    (ept, idx) = seglines.LineIntSecAndIdxByMax(i, SeglineIndex[i].Item2, false);
                    if (PtsToExtend.ContainsKey(idx)) PtsToExtend[idx].Add(ept.Copy());
                    else PtsToExtend.Add(idx, new List<Coordinate> { ept.Copy() });
                    if (baseLines.Slice(SeglineIndex[i].Item2).Contains(null))
                    {
                        if (shell.Contains(ept.ToPoint())) posConnect = true;
                    }
                }
                else
                {
                    ept = boundPts.Last();
                }
                var tempLine = new LineSegment(spt, ept);
                if (ept.PositiveTo(spt)) seglines[i] = new LineSegment(spt, ept);
                else negIdxs.Add(i);
            }
            foreach (var key in PtsToExtend.Keys)
            {
                if (baseLines[key] == null) continue;//splitter为null，无需延长
                var coors = PtsToExtend[key];
                coors.Add(seglines[key].P0);
                coors.Add(seglines[key].P1);
                coors = coors.PositiveOrder();
                seglines[key] = new LineSegment(coors.First(), coors.Last());
            }
            for (int i = 0; i < seglines.Count; i++)
            {
                if (baseLines[i] == null || negIdxs.Contains(i)) seglines[i] = null;
                else seglines[i] = seglines[i].GetBaseLine(shell).OExtend(ExtendTol);
            }
        }
        public static (Coordinate,int) LineIntSecAndIdxByMax(this List<LineSegment> seglines,int curIdx,List<int> connectTo,bool IsNegative)
        {
            var segLine = seglines[curIdx];
            Coordinate pt;
            var pts = seglines.Slice(connectTo).Select(l => l.LineIntersection(segLine)).ToList();
            if (IsNegative) pt = pts.Where(c => c != null).PositiveOrder().First();
            else pt = pts.Where(c => c != null).PositiveOrder().Last();
            int idx = connectTo[pts.FindIndex(p =>p.Equals(pt))];
            return (pt,idx);
        }
        #endregion
        #region 更新分割线
        //输入:未处理的分割线（可能刚移动，还未保持连接关系）
        //     分割线连接关系
        //     地库信息（边界--无孔polygon，Spindex--空间索引）
        //输出:符合车道宽的部分，或null 

        //1.将分割线按连接关系连到其他线上
        //2.求中间部分，判断中间部分是否满足车道宽度，不满足则返回null
        //3.所有线缩回到边界内
        //3.利用地库的连接关系，正向或负向连到边界
        public static void UpdateSegLines(this List<SegLine> seglines, List<(List<int>, List<int>)> SeglineIndex,
            Polygon shell, MNTSSpatialIndex BoundarySpatialIndex, Polygon BaseLineBoundary = null)
        {
            //var splitters = seglines.Select(seg => seg.Splitter).ToList().Connect(SeglineIndex, shell);
            var splitters = seglines.Select(seg => seg.Splitter).ToList();
            splitters.Connect2(SeglineIndex, shell);
            for (int i = 0; i < seglines.Count; i++)
            {
                var connections = (SeglineIndex[i].Item1.Count == 0, SeglineIndex[i].Item2.Count == 0);
                var splitter = splitters[i];
                var vaildLane = splitter.GetVaildLane(connections, BoundarySpatialIndex, seglines[i].RoadWidth , BaseLineBoundary);
                seglines[i].Splitter = splitter;
                seglines[i].VaildLane = vaildLane;
            }
        }
        //求单个有效车道
        //输入 连接到其他线上 + 求中间部分 + 连接到边界的线 
        //     分割线连接到边界的关系
        //     所有地库障碍物的spatial index
        //输出 满足车道宽的线

        //效率提高：因为障碍物要外扩，可以预处理时将所有障碍物外扩合并之后再传入
        public static LineSegment GetVaildLane(this LineSegment connectedPart, (bool, bool) Connections, MNTSSpatialIndex BoundarySpatialIndex,
            int roadWidth = -1, Polygon BaseLineBoundary = null)
        {
            double halfWidth;
            if (roadWidth == -1) halfWidth = (VMStock.RoadWidth / 2);
            else halfWidth = (roadWidth / 2);
            if(connectedPart == null) return null;
            //筛选车道范围内的障碍物
            LineSegment baseLine;
            if(roadWidth == -1)
            {
                baseLine = connectedPart.GetBaseLine(BaseLineBoundary,true);
            }
            else
            {
                //2.分割线 - 障碍物外扩2750-0.1 的最长线
                var bounds = BoundarySpatialIndex.SelectCrossingGeometry(connectedPart.ToLineString().Buffer(halfWidth));
                var bufferedBuildings = new GeometryCollection(bounds.ToArray()).
                    Buffer(halfWidth - SegTol).Union().Get<Polygon>(true);//提取范围内全部障碍物 + 外扩 + 合并 + 去孔
                baseLine = connectedPart.GetBaseLine(new MultiPolygon(bufferedBuildings.ToArray()), false);//获取基线
            }
            if(baseLine == null) return null;
            //3.(若需要连到边界)则以2的线中点为基点，左右buffer找到最远距离对应的线
            var basePt = baseLine.MidPoint;
            var bufferLine = basePt.LineBuffer(halfWidth-(2*SegTol), baseLine.DirVector());//buffer基线,确保不碰到上一步的障碍物
            var bufferLstr = bufferLine.ToLineString();
            var normalVec = bufferLine.NormalVector();
            Coordinate p0 = baseLine.P0;
            Coordinate p1 = baseLine.P1;
            if (Connections.Item1)//起始连接到边界
            {
                var rect = bufferLine.ShiftBuffer(MaxDistance, normalVec.Negate());//无限远buffer
                var intersection = new GeometryCollection(BoundarySpatialIndex.SelectCrossingGeometry(rect).ToArray()).Intersection(rect);
                var distance = bufferLstr.Distance(intersection)-ExtendTol;
                p0 = normalVec.Negate().Multiply(distance).Translate(basePt);
            }
            if (Connections.Item2)//终止连接
            {
                var rect = bufferLine.ShiftBuffer(MaxDistance, normalVec);//无限远buffer
                var intersection = new GeometryCollection(BoundarySpatialIndex.SelectCrossingGeometry(rect).ToArray()).Intersection(rect);
                var distance = bufferLstr.Distance(intersection)-ExtendTol;
                p1 = normalVec.Multiply(distance).Translate(basePt);
            }
            return new LineSegment(p0, p1);
        }
        #endregion
        #region 分区线分组
        public static List<List<int>> GroupSegLines(this List<SegLine> SegLines,int groupFlag = 0)
        {
            switch (groupFlag)
            {
                case 0:
                    return SegLines.Select(l => l.VaildLane).ToList().GroupSegLines(0);
                case 1:
                    return SegLines.Select(l => l.Splitter).ToList().GroupSegLines(1);
                case 2:
                    return SegLines.Select(l => l.Splitter).ToList().GroupSegLines(0);
                default:
                    return null;
            }
        }

        public static List<List<int>> GroupSegLines(this List<LineSegment> SegLines, int groupFlag = 0)
        {
            var groups = new List<List<int>>();
            var rest_idx = new List<int>();
            for (int i = 0; i < SegLines.Count; ++i) rest_idx.Add(i);
            while (rest_idx.Count != 0)
            {
                bool foundRelation = false;
                foreach (var group in groups)
                {
                    foreach (var idx in rest_idx)
                    {
                        var line = SegLines[idx];
                        switch (groupFlag)
                        {
                            case 0:
                                foundRelation = line.ConnectWithAny(SegLines.Slice(group));
                                break;
                            case 1:
                                foundRelation = line.CanMergeWithAny(SegLines.Slice(group));
                                break;
                        }
                        if (foundRelation)
                        {
                            foundRelation = true;
                            group.Add(idx);
                            rest_idx.Remove(idx);
                            break;
                        }
                    }
                    if (foundRelation) break;
                }
                if (!foundRelation)
                {
                    groups.Add(new List<int> { rest_idx.First() });
                    rest_idx.RemoveAt(0);
                }
            }
            return groups;
        }
        public static bool ConnectWithAny(this SegLine line, List<SegLine> otherLines)
        {
            return line.VaildLane.ConnectWithAny(otherLines.Select(l => l.VaildLane).ToList());
        }
        public static bool ConnectWithAny(this LineSegment line, List<LineSegment> otherLines)
        {
            if (line == null) return false;
            foreach (var l2 in otherLines)
            {
                if (l2== null) continue;
                if (!line.ParallelTo(l2) && line.Intersection(l2) != null) return true;
            }
            return false;
        }

        public static bool CanMergeWithAny(this SegLine line, List<SegLine> otherLines)
        {
            return line.Splitter.CanMergeWithAny(otherLines.Select(l => l.Splitter).ToList());
        }
        public static bool CanMergeWithAny(this LineSegment line, List<LineSegment> otherLines)
        {
            if (line == null) return false;
            foreach (var l2 in otherLines)
            {
                if (l2 == null) continue;
                if (line.ParallelTo(l2) && line.Distance(l2) < ExtendTol) return true;
            }
            return false;
        }
        #endregion
        #region 
        public static List<SegLine> MergeSegs(this List<SegLine> segLines, List<List<int>> idToMerge)
        {
            var newsegs = new List<SegLine>();
            foreach (var group in idToMerge)
            {
                var merged = segLines.Slice(group).Merge();
                if (merged != null) newsegs.Add(merged);
            }
            return newsegs;
        }
        public static List<LineSegment> MergeSegs(this List<LineSegment> segLines,List<List<int>> idToMerge)
        {
            var newsegs = new List<LineSegment>();
            foreach(var group in idToMerge)
            {
                var merged = segLines.Slice(group).Merge();
                if(merged != null) newsegs.Add(merged);
            }
            return newsegs;
        }
        public static LineSegment Merge(this List<LineSegment> lineSegments)
        {
            if (lineSegments.Count == 0) return null;
            var coors = new List<Coordinate>();
            foreach (var l in lineSegments)
            {
                coors.Add(l.P0);
                coors.Add(l.P1);
            }
            var ordered = coors.PositiveOrder();
            return new LineSegment(coors.First(),coors.Last());
        }
        public static SegLine Merge(this List<SegLine> segLines)
        {
            var newSplitter = segLines.Select(l =>l.Splitter).ToList().Merge();
            var isFixed = segLines.Any(l => l.IsFixed);
            var roadWidth = segLines.Max(l =>l.RoadWidth);
            return new SegLine(newSplitter, isFixed, roadWidth);
        }
        #endregion
        #region 获取子区域内的车道，以及墙线
        //获取相同部分
        public static List<LineSegment> GetCommonParts(this List<LineString> lanes,Polygon area,double tol  = 0.01)
        {
            var vaildParts = new List<LineSegment>();
            //var bounds = new MNTSSpatialIndex(shell.ToLineStrings());
            var bound = area.Buffer(tol);
            foreach(var segLine in lanes)
            {
                var commonPart = segLine.Intersection(bound).Get<LineString>();
                foreach(var lstr in commonPart)
                {
                    var ordered = lstr.Coordinates.PositiveOrder();
                    var vaildPart = new LineSegment(ordered.First(), ordered.Last());
                    if(vaildPart.Length > LengthTol) vaildParts.Add(vaildPart);
                }  
            }
            return vaildParts;
        }

        public static List<LineString> GetWalls(this List<LineString> SegLines, LineString shell, double tol = 0.01)
        {
            Geometry walls = shell.Copy();
            foreach (var segLine in SegLines)
            {
                var bufferedLine = segLine.Buffer(tol);
                var commonPart = bufferedLine.Intersection(walls);
                if (commonPart.Length > LengthTol)
                {
                    walls = walls.Difference(bufferedLine);
                }
            }
            return walls.Get<LineString>();
        }
        #endregion

        #region 更新分区线迭代范围

        public static bool UpdateLowerUpperBound(this SegLine segLine, Polygon WallLine, MNTSSpatialIndex BuildingSpatialIndex,
           MNTSSpatialIndex InnerBoundSPIndex, MNTSSpatialIndex OuterBoundSPIndex)
        {
            //segLine.IsInitLine = true;
            if (segLine.IsFixed) return true;
            if(!segLine.IsInitLine) return false;//非初始线
            var splitterLstr = segLine.Splitter.ToLineString();
            var IntSected = BuildingSpatialIndex.SelectCrossingGeometry(splitterLstr);
            var halfWidth = VMStock.RoadWidth / 2;
            if (IntSected.Any(b => InnerBoundSPIndex.SelectCrossingGeometry(b).Count > 0))//判断是否有相交到的障碍物在内部建筑中
            {
                segLine.IsFixed = true;//穿了中间障碍物，固定该分区线
                return false;//不满足车道宽
            }
            //基于分割线求矩形buffer
            //var laneBuffer = segLine.Splitter.OGetRect(halfWidth);
            var ignoreBound =new MultiPolygon( OuterBoundSPIndex.SelectCrossingGeometry(splitterLstr).Cast<Polygon>().ToArray());

            var normalVector = segLine.Splitter.NormalVector();
            double maxVal = OutSideTol;
            double minVal = -OutSideTol;
            var posBuffer = segLine.Splitter.ShiftBuffer(MaxDistance, normalVector);
            var posobjs = BuildingSpatialIndex.SelectCrossingGeometry(posBuffer).Where(b => b.Disjoint(ignoreBound)).Cast<Polygon>();
            if (posobjs.Count() != 0)//正向有建筑
            {
                var intSection = new MultiPolygon(posobjs.ToArray());
                maxVal = splitterLstr.Distance(intSection) - halfWidth;
            }
            else
            {
                var coors = WallLine.Shell.Intersection(posBuffer).Coordinates;
                if (coors.Count() != 0)
                {
                    maxVal = coors.Max(c => segLine.Splitter.Distance(c)) + OutSideTol;
                }
            }

            var negBuffer = segLine.Splitter.ShiftBuffer(-MaxDistance, normalVector);
            var negobjs = BuildingSpatialIndex.SelectCrossingGeometry(negBuffer).Where(b => b.Disjoint(ignoreBound)).Cast<Polygon>();
            if (negobjs.Count() != 0)//负向有建筑
            {
                var intSection = new MultiPolygon(negobjs.ToArray());
                minVal = -splitterLstr.Distance(intSection) + halfWidth;
            }
            else
            {
                var coors = WallLine.Shell.Intersection(negBuffer).Coordinates;
                if (coors.Count() != 0)
                {
                    minVal = -coors.Max(c => segLine.Splitter.Distance(c)) - OutSideTol;
                }
            }
            if (maxVal - minVal < 0)
            {
                segLine.IsFixed = true;
                return false;
            }
            segLine.SetMinMaxValue(minVal, maxVal);

            return true;
        }

        public static bool UpdateLowerUpperBound(this SegLine segLine, Polygon WallLine, MNTSSpatialIndex BuildingSpatialIndex,
             MNTSSpatialIndex OuterBoundSPIndex)
        {
            //if (segLine.Splitter.Distance(new Coordinate(2402180.0370, 4763590.3175)) < 100)
            //{
            //    ;
            //}
            double roadWidth;
            if (segLine.RoadWidth == -1) roadWidth = VMStock.RoadWidth;
            else roadWidth = segLine.RoadWidth;
            //segLine.IsInitLine = true;
            if (segLine.IsFixed) return true;
            if (!segLine.IsInitLine) return false;//非初始线
            if (segLine.Splitter == null) return false;
            var splitterLstr = segLine.Splitter.ToLineString();
            var normalVector = segLine.Splitter.NormalVector();
            double posDistance =0;//分割线法向正方向距建筑距离
            double negDistance =0;//分割线法向负方向距建筑距离
            double maxVal = OutSideTol;
            double minVal = -OutSideTol;
            bool needFilter = true;//是否需要过滤
            var IntSected = BuildingSpatialIndex.SelectCrossingGeometry(splitterLstr);
            if(IntSected.Count() == 0)//count 不为0则必须过滤
            {
                (negDistance,posDistance) = segLine.RecDistance(BuildingSpatialIndex);
                if (posDistance < 0 || negDistance < 0) needFilter = false;//正向和负向没有建筑，无需过滤 
                else needFilter = (posDistance  + negDistance) < roadWidth;//距离和小于道路宽则需要过滤
            }
            var halfWidth = roadWidth / 2;
            //MultiPolygon ignoreBound = MultiPolygon.Empty;
            if (needFilter)
            {
                var ignoreBuffer = segLine.Splitter.OGetRect(SegTol + halfWidth - VMStock.RoadWidth / 2);//解决非标准车道
                //基于分割线求矩形buffer
                var ignoreBound = new MultiPolygon(OuterBoundSPIndex.SelectCrossingGeometry(ignoreBuffer).Cast<Polygon>().ToArray());
                (negDistance,posDistance) = segLine.RecDistance(BuildingSpatialIndex,ignoreBound);
            }
            if(posDistance > 0)
            {
                maxVal = posDistance - halfWidth;
            }
            else//正向无建筑
            {
                var posBuffer = segLine.Splitter.ShiftBuffer(MaxDistance, normalVector);
                var coors = WallLine.Shell.Intersection(posBuffer).Coordinates;
                if (coors.Count() != 0)
                {
                    maxVal = coors.Max(c => segLine.Splitter.Distance(c)) + OutSideTol;
                }
            }
            if (negDistance > 0)
            {
                minVal = -negDistance + halfWidth;

            }
            else//负向无建筑
            {
                var negBuffer = segLine.Splitter.ShiftBuffer(-MaxDistance, normalVector);
                var coors = WallLine.Shell.Intersection(negBuffer).Coordinates;
                if (coors.Count() != 0)
                {
                    minVal = -coors.Max(c => segLine.Splitter.Distance(c)) - OutSideTol;
                }
            }
            if (maxVal - minVal < 0)
            {
                segLine.IsFixed = true;
                return false;
            }
            segLine.SetMinMaxValue(minVal, maxVal);
            return true;
        }

        //求分割线法向正方向以及负方向距离障碍物的距离（矩形框，框到的障碍物）
        public static (double,double) RecDistance(this SegLine segLine, MNTSSpatialIndex BuildingSpatialIndex, MultiPolygon ignoreBound = null)
        {
            if (ignoreBound == null) ignoreBound = MultiPolygon.Empty;
            var splitterLstr = segLine.Splitter.ToLineString();
            var normalVector = segLine.Splitter.NormalVector();
            double posDistance = -1;//法向正方向距离
            double negDistance = -1;//法向负方向距离
            var posBuffer = segLine.Splitter.ShiftBuffer(MaxDistance, normalVector);
            var posobjs = BuildingSpatialIndex.SelectCrossingGeometry(posBuffer).Where(b => b.Disjoint(ignoreBound)).Cast<Polygon>();
            if (posobjs.Count() != 0)//正向有建筑
            {
                var intSection = new MultiPolygon(posobjs.ToArray());
                posDistance = splitterLstr.Distance(intSection);
            }

            var negBuffer = segLine.Splitter.ShiftBuffer(-MaxDistance, normalVector);
            var negobjs = BuildingSpatialIndex.SelectCrossingGeometry(negBuffer).Where(b => b.Disjoint(ignoreBound)).Cast<Polygon>();
            if (negobjs.Count() != 0)//负向有建筑
            {
                var intSection = new MultiPolygon(negobjs.ToArray());
                negDistance = splitterLstr.Distance(intSection) ;
            }
            return (negDistance,posDistance);
        }
        #endregion
    }
}
