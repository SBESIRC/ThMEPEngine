using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Catel.Linq;
using DotNetARX;
using GeometryExtensions;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundSpraySystem.Method
{
    public static class TermPtDeal
    {
        public static void CreateTermPt(this SprayIn sprayIn, ThCADCoreNTSSpatialIndex textSpatialIndex)
        {
            foreach (var pt in sprayIn.PtDic.Keys)
            {
                try
                {
                    bool flag = false;
                    if(pt._pt.DistanceTo(new Point3d(18367083.8131, 21124583.2218, 0)) < 10)
                    {
                        ;
                    }
                    if(sprayIn.PtTextDic.ContainsKey(pt))//当前点存在标注
                    {
                        if(sprayIn.PtTextDic[pt].First() is null)
                        {
                            sprayIn.PtTextDic.Remove(pt);//删掉空标注
                        }
                        else if(!sprayIn.PtTextDic[pt].First().Equals(""))//标注不是null, 且不为 ""
                        {
                            continue;//直接退出
                        }
                        else//标注为空
                        {
                            sprayIn.PtTextDic.Remove(pt);//删掉空标注
                        }
                    }
                    if (sprayIn.PtDic[pt].Count == 1)
                    {
                        foreach (var v in sprayIn.Verticals)
                        {
                            if (v._pt.DistanceTo(pt._pt) < 100)
                            {
                                if (sprayIn.PtTextDic.ContainsKey(v))
                                {
                                    sprayIn.PtTextDic.Add(pt, sprayIn.PtTextDic[v]);
                                }
                                if (sprayIn.TermPtTypeDic.ContainsKey(v))
                                {
                                    sprayIn.TermPtTypeDic.Add(pt, sprayIn.TermPtTypeDic[v]);
                                }
                                if (sprayIn.TermPtDic.ContainsKey(v))
                                {
                                    sprayIn.TermPtDic.Add(pt, sprayIn.TermPtDic[v]);
                                    flag = true;
                                    break;
                                }
                            }
                        }
                        if (flag)
                        {
                            continue;
                        }
                        var tpt = new TermPoint2(pt);
                        tpt.SetLines(sprayIn);
                        tpt.SetPipeNumber(textSpatialIndex);
                        tpt.SetType();
                        var strs = new List<string>() { tpt.PipeNumber, tpt.PipeNumber2 };
                        sprayIn.PtTextDic.Add(pt, strs);
                        sprayIn.TermPtTypeDic.Add(pt, tpt.Type);
                        sprayIn.TermPtDic.Add(pt, tpt);
                    }
                }
                catch
                {
                    ;
                }
            }
        }


        public static void CreateTermPtDic(this SprayIn sprayIn, ThCADCoreNTSSpatialIndex textSpatialIndex, 
            ThCADCoreNTSSpatialIndex pipeDNSpatialIndex)
        {
            foreach(var pt in sprayIn.Verticals)
            {
                try
                {
                    if(pt._pt.DistanceTo(new Point3d(18399664.2717, 21206988.0413, 0)) < 10)
                    {
                        ;
                    }
                    GetCollectiveLabelDic(pt, ref sprayIn, textSpatialIndex, pipeDNSpatialIndex);
                }
                catch
                {
                    
                }
            }
            foreach (var pt in sprayIn.PtDic.Keys)
            {
                try
                {
                    GetNoVerticalLabelDic(pt, ref sprayIn);
                }
                catch
                {
                    
                }
            }
        }

        public static void GetCollectiveLabelDic(Point3dEx pt, ref SprayIn sprayIn, ThCADCoreNTSSpatialIndex textSpatialIndex, ThCADCoreNTSSpatialIndex pipeDNSpatialIndex)
        {
            //集体标注处理
            if(sprayIn.PtTextDic.ContainsKey(pt))
            {
                if(sprayIn.PtTextDic[pt].First() is null || sprayIn.PtTextDic[pt].First().Equals(""))
                {
                    ;
                }
                else
                {
                    return;
                }
            }
            var OriginTermStartPtDic = GetOriginTermStartPtEx(sprayIn, pt, textSpatialIndex, pipeDNSpatialIndex);

            foreach (var pt2 in OriginTermStartPtDic.Keys)
            {
                var tpt = new TermPoint2(pt2);
                tpt.PipeNumber = OriginTermStartPtDic[pt2].TextString;
                tpt.SetType();
                sprayIn.PtTextDic.Add(pt2, new List<string>() { tpt.PipeNumber, tpt.PipeNumber2 });
                sprayIn.TermPtTypeDic.Add(pt2, tpt.Type);
                sprayIn.TermPtDic.Add(pt2, tpt);
            }
        }

        public static void GetNoVerticalLabelDic(Point3dEx pt, ref SprayIn sprayIn)
        {
            //无立管标注的处理
            //ToDo
        }

        private static Dictionary<Point3dEx, DBText> GetOriginTermStartPtEx(SprayIn sprayIn, Point3dEx termPtEx,
            ThCADCoreNTSSpatialIndex textIndex, ThCADCoreNTSSpatialIndex pipeDNSpatialIndex)
        {
            var verPipeCenters = sprayIn.Verticals;
            var verPipeBounds = verPipeCenters.Select(c =>
            {
                Polyline pl = CreatePolyline(c);
                return pl;
            }
            );
            var verPipeBoundSpatialIndex = new ThCADCoreNTSSpatialIndex(verPipeBounds.ToList().ToCollection());

            var leaderLines = sprayIn.LeadLines;
            var leaderLineSpatialIndex = new ThCADCoreNTSSpatialIndex(leaderLines.ToCollection());

            var termStartPtEx = GetTermStartPtEx(termPtEx, leaderLineSpatialIndex, verPipeBoundSpatialIndex);
            if(termStartPtEx._pt.DistanceTo(new Point3d(18399982.3078,21207490.5105,0)) <10)
            {
            }
            var rstText2PipeBoundDic = GetTermPtTextDic(sprayIn, termStartPtEx, textIndex, pipeDNSpatialIndex,
                                          leaderLineSpatialIndex, verPipeBoundSpatialIndex);

            return rstText2PipeBoundDic;
        }

        private static Point3dEx GetTermStartPtEx(Point3dEx termPtEx, ThCADCoreNTSSpatialIndex leaderLineSpatialIndex, 
            ThCADCoreNTSSpatialIndex verPipeBoundSpatialIndex)
        {
            //get terminal origin point
            var termStartPtEx = new Point3dEx(Point3d.Origin);
            var termPtPolyline = CreatePolyline(termPtEx, 100);
            var selected = verPipeBoundSpatialIndex.SelectCrossingPolygon(termPtPolyline);

            if (selected.Count > 0)
            {
                var curPtBounds = selected[0];
                var curLineEntity = leaderLineSpatialIndex.SelectCrossingPolygon(curPtBounds as Entity);
                if (curLineEntity.Count > 0)
                {
                    var curLine = curLineEntity[0] as Line;
                    Queue<Point3d> q = new Queue<Point3d>();
                    q.Enqueue(curLine.StartPoint);
                    q.Enqueue(curLine.EndPoint);
                    HashSet<Point3d> visited2 = new HashSet<Point3d>();
                    while (q.Count > 0)
                    {
                        var curPt = q.Dequeue();
                        visited2.Add(curPt);
                        var curPtBounds2 = CreatePolyline(new Point3dEx(curPt));
                        var selectedLines = leaderLineSpatialIndex.SelectCrossingPolygon(curPtBounds2);

                        if (selectedLines.Count == 1)
                        {
                            curLine = selectedLines[0] as Line;
                            var ptEx = new Point3dEx(curLine.StartPoint);
                            termPtPolyline = CreatePolyline(ptEx, 120);
                            selected = verPipeBoundSpatialIndex.SelectCrossingPolygon(termPtPolyline);
                            if (selected.Count == 1)
                            {
                                termStartPtEx = new Point3dEx(curPt);
                                break;
                            }

                            ptEx = new Point3dEx(curLine.EndPoint);
                            termPtPolyline = CreatePolyline(ptEx, 120);
                            selected = verPipeBoundSpatialIndex.SelectCrossingPolygon(termPtPolyline);
                            if (selected.Count == 1)
                            {
                                termStartPtEx = new Point3dEx(curPt);
                                break;
                            }
                            if (!visited2.Contains(curLine.StartPoint))
                                q.Enqueue(curLine.StartPoint);

                            if (!visited2.Contains(curLine.EndPoint))
                                q.Enqueue(curLine.EndPoint);
                        }
                        else
                        {
                            var adjs = new List<Point3d>();
                            foreach (var l in selectedLines)
                            {
                                curLine = l as Line;
                                if (!visited2.Contains(curLine.StartPoint))
                                    q.Enqueue(curLine.StartPoint);

                                if (!visited2.Contains(curLine.EndPoint))
                                    q.Enqueue(curLine.EndPoint);
                            }
                        }
                    }
                }
            }
            return termStartPtEx;
        }

        private static Dictionary<Point3dEx, DBText> GetTermPtTextDic(SprayIn sprayIn, Point3dEx termStartPtEx,
            ThCADCoreNTSSpatialIndex textIndex, ThCADCoreNTSSpatialIndex pipeDNSpatialIndex, 
            ThCADCoreNTSSpatialIndex leaderLineSpatialIndex, ThCADCoreNTSSpatialIndex verPipeBoundSpatialIndex)
        {
            var rstText2PipeBoundDic = new Dictionary<Point3dEx, DBText>();

            //map vertical pipe point to text
            var originPtExBounds = CreatePolyline(termStartPtEx);
            var selectedLeaderLines = leaderLineSpatialIndex.SelectCrossingPolygon(originPtExBounds);

            if (selectedLeaderLines.Count == 1)
            {
                List<Polyline> orderedTermPolyLines = new List<Polyline>();

                Queue<Line> lineQueue = new Queue<Line>();
                var originLeader = selectedLeaderLines[0] as Line;
                lineQueue.Enqueue(originLeader as Line);
                var visitedLines = new HashSet<Line>();

                var textsSet = new HashSet<DBText>();
                var DnSet = new HashSet<DBText>();//保存管径
                var lastLine = new Line();
                var startPt = termStartPtEx._pt.ToPoint2D();
                while (lineQueue.Count > 0)
                {
                    var curLine = lineQueue.Dequeue();
                    if (!sprayIn.LeadLineDic.ContainsKey(curLine)) continue;
                    var lineBufferBounds = curLine.Buffer(10);
                    var selectedVerPipeBounds = verPipeBoundSpatialIndex.SelectCrossingPolygon(lineBufferBounds).Cast<Polyline>().ToList();

                    var orderedTempPipeBounds =
                        selectedVerPipeBounds.OrderBy(b => b.GeometricExtents.MinPoint.ToPoint2D().GetDistanceTo(startPt));

                    foreach (var b in orderedTempPipeBounds)
                    {
                        if (!orderedTermPolyLines.Contains(b))
                            orderedTermPolyLines.Add(b);
                    }

                    var adjs = sprayIn.LeadLineDic[curLine];
                    int cnt = 0;
                    foreach (var l in adjs)
                    {
                        if (visitedLines.Contains(l)) continue;
                        cnt += 1;
                    }
                    if (cnt >= 2)
                    {
                        foreach (var l in adjs)
                        {
                            if (visitedLines.Contains(l)) continue;

                            var lineBounds = CreateLineHalfBuffer(l, 300);

                            var texts = textIndex.SelectCrossingPolygon(lineBounds);
                            var dn = pipeDNSpatialIndex.SelectCrossingPolygon(lineBounds);
                            if(texts.Count == 0 || dn.Count == 0)
                            {
                                ;
                            }
                            if (texts.Count > 0)
                            {
                                foreach (var t in texts)
                                    textsSet.Add(t as DBText);
                            }
                            if (dn.Count > 0)
                            {
                                foreach (var t in dn)
                                    DnSet.Add(t as DBText);
                            }
                            lineQueue.Enqueue(l);
                        }
                        break;
                    }
                    foreach (var l in adjs)
                    {
                        if (visitedLines.Contains(l)) continue;

                        lineQueue.Enqueue(l);
                    }

                    visitedLines.Add(curLine);
                    if (lastLine.StartPoint.DistanceTo(new Point3d(0, 0, 0)) > 1)
                    {
                        if (curLine.StartPoint.DistanceTo(lastLine.StartPoint) > curLine.EndPoint.DistanceTo(lastLine.StartPoint))
                        {
                            startPt = curLine.EndPoint.ToPoint2D();
                        }
                    }
                    lastLine = curLine;
                }

                var orderedTexts = textsSet.OrderBy(t => t.GeometricExtents.MinPoint.TransformBy(Active.Editor.WCS2UCS()).Y).ToList();
                var orderDNs = DnSet.OrderBy(t => t.GeometricExtents.MinPoint.TransformBy(Active.Editor.WCS2UCS()).Y).ToList();
                var dbTextIndex = orderedTexts.Count - 1;
                var pipeBoundsIndex = orderedTermPolyLines.Count - 1;
                while (dbTextIndex >= 0 && pipeBoundsIndex >= 0)
                {
                    var bounds = orderedTermPolyLines[pipeBoundsIndex].GeometricExtents;
                    var centerPt = PtTools.GetMidPt(bounds.MaxPoint, bounds.MinPoint);
                    var text = orderedTexts[dbTextIndex];
                    rstText2PipeBoundDic.Add(new Point3dEx(centerPt), text);

                    dbTextIndex--;
                    pipeBoundsIndex--;
                }

                var dnIndex = orderDNs.Count - 1;
                pipeBoundsIndex = orderedTermPolyLines.Count - 1;
                while (dnIndex >= 0 && pipeBoundsIndex >= 0)
                {
                    var bounds = orderedTermPolyLines[pipeBoundsIndex].GeometricExtents;
                    var centerPt = PtTools.GetMidPt(bounds.MaxPoint, bounds.MinPoint);
                    var text = orderDNs[dnIndex].TextString;
                    var targetPt = new Point3d(18399910.5175,21207031.1996, 0);
                    if (centerPt.DistanceTo(targetPt) <100)
                    {
                        ;
                    }
                    sprayIn.TermDnDic.Add(new Point3dEx(centerPt), text);

                    dnIndex--;
                    pipeBoundsIndex--;
                }
            }

            return rstText2PipeBoundDic;
        }
        private static Polyline CreatePolyline(Point3dEx c, int tolerance = 50)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();
            pts.Add(new Point2d(c._pt.X - tolerance, c._pt.Y - tolerance)); // low left
            pts.Add(new Point2d(c._pt.X - tolerance, c._pt.Y + tolerance)); // high left
            pts.Add(new Point2d(c._pt.X + tolerance, c._pt.Y + tolerance)); // high right
            pts.Add(new Point2d(c._pt.X + tolerance, c._pt.Y - tolerance)); // low right
            pts.Add(new Point2d(c._pt.X - tolerance, c._pt.Y - tolerance)); // low left
            pl.CreatePolyline(pts);
            return pl;
        }

        private static Polyline CreateLineHalfBuffer(Line line, int tolerance = 50)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();

            var spt = line.StartPoint;
            var ept = line.EndPoint;
            pts.Add(spt.ToPoint2D()); // low left
            pts.Add(spt.OffsetY(tolerance).ToPoint2D()); // high left
            pts.Add(ept.OffsetY(tolerance).ToPoint2D()); // low right
            pts.Add(ept.ToPoint2D()); // high right
            pts.Add(spt.ToPoint2D()); // low left
            pl.CreatePolyline(pts);
            using (AcadDatabase currentDb = AcadDatabase.Active())
            {
                currentDb.CurrentSpace.Add(pl);
            }
            return pl;
        }
    }
}
