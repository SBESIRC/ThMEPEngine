using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
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
        public static List<Line> ConnectVerticalLine(this List<Line> pipeLines, SprayIn sprayIn)
        {
            //基于竖管连接管线
            var pipeLinesSaptialIndex = new ThCADCoreNTSSpatialIndex(pipeLines.ToCollection());
            var lines = new List<Line>();
            var connectVreticals = new List<Point3dEx>();
            foreach (var ver in sprayIn.Verticals)
            {
                if(ver._pt.DistanceTo(new Point3d(927732.7, 419553, 0)) < 200)
                {
                    ;
                }
                var rect = ver._pt.GetRect();
                var dbObjs = pipeLinesSaptialIndex.SelectCrossingPolygon(rect);
                var flag = sprayIn.AddNewPtDic(dbObjs, ver._pt, ref lines);
                if(flag)
                {
                    connectVreticals.Add(ver);
                }
            }
            foreach(var cv in connectVreticals)
            {
                sprayIn.Verticals.Remove(cv);
            }
            
            pipeLines.AddRange(lines);
            return pipeLines;
        }
        public static List<Line> PipeLineAutoConnect(this List<Line> lineList, SprayIn sprayIn)
        {
            lineList = PipeLineList.CleanLaneLines3(lineList);//merge
            var GLineSegList = new List<GLineSegment>();//line 转 GLineSegment
            foreach (var l in lineList)
            {
                var GLineSeg = new GLineSegment(l.StartPoint.X, l.StartPoint.Y, l.EndPoint.X, l.EndPoint.Y);
                GLineSegList.Add(GLineSeg);
            }
            var GLineConnectList = GeoFac.AutoConn(GLineSegList, 1000, 2);//打断部分 自动连接

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
                    if (sprayIn.PtDic.ContainsKey(pt1) && sprayIn.PtDic.ContainsKey(pt2))
                    {
                        if (sprayIn.PtDic[pt1].Count >= 3 || sprayIn.PtDic[pt2].Count >= 3)
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
            return PipeLineList.CleanLaneLines3(lineList);//merge
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
