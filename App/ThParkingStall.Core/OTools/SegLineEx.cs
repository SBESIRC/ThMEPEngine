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
        #region 获取某一根分割线在其他分割线的中间部分
        //返回为null则当前分割线无交点,可丢弃
        //返回长度为0 则只有一个交点，后续需连到边界 且判断方向是否正确
        //其余情况则返回正常
        public static LineSegment GetMiddlePart(this List<LineSegment> seglines, int idx)
        {
            var coors =new List<Coordinate>();
            var segLine = seglines[idx];
            if(segLine == null) return null;
            for (int i = 0; i < seglines.Count; i++)
            {
                if (i == idx) continue;
                var intSecPt = segLine.Intersection(seglines[i]);
                if(intSecPt != null) coors.Add(intSecPt);
            }
            var ordered = coors.OrderBy(c => c.X).ThenBy(c => c.Y);
            if (coors.Count > 0) return new LineSegment(ordered.First(), ordered.Last());
            return null;
        }
        public static SegLine GetMiddlePart(this List<SegLine> seglines,int idx)
        {
            var middlePart = seglines.Select(l => l.Splitter).ToList().GetMiddlePart(idx);
            var segLine = seglines[idx].Clone();
            segLine.Splitter = middlePart;
            return segLine;
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
        #region 分割线连到边界内
        //如果相交到边界（距离大于1）缩回边界内
        public static LineSegment IndentInside(this LineSegment segLine, Polygon shell)
        {
            var baseLine = segLine.GetBaseLine(shell);//基线 原分割线与地库交集
            if (baseLine == null) return null;//不在边界内
            if (baseLine.Length < LengthTol) return null;//太短不要了
            Coordinate spt = baseLine.P0;
            Coordinate ept = baseLine.P1;
            //回缩
            if (new Point(spt).Distance(shell.Shell) < ExtendTol) spt = baseLine.NormalVector().Multiply(2* ExtendTol).Translate(spt);
            if (new Point(ept).Distance(shell.Shell) < ExtendTol) ept = baseLine.NormalVector().Negate().Multiply(2 * ExtendTol).Translate(ept);
            return new LineSegment(spt, ept);
        }
        //需要连到边界的连到边界
        //无需连接的，内缩回边界内
        //输出延长1
        public static LineSegment ConnectToBound(this LineSegment segLine, (bool, bool) Connections, Polygon shell)
        {
            var baseLine = segLine.GetBaseLine(shell);//基线 原分割线与地库交集
            if(baseLine == null) return null;
            if (baseLine.Length < LengthTol) return null;//太短不要了
            var extended = baseLine.Extend(MaxDistance).ToLineString();//无限延长+相交
            var basePt = new Point(baseLine.MidPoint);//基线中点
            var intersection = extended.Intersection(shell).Get<LineString>().OrderBy(lstr => basePt.Distance(lstr)).First();//筛选延长后与地库交集
            var orderedPts = intersection.Coordinates.OrderBy(c => c.X).ThenBy(c => c.Y);
            Coordinate spt;
            Coordinate ept;
            if (Connections.Item1) spt = orderedPts.First();//P0需要连接
            else if (baseLine.P0.Distance(orderedPts.First()) > ExtendTol) spt = baseLine.P0;
            else spt = baseLine.NormalVector().Multiply(3* ExtendTol).Translate(orderedPts.First());//无需连接的移回到边界内
            if (Connections.Item2) ept = orderedPts.Last();//P1需要连接
            else if (baseLine.P0.Distance(orderedPts.Last()) > ExtendTol) ept = baseLine.P1;
            else ept = baseLine.NormalVector().Negate().Multiply(3 * ExtendTol).Translate(orderedPts.Last());
            return new LineSegment(spt, ept).Extend(ExtendTol);
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
        public static void UpdateSegLine(this List<SegLine> seglines, List<(List<int>, List<int>)> SeglineIndex,
            Polygon shell, MNTSSpatialIndex BoundarySpatialIndex)
        {
            var splitters = seglines.Select(seg => seg.Splitter).ToList().RebuildSegLines(SeglineIndex, shell);
            for (int i = 0; i < seglines.Count; i++)
            {
                var connections = (SeglineIndex[i].Item1.Count == 0, SeglineIndex[i].Item2.Count == 0);
                var splitter = splitters[i];
                var vaildLane = splitter.GetVaildLane(connections, BoundarySpatialIndex);
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
        public static LineSegment GetVaildLane(this LineSegment connectedPart, (bool, bool) Connections, MNTSSpatialIndex BoundarySpatialIndex)
        {
            double halfWidth = (VMStock.RoadWidth / 2);
            if(connectedPart == null) return null;
            //筛选车道范围内的障碍物
            var bounds = BoundarySpatialIndex.SelectCrossingGeometry(connectedPart.GetRect(halfWidth));
            //2.分割线 - 障碍物外扩2750-0.1 的最长线
            var bufferedBuildings =new MultiPolygon( bounds.Where(geo =>geo is Polygon).Cast<Polygon>().ToArray()).
                Buffer(halfWidth - SegTol).Union().Get<Polygon>(true);//提取范围内全部障碍物 + 外扩 + 合并 + 去孔
            var baseLine = connectedPart.GetBaseLine(new MultiPolygon(bufferedBuildings.ToArray()),false);//获取基线
            //3.(若需要连到边界)则以2的线中点为基点，左右buffer找到最远距离对应的线
            var basePt = baseLine.MidPoint;
            var bufferLine = basePt.LineBuffer(halfWidth-(2*SegTol), baseLine.NormalVector());//buffer基线,确保不碰到上一步的障碍物
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
        #region 重塑分割线(可用于分割的分割线）
        //根据连接关系重塑分割线
        //输入:移动后的分割线，连接关系,以及地库边界（无孔polygon）

        //输出:重塑后的线
        //重塑后的线该连接到边界的会连到边界
        //不该连到边界的会内缩回边界内
        //线会出头1mm

        public static List<LineSegment> RebuildSegLines(this List<LineSegment> seglines, List<(List<int>, List<int>)> SeglineIndex, Polygon shell)
        {
            var splitters = new List<LineSegment>();
            for (int i = 0; i < seglines.Count; i++)
            {
                splitters[i] = splitters.ConnectLines(i, SeglineIndex, shell.Shell);//分割线按连接关系重构
            }
            var Rebuilded = new List<LineSegment>();
            for (int i = 0; i < splitters.Count; i++)
            {
                var connections = (SeglineIndex[i].Item1.Count == 0, SeglineIndex[i].Item2.Count == 0);
                var connectedPart = splitters.GetMiddlePart(i).ConnectToBound(connections, shell);//获取中间部分 +连到边界(不连到边界的自动缩回）
                Rebuilded.Add(connectedPart);
            }
            return Rebuilded;
        }

        //根据连接关系连接分割线,返回null则不合理
        public static LineSegment ConnectLines(this List<LineSegment> seglines, int idx, List<(List<int>, List<int>)> SeglineIndex, LinearRing shell=null)
        {
            Coordinate startPt;
            Coordinate endPt;
            (startPt,endPt) = seglines.GetStartEndPt(idx,SeglineIndex,shell);
            if(startPt==null||endPt ==null) return null;
            var tempLine = new LineSegment(startPt, endPt);
            if (!tempLine.IsPositive()) return null;
            return tempLine;
        }
        public static SegLine ConnectLines(this List<SegLine> seglines, int idx, List<(List<int>, List<int>)> SeglineIndex, LinearRing shell = null)
        {
            var line = seglines.Select(l => l.Splitter).ToList().ConnectLines(idx,SeglineIndex,shell);
            var segLine = seglines[idx].Clone();
            segLine.Splitter = line;
            return segLine;
        }
        #endregion
        #region 获取起点和终点
        public static (Coordinate,Coordinate) GetStartEndPt(this List<LineSegment> seglines, int idx, 
            List<(List<int>, List<int>)> SeglineIndex, LinearRing shell = null)
        {
            Coordinate startPt = null;
            Coordinate endPt = null;
            var segLine = seglines[idx];
            IOrderedEnumerable<Coordinate> pts;
            if (SeglineIndex[idx].Item1.Count == 0)//起始连接到边界最远点
            {
                if (shell!= null)
                {
                    pts = segLine.ToLineString().Intersection(shell).Coordinates.OrderBy(c => c.X).ThenBy(c => c.Y);
                    if (pts.Count()!= 0)startPt = pts.First();//返回最远交点
                }
            }
            else
            {
                pts = seglines.Slice(SeglineIndex[idx].Item1).Select(l => l.LineIntersection(segLine)).OrderBy(c => c.X).ThenBy(c => c.Y);
                startPt = pts.First();
            }

            if (SeglineIndex[idx].Item2.Count == 0)//终点连接到边界
            {
                if (shell != null)
                {
                    pts = segLine.ToLineString().Intersection(shell).Coordinates.OrderBy(c => c.X).ThenBy(c => c.Y);
                    if (pts.Count() != 0) endPt = pts.Last();
                }
            }
            else
            {
                pts = seglines.Slice(SeglineIndex[idx].Item2).Select(l => l.LineIntersection(segLine)).OrderBy(c => c.X).ThenBy(c => c.Y);
                endPt = pts.Last();
            }
            return (startPt, endPt);
        }
        public static (Coordinate, Coordinate) GetStartEndPt(this List<SegLine> seglines, int idx,
    List<(List<int>, List<int>)> SeglineIndex, LinearRing shell=null)
        {
            return GetStartEndPt(seglines.Select(l => l.Splitter).ToList(), idx, SeglineIndex, shell);
        }
        #endregion
    }
}
