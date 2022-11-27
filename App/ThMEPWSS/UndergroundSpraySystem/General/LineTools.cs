using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Service;
using ThMEPWSS.Assistant;
using ThMEPWSS.Uitl;
using ThMEPWSS.UndergroundFireHydrantSystem.Method;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;


namespace ThMEPWSS.UndergroundSpraySystem.General
{
    public static class LineTools
    {
        public static Line LineZ0(this Line line)
        {
            return new Line(line.StartPoint.Point3dZ0(), line.EndPoint.Point3dZ0());
        }

        public static Point3d GetClosedPt(this Line line, Point3dEx verticalPt)
        {
            var spt = line.StartPoint;
            var ept = line.EndPoint;
            var sdist = spt.DistanceTo(verticalPt._pt);
            var edist = ept.DistanceTo(verticalPt._pt);
            return sdist < edist ? spt : ept;
        }

        public static List<Line> DealPipeLines(List<Line> pipeLines, List<Point3d> alarmPts, SprayIn sprayIn)
        {
            pipeLines = PipeLineAutoConnect(pipeLines, sprayIn);//1. 对齐的线自动连接
            pipeLines = ConnectVerticalLine(pipeLines, sprayIn);//2. 依靠立管的线连接
            pipeLines = ConnectWithAlarmValve(pipeLines, sprayIn, alarmPts);//3. 连接报警阀连接的线
            pipeLines = PipeLineSplit(pipeLines);//4. 节点处打断
            return pipeLines;
        }

        public static List<Line> PipeLineAutoConnect(List<Line> lineList, SprayIn sprayIn)
        {
            var lineSpatialIndex = new ThCADCoreNTSSpatialIndex(lineList.ToCollection());
            var verticals = new List<Polyline>();
            foreach(var vpt in sprayIn.Verticals.Keys)
            {
                var rect = vpt._pt.GetRect(20);
                verticals.Add(rect);
            }
            var verticalSpatialIndex = new ThCADCoreNTSSpatialIndex(verticals.ToCollection());
            var GLineSegList = new List<GLineSegment>();
            foreach (var l in lineList)
            {
                var GLineSeg = new GLineSegment(l.StartPoint.X, l.StartPoint.Y, l.EndPoint.X, l.EndPoint.Y);
                GLineSegList.Add(GLineSeg);
            }
            var GLineConnectList = GeoFac.AutoConn(GLineSegList, 1001, 2);

            foreach (var gl in GLineConnectList)
            {
                var pt1 = new Point3d(gl.StartPoint.X, gl.StartPoint.Y, 0);
                var pt2 = new Point3d(gl.EndPoint.X, gl.EndPoint.Y, 0);

                if (pt1.DistanceTo(pt2) < 1) continue;

                var rect1 = pt1.GetRect(5);
                var rect2 = pt2.GetRect(5);

                var vrst1 = verticalSpatialIndex.SelectCrossingPolygon(rect1);
                var vrst2 = verticalSpatialIndex.SelectCrossingPolygon(rect2);
                if (vrst1.Count > 0 || vrst2.Count > 0) continue;

                var rst1 = lineSpatialIndex.SelectCrossingPolygon(rect1);
                var rst2 = lineSpatialIndex.SelectCrossingPolygon(rect2);
                
                if (rst1.Count==1||rst2.Count==1)
                {
                    var line = new Line(pt1, pt2);
                    lineList.Add(line);
                }
            }
            return lineList;
        }

