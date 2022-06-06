using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
namespace ThParkingStall.Core.Tools
{
    public static class SegLineEx
    {
        public static List<List<int>> GetSegLineIntsecList(this List<LineSegment> segLines)
        {
            var seglineIntsecList = new List<List<int>>();

            for (int i = 0; i < segLines.Count; i++)
            {
                seglineIntsecList.Add(new List<int>());
                for (int j = 0; j < segLines.Count; j++)
                {
                    if (i == j) continue;
                    if (segLines[i].IsVertical() == segLines[j].IsVertical()) continue;
                    if (segLines[i].Intersection(segLines[j]) != null)
                    {
                        seglineIntsecList[i].Add(j);
                    }
                }
            }
            return seglineIntsecList;
        }
        public static void ExtendAndIntSect(this List<LineSegment> segLines, List<List<int>> seglineIntsecList)
        {
            for (int i = 0; i < segLines.Count; i++)
            {
                foreach (var j in seglineIntsecList[i])
                {
                    if (segLines[i].Intersection(segLines[j]) != null)//邻接表中连接的线不需要扩展
                    {
                        continue;
                    }
                    //两条线没有交上，进行延展
                    var linei = segLines[i];
                    var linej = segLines[j];
                    ExtendBoth(ref linei, ref linej);
                    segLines[i] = linei;
                    segLines[j] = linej;
                }
            }
        }
        //延长两根线使之正好相交
        private static void ExtendBoth(ref LineSegment line, ref LineSegment line2)
        {
            var IntSecPt = line.LineIntersection(line2);
            if (IntSecPt == null) return;
            line.ExtendToPoint(IntSecPt);
            line2.ExtendToPoint(IntSecPt);
        }
        public static List<(bool, bool)> GetSeglineConnectToBound(this List<LineSegment> SegLines, Polygon WallLine)
        {
            var seglineConnectToBound = new List<(bool, bool)>();
            for (int i = 0; i < SegLines.Count; i++)
            {
                var segLine = SegLines[i].ToLineString();
                var IntSection = segLine.Intersection(WallLine);
                
                var IntSecPts = segLine.Intersection(WallLine.Shell).Coordinates.OrderBy(c=>c.X+c.Y);
                if(IntSecPts.Count() == 0|| IntSection.IsEmpty)
                {
                    seglineConnectToBound.Add((false, false));
                    continue;
                }
                var mid = IntSection.Centroid;
                var lastCoor = IntSecPts.Last();
                var firstCoor = IntSecPts.First();
                var posConnected = (mid.X+mid.Y) < (lastCoor.X + lastCoor.Y);
                var negConnected = (mid.X + mid.Y) > (firstCoor.X + firstCoor.Y);
                seglineConnectToBound.Add((negConnected,posConnected));
            }
            return seglineConnectToBound;
        }
        public static void ExtendToBound(this List<LineSegment> SegLines, Polygon WallLine, List<(bool, bool)> seglineConnectToBound)
        {
            for (int i = 0; i < SegLines.Count; i++)
            {
                var segLine = SegLines[i];
                SegLines[i] = segLine.ExtendToBound(WallLine, seglineConnectToBound[i]);
            }
        }
        public static LineSegment ExtendToBound(this LineSegment SegLine, Polygon WallLine, (bool, bool) seglineConnectToBound)
        {
            if (!seglineConnectToBound.Item1 && !seglineConnectToBound.Item2) return SegLine;
            var SegLineStr = SegLine.ToLineString();
            if(!WallLine.Contains(SegLineStr.Centroid)) return SegLine;
            List<Coordinate> LineIntSecPts = null;
            var IntSection = SegLineStr.Intersection(WallLine);
            var mid = IntSection.Centroid;
            var IntSecPts = SegLineStr.Intersection(WallLine.Shell).Coordinates.OrderBy(c => c.X + c.Y);
            Coordinate P0;
            Coordinate P1;
            if(SegLine.P0.X + SegLine.P0.Y < SegLine.P1.X + SegLine.P1.Y)
            {
                P0 = SegLine.P0;
                P1 = SegLine.P1;
            }  
            else
            {
                P0 = SegLine.P1;
                P1 = SegLine.P0;
            }
            if (seglineConnectToBound.Item1 && !mid.Coordinate.ExistPtInDirection(IntSecPts, false))//负向需要连接，且未连接
            {
                if (LineIntSecPts == null) LineIntSecPts = SegLine.LineIntersection(WallLine.Shell).OrderBy(c => c.X + c.Y).ToList();
                var quryed = LineIntSecPts.Where(c => c.X + c.Y < mid.X + mid.Y);
                if(quryed.Count() > 0) P0 = quryed.Last();
            }
            if (seglineConnectToBound.Item2&&!mid.Coordinate.ExistPtInDirection(IntSecPts,true))
            {
                if (LineIntSecPts == null) LineIntSecPts = SegLine.LineIntersection(WallLine.Shell).OrderBy(c => c.X + c.Y).ToList();
                var quryed = LineIntSecPts.Where(c => c.X + c.Y > mid.X + mid.Y);
                if (quryed.Count() > 0) P0 = quryed.First();
            }
            return new LineSegment(P0, P1);
        }

