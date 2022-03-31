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
using ThMEPEngineCore.Service;
using ThMEPWSS.Assistant;
using ThMEPWSS.Uitl;
using ThMEPWSS.UndergroundFireHydrantSystem.Method;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;
using Draw = ThMEPWSS.UndergroundSpraySystem.Method.Draw;


namespace ThMEPWSS.UndergroundSpraySystem.General
{
    public static class LineTools
    {
        private static Point3d GetClosedPt(this Line line, Point3dEx verticalPt)
        {
            var spt = line.StartPoint;
            var ept = line.EndPoint;
            var sdist = spt.DistanceTo(verticalPt._pt);
            var edist = ept.DistanceTo(verticalPt._pt);
            if(sdist < edist)
            {
                return spt;
            }
            else
            {
                return ept;
            }
        }
        public static List<Line> ConnectVerticalLine(this List<Line> pipeLines, SprayIn sprayIn)
        {
            //基于竖管连接管线
            var pipeLinesSaptialIndex = new ThCADCoreNTSSpatialIndex(pipeLines.ToCollection());
            var lines = new List<Line>();
            var connectVreticals = new List<Point3dEx>();
            foreach (var ver in sprayIn.Verticals)
            {
                if (ver._pt.DistanceTo(new Point3d(1016754.2, -2354896.8, 0)) < 10)
                    ;
                var rect = ver._pt.GetRect(100);
                var dbObjs = pipeLinesSaptialIndex.SelectCrossingPolygon(rect);
                var flag = sprayIn.AddNewPtDic(dbObjs, ver._pt, ref lines);
                if (dbObjs.Count >= 2)
                {
                    connectVreticals.Add(ver);
                }
                else if(dbObjs.Count == 1)
                {
                    var l = dbObjs[0] as Line;
                    var closedPt = l.GetClosedPt(ver);//获取最近点
                    var cl = new Line(closedPt, ver._pt);
                    if(cl.Length > 1.0 && cl.Length < 100)
                    {
                        pipeLines.Add(cl);
#if DEBUG
                        using (AcadDatabase acadDatabase = AcadDatabase.Active())
                        {
                            var layerNames = "立管和支管的单链接线";
                            if (!acadDatabase.Layers.Contains(layerNames))
                            {
                                ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                            }
                            cl.LayerId = DbHelper.GetLayerId(layerNames);
                            cl.ColorIndex = (int)ColorIndex.Red;
                            acadDatabase.CurrentSpace.Add(cl);

                        }
#endif
                    }

                }
            }
            var leadLines = sprayIn.LeadLines.ToCollection();
            var leadLineSpatialIndex = new ThCADCoreNTSSpatialIndex(leadLines);



            foreach (var cv in connectVreticals)
            {
                if (cv._pt.DistanceTo(new Point3d(1016754.2, -2354896.8, 0)) < 10)
                    ;
                var rect = cv._pt.GetRect(50);
                var rst = leadLineSpatialIndex.SelectCrossingPolygon(rect);
                if(rst.Count == 0)
                {
                    Draw.RemovedVerticalPt(cv);
                    sprayIn.Verticals.Remove(cv);
                }
            }

            pipeLines.AddRange(lines);

            //把单连通立管和横管连起来


            return pipeLines;
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
                if (pt1.DistanceToEx(pt2) > 763 && pt1.DistanceToEx(pt2) < 764)
                    ;
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

#if DEBUG
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    var layerNames = "自动连接线";
                    if (!acadDatabase.Layers.Contains(layerNames))
                    {
                        ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                    }
                    line.LayerId = DbHelper.GetLayerId(layerNames);
                    line.ColorIndex = (int)ColorIndex.Red;
                    acadDatabase.CurrentSpace.Add(line);

                }
#endif
            }


