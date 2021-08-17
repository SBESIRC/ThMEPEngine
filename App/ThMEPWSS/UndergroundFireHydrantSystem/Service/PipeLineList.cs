using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using ThMEPWSS.Assistant;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Uitl;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    class PipeLineList
    {
        public static void PipeLineAutoConnect(ref List<Line> lineList, ref FireHydrantSystemIn fireHydrantSysIn)
        {
            var GLineSegList = new List<GLineSegment>();//line 转 GLineSegment
            lineList = CleanLaneLines3(lineList);
            foreach (var l in lineList)
            {
                var GLineSeg = new GLineSegment(l.StartPoint.X, l.StartPoint.Y, l.EndPoint.X, l.EndPoint.Y);
                GLineSegList.Add(GLineSeg);
            }

            var GLineConnectList = GeoFac.AutoConn(GLineSegList, 1000, 1);//打断部分 自动连接
            foreach (var l in GLineConnectList)
            {
                GLineSegList.Add(l);
            }
            lineList = new List<Line>();//GLineSegment 转 line
            foreach (var gl in GLineSegList)
            {
                var pt1 = new Point3dEx(gl.StartPoint.X, gl.StartPoint.Y, 0);
                var pt2 = new Point3dEx(gl.EndPoint.X, gl.EndPoint.Y, 0);
                if (pt1.Equals(new Point3dEx(0, 0, 0)) || pt2.Equals(new Point3dEx(0, 0, 0)))
                {
                    ;
                }
                var line = new Line(pt1._pt, pt2._pt);
                if(line.Length<1)
                {
                    continue;
                }
                lineList.Add(line);
                if(!fireHydrantSysIn.PtDic[pt1].Contains(pt2))
                {
                    fireHydrantSysIn.PtDic[pt1].Add(pt2);
                }
                if (!fireHydrantSysIn.PtDic[pt2].Contains(pt1))
                {
                    fireHydrantSysIn.PtDic[pt2].Add(pt1);
                }
            }
            lineList = CleanLaneLines3(lineList);
        }

         public static void PipeLineAutoConnect(ref List<Line> lineList)
        {
            var GLineSegList = new List<GLineSegment>();//line 转 GLineSegment
            lineList = CleanLaneLines3(lineList);
            foreach (var l in lineList)
            {
                var GLineSeg = new GLineSegment(l.StartPoint.X, l.StartPoint.Y, l.EndPoint.X, l.EndPoint.Y);
                GLineSegList.Add(GLineSeg);
            }

            var GLineConnectList = GeoFac.AutoConn(GLineSegList,  1000, 1);//打断部分 自动连接

            foreach (var l in GLineConnectList)
            {
                GLineSegList.Add(l);
            }

            var tmpLines = new List<Line>();//GLineSegment 转 line
            foreach (var gl in GLineSegList)
            {
                var pt1 = new Point3d(gl.StartPoint.X, gl.StartPoint.Y, 0);
                var pt2 = new Point3d(gl.EndPoint.X, gl.EndPoint.Y, 0);
                var line = new Line(pt1, pt2);
                tmpLines.Add(line);
            }
            lineList = CleanLaneLines3(tmpLines);
        }


        public static void RemoveFalsePipe(ref List<Line> lineList, List<Point3dEx> hydrantPosition)
        {
            foreach (var line in lineList.ToArray())//删除两个点都是端点的线段
            {
                if (PtInPtList.PtIsTermLine(line, hydrantPosition))
                {
                    lineList.Remove(line);
                }
            }
        }

        public static void ConnectBreakLine(ref List<Line> lineList, FireHydrantSystemIn fireHydrantSysIn, 
            ref List<Point3dEx> pointList, List<Point3dEx> stopPts)
        {
            //连接不是端点的孤立线段
            var connectLine = new List<Line>();
            foreach(var line in lineList)
            {
                var pt1 = new Point3dEx(line.StartPoint);
                var pt2 = new Point3dEx(line.EndPoint);
                var flag1 = true;
                var flag2 = true;
                foreach(var stop in stopPts)
                {
                    if(pt1._pt.DistanceTo(stop._pt) < 10)
                    {
                        flag1 = false;
                        break;
                    }
                    if (pt2._pt.DistanceTo(stop._pt) < 10)
                    {
                        flag2 = false;
                        break;
                    }
                }
                if(line.Length < 50)
                {
                    continue;//把一些短线直接跳过
                }
                if (flag1 && fireHydrantSysIn.PtDic[pt1].Count == 1 && !PtInPtList.PtIsTermPt(pt1, fireHydrantSysIn.HydrantPosition))
                {
                    foreach(var l in lineList)
                    {
                        if(l.GetClosestPointTo(pt1._pt, false).DistanceTo(pt1._pt) < 150 && !l.Equals(line))
                        {
                            var pts = new Point3dCollection();
                            l.IntersectWith(line, (Intersect)2, pts, (IntPtr)0, (IntPtr)0);
                            if(pts.Count > 0)
                            {
                                if (pts[0].DistanceTo(pt1._pt) < 150 && pts[0].DistanceTo(pt1._pt) > 1)
                                {
                                    connectLine.Add(new Line(pts[0], pt1._pt));
                                }
                            }
                            
                        }
                    }
                }
                if(flag2 && fireHydrantSysIn.PtDic[pt2].Count == 1 && !PtInPtList.PtIsTermPt(pt2, fireHydrantSysIn.HydrantPosition))
                {
                    foreach (var l in lineList)
                    {
                        if (l.GetClosestPointTo(pt2._pt, false).DistanceTo(pt2._pt) < 150 && !l.Equals(line))
                        {
                            var pts = new Point3dCollection();
                            l.IntersectWith(line, (Intersect)2, pts, (IntPtr)0, (IntPtr)0);
                            if (pts.Count > 0)
                            {
                                if (pts[0].DistanceTo(pt2._pt) < 150)
                                {
                                    connectLine.Add(new Line(pts[0], pt2._pt));
                                }
                            }

                        }
                    }
                }
            }
            foreach(var l in connectLine)
            {
                if(l.Length > 1)
                {
                    lineList.Add(l);
                    var pt1 = new Point3dEx(l.StartPoint);
                    var pt2 = new Point3dEx(l.EndPoint);
                    if (pt1.Equals(new Point3dEx(0, 0, 0)) || pt2.Equals(new Point3dEx(0, 0, 0)))
                    {
                        ;
                    }
                    if (!pointList.Contains(pt1))
                    {
                        pointList.Add(pt1);
                    }
                    if (!pointList.Contains(pt2))
                    {
                        pointList.Add(pt2);
                    }

                    if (fireHydrantSysIn.PtDic.ContainsKey(pt1))
                    {
                        if(!fireHydrantSysIn.PtDic[pt1].Contains(pt2))
                        {
                            fireHydrantSysIn.PtDic[pt1].Add(pt2);
                        }
                    }
                    else
                    {
                        var ptls = new List<Point3dEx>(){ pt2 };
                        fireHydrantSysIn.PtDic.Add(pt1, ptls);
                    }
                    if (fireHydrantSysIn.PtDic.ContainsKey(pt2))
                    {
                        if (!fireHydrantSysIn.PtDic[pt2].Contains(pt1))
                        {
                            fireHydrantSysIn.PtDic[pt2].Add(pt1);
                        }
                    }
                    else
                    {
                        var ptls = new List<Point3dEx>() { pt1 };
                        fireHydrantSysIn.PtDic.Add(pt2, ptls);
                    }
                }
            }

            foreach(var pt1 in pointList)
            {
                foreach(var pt2  in pointList)
                {

                    if(!pt1.Equals(pt2))
                    {
                        var line = new Line(pt1._pt, pt2._pt);
                        if(line.Length < 10)
                        {
                            if(!lineList.Contains(line))
                            {
                                lineList.Add(new Line(pt1._pt, pt2._pt));
                            }
                            if(!fireHydrantSysIn.PtDic[pt1].Contains(pt2))
                            {
                                fireHydrantSysIn.PtDic[pt1].Add(pt2);
                            }
                            if(!fireHydrantSysIn.PtDic[pt2].Contains(pt1))
                            {
                                fireHydrantSysIn.PtDic[pt2].Add(pt1);
                            }
                        }
                    }
                }
            }

        }

        public static List<Line> CleanLaneLines2(List<Line> lines)
        {
            var rstLines = new List<Line>();

            var lineSegs = lines.Select(l => new LineSegment2d(l.StartPoint.ToPoint2D(), l.EndPoint.ToPoint2D())).ToList();


            //Grouping
            List<HashSet<LineSegment2d>> lineSegGroups = new List<HashSet<LineSegment2d>>();
            for (int j = 0; j < lineSegs.Count(); ++j)
            {
                bool alreadyContains = false;
                foreach (var g in lineSegGroups)
                {
                    if (g.Contains(lineSegs[j]))
                    {
                        alreadyContains = true;
                        break;
                    }
                }

                if (alreadyContains) continue;

                var colinerSegs = lineSegs.Where(l => l.IsColinearTo(lineSegs[j])).ToHashSet();
                lineSegGroups.Add(colinerSegs);
            }
            
            //Processing
            foreach (var lg in lineSegGroups)
            {
                var processedSegs = new HashSet<LineSegment2d>();

                var tobeProcessedSegs = new List<LineSegment2d>(lg);
                var rstLineSegs = new List<LineSegment2d>();

                while (tobeProcessedSegs.Count != 0)
                {
                    tobeProcessedSegs = tobeProcessedSegs.Except(processedSegs).ToList();
                    if (tobeProcessedSegs.Count == 0) break;
                    var longestLineSeg = tobeProcessedSegs.OrderBy(l => l.Length).ToList().Last();
                    processedSegs.Add(longestLineSeg);
                    tobeProcessedSegs.Remove(longestLineSeg);

                    var tempMergedSeg = longestLineSeg;// new LineSegment2d(longestLineSeg.StartPoint, longestLineSeg.EndPoint);

                    for (var i = 0; i < tobeProcessedSegs.Count; ++i)
                    {
                        var curSeg = tobeProcessedSegs[i];

                        var ptSet = new HashSet<Point3dEx>();
                        ptSet.Add(new Point3dEx(tempMergedSeg.StartPoint.X, tempMergedSeg.StartPoint.Y, 0.0, 1E-5));
                        ptSet.Add(new Point3dEx(tempMergedSeg.EndPoint.X, tempMergedSeg.EndPoint.Y, 0.0, 1E-5));
                        ptSet.Add(new Point3dEx(curSeg.StartPoint.X, curSeg.StartPoint.Y, 0.0, 1E-5));
                        ptSet.Add(new Point3dEx(curSeg.EndPoint.X, curSeg.EndPoint.Y, 0.0, 1E-5));

                        var overlapedSeg = tempMergedSeg.Overlap(curSeg);
                        if (overlapedSeg == null ) 
                        {
                            //remove mid-point
                            if(ptSet.Count == 3)
                            {
                                var tempMergedSegPts = new HashSet<Point3dEx>();
                                tempMergedSegPts.Add(new Point3dEx(tempMergedSeg.StartPoint.X, tempMergedSeg.StartPoint.Y, 0.0, 1E-5));
                                tempMergedSegPts.Add(new Point3dEx(tempMergedSeg.EndPoint.X, tempMergedSeg.EndPoint.Y, 0.0, 1E-5));

                                var curPts = new HashSet<Point3dEx>();
                                curPts.Add(new Point3dEx(curSeg.StartPoint.X, curSeg.StartPoint.Y, 0.0, 1E-5));
                                curPts.Add(new Point3dEx(curSeg.EndPoint.X, curSeg.EndPoint.Y, 0.0, 1E-5));

                                var leftStart = tempMergedSegPts.Except(curPts).First();
                                var leftEnd = curPts.Except(tempMergedSegPts).First();

                                tempMergedSeg = new LineSegment2d(leftStart._pt.ToPoint2D(), leftEnd._pt.ToPoint2D());
                                processedSegs.Add(curSeg);

                            }
                            continue;
                        }

                        //having overlop
                        if (ptSet.Count == 3)
                        {
                            var tempMergedSegPts = new HashSet<Point3dEx>();
                            tempMergedSegPts.Add(new Point3dEx(tempMergedSeg.StartPoint.X, tempMergedSeg.StartPoint.Y,0.0,1E-5));
                            tempMergedSegPts.Add(new Point3dEx(tempMergedSeg.EndPoint.X, tempMergedSeg.EndPoint.Y,0.0,1E-5));

                            var tobeRemovedPt = ptSet.Except(tempMergedSegPts).First();

                            ptSet.Remove(tobeRemovedPt);

                        }
                        else if(ptSet.Count == 4)
                        {
                            ptSet.Remove(new Point3dEx(overlapedSeg.StartPoint.X, overlapedSeg.StartPoint.Y,0.0,1E-5));
                            ptSet.Remove(new Point3dEx(overlapedSeg.EndPoint.X, overlapedSeg.EndPoint.Y,0.0,1E-5));
                        }

                        System.Diagnostics.Debug.Assert(ptSet.Count() == 2);
                        if (ptSet.Count == 2)
                        {
                            tempMergedSeg = new LineSegment2d(ptSet.First()._pt.ToPoint2D(), ptSet.Last()._pt.ToPoint2D());
                            processedSegs.Add(curSeg);
                        }
                    }

                    rstLineSegs.Add(tempMergedSeg);
                }
                //rstLineSegs.AddRange(tobeProcessedSegs);
                
                rstLines.AddRange(rstLineSegs.Select(sg => new Line( sg.StartPoint.ToPoint3d(), sg.EndPoint.ToPoint3d())));
            }
            return rstLines;
        }

        public static List<Line> CleanLaneLines3(List<Line> lines)
        {
            var rstLines = new List<Line>();

            //Grouping
            var lineSegs = lines.Select(l => new LineSegment2d(l.StartPoint.ToPoint2D(), l.EndPoint.ToPoint2D())).ToList();
            List<HashSet<LineSegment2d>> lineSegGroups = new List<HashSet<LineSegment2d>>();

            while (lineSegs.Count() != 0)
            {
                var tmpLineSeg = lineSegs.First();
                bool alreadyContains = false;
                foreach (var g in lineSegGroups)
                {
                    if (g.Contains(tmpLineSeg))
                    {
                        alreadyContains = true;
                        break;
                    }
                }

                if (alreadyContains) continue;
                
                var colinerSegs = lineSegs.Where(l =>l.IsParallelTo(tmpLineSeg,new Tolerance(0.001,0.001))).ToHashSet();
                lineSegGroups.Add(colinerSegs);
                lineSegs = lineSegs.Except(colinerSegs).ToList();
            }

            foreach (var lg in lineSegGroups)
            {
                rstLines.AddRange(MergeGroupLines(lg));
            }

            return rstLines;
        }
        private static List<Line> MergeGroupLines(HashSet<LineSegment2d> lineGroup)
        {
            var rstLines = new List<Line>();
            while(lineGroup.Count != 0)
            {
                var l = lineGroup.First();
                lineGroup.Remove(l);
                rstLines.Add(MergeLine(ref l, ref lineGroup));
            }
            return rstLines;

        }

        private static Line MergeLine(ref LineSegment2d l, ref HashSet<LineSegment2d> lineGroup)
        {
            Line rstLine = new Line();

            MergeLineEx(ref l,ref lineGroup);
            rstLine.StartPoint = l.StartPoint.ToPoint3d();
            rstLine.EndPoint = l.EndPoint.ToPoint3d();
            return rstLine;
        }
        private static void MergeLineEx(ref LineSegment2d l, ref HashSet<LineSegment2d> lineGroup)
        {
            //如果 l 与 group里面任何一条线都没有交点，那么就把该l返回
            var overlapLine = IsOverlapLine(l, lineGroup);
            if (overlapLine.Count == 0)//如果没有相交
            {
                return;
            }
            else
            {
                //找到与l相交的线，然后，进行merge,并且把相交的线，从group里面删除
                l = MergeLineEX2(l, overlapLine);
                foreach (var line in overlapLine)
                {
                    lineGroup.Remove(line);
                }
                //merge 以后，继续执行MergeLine;
                MergeLineEx(ref l, ref lineGroup);
            }
        }

        private static HashSet<LineSegment2d> IsOverlapLine( LineSegment2d line,  HashSet<LineSegment2d> lineGroup)
        {
            HashSet<LineSegment2d> overlapLine = new HashSet<LineSegment2d>();
            foreach(var l in lineGroup)
            {
                if(IsOverlapLine(line,l))
                {
                    overlapLine.Add(l);
                }
            }

            return overlapLine;
        }

        private static bool IsOverlapLine(LineSegment2d firLine, LineSegment2d secLine)
        {
            var overlapedSeg = firLine.Overlap(secLine,new Tolerance(0.01,0.01));
            if (overlapedSeg != null)
            {
                return true;
            }
            else
            {
                var ptSet = new HashSet<Point3dEx>();
                var tol = 1E-2;
                ptSet.Add(new Point3dEx(firLine.StartPoint.X, firLine.StartPoint.Y, 0.0, tol));
                ptSet.Add(new Point3dEx(firLine.EndPoint.X, firLine.EndPoint.Y, 0.0, tol));
                ptSet.Add(new Point3dEx(secLine.StartPoint.X, secLine.StartPoint.Y, 0.0, tol));
                ptSet.Add(new Point3dEx(secLine.EndPoint.X, secLine.EndPoint.Y, 0.0, tol));
                if(ptSet.Count() == 3)
                {
                    return true;
                }
            }
            return false;
        }

        private static LineSegment2d MergeLineEX2(LineSegment2d line, HashSet<LineSegment2d> overlapLines)
        {
            List<Point3d> pts = new List<Point3d>();
            pts.Add(line.StartPoint.ToPoint3d());
            pts.Add(line.EndPoint.ToPoint3d());
            foreach(var l in overlapLines)
            {
                pts.Add(l.StartPoint.ToPoint3d());
                pts.Add(l.EndPoint.ToPoint3d());
            }
            var pairPt = pts.GetCollinearMaxPts();
            return new LineSegment2d(pairPt.Item1.ToPoint2d(), pairPt.Item2.ToPoint2d());
        }
    }
}