        public static List<(int, int, int, int)> GetSegLineIntSecNode(this List<LineSegment> SegLines,List<Point> nodes)
        {
            var SegLineIntSecNode = new List<(int, int, int, int)>();

            foreach(var pt in nodes)
            {
                SegLineIntSecNode.Add(pt.GetNeighbor(SegLines));
            }
            return SegLineIntSecNode;
        }
        private static (int, int, int, int) GetNeighbor(this Point pt, List<LineSegment> SegLines)
        {
            int top =-1;
            int bottom = -1;
            int left = -1;
            int right = -1;
            for(int i = 0; i < SegLines.Count; i++)
            {
                var segLine = SegLines[i];
                if(segLine.Distance(pt.Coordinate) < 1)
                {
                    if (segLine.IsVertical())
                    {
                        if (segLine.MidPoint.Y > pt.Coordinate.Y) top = i;
                        else bottom = i;
                    }
                    else
                    {
                        if (segLine.MidPoint.X > pt.Coordinate.X) right = i;
                        else left = i;
                    }
                }
            }
            if (top == -1 || bottom == -1 || left == -1 || right == -1) throw new Exception("Can not find All the neighbors");
            return(top,bottom,left,right);
        }

        public static void SeglinePrecut(this List<LineSegment> SegLines, Polygon WallLine, double prop = 0.8)
        {
            for (int i = 0; i < SegLines.Count; ++i)
            {
                var line1 = SegLines[i];
                for (int j = i; j < SegLines.Count; ++j)
                {
                    if (i == j) continue;
                    var line2 = SegLines[j];
                    //找交点
                    var IntSecPt = new Point(line1.Intersection(line2));
                    if (IntSecPt.Coordinate != null)
                    {
                        if (!WallLine.Contains(IntSecPt))//交点在边界外，需要切割
                        {
                            //点不在区域内部，需要切割
                            //1.找到在边界上距离pt最近的点

                            //line1在边界上距离pt最近的点
                            var flag1 = GetNearestOnWall(line1, WallLine, IntSecPt, out Point wpt1);

                            //line2在边界上距离pt最近的点
                            var flag2 = GetNearestOnWall(line2, WallLine, IntSecPt, out Point wpt2);

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

                            //在边界内的切一下
                            if (flag1)
                            {
                                CutLine(ref line1, wpt1, IntSecPt, prop);
                                SegLines[i] = line1;
                            }
                            if (flag2)
                            {
                                CutLine(ref line2, wpt2, IntSecPt, prop);
                                SegLines[j] = line2;
                            }
                        }
                    }
                }
            }
        }