            //处理pipes 1.清除重复线段 ；2.将同线的线段连接起来；
            ThLaneLineCleanService cleanServiec = new ThLaneLineCleanService();
            var lineColl = cleanServiec.CleanNoding(lineList.ToCollection());
            var tmpLines = new List<Line>();
            foreach (var l in lineColl)
            {
                tmpLines.Add(l as Line);
            }
            var cleanLines = LineMerge.CleanLaneLines(tmpLines);

            ;

            //var cleanLine = PipeLineList.CleanLaneLines3(lineList);//merge
#if DEBUG

            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var layerNames = "自动连接并合并的线";
                if (!acadDatabase.Layers.Contains(layerNames))
                {
                    ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                }
                foreach (var line in cleanLines)
                {
                    line.LayerId = DbHelper.GetLayerId(layerNames);
                    line.ColorIndex = (int)ColorIndex.Red;
                    acadDatabase.CurrentSpace.Add(line);
                }


            }
#endif


            return cleanLines;
            //return lineList;//merge
        }

        public static void PipeLineAutoConnect(this List<Line> lineList, SprayIn sprayIn, List<Point3d> alarmPts)
        {
            var dbPts = new List<DBPoint>();
            sprayIn.PtDic.Keys.ToList().ForEach(p => dbPts.Add(new DBPoint(p._pt)));
            var dbPtSpatialIndex = new ThCADCoreNTSSpatialIndex(dbPts.ToCollection());
            foreach(var apt in alarmPts)
            {
                var rect = apt.GetRect(210);
                var rst = dbPtSpatialIndex.SelectCrossingPolygon(rect);
                foreach(var obj in rst)
                {
                    var pt = (obj as DBPoint).Position;
                    var line = new Line(apt, pt);
                    if(line.Length > 1.0)
                    {
                        lineList.Add(line);
#if DEBUG
                        using (AcadDatabase acadDatabase = AcadDatabase.Active())
                        {
                            var layerNames = "报警阀连接的线段";
                            if (!acadDatabase.Layers.Contains(layerNames))
                            {
                                ThMEPEngineCoreLayerUtils.CreateAILayer(acadDatabase.Database, layerNames, 30);
                            }
                            line.LayerId = DbHelper.GetLayerId(layerNames);
                            line.ColorIndex = (int)ColorIndex.Red;
                            acadDatabase.CurrentSpace.Add(line);
                        }
#endif
                    }
                }
            }
        }


        public static void AddPtDic(SprayIn sprayIn, List<Point3d> pts, Point3d centerPt)
        {
            var centPtex = new Point3dEx(centerPt);//报警阀中心点
            if (sprayIn.PtDic.Keys.Contains(centPtex))//字典包含报警阀中心点
            {
                foreach (var pt in pts)
                {
                    var ptex = new Point3dEx(pt);
                    if (centPtex.Equals(ptex))
                    {
                        continue;
                    }
                    if (!sprayIn.PtDic[centPtex].Contains(ptex))
                    {
                        sprayIn.PtDic[centPtex].Add(ptex);
                    }
                }
            }
            else//字典不包含报警阀中心点
            {
                var ptsNew = new List<Point3dEx>();
                foreach (var pt in pts)
                {
                    var ptex = new Point3dEx(pt);
                    if (centPtex.Equals(ptex))
                    {
                        continue;
                    }
                    ptsNew.Add(ptex);
                }
                sprayIn.PtDic.Add(centPtex, ptsNew);
            }

            foreach (var pt in pts)
            {
                var ptex = new Point3dEx(pt);
                if (centPtex.Equals(ptex))
                {
                    continue;
                }
                if (sprayIn.PtDic.Keys.Contains(ptex))
                {
                    if (!sprayIn.PtDic[ptex].Contains(centPtex))
                    {
                        sprayIn.PtDic[ptex].Add(centPtex);
                    }
                }
                else
                {
                    sprayIn.PtDic.Add(ptex, new List<Point3dEx>() { centPtex });
                }
            }
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
