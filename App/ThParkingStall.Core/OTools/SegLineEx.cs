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
        public static double MaxDistance = 100000000;//最大距离，10公里
        public static double SegTol = 0.1;//分割线容差，距离大于半车道宽 -0.1则判断满足车道宽
        public static double ExtendTol = 1.0;//延长容差，分割线使用的延长或回缩容差
        public static double LengthTol = 100.0;//分割线过滤容差，小于该值自动丢弃
        #region 获取分割线关系
        public static List<(List<int>, List<int>)> GetSegLineIndex(this List<LineSegment>segLines,LinearRing wallLine,Polygon baseLineBoundary)
        {
            var seglineIndex = new List<(List<int>, List<int>)>();
            for(int i = 0; i < segLines.Count; i++)
            {
                List<int> item1 = null;
                List<int> item2 = null;
                var segLine = segLines[i];
                //筛选有效范围内的交点
                var coors = segLines.GetIntersections(i).Where(pt => baseLineBoundary.Contains(pt.ToPoint())).ToList();
                if (coors.Count == 0)
                {
                    seglineIndex.Add((null, null));//不与其他线相交，移除
                    continue;
                }
                var shellIntSecs = segLine.ToLineString().Intersection(wallLine).Coordinates.OrderBy(c => c.X).ThenBy(c => c.Y);
                if(shellIntSecs.Count() > 0 && !shellIntSecs.First().PositiveTo(coors.First()))
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
        public static List<(List<int>, List<int>)> GetSegLineIndex(this List<SegLine> segLines, LinearRing wallLine, Polygon baseLineBoundary)
        {
            return segLines.Select(seg => seg.Splitter).ToList().GetSegLineIndex(wallLine, baseLineBoundary);
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
            var ordered = coors.OrderBy(c => c.X).ThenBy(c => c.Y);
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
            var ordered = coors.OrderBy(c => c.X).ThenBy(c => c.Y);
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
            var orderedBase = baseIntSection.Last().Coordinates.OrderBy(c => c.X).ThenBy(c => c.Y);
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
            var clone = segLine.Clone();
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
            IOrderedEnumerable<Coordinate> pts;
            var boundPts = new List<Coordinate>();
            if (SeglineIndex[idx].Item1.Count == 0 || SeglineIndex[idx].Item2.Count == 0)//需要连到边界
            {
                var baseLine = segLine.GetBaseLine(shell);
                if (baseLine == null) return null;
                var extended = baseLine.OExtend(MaxDistance).ToLineString();//无限延长+相交
                var basePt = new Point(baseLine.MidPoint);//基线中点
                var intersection = extended.Intersection(shell).Get<LineString>().OrderBy(lstr => basePt.Distance(lstr)).First();//筛选延长后与地库交集
                boundPts = intersection.Coordinates.OrderBy(c => c.X).ThenBy(c => c.Y).ToList();
            }
            if (SeglineIndex[idx].Item1.Count > 0)//需连到其他分割线
            {
                pts = seglines.Slice(SeglineIndex[idx].Item1).Select(l => l.LineIntersection(segLine)).
                    Where(c =>c!= null).OrderBy(c => c.X).ThenBy(c => c.Y);
                spt = pts.First();
            }
            else//需连到边界
            {
                spt = boundPts.First();
            }
            if (SeglineIndex[idx].Item2.Count > 0)
            {
                pts = seglines.Slice(SeglineIndex[idx].Item2).Select(l => l.LineIntersection(segLine)).
                    Where(c => c != null).OrderBy(c => c.X).ThenBy(c => c.Y);
                ept = pts.Last();
            }
            else
            {
                ept = boundPts.Last();
            }
            return new LineSegment(spt, ept).GetBaseLine(shell).OExtend(ExtendTol);
        }

        public static List<LineSegment> Connect(this List<LineSegment> seglines, List<(List<int>, List<int>)> SeglineIndex, Polygon shell)
        {
            var connected = new List<LineSegment>();
            for(int i = 0; i < seglines.Count; i++)
            {
                connected.Add(seglines.Connect(i, SeglineIndex, shell));
            }
            return connected;
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
            var splitters = seglines.Select(seg => seg.Splitter).ToList().Connect(SeglineIndex, shell);
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
        #region 分区线合并
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
            var ordered = coors.OrderBy(c => c.X).ThenBy(c => c.Y);
            return new LineSegment(coors.First(),coors.Last());
        }
        #endregion

        #region 获取子区域内的车道，以及墙线
        //获取相同部分
        public static List<LineSegment> GetCommonParts(this List<LineString> SegLines,LineString shell,double tol  = 0.01)
        {
            var vaildParts = new List<LineSegment>();
            //var bounds = new MNTSSpatialIndex(shell.ToLineStrings());
            var bound = shell.Buffer(tol);
            foreach(var segLine in SegLines)
            {
                var commonPart = segLine.Intersection(bound);
                if(commonPart.Length > LengthTol)
                {
                    var orderd = commonPart.Coordinates.OrderBy(c => c.X).ThenBy(c => c.Y);
                    vaildParts.Add(new LineSegment(orderd.First(), orderd.Last()));
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
    }
}