        public static List<Line> ConnectVerticalLine(List<Line> pipeLines, SprayIn sprayIn)
        {
            //基于竖管连接管线
            var pipeLinesSaptialIndex = new ThCADCoreNTSSpatialIndex(pipeLines.ToCollection());

            var lines = new List<Line>();
            var connectVreticals = new List<Point3dEx>();
            foreach (var ver in sprayIn.Verticals.Keys)
            {
                double tor = sprayIn.Verticals[ver] + 5;
                var rect = ver._pt.GetRect(tor);
                var dbObjs = pipeLinesSaptialIndex.SelectCrossingPolygon(rect);
                var flag = sprayIn.AddNewPtDic(dbObjs, ver, ref lines);

                if (dbObjs.Count >= 2)
                {
                    connectVreticals.Add(ver);
                    var closedPt1 = (dbObjs[0] as Line).GetClosedPt(ver);//获取最近点1
                    var closedPt2 = (dbObjs[1] as Line).GetClosedPt(ver);//获取最近点2
                }
                else if(dbObjs.Count == 1)
                {
                    var l = dbObjs[0] as Line;
                    var closedPt = l.GetClosedPt(ver);//获取最近点
                    var cl = new Line(closedPt, ver._pt);
                    if(cl.Length > 1.0 && cl.Length < tor)
                    {
                        pipeLines.Add(cl);
                    }
                }
            }
            var leadLines = sprayIn.LeadLines.ToCollection();
            var leadLineSpatialIndex = new ThCADCoreNTSSpatialIndex(leadLines);

            foreach (var cv in connectVreticals)
            {
                var rect = cv._pt.GetRect(50);
                var rst = leadLineSpatialIndex.SelectCrossingPolygon(rect);
                if(rst.Count == 0)
                {
                    sprayIn.Verticals.Remove(cv);
                }
            }

            pipeLines.AddRange(lines);

            return pipeLines;
        }

        public static List<Line> PipeLineSplit(List<Line> pipeLines)
        {
            var pts = GetPts(pipeLines);
            foreach (var pt in pts)
            {
                var rect = pt.GetRect(5);
                var linesSaptialIndex = new ThCADCoreNTSSpatialIndex(pipeLines.ToCollection());
                var rst = linesSaptialIndex.SelectCrossingPolygon(rect);
                foreach(var obj in rst)
                {
                    var line = obj as Line;
                    var spt = line.StartPoint;
                    var ept = line.EndPoint;
                    if(pt.DistanceTo(spt) > 1 && pt.DistanceTo(ept) > 1)
                    {
                        var closedPt = line.GetClosestPointTo(pt,false);
                        pipeLines.Remove(line);
                        if(closedPt.DistanceTo(pt) > 1)
                        {
                            pipeLines.Add(new Line(closedPt, pt));
                        }
                        pipeLines.Add(new Line(closedPt, spt));
                        pipeLines.Add(new Line(closedPt, ept));
                    }
                }
            }

            return pipeLines;
        }

        private static List<Point3d> GetPts(List<Line> pipeLines)
        {
            var pts = new List<Point3d>();
            foreach(var line in pipeLines)
            {
                pts.Add(line.StartPoint);
                pts.Add(line.EndPoint);
            }
            return pts;
        }

        public static List<Line> PipeLineAutoConnect(this List<Line> lineList, SprayIn sprayIn, ThCADCoreNTSSpatialIndex verticalSpatialIndex = null)
        {
            var GLineSegList = new List<GLineSegment>();//line 转 GLineSegment

            foreach (var l in lineList)
            {
                var GLineSeg = new GLineSegment(l.StartPoint.X, l.StartPoint.Y, l.EndPoint.X, l.EndPoint.Y);
                GLineSegList.Add(GLineSeg);
            }
            var GLineConnectList = GeoFac.AutoConn(GLineSegList, 1001, 2);//打断部分 自动连接

            foreach (var gl in GLineConnectList)
            {
                var pt1 = new Point3dEx(gl.StartPoint.X, gl.StartPoint.Y, 0);
                var pt2 = new Point3dEx(gl.EndPoint.X, gl.EndPoint.Y, 0);

                if (pt1.DistanceToEx(pt2) > 1001 || pt1.DistanceToEx(pt2) < 1)
                {
                    continue;
                }
                if (sprayIn.PtDic.ContainsKey(pt1) && sprayIn.PtDic.ContainsKey(pt2))
                {
                    if (sprayIn.PtDic[pt1].Count >= 2 || sprayIn.PtDic[pt2].Count >= 2)
                    {
                        continue;
                    }
                }
                var line = new Line(pt1._pt, pt2._pt);
                if (verticalSpatialIndex is null)
                {
                    lineList.Add(line);
                }
                else
                {
                    var rst = verticalSpatialIndex.SelectFence(line);
                    if (rst.Count == 0)
                    {
                        lineList.Add(line);
                    }
                }
            }

            //处理pipes 1.清除重复线段 ；2.将同线的线段连接起来；
            if(GLineConnectList.Count() > 0)
            {
                ThLaneLineCleanService cleanServiec = new ThLaneLineCleanService();
                var lineColl = cleanServiec.CleanNoding(lineList.ToCollection());
                var tmpLines = new List<Line>();
                foreach (var l in lineColl)
                {
                    tmpLines.Add(l as Line);
                }
                var cleanLines = LineMerge.CleanLaneLines(tmpLines);


                return cleanLines;
            }

            return lineList;//merge
        }

