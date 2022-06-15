using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore;
using ThMEPWSS.Assistant;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.Uitl;
using ThMEPWSS.UndergroundFireHydrantSystem.Method;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundSpraySystem.General;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public static class PipeLineList
    {
        public static void PipeLineAutoConnect(List<Line> lineList, FireHydrantSystemIn fireHydrantSysIn)
        {
            var GLineSegList = new List<GLineSegment>();//line 转 GLineSegment
            lineList = CleanLaneLines3(lineList);
            PtDic.CreatePtDic(fireHydrantSysIn, lineList);//字典对更新  
            foreach (var l in lineList)
            {
                var GLineSeg = new GLineSegment(l.StartPoint.X, l.StartPoint.Y, l.EndPoint.X, l.EndPoint.Y);
                GLineSegList.Add(GLineSeg);
            }
            var GLineConnectList = GeoFac.AutoConn(GLineSegList, 880, 1);//打断部分 自动连接
            var GLineConnectList2 = GeoFac.AutoConn(GLineSegList, 20, 30);//打断部分 自动连接
            foreach (var gl in GLineConnectList)
            {
                try
                {
                    var pt1 = new Point3dEx(gl.StartPoint.X, gl.StartPoint.Y, 0);
                    var pt2 = new Point3dEx(gl.EndPoint.X, gl.EndPoint.Y, 0);
                    if (pt1.DistanceToEx(pt2) > 1000 || pt1.DistanceToEx(pt2) < 1)
                    {
                        continue;
                    }
                    if (fireHydrantSysIn.PtDic.ContainsKey(pt1) && fireHydrantSysIn.PtDic.ContainsKey(pt2))
                    {
                        if (fireHydrantSysIn.PtDic[pt1].Count >= 2 || fireHydrantSysIn.PtDic[pt2].Count >= 2)
                        {
                            continue;
                        }
                    }

                    var line = new Line(pt1._pt, pt2._pt);
                    lineList.Add(line);
                }
                catch
                {
                    ;
                }
                
            }
            foreach (var gl in GLineConnectList2)
            {
                try
                {
                    var pt1 = new Point3dEx(gl.StartPoint.X, gl.StartPoint.Y, 0);
                    var pt2 = new Point3dEx(gl.EndPoint.X, gl.EndPoint.Y, 0);
                    
                    if (pt1.DistanceToEx(pt2) > 20 || pt1.DistanceToEx(pt2) < 1)
                    {
                        continue;
                    }
                    if (fireHydrantSysIn.PtDic.ContainsKey(pt1) && fireHydrantSysIn.PtDic.ContainsKey(pt2))
                    {
                        if (fireHydrantSysIn.PtDic[pt1].Count >= 2 || fireHydrantSysIn.PtDic[pt2].Count >= 2)
                        {
                            continue;
                        }
                    }

                    var line = new Line(pt1._pt, pt2._pt);
                    lineList.Add(line);
                }
                catch
                {
                    ;
                }

            }
            //lineList = CleanLaneLines3(lineList);
        }

        public static void PipeLineAutoConnect(List<Line> lineList)
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

        public static void RemoveFalsePipe(List<Line> lineList, List<Point3dEx> hydrantPosition)
        {
            foreach (var line in lineList.ToArray())//删除两个点都是端点的线段
            {
                if (PtInPtList.PtIsTermLine(line, hydrantPosition))
                {
                    lineList.Remove(line);
                }
            }
        }

        public static void ConnectWithVertical(List<Line> lineList, FireHydrantSystemIn fireHydrantSysIn, List<Line> labelLine)
        {
            //基于竖管连接管线
            var pipeLinesSaptialIndex = new ThCADCoreNTSSpatialIndex(lineList.ToCollection());
            var labelLineSaptialIndex = new ThCADCoreNTSSpatialIndex(labelLine.ToCollection());
            var lines = new List<Line>();
            var connectVreticals = new List<Point3dEx>();
            var sePts = fireHydrantSysIn.StartEndPts;
            foreach (var ver in fireHydrantSysIn.VerticalPosition)
            {
                foreach(var sept in sePts)
                {
                    if(ver.DistanceToEx(sept)< 120)
                    {
                        continue;//到起始终止点距离小于tor的立管直接删除
                    }
                }
                var rect = ver._pt.GetRect(120);
                var dbObjs = pipeLinesSaptialIndex.SelectCrossingPolygon(rect);
                var labelLineObjs = labelLineSaptialIndex.SelectCrossingPolygon(rect);
                var isMiddleRiser = (labelLineObjs.Count == 1 && dbObjs.Count == 2);//连接两段管线且有标注线穿过的立管

                var flag = fireHydrantSysIn.AddNewPtDic(dbObjs, ver._pt, ref lines, isMiddleRiser);
                if (dbObjs.Count >= 2 && !isMiddleRiser)
                {
                    connectVreticals.Add(ver);
                }
                else if (dbObjs.Count == 1)
                {
                    var l = dbObjs[0] as Line;
                    var closedPt = l.GetClosedPt(ver);//获取最近点
                    var cl = new Line(closedPt, ver._pt);
                    if (cl.Length > 1.0 && cl.Length < 120)
                    {
                        lineList.Add(cl);
#if DEBUG
                        //using (AcadDatabase acadDatabase = AcadDatabase.Active())
                        //{
                        //    var layerNames = "立管和支管的单链接线";
                        //    if (!acadDatabase.Layers.Contains(layerNames))
                        //    {
                        //        ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                        //    }
                        //    cl.LayerId = DbHelper.GetLayerId(layerNames);
                        //    cl.ColorIndex = (int)ColorIndex.Red;
                        //    acadDatabase.CurrentSpace.Add(cl);

                        //}
#endif
                    }

                }
            }

            lineList.AddRange(lines);
        }

        public static void ConnectClosedPt(List<Line> lineList, FireHydrantSystemIn fireHydrantSysIn)
        {
            var pts = new List<Point3dEx>();
            foreach(var pt in fireHydrantSysIn.PtDic.Keys)
            {
                if(fireHydrantSysIn.PtDic[pt].Count == 1)
                {
                    pts.Add(pt);
                }
            }
            var usedPts = new HashSet<int>();
            for(int i = 0; i < pts.Count - 1; i++)
            {
                if(usedPts.Contains(i))
                {
                    continue;
                }
                for(int j = i + 1; j < pts.Count; j++)
                {
                    if (usedPts.Contains(j))
                    {
                        continue;
                    }
                    var dist = pts[i].DistanceToEx(pts[j]);

                    if (dist < 20)
                    {
                        usedPts.Add(i);
                        usedPts.Add(j);
                        lineList.Add(new Line(pts[i]._pt, pts[j]._pt));
                    }
                }
            }
            var lineSpatialIndex = new ThCADCoreNTSSpatialIndex(lineList.ToCollection());
            for(int i = 0; i < pts.Count; i++)
            {
                
                if(usedPts.Contains(i))
                {
                    continue;
                }
                var pt = pts[i];
                var rect = pt._pt.GetRect(10);
                var rst = lineSpatialIndex.SelectCrossingPolygon(rect);
                var line = GetNeighborLine(rst,pt._pt, out Point3d closedPt);
                if(line is not null)
                {
                    //var closedPt = line.GetClosestPointTo(pt._pt, false);
                    lineList.Remove(line);
                    lineList.Add(new Line(closedPt, pt._pt));
                    lineList.Add(new Line(line.StartPoint, closedPt));
                    lineList.Add(new Line(line.EndPoint, closedPt));
                }
            }
        }

        private static Line GetNeighborLine(DBObjectCollection objs, Point3d pt, out Point3d closedPt)
        {
            foreach(var obj in objs)
            {
                var line = obj as Line;
                closedPt = line.GetClosestPointTo(pt, false);
                var dist = closedPt.DistanceTo(pt);
                if(dist<10 && dist >1)
                {
                    return line;
                }
            }
            closedPt = new Point3d();
            return null;
        }

        public static void ConnectBreakLineWithoutPtdic(List<Line> lineList, FireHydrantSystemIn fireHydrantSysIn,
            List<Point3dEx> pointList, List<Point3dEx> stopPts)
        {
            double tor = 10.0;//断开阈值
            //连接不是端点的孤立线段
            var connectLine = new List<Line>();
            foreach (var line in lineList)
            {
                if (line.Length < 50)
                {
                    continue;//把一些短线直接跳过
                }
                var pt1 = new Point3dEx(line.StartPoint);
                var pt2 = new Point3dEx(line.EndPoint);
                var flag1 = pt1.IsNotStopPt(stopPts, tor);
                var flag2 = pt2.IsNotStopPt(stopPts, tor);
                CreateNewConnectLine(lineList, fireHydrantSysIn, tor, connectLine, line, pt1, flag1);
                CreateNewConnectLine(lineList, fireHydrantSysIn, tor, connectLine, line, pt2, flag2);
            }
            foreach (var l in connectLine)
            {
                lineList.Add(l);
            }
            foreach (var pt1 in pointList)
            {
                foreach (var pt2 in pointList)
                {
                    double dist = pt1.DistanceToEx(pt2);
                    if (dist < 10 && dist > 1)
                    {
                        var line = new Line(pt1._pt, pt2._pt);
                        if (!lineList.Contains(line))
                        {
                            lineList.Add(new Line(pt1._pt, pt2._pt));
                        }
                    }
                }
            }
            lineList = CleanLaneLines3(lineList);

            PtDic.CreatePtDic(fireHydrantSysIn, lineList);//字典对更新 
        }

        private static void CreateNewConnectLine(List<Line> lineList, FireHydrantSystemIn fireHydrantSysIn, double tor, List<Line> connectLine, Line line, Point3dEx pt1, bool flag1)
        {
            if (flag1 && !PtInPtList.PtIsTermPt(pt1, fireHydrantSysIn.VerticalPosition))
            {
                foreach (var l in lineList)
                {
                    if (l.GetClosestPointTo(pt1._pt, false).DistanceTo(pt1._pt) < tor && !l.Equals(line))
                    {
                        var pts = new Point3dCollection();
                        l.IntersectWith(line, (Intersect)2, pts, (IntPtr)0, (IntPtr)0);
                        if (pts.Count > 0)
                        {
                            if (pts[0].DistanceTo(pt1._pt) < tor && pts[0].DistanceTo(pt1._pt) > 1)
                            {
                                connectLine.Add(new Line(pts[0], pt1._pt));
                            }
                        }
                    }
                }
            }
        }

        private static bool IsNotStopPt(this Point3dEx pt, List<Point3dEx> stopPts, double tor)
        {
            foreach (var stop in stopPts)
            {
                if (pt._pt.DistanceTo(stop._pt) < tor)
                {
                    return false;
                }
            }
            return true;
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
            var pairPt = pts.GetCollinearMaxPts2();
            return new LineSegment2d(pairPt.Item1.ToPoint2d(), pairPt.Item2.ToPoint2d());
        }
    }
}