        private static bool GetNearestOnWall(LineSegment line, Polygon Area, Point IntPt, out Point wpt)
        {
            var templ = Area.Shell.GetIntersectPts(line);
            if (templ.Count == 0)
            {
                wpt = new Point(0,0);
                return false;
                //throw new ArgumentException("线不与边界相交");
            }

            if (templ.Count == 1)
            {
                wpt = templ.First();
                //return IntPt.DistanceTo(wpt);
            }

            else
            {
                if (line.IsVertical)
                {
                    templ = templ.OrderBy(pt => pt.Y).ToList();
                    if (IntPt.Distance(templ.First()) < IntPt.Distance(templ.Last()))
                    {
                        wpt = templ.First();
                    }
                    else
                    {
                        wpt = templ.Last();
                    }
                    //return IntPt.DistanceTo(wpt);
                }
                else
                {
                    templ = templ.OrderBy(pt => pt.X).ToList();
                    if (IntPt.Distance(templ.First()) < IntPt.Distance(templ.Last()))
                    {
                        wpt = templ.First();
                    }
                    else
                    {
                        wpt = templ.Last();
                    }
                    //return IntPt.DistanceTo(wpt);
                }
            }
            return true;
        }
        private static void CutLine(ref LineSegment line, Point _wpt, Point _pt, double prop)
        {
            // 把line切割,在边界外的线只保留交点和墙线之前部分的prop比例的线
            // wpt 在墙线上的点
            // pt 在外部的交点
            // prop 切断后留的比例
            var wpt = new Coordinate(_wpt.X, _wpt.Y);
            var pt = new Coordinate(_pt.X, _pt.Y);
            var spt = line.P0;
            var ept = line.P1;
            double dis = pt.Distance(wpt);
            Coordinate tempPT;// 切割后的线的一个端点
            if (line.IsVertical)
            {
                if (wpt.Y < pt.Y)
                {
                    tempPT = new Coordinate(wpt.X, wpt.Y + dis * prop);
                }
                else
                {
                    tempPT = new Coordinate(wpt.X, wpt.Y - dis * prop);
                }
            }
            else
            {
                if (wpt.X < pt.X)
                {
                    tempPT = new Coordinate(wpt.X + dis * prop, wpt.Y);
                }
                else
                {
                    tempPT = new Coordinate(wpt.X - dis * prop, wpt.Y);
                }
            }
            if (spt.Distance(wpt) < spt.Distance(pt))
            {
                // 起始点在保留的一侧
                line.P1 = tempPT;
            }
            else
            {
                // 终点在保留的一侧
                line.P0 = tempPT;
            }
        }
        //移除孤立分割线
        public static void Clean(this List<LineSegment> SegLines)
        {
            for (int i = SegLines.Count; i-- > 0;)
            {
                if (!SegLines.ConnectWithAny(i))
                {
                    SegLines.RemoveAt(i);
                }
            }
        }
        public static void CleanLineWithOneIntSecPt(this List<LineSegment> SegLines, Polygon Area)
        {
            for (int i = SegLines.Count - 1; i >= 0; i--)
            {
                var segLine = SegLines[i];
                if (GetAllIntSecPs(i, SegLines, Area).Count < 2) SegLines.RemoveAt(i);//移除仅有一个交点的线

            }
        }
        //判断分割线是否全部相连
        public static bool Allconnected(this List<LineSegment> SegLines)
        {
            if(SegLines.Count == 0) return false;
            var CheckedLines = new List<LineSegment>();
            CheckedLines.Add(SegLines[0]);
            var rest_idx = new List<int>();
            for (int i = 1; i < SegLines.Count; ++i) rest_idx.Add(i);
            while (rest_idx.Count != 0)
            {
                var curCount = rest_idx.Count;// 记录列表个数
                for (int j = 0; j < curCount; ++j)
                {
                    var idx = rest_idx[j];
                    var line = SegLines[idx];
                    if (line.ConnectWithAny(CheckedLines))
                    {
                        CheckedLines.Add(line);
                        rest_idx.RemoveAt(j);
                        break;
                    }
                    if (j == curCount - 1)
                    {
                        return false;// 当前线不与任何线相交
                    }

                }
            }
            return true;
        }
        public static List<List<LineSegment>> GroupSegLines(this List<LineSegment> SegLines)
        {
            var groups = new List<List<LineSegment>>();
            var rest_idx = new List<int>();
            for (int i = 0; i < SegLines.Count; ++i) rest_idx.Add(i);
            while (rest_idx.Count != 0)
            {
                bool new_group = true;
                foreach (var group in groups)
                {
                    foreach(var idx in rest_idx)
                    {
                        var line = SegLines[idx];
                        if (line.ConnectWithAny(group))
                        {
                            new_group = false;
                            group.Add(line);
                            rest_idx.Remove(idx);
                            break;
                        }
                    }
                    if (!new_group) break;
                }
                if (new_group)
                {
                    groups.Add(new List<LineSegment> { SegLines[rest_idx.First()] });
                    rest_idx.RemoveAt(0);
                }
            }
            return groups;
        }
        //判断idx位置的分割线是否与其他的存在相交关系
        public static bool ConnectWithAny(this List<LineSegment> SegLines, int idx)
        {
            var thisline = SegLines[idx];
            for(int i = 0; i < SegLines.Count; i++)
            {
                if (i == idx) continue;
                LineSegment line = SegLines[i];
                if(thisline.Intersection(line) != null) return true;
            }
            return false;
        }
        public static List<LineSegment> RemoveOuterLine(List<LineSegment> SegLines, Polygon Area)
        {
            List<LineSegment> SegLines2 = new List<LineSegment>();
            foreach (var l in SegLines)
            {
                if (Area.Shell.Intersects(l.GetLineString())) SegLines2.Add(l);
            }
            return SegLines2;
        }