        public static List<Line> ConnectWithAlarmValve(List<Line> lineList, SprayIn sprayIn, List<Point3d> alarmPts)
        {
            foreach (var apt in alarmPts)
            {
                var lineSpatialIndex = new ThCADCoreNTSSpatialIndex(lineList.ToCollection());
                var rect = apt.GetRect(210);
                var rst = lineSpatialIndex.SelectCrossingPolygon(rect);
                var pts = new List<Point3d>();
                foreach(var obj in rst)
                {
                    var l = obj as Line;
                    lineList.Remove(l);
                    pts.Add(l.StartPoint);
                    pts.Add(l.EndPoint);
                }
                var orderPts = pts.OrderByDescending(p=>p.DistanceTo(apt)).ToList();
                if(orderPts.Count>3)
                {
                    lineList.Add(new Line(apt, orderPts[0]));
                    lineList.Add(new Line(apt, orderPts[1]));
                    lineList.Add(new Line(apt, orderPts[2]));
                }
            }

            return lineList;
        }

        public static List<Line> ConnectBreakLine(this List<Line> lineList, SprayIn sprayIn)
        {
            //连接不是端点的孤立线段
            var connectLine = new List<Line>();
            foreach (var line in lineList)
            {
                var pt1 = new Point3dEx(line.StartPoint);
                var pt2 = new Point3dEx(line.EndPoint);
                if (line.Length < 10)
                {
                    continue;//把一些短线直接跳过
                }
                if (sprayIn.PtDic[pt1].Count == 1)
                {
                    foreach (var l in lineList)
                    {
                        if (l.GetClosestPointTo(pt1._pt, false).DistanceTo(pt1._pt) < 150 && !l.Equals(line))
                        {
                            var pts = new Point3dCollection();
                            l.IntersectWith(line, (Intersect)2, pts, (IntPtr)0, (IntPtr)0);
                            if (pts.Count > 0)
                            {
                                if (pts[0].DistanceTo(pt1._pt) < 150 && pts[0].DistanceTo(pt1._pt) > 1)
                                {
                                    connectLine.Add(new Line(pts[0], pt1._pt));
                                }
                            }
                        }
                    }
                }
                if (sprayIn.PtDic[pt2].Count == 1)
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
            foreach (var l in connectLine)
            {
                if (l.Length > 1)
                {
                    lineList.Add(l);
                }
            }
            return PipeLineList.CleanLaneLines3(lineList);//merge
        }

        public static List<Line> PipeLineSplit(this List<Line> pipeLineList, List<Point3dEx> pts, double toleranceForPointIsLineTerm = 1.0, double toleranceForPointOnLine = 1.0)
        {
            foreach (var pt in pts)//管线打断
            {
                var line = PointCompute.PointInLine(pt._pt, pipeLineList, toleranceForPointIsLineTerm, toleranceForPointOnLine);
                if (!PointCompute.IsNullLine(line))
                {
                    if (!PointCompute.PointIsLineTerm(pt._pt, line, toleranceForPointIsLineTerm))
                    {
                        pipeLineList.Remove(line);
                        var lList = PointCompute.CreateNewLine(pt._pt, line);

                        foreach (var ls in lList)
                        {
                            pipeLineList.Add(ls);
                        }
                    }
                }
            }

            return pipeLineList;
        }
    }
}
