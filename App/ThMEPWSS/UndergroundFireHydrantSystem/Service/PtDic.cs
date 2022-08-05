using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPWSS.UndergroundFireHydrantSystem.Method;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Service
{
    public static class PtDic
    {
        public static void CreatePtDic(FireHydrantSystemIn fireHydrantSysIn, List<Line> lineList)
        {
            //管线添加
            fireHydrantSysIn.PtDic = new Dictionary<Point3dEx, List<Point3dEx>>();//清空  当前点和邻接点字典对
            foreach (var L in lineList)
            {
                var pt1 = new Point3dEx(L.StartPoint);
                var pt2 = new Point3dEx(L.EndPoint);

                if (pt1.Equals(new Point3dEx(0, 0, 0)) || pt2.Equals(new Point3dEx(0, 0, 0)))
                {
                    continue;
                }
                ThPointCountService.AddPoint(ref fireHydrantSysIn, ref pt1, ref pt2);
            }

            var lonelyPtList = new List<Point3dEx>();
            foreach (var ptKey in fireHydrantSysIn.PtDic.Keys)
            {
                if (fireHydrantSysIn.PtDic[ptKey].Count == 1)
                {
                    if (fireHydrantSysIn.PtDic[fireHydrantSysIn.PtDic[ptKey][0]].Equals(ptKey))
                    {
                        lonelyPtList.Add(ptKey);//找出孤立线段
                    }
                    if (fireHydrantSysIn.PtDic[ptKey][0]._pt.DistanceTo(ptKey._pt) < 50)
                    {
                        lonelyPtList.Add(ptKey);//找出边缘短线段
                    }
                }
            }

            foreach (var pt in lonelyPtList)
            {
                if (fireHydrantSysIn.PtDic[pt].Count == 0)
                {
                    continue;
                }
                if (fireHydrantSysIn.PtDic.ContainsKey(fireHydrantSysIn.PtDic[pt][0]))
                {
                    fireHydrantSysIn.PtDic[fireHydrantSysIn.PtDic[pt][0]].Remove(pt);
                }
            }
            foreach (var pt in lonelyPtList)
            {
                if (fireHydrantSysIn.PtDic.ContainsKey(pt))
                {
                    fireHydrantSysIn.PtDic.Remove(pt);
                }
            }

            var plineList = new List<Line>();
            foreach (var line in lineList)
            {
                plineList.Add(line);
            }
        }

        public static void CreateLeadPtDic(FireHydrantSystemIn fireHydrantSysIn, List<Line> lineList)
        {
            double tolerance = 30;
            //管线添加
            fireHydrantSysIn.LeadLineDic = new Dictionary<Line, List<Line>>();//清空  当前点和邻接点字典对
            Line l1, l2;

            for (int i = 0; i < lineList.Count - 1; i++)
            {
                l1 = lineList[i];
                for (int j = i + 1; j < lineList.Count; j++)
                {
                    l2 = lineList[j];
                    if (l1.GetLinesDist(l2) < tolerance)
                    {
                        fireHydrantSysIn.AddAdjsDic(l1, l2);
                        fireHydrantSysIn.AddAdjsDic(l2, l1);
                        continue;
                    }
                    if (l1.GetLineDist2(l2) < tolerance)
                    {
                        fireHydrantSysIn.AddAdjsDic(l1, l2);
                        fireHydrantSysIn.AddAdjsDic(l2, l1);
                        continue;
                    }
                    if (l2.GetLineDist2(l1) < tolerance)
                    {
                        fireHydrantSysIn.AddAdjsDic(l1, l2);
                        fireHydrantSysIn.AddAdjsDic(l2, l1);
                        continue;
                    }

                    if (l1.LineIsIntersection(l2))
                    {
                        fireHydrantSysIn.AddAdjsDic(l1, l2);
                        fireHydrantSysIn.AddAdjsDic(l2, l1);
                        continue;
                    }
                }
            }
        }

        private static void AddAdjsDic(this FireHydrantSystemIn fireHydrantSysIn, Line l1, Line l2)
        {
            if (fireHydrantSysIn.LeadLineDic.ContainsKey(l1))
            {
                fireHydrantSysIn.LeadLineDic[l1].Add(l2);
            }
            else
            {
                fireHydrantSysIn.LeadLineDic.Add(l1, new List<Line>() { l2 });
            }
        }

        public static void CreateDNDic(FireHydrantSystemIn fireHydrantSysIn, DBObjectCollection PipeDN, List<Line> lineList)
        {
            foreach (var dn in PipeDN)//创建DN字典对
            {
                var dbtext = dn as DBText;
                var cenPt = General.GetMidPt(dbtext.GeometricExtents.MaxPoint, dbtext.GeometricExtents.MinPoint);
                var checkPt = new Point3d(dbtext.Position.X, cenPt.Y, 0);
                if (Math.Abs(dbtext.Rotation - Math.PI / 2) < 0.035)
                {
                    checkPt = new Point3d(cenPt.X, dbtext.Position.Y, 0);
                }

                foreach (var line in lineList)
                {
                    var ang = line.Angle;
                    if (ang.IsParallelTo(dbtext.Rotation) &&
                       line.GetClosestPointTo(dbtext.Position, false).DistanceTo(checkPt) < 400)
                    {
                        if (!fireHydrantSysIn.PtDNDic.ContainsKey(new LineSegEx(line)))
                        {
                            if (Regex.IsMatch(dbtext.TextString, @"[\u4E00-\u9FA5]+$"))
                            {
                                continue;
                            }
                            fireHydrantSysIn.PtDNDic.Add(new LineSegEx(line), dbtext.TextString);//贴边标注
                        }
                    }
                }
            }
        }

        public static void CreateDNDic(SprayIn sprayIn, DBObjectCollection PipeDN, List<Line> lineList)
        {
            foreach (var dn in PipeDN)//创建DN字典对
            {
                var dbtext = dn as DBText;
                var cenPt = General.GetMidPt(dbtext.GeometricExtents.MaxPoint, dbtext.GeometricExtents.MinPoint);
                var checkPt = new Point3d(dbtext.Position.X, cenPt.Y, 0);
                if (Math.Abs(dbtext.Rotation - Math.PI / 2) < 0.035)
                {
                    checkPt = new Point3d(cenPt.X, dbtext.Position.Y, 0);
                }

                foreach (var line in lineList)
                {
                    var ang = line.Angle;
                    if (ang.IsParallelTo(dbtext.Rotation) &&
                       line.GetClosestPointTo(dbtext.Position, false).DistanceTo(checkPt) < 400)
                    {
                        if (!sprayIn.PtDNDic.ContainsKey(new LineSegEx(line)))
                        {
                            if (Regex.IsMatch(dbtext.TextString, @"[\u4E00-\u9FA5]+$"))
                            {
                                continue;
                            }
                            sprayIn.PtDNDic.Add(new LineSegEx(line), dbtext.TextString);//贴边标注
                        }
                    }
                }
            }
        }

        public static void CreateTermPtDic(FireHydrantSystemIn fireHydrantSysIn, List<Point3dEx> pointList,
            List<Line> labelLine, ThCADCoreNTSSpatialIndex textSpatialIndex, Dictionary<Point3dEx, string> ptTextDic,
            ThCADCoreNTSSpatialIndex fhSpatialIndex)
        {
            foreach (var pt in fireHydrantSysIn.VerticalPosition)//每个圈圈的中心点
            {
                try
                {
                    CreateTermPtDic2(pt, fireHydrantSysIn, pointList, labelLine, textSpatialIndex, fhSpatialIndex);
                }
                catch(Exception ex)
                {
                    ;
                }
            }

            foreach(var pt in fireHydrantSysIn.PtDic.Keys)
            {
                bool ignore = false;
                foreach (var vpt in fireHydrantSysIn.VerticalPosition)
                {
                    if(pt.DistanceToEx(vpt)< 100)
                    {
                        ignore = true;
                        break;
                    }
                }
                if(ignore)
                {
                    continue;
                }
                if (fireHydrantSysIn.PtDic[pt].Count == 1)//邻接点数为1
                {
                    if(!fireHydrantSysIn.TermPointDic.ContainsKey(pt))//且没有标记
                    {
                        var termPoint = new TermPoint(pt);
                        termPoint.SetLines(fireHydrantSysIn, labelLine);
                        if (termPoint.TextLine is not null)
                        {
                            termPoint.SetPipeNumber(textSpatialIndex);
                        }
                        termPoint.SetType(false, false);
                        fireHydrantSysIn.TermPointDic.Add(pt, termPoint);
                    }
                }
            }
        }



        private static void CreateTermPtDic2(Point3dEx pt, FireHydrantSystemIn fireHydrantSysIn, List<Point3dEx> pointList,
            List<Line> labelLine, ThCADCoreNTSSpatialIndex textSpatialIndex, ThCADCoreNTSSpatialIndex fhSpatialIndex)
        {
            var tpt = pt;

            fireHydrantSysIn.TermPtDic.Add(tpt);//把带立管的端点加入末端点列表
            var verticalHasHydrant = fireHydrantSysIn.VerticalHasHydrant.Contains(pt);
            var termPoint = new TermPoint(pt);
            termPoint.SetLines(fireHydrantSysIn, labelLine);
            if(termPoint.TextLine is not null)
            {
                termPoint.SetPipeNumber(textSpatialIndex);
            }
            termPoint.SetType(verticalHasHydrant,true);
            fireHydrantSysIn.TermPointDic.Add(tpt, termPoint);
            if (termPoint.StartLine is null)
            {
                return;
            }

            if (termPoint.TextLine is null)
            {
                var termPtEx = termPoint.PtEx;
                var OriginTermStartPtDic = GetOriginTermStartPtEx(fireHydrantSysIn, termPtEx, textSpatialIndex, labelLine);
                if (OriginTermStartPtDic.ContainsKey(pt))
                {
                    termPoint.PipeNumber = OriginTermStartPtDic[pt].TextString;
                    termPoint.Type = 2;
                    var dbText = ThTextSet.ThText(new Point3d(), termPoint.PipeNumber);
                    double textWidth = dbText.GeometricExtents.MaxPoint.X - dbText.GeometricExtents.MinPoint.X;
                    if (textWidth > 1300)
                    {
                        termPoint.TextWidth = textWidth + 100;
                        termPoint.PipeWidth = textWidth + 300;
                    }
                    fireHydrantSysIn.TermPointDic.Add(tpt, termPoint);
                }
                return;
            }
            termPoint.SetPipeNumber(textSpatialIndex);
            if (termPoint.PipeNumber is null || termPoint.PipeNumber?.Equals("") == true)
            {
                var termPtEx = termPoint.PtEx;
                var OriginTermStartPtDic = GetOriginTermStartPtEx(fireHydrantSysIn, termPtEx, textSpatialIndex, labelLine);
                if (OriginTermStartPtDic.ContainsKey(pt))
                {
                    termPoint.PipeNumber = OriginTermStartPtDic[pt].TextString;
                    termPoint.Type = 2;
                    var dbText = ThTextSet.ThText(new Point3d(), termPoint.PipeNumber);
                    double textWidth = dbText.GeometricExtents.MaxPoint.X - dbText.GeometricExtents.MinPoint.X;
                    if (textWidth > 1300)
                    {
                        termPoint.TextWidth = textWidth + 100;
                        termPoint.PipeWidth = textWidth + 300;
                    }
                    fireHydrantSysIn.TermPointDic.Add(tpt, termPoint);
                }
                return;
            }

            termPoint.SetType(verticalHasHydrant, true);
            if (fireHydrantSysIn.TermPointDic.ContainsKey(tpt))
            {
                fireHydrantSysIn.TermPointDic.Remove(tpt);
                fireHydrantSysIn.TermPointDic.Add(tpt, termPoint);
            }
            else
            {
                fireHydrantSysIn.TermPointDic.Add(tpt, termPoint);
            }
        }

        private static Dictionary<Point3dEx, DBText> GetOriginTermStartPtEx(FireHydrantSystemIn fireHydrantSysIn, Point3dEx termPtEx,
            ThCADCoreNTSSpatialIndex textIndex, List<Line> labelLine)
        {
            var rstText2PipeBoundDic = new Dictionary<Point3dEx, DBText>();

            //get terminal origin point
            var termStartPtEx = new Point3dEx(Point3d.Origin);
            var verPipeCenters = fireHydrantSysIn.VerticalPosition;
            var verPipeBounds = verPipeCenters.Select(c =>
            {
                Polyline pl = CreatePolyline(c);
                return pl;
            }
            );

            var verPipeBoundSpatialIndex = new ThCADCoreNTSSpatialIndex(verPipeBounds.ToList().ToCollection());

            var termPtPolyline = CreatePolyline(termPtEx, 100);
            var selected = verPipeBoundSpatialIndex.SelectCrossingPolygon(termPtPolyline);

            var leaderLines = labelLine;
            var leaderLineSpatialIndex = new ThCADCoreNTSSpatialIndex(leaderLines.ToCollection());
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
                            termPtPolyline = CreatePolyline(ptEx, 100);
                            selected = verPipeBoundSpatialIndex.SelectCrossingPolygon(termPtPolyline);
                            if (selected.Count == 1)
                            {
                                termStartPtEx = ptEx;
                                break;
                            }

                            ptEx = new Point3dEx(curLine.EndPoint);
                            termPtPolyline = CreatePolyline(ptEx, 100);
                            selected = verPipeBoundSpatialIndex.SelectCrossingPolygon(termPtPolyline);
                            if (selected.Count == 1)
                            {
                                termStartPtEx = ptEx;
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
                var lastLine = new Line();
                var startPt = termStartPtEx._pt.ToPoint2D();
                while (lineQueue.Count > 0)
                {
                    var curLine = lineQueue.Dequeue();
                    if (!fireHydrantSysIn.LeadLineDic.ContainsKey(curLine)) continue;
                    var lineBufferBounds = curLine.Buffer(10);
                    var selectedVerPipeBounds = verPipeBoundSpatialIndex.SelectCrossingPolygon(lineBufferBounds).Cast<Polyline>().ToList();

                    var orderedTempPipeBounds =
                        selectedVerPipeBounds.OrderBy(b => b.GeometricExtents.MinPoint.ToPoint2D().GetDistanceTo(startPt));

                    foreach (var b in orderedTempPipeBounds)
                    {
                        if (!orderedTermPolyLines.Contains(b))
                            orderedTermPolyLines.Add(b);
                    }

                    foreach (var b in orderedTempPipeBounds)
                    {
                        if (!orderedTermPolyLines.Contains(b))
                            orderedTermPolyLines.Add(b);
                    }

                    var adjs = fireHydrantSysIn.LeadLineDic[curLine];
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

                            var lineBounds = CreateLineHalfBuffer(l, 200);
                            var texts = textIndex.SelectCrossingPolygon(lineBounds);
                            if (texts.Count > 0)
                            {
                                foreach (var t in texts)
                                {
                                    var textStr = (t as DBText).TextString;
                                    if (Regex.IsMatch(textStr, @"[\u4E00-\u9FA5]+$"))
                                    {
                                        continue;
                                    }
                                    textsSet.Add(t as DBText);
                                }
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
                }

                var orderedTexts = textsSet.OrderBy(t => t.GeometricExtents.MinPoint.ToPoint2D().GetDistanceTo(termStartPtEx._pt.ToPoint2D())).ToList();

                var dbTextIndex = orderedTexts.Count - 1;
                var pipeBoundsIndex = orderedTermPolyLines.Count - 1;

                while (dbTextIndex >= 0 && pipeBoundsIndex >= 0)
                {
                    var bounds = orderedTermPolyLines[pipeBoundsIndex].GeometricExtents;
                    var centerPt = General.GetMidPt(bounds.MaxPoint, bounds.MinPoint);
                    var text = orderedTexts[dbTextIndex];
                    rstText2PipeBoundDic.Add(new Point3dEx(centerPt), text);
                    dbTextIndex--;
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
            var spt = new Point2d(line.StartPoint.X, line.StartPoint.Y);
            var ept = new Point2d(line.EndPoint.X, line.EndPoint.Y);
            Point2d pt1, pt2;
            if (spt.X > ept.X)
            {
                pt1 = ept;
                pt2 = spt;
            }
            else
            {
                pt1 = spt;
                pt2 = ept;
            }
            pts.Add(pt1); // low left
            pts.Add(new Point2d(pt1.X, pt1.Y + tolerance)); // high left
            pts.Add(new Point2d(pt2.X, pt2.Y + tolerance)); // low right
            pts.Add(pt2); // high right
            pts.Add(pt1); // low left
            pl.CreatePolyline(pts);
            return pl;
        }

        public static Dictionary<Line, List<Point3d>> CreateLabelPtDic(List<Point3dEx> hydrantPosition, List<Line> labelLine)
        {
            var labelPtDic = new Dictionary<Line, List<Point3d>>();//把在同一条标记线上的点聚集
            foreach (var pt in hydrantPosition)//遍历点
            {
                foreach (var l in labelLine)//遍历线
                {
                    if (PtOnLine.PtIsOnLine(pt._pt, l))//点在线上
                    {
                        if (labelPtDic.ContainsKey(l))//线存在字典
                        {
                            labelPtDic[l].Add(pt._pt);//直接添加
                        }
                        else
                        {
                            var ptls = new List<Point3d>();//新建后添加
                            ptls.Add(pt._pt);
                            labelPtDic.Add(l, ptls);
                        }
                    }
                }
            }
            foreach (var l in labelPtDic.Keys.ToArray())
            {
                if (labelPtDic[l].Count <= 1)
                {
                    labelPtDic.Remove(l);//删除掉单点
                }
                else//进行排序
                {
                    var ptList = labelPtDic[l];
                    ptList.PointsSort();
                    labelPtDic.Remove(l);
                    labelPtDic.Add(l, ptList);
                }
            }
            return labelPtDic;
        }

        public static Dictionary<Line, List<Line>> CreateLabelLineDic(Dictionary<Line, List<Point3d>> labelPtDic, List<Line> labelLine)
        {
            var labelLineDic = new Dictionary<Line, List<Line>>();
            foreach (var lk in labelPtDic.Keys)
            {
                var listLine = new List<Line>();
                foreach (var l in labelLine)
                {
                    if (labelPtDic.ContainsKey(l))
                    {
                        continue;
                    }
                    if (PtOnLine.PtIsOnLine(l.StartPoint, lk) || PtOnLine.PtIsOnLine(l.EndPoint, lk))
                    {
                        listLine.Add(l);
                    }

                }
                listLine.LinesSort();
                labelLineDic.Add(lk, listLine);
            }
            return labelLineDic;
        }

        public static Dictionary<Point3dEx, string> CreatePtTextDic(Dictionary<Line, List<Point3d>> labelPtDic,
            Dictionary<Line, List<Line>> labelLineDic, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            var ptTextDic = new Dictionary<Point3dEx, string>();
            foreach (var lk in labelPtDic.Keys)
            {
                for (int i = 0; i < Math.Min(labelPtDic[lk].Count, labelLineDic[lk].Count); i++)
                {
                    var line = labelLineDic[lk][i];

                    var text = GetText(spatialIndex, line);

                    var ptex = new Point3dEx(labelPtDic[lk][i]);
                    if(!ptTextDic.ContainsKey(ptex))
                    {
                        ptTextDic.Add(ptex, text);
                    }
                }
            }
            return ptTextDic;
        }

        public static string GetText(ThCADCoreNTSSpatialIndex spatialIndex, Line TextLine)
        {
            string text = "";
            var leftX = 0.0;
            var rightX = 0.0;
            var leftY = 0.0;
            var rightY = 0.0;
            var textHeight = 500;
            if (TextLine.StartPoint.X < TextLine.EndPoint.X)
            {
                leftX = TextLine.StartPoint.X;
                rightX = TextLine.EndPoint.X;
                leftY = TextLine.StartPoint.Y;
                rightY = TextLine.EndPoint.Y;
            }
            else
            {
                leftX = TextLine.EndPoint.X;
                rightX = TextLine.StartPoint.X;
                leftY = TextLine.EndPoint.Y;
                rightY = TextLine.StartPoint.Y;
            }

            var pt1 = new Point3d(leftX, leftY + textHeight, 0);
            var pt2 = new Point3d(rightX, rightY + 150, 0);
            var tuplePoint = new Tuple<Point3d, Point3d>(pt1, pt2);//文字范围

            var selectArea = ThFireHydrantSelectArea.CreateArea(tuplePoint);//生成候选区域
            var DBObjs = spatialIndex.SelectCrossingPolygon(selectArea);
            foreach (var obj in DBObjs)
            {
                if (obj is DBText br)
                {
                    text = br.TextString;

                }
            }

            return text;
        }
        public static void CreateBranchDic(ref Dictionary<Point3dEx, List<Point3dEx>> branchDic, ref Dictionary<Point3dEx, List<Point3dEx>> ValveDic,
    List<List<Point3dEx>> mainPathList, FireHydrantSystemIn fireHydrantSysIn, HashSet<Point3dEx> visited)
        {
            foreach (var rstPath in mainPathList)
            {
                int i = 0;
                foreach (var pt in rstPath)//遍历主环路的点
                {
                    i++;
                    if (!fireHydrantSysIn.PtTypeDic.ContainsKey(pt))
                    {
                        continue;
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("Branch"))//是支点
                    {
                        var termPts = new List<Point3dEx>();
                        var valvePts = new List<Point3dEx>();

                        Queue<Point3dEx> q = new Queue<Point3dEx>();
                        q.Enqueue(pt);
                        HashSet<Point3dEx> visited2 = new HashSet<Point3dEx>();
                        visited.Add(pt);
                        while (q.Count > 0)
                        {
                            var curPt = q.Dequeue();
                            if (fireHydrantSysIn.TermPtDic.Contains(curPt))//当前点是末端点
                            {
                                termPts.Add(curPt);
                                continue;
                            }
                            if (fireHydrantSysIn.PtTypeDic.ContainsKey(curPt))
                            {
                                var type = fireHydrantSysIn.PtTypeDic[curPt];
                                if (type.Contains("DieValve"))
                                {
                                    valvePts.Add(curPt);
                                }
                                if (type.Contains("GateValve"))
                                {
                                    valvePts.Add(curPt);
                                }
                                if (type.Contains("Casing"))
                                {
                                    valvePts.Add(curPt);
                                }
                            }

                            var adjs = fireHydrantSysIn.PtDic[curPt];
                            if (adjs.Count == 1)
                            {
                                termPts.Add(curPt);
                                continue;
                            }

                            foreach (var adj in adjs)
                            {
                                if (rstPath.Contains(adj))
                                    continue;

                                if (visited2.Contains(adj))
                                {
                                    continue;
                                }

                                visited2.Add(adj);
                                q.Enqueue(adj);
                            }
                        }
                        if (termPts.Count != 0)
                        {
                            if (branchDic.ContainsKey(pt))
                            {
                                continue;
                            }
                            branchDic.Add(pt, termPts);
                            if (valvePts.Count != 0)
                            {
                                ValveDic.Add(pt, valvePts);
                            }
                        }
                    }
                }
            }
        }

        public static void CreateBranchDic(Dictionary<Point3dEx, List<Point3dEx>> branchDic, Dictionary<Point3dEx, List<ValveCasing>> ValveDic,
            List<List<Point3dEx>> mainPathList, FireHydrantSystemIn fireHydrantSysIn, HashSet<Point3dEx> visited)
        {
            foreach (var rstPath in mainPathList)
            {
                int i = 0;
                foreach (var pt in rstPath)//遍历主环路的点
                {
                    i++;
                    if (!fireHydrantSysIn.PtTypeDic.ContainsKey(pt))
                    {
                        continue;
                    }

                    if (fireHydrantSysIn.PtTypeDic[pt].Equals("Branch"))//是支点
                    {
                        var termPts = new List<Point3dEx>();
                        var valvePts = new List<ValveCasing>();

                        Queue<Point3dEx> q = new Queue<Point3dEx>();
                        q.Enqueue(pt);
                        HashSet<Point3dEx> visited2 = new HashSet<Point3dEx>();
                        visited.Add(pt);
                        while (q.Count > 0)
                        {
                            var curPt = q.Dequeue();
                            if(fireHydrantSysIn.TermPtDic.Contains(curPt))//当前点是末端点
                            {
                                termPts.Add(curPt);
                                continue;
                            }
                            if (fireHydrantSysIn.PtTypeDic.ContainsKey(curPt))
                            {
                                var type = fireHydrantSysIn.PtTypeDic[curPt];
                                if(type.Contains("DieValve"))
                                {
                                    valvePts.Add(new ValveCasing(curPt,1));
                                }
                                if (type.Contains("GateValve"))
                                {
                                    valvePts.Add(new ValveCasing(curPt, 2));
                                }
                                if(type.Contains("Casing"))
                                {
                                    valvePts.Add(new ValveCasing(curPt, 0));
                                }
                            }

                            var adjs = fireHydrantSysIn.PtDic[curPt];
                            if (adjs.Count == 1)
                            {
                                //if(fireHydrantSysIn.TermPointDic.ContainsKey(curPt))
                                {
                                    termPts.Add(curPt);
                                }
                                continue;
                            }

                            foreach (var adj in adjs)
                            {
                                if (rstPath.Contains(adj))
                                    continue;

                                if (visited2.Contains(adj))
                                {
                                    continue;
                                }

                                visited2.Add(adj);
                                q.Enqueue(adj);
                            }
                        }
                        if (termPts.Count != 0)
                        {
                            if (branchDic.ContainsKey(pt))
                            {
                                branchDic[pt]=termPts;
                            }
                            else
                            {
                                branchDic.Add(pt, termPts);
                            }
                            if (valvePts.Count != 0)
                            {
                                ValveDic.Add(pt, valvePts);
                            }
                        }
                    }
                }
            }
        }

        public static void CreateBranchDNDic(FireHydrantSystemIn fireHydrantSysIn, ThCADCoreNTSSpatialIndex pipeDNSpatialIndex)
        {
            foreach (var pt in fireHydrantSysIn.PtDic.Keys)
            {
                var visited = new HashSet<Point3dEx>();
                var cnt = fireHydrantSysIn.PtDic[pt].Count;
                if(cnt==0)
                {
                    continue;
                }
                var curPt = pt;

                var nextPt = fireHydrantSysIn.PtDic[pt][0];
                if (cnt == 1)
                {
                    while (cnt < 3)
                    {
                        var breakFlag = false;
                        var line = new Line(curPt._pt, nextPt._pt);
                        if (line.Length < 1)
                        {
                            return;
                        }
                        visited.Add(curPt);
                        var area = line.Buffer(200);
                        var dbObjs = pipeDNSpatialIndex.SelectCrossingPolygon(area);
                        if (dbObjs.Count > 0)
                        {
                            foreach (var db in dbObjs)
                            {
                                if (db is DBText dBText)
                                {
                                    if (dBText.Rotation.IsParallelTo(line.Angle))
                                    {
                                        fireHydrantSysIn.TermDnDic.Add(pt, dBText.TextString);
                                        breakFlag = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (breakFlag) break;
                        curPt = new Point3dEx(nextPt._pt);
                        foreach (var pt1 in fireHydrantSysIn.PtDic[curPt])
                        {
                            if (!visited.Contains(pt1))
                            {
                                nextPt = pt1;
                            }
                        }
                        cnt = fireHydrantSysIn.PtDic[curPt].Count;
                    }
                }
            }
        }

        public static void CreateBranchDNDic(SprayIn sprayIn, ThCADCoreNTSSpatialIndex pipeDNSpatialIndex)
        {
            foreach (var pt in sprayIn.PtDic.Keys)
            {
                var visited = new HashSet<Point3dEx>();
                var cnt = sprayIn.PtDic[pt].Count;
                var curPt = pt;

                var nextPt = sprayIn.PtDic[pt][0];
                if (cnt == 1)
                {
                    while (cnt < 3)
                    {
                        var breakFlag = false;
                        var line = new Line(curPt._pt, nextPt._pt);
                        if (line.Length < 1)
                        {
                            return;
                        }
                        visited.Add(curPt);
                        var area = line.Buffer(200);
                        var dbObjs = pipeDNSpatialIndex.SelectCrossingPolygon(area);
                        if (dbObjs.Count > 0)
                        {
                            foreach (var db in dbObjs)
                            {
                                if (db is DBText dBText)
                                {
                                    if (dBText.Rotation.IsParallelTo(line.Angle))
                                    {
                                        sprayIn.TermDnDic.Add(pt, dBText.TextString);
                                        breakFlag = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (breakFlag) break;
                        curPt = new Point3dEx(nextPt._pt);
                        foreach (var pt1 in sprayIn.PtDic[curPt])
                        {
                            if (!visited.Contains(pt1))
                            {
                                nextPt = pt1;
                            }
                        }
                        cnt = sprayIn.PtDic[curPt].Count;
                    }
                }
            }
        }
    }
}