        public static List<LineSegment> GetVaildSegLines(this List<LineSegment> seglines, Polygon Area, double tolProp = 2)
        {
            var vaildSegLines = new List<LineSegment>();
            for (int i = 0; i < seglines.Count; ++i)
            {
                var vaildSegLine = GetVaildSegLine(i, seglines,Area, tolProp);
                if(vaildSegLine != null) vaildSegLines.Add(vaildSegLine);
            }
            return vaildSegLines;
        }
        // tolProp 保留的车位宽的数量
        public static LineSegment GetVaildSegLine(int idx, List<LineSegment> seglines, Polygon Area, double tolProp )
        {
            var pts = GetAllIntSecPs(idx, seglines, Area, tolProp);
            var vaildSegLine = new LineSegment(pts.First().Coordinate, pts.Last().Coordinate);
            if (vaildSegLine.Length > 0) return vaildSegLine;
            else return null;
        }
        //获取segline中某一跟全部的交点
        //跟外边框的交点选取最外的交点-有效长度
        public static List<Point> GetAllIntSecPs(int idx, List<LineSegment> segline, Polygon Area,double tolProp = 2)
        {
            double tol = VMStock.VerticalSpotWidth * tolProp;// 与边界连接线忽略的长度
            var IntSecPoints = new List<Point>();//交点列表
            var line1 = segline[idx];
            var VerticalDirection = line1.IsVertical;
            var templ = Area.Shell.GetIntersectPts(line1);
            if (templ.Count != 0)//初始线和外包框有交点
            {
                Point pt1;
                Point pt2;
                if (templ.Count < 2)
                {
                    var pt = templ.First();
                    if (VerticalDirection)
                    {
                        pt1 = new Point(pt.X, pt.Y + tol);
                        pt2 = new Point(pt.X, pt.Y - tol);
                    }
                    else
                    {
                        pt1 = new Point(pt.X + tol, pt.Y);
                        pt2 = new Point(pt.X - tol, pt.Y);
                    }
                    if (Area.Contains(pt1)) IntSecPoints.Add(pt1);
                    else IntSecPoints.Add(pt2);
                }
                else
                {
                    if (VerticalDirection)
                    {
                        templ = templ.OrderBy(i => i.Y).ToList();// 垂直order by Y
                        pt1 = templ.First();
                        pt1 = new Point(pt1.X, pt1.Y + tol);//取不到两个车位的阈值
                        pt2 = templ.Last();
                        pt2 = new Point(pt2.X, pt2.Y - tol);
                    }
                    else
                    {
                        templ = templ.OrderBy(i => i.X).ToList();//水平orderby X
                        pt1 = templ.First();
                        pt1 = new Point(pt1.X + tol, pt1.Y);
                        pt2 = templ.Last();
                        pt2 = new Point(pt2.X - tol, pt2.Y);
                    }
                    IntSecPoints.Add(pt1);
                    IntSecPoints.Add(pt2);
                }
            }
            for (int i = 0; i < segline.Count; ++i)
            {
                if (i == idx) continue;
                var line2 = segline[i];
                var pt = line1.Intersection(line2);
                if (pt != null) IntSecPoints.Add(new Point(pt));
            }
            if (VerticalDirection) return IntSecPoints.OrderBy(i => i.Y).ToList();
            else return IntSecPoints.OrderBy(i => i.X).ToList();
        }
        //有效分割线是否满足车道宽

        //有效分割线的准确计算方法
        public static List<LineSegment> GetVaildLanes(this List<LineSegment> seglines, Polygon Area, MNTSSpatialIndex BoundaryObjectsSPIDX)
        {
            var vaildLanes = new List<LineSegment>();
            for (int i = 0; i < seglines.Count; ++i)
            {
                var vaildSegLine = GetVaildLane(i, seglines, Area, BoundaryObjectsSPIDX);
                vaildLanes.Add(vaildSegLine);
            }
            return vaildLanes;
        }
        // spIndex 墙线转化为线段，+ 可穿障碍物的spatialindex
        public static LineSegment GetVaildLane(int idx, List<LineSegment> seglines, Polygon Area, MNTSSpatialIndex BoundaryObjectsSPIDX)
        {
            // 获取和地库边界的最远两个交点
            var segline = seglines[idx];
            var VerticalDirection = segline.IsVertical();
            var pts = segline.GetIntSecPointWithWall(Area);
            var IntSecPoints = GetAllIntSecPs(idx, seglines);
            var BasePt = new Point(seglines[idx].MidPoint);
            Point Spt =null;
            Point Ept = null;
            LineSegment vaildLane;
            if (IntSecPoints.Count != 0)
            {
                Spt = IntSecPoints.First();
                Ept = IntSecPoints.Last();
            }
            if (pts.Item1!= null)//坐标减少方向有区域交点
            {
                if (IntSecPoints.Count != 0) BasePt = IntSecPoints.First();
                var baseLine = BasePt.LineBuffer((VMStock.RoadWidth-0.05) / 2, segline);
                var buffer = baseLine.GetHalfBuffer(segline, false);
                var objs = new GeometryCollection(BoundaryObjectsSPIDX.SelectCrossingGeometry(buffer).ToArray()).Intersection(buffer);
                var distance = baseLine.ToLineString().Distance(objs)-0.1;
                if (VerticalDirection) Spt = BasePt.Move(distance, 1);
                else Spt = BasePt.Move(distance, 2);
            }
            if (pts.Item2 != null)//坐标增加方向有区域交点
            {
                if (IntSecPoints.Count != 0) BasePt = IntSecPoints.Last();
                var baseLine = BasePt.LineBuffer((VMStock.RoadWidth - 0.05) / 2, segline);
                var buffer = baseLine.GetHalfBuffer(segline, true);
                var objs = new GeometryCollection(BoundaryObjectsSPIDX.SelectCrossingGeometry(buffer).ToArray()).Intersection(buffer);
                var distance = baseLine.ToLineString().Distance(objs)-0.1;
                if (VerticalDirection) Ept = BasePt.Move(distance, 0);
                else Ept = BasePt.Move(distance, 3);
            }
            if(Spt!= null && Ept != null)
            {
                vaildLane = new LineSegment(Spt.Coordinate, Ept.Coordinate);
                if (vaildLane.Length > 1) return vaildLane;
            }
            return null;
        }

        public static (Point,Point) GetIntSecPointWithWall(this LineSegment segline, Polygon Area)
        {
            var templ = Area.Shell.GetIntersectPts(segline);
            var VerticalDirection = segline.IsVertical();
            var midPoint = new Point(segline.MidPoint);
            Point pt1 = null;//X或者Y坐标比中点小的点
            Point pt2 = null;//X或者Y坐标比中点大的点
            if (templ.Count != 0)//初始线和外包框有交点
            {
                if (templ.Count == 1)
                {
                    var pt = templ.First();
                    if (VerticalDirection)
                    {
                        if (!(midPoint.Y > pt.Y ^ Area.Contains(midPoint))) pt1 = pt;
                        else pt2 = pt;
                    }
                    else
                    {
                        if (!(midPoint.X > pt.X ^ Area.Contains(midPoint))) pt1 = pt;
                        else pt2 = pt;
                    }
                }
                else
                {
                    if (VerticalDirection)
                    {
                        templ = templ.OrderBy(i => i.Y).ToList();// 垂直order by Y
                        pt1 = templ.First();
                        pt2 = templ.Last();
                    }
                    else
                    {
                        templ = templ.OrderBy(i => i.X).ToList();//水平orderby X
                        pt1 = templ.First();
                        pt2 = templ.Last();
                    }
                }
            }
            return (pt1, pt2);
        }
        public static List<Point> GetAllIntSecPs(this List<LineSegment> seglines)
        {
            var pts = new HashSet<Point>();
            for (int i = 0; i < seglines.Count-1; ++i)
            {
                var segline = seglines[i];
                var VerticalDirection = segline.IsVertical();
                for (int j = i+1; j < seglines.Count; ++j)
                {
                    var segline2 = seglines[j];
                    if (VerticalDirection != segline2.IsVertical())
                    {
                        var pt = segline.Intersection(segline2);
                        if (pt != null) pts.Add(new Point(pt));
                    }
                }
            }
            return pts.ToList();
        }
        public static List<Point> GetAllIntSecPs(int idx, List<LineSegment> seglines)//获取分割线交点
        {
            var segline = seglines[idx];
            var VerticalDirection = segline.IsVertical();
            var IntSecPoints = new List<Point>();//交点列表
            for (int i = 0; i < seglines.Count; ++i)
            {
                if (i == idx) continue;
                var line2 = seglines[i];
                var pt = segline.Intersection(line2);
                if (pt != null) IntSecPoints.Add(new Point(pt));
            }
            if (VerticalDirection) return IntSecPoints.OrderBy(i => i.Y).ToList();
            else return IntSecPoints.OrderBy(i => i.X).ToList();
        }
        public static bool VaildLaneWidthSatisfied(this List<LineSegment> VaildSegLines, MNTSSpatialIndex BoundarySpatialIndex)
        {
            double tol = VMStock.RoadWidth  -0.1;// 5500 -0.1
            for (int i = 0; i < VaildSegLines.Count; i++)
            {
                var segline = VaildSegLines[i];
                if (segline == null) continue;
                var rect = segline.GetRect(tol);
                var rst = BoundarySpatialIndex.SelectCrossingGeometry(rect);
                if (rst.Count > 0)
                {
                    return false;
                }
            }
            return true;
        }

        public static Polygon GetRect(this LineSegment segline, double width)
        {
            var distance = width / 2;
            if (segline.IsVertical())
            {
                var p_org = new Coordinate(segline.P0.X - distance, segline.P0.Y);
                var coors = new Coordinate[] { p_org,
                                               new Coordinate(segline.P0.X+distance, segline.P0.Y),
                                               new Coordinate(segline.P1.X+distance, segline.P1.Y),
                                               new Coordinate(segline.P1.X-distance, segline.P1.Y),
                                               p_org};
                return new Polygon(new LinearRing(coors));
            }
            else
            {
                var p_org = new Coordinate(segline.P0.X, segline.P0.Y - distance);
                var coors = new Coordinate[] { p_org,
                                               new Coordinate(segline.P0.X, segline.P0.Y +distance),
                                               new Coordinate(segline.P1.X, segline.P1.Y +distance),
                                               new Coordinate(segline.P1.X, segline.P1.Y -distance),
                                               p_org};
                return new Polygon(new LinearRing(coors));
            }
        }
        public static Polygon GetHalfBuffer(this LineSegment segline, double buffersize,bool positive)
        {
            double distance;
            if (positive) distance = buffersize;
            else distance = -buffersize;

            if (segline.IsVertical())
            {
                var coors = new Coordinate[] { segline.P0.Copy(),
                                               new Coordinate(segline.P0.X+distance, segline.P0.Y),
                                               new Coordinate(segline.P1.X+distance, segline.P1.Y),
                                               new Coordinate(segline.P1.X, segline.P1.Y),
                                               segline.P0.Copy()};
                return new Polygon(new LinearRing(coors));
            }
            else
            {
                var coors = new Coordinate[] { segline.P0.Copy(),
                                               new Coordinate(segline.P0.X, segline.P0.Y +distance),
                                               new Coordinate(segline.P1.X, segline.P1.Y +distance),
                                               new Coordinate(segline.P1.X, segline.P1.Y),
                                               segline.P0.Copy()};
                return new Polygon(new LinearRing(coors));
            }
        }

        public static Polygon GetHalfBuffer(this LineSegment segline, LineSegment segline2, bool positive)
        {
            if (segline.IsVertical() == segline2.IsVertical()) throw (new ArgumentException("Two Line must be perpendicular"));
            double distance;
            if (segline.IsVertical())
            {
                if (!(segline2.P0.X < segline.P0.X ^ positive)) distance = segline.Distance(segline2.P1);
                else distance = segline.Distance(segline2.P0);
            }
            else
            {
                if (!(segline2.P0.Y < segline.P0.Y ^ positive)) distance = segline.Distance(segline2.P1);
                else distance = segline.Distance(segline2.P0);
            }
            return segline.GetHalfBuffer(distance, positive);
        }
    }
}
