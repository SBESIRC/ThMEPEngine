using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPHVAC.IndoorFanLayout.Models;

namespace ThMEPHVAC.IndoorFanLayout.Business
{
    class AxisArcSimpleRegion
    {
        public AxisArcSimpleRegion()
        {

        }
        public List<DivisionArea> AxisRegionResult(AxisGroupResult axisGroup, double mergeSpacing)
        {
            var results = new List<DivisionArea>();
            if (axisGroup == null || !axisGroup.IsArc)
                return results;

            //step1 先处理垂直线没有相交上的问题,只会进行延长操作，不会进行缩短操作。
            var step1OldLine1 = axisGroup.MainCurves;//MainLine为弧线
            var step1OldLine2 = axisGroup.OtherLines;
            //这里一般是线延长到弧线上，获取弧线的角度改变延长得到新的线
            var step1AxisLines1 = LineConnectToNearLines(step1OldLine1.Cast<Arc>().ToList(), step1OldLine2.Cast<Line>().ToList());
            var step1AxisLines2 = LineConnectToNearLines(step1OldLine2.Cast<Line>().ToList(), step1OldLine1.Cast<Arc>().ToList());
            axisGroup.MainCurves.Clear();
            axisGroup.OtherLines.Clear();
            axisGroup.MainCurves.AddRange(step1AxisLines1);
            axisGroup.OtherLines.AddRange(step1AxisLines2);

            //step2 合并线，这里只考虑平行且共线的线进行合并
            var step2OldLine1 = axisGroup.MainCurves;
            var step2OldLine2 = axisGroup.OtherLines;
            var step2AxisLines1 = ParallelLineMerge(step2OldLine1.Cast<Arc>().ToList());
            var step2AxisLines2 = ParallelLineMerge(step2OldLine2.Cast<Line>().ToList());
            axisGroup.MainCurves.Clear();
            axisGroup.OtherLines.Clear();
            axisGroup.MainCurves.AddRange(step2AxisLines1);
            axisGroup.OtherLines.AddRange(step2AxisLines2);
            var mainLines = axisGroup.MainCurves.Cast<Arc>().ToList();
            var otherLines = axisGroup.OtherLines.Cast<Line>().ToList();
            var firstStep = LineMergeBySpacingFirstStep(mainLines, otherLines, mergeSpacing);
            var mainCurves = ArcMergeBySpacing(firstStep, otherLines, mergeSpacing);
            otherLines = BreakLineByOtherDirLines(otherLines, mainCurves);
            var secondStep = LineMergeBySpacingFirstStep(otherLines, mainCurves, mergeSpacing);
            var otherCurves = LineMergeBySpacing(secondStep, mainCurves, mergeSpacing);
            mainCurves = mainCurves.OrderBy(c => c.Length).ToList();
            mainCurves = BreakLineByOtherDirLines(mainCurves, otherCurves);
            otherCurves = BreakLineByOtherDirLines(otherCurves, mainCurves);
            return CalcAreaRegions(mainCurves,otherCurves);
        }
        List<DivisionArea> CalcAreaRegions(List<Arc> mainCurves, List<Line> otherCurves)
        {
            var results = new List<DivisionArea>();
            var allAreas = GetAllAreaRegions(mainCurves, otherCurves);
            foreach (Polyline polyline in allAreas)
            {
                if (!polyline.Closed || polyline.Area < 100)
                    continue;
                var allPoints = IndoorFanCommon.GetPolylinePoints(polyline);
                var plineCurves = GetThisPolyCurves(mainCurves, otherCurves, polyline);
                if (plineCurves.Count < 3)
                    continue;
                //根据这些线构造Polyline
                var startLine = CalcFirstCurve(polyline, plineCurves, out Curve rmCurve);
                plineCurves.Remove(rmCurve);
                var segments = new PolylineSegmentCollection();
                if (startLine is Line sLine)
                    segments.Add(new PolylineSegment(sLine.StartPoint.ToPoint2D(), sLine.EndPoint.ToPoint2D()));
                else if (startLine is Arc sArc)
                    segments.Add(new PolylineSegment(sArc.EndPoint.ToPoint2D(), sArc.StartPoint.ToPoint2D(), sArc.BulgeFromCurve(sArc.IsClockWise())));
                var startPoint = startLine.StartPoint;
                var endPoint = startLine.EndPoint;
                while (plineCurves.Count > 0)
                {
                    var currentSp = endPoint;
                    Curve currentLine = null;
                    foreach (var curve in plineCurves)
                    {
                        if (curve.StartPoint.DistanceTo(currentSp) < 5 || curve.EndPoint.DistanceTo(currentSp) < 5)
                        {
                            currentLine = curve;
                            break;
                        }
                    }
                    if (currentLine == null)
                    {
                        break;
                    }
                    plineCurves.Remove(currentLine);
                    bool isReverse = isReverse = currentLine.EndPoint.DistanceTo(currentSp) < 5;
                    var sPoint = currentLine.StartPoint;
                    var ePoint = currentLine.EndPoint;
                    if (isReverse) 
                    {
                        sPoint = currentLine.EndPoint;
                        ePoint = currentLine.StartPoint;
                    }
                    endPoint = ePoint;
                    if (currentLine is Line)
                    {
                        segments.Add(new PolylineSegment(sPoint.ToPoint2D(), ePoint.ToPoint2D()));
                    }
                    else if (currentLine is Arc arc)
                    {
                        var bulge = arc.BulgeFromCurve(arc.IsClockWise());
                        bulge = isReverse ? -bulge : bulge;
                        segments.Add(new PolylineSegment(sPoint.ToPoint2D(), ePoint.ToPoint2D(), bulge));
                    }
                }
                var temp = segments.Join(new Tolerance(2, 2));
                var newPLine = temp.First().ToPolyline();
                var outPL = newPLine.GetOffsetCurves(1);
                foreach (var item in outPL)
                {
                    if (item is Polyline pl)
                    {
                        var innerPL = pl.GetOffsetCurves(-1);
                        foreach (var newPl in innerPL)
                        { 
                            newPLine = newPl as Polyline;
                            newPLine.Closed = true;
                        }
                    }
                }
                if (!newPLine.Closed) 
                {
                    newPLine.Closed = true;
                }
                var division = new DivisionArea(true, newPLine);
                results.Add(division);
            }
            return results;
        }
        Curve CalcFirstCurve(Polyline polyline, List<Curve> targetCurves,out Curve rmCurve)
        {
            var allLines = IndoorFanCommon.GetPolylineCurves(polyline);
            var startLine = targetCurves.First();
            rmCurve = targetCurves.First();
            foreach (var item in targetCurves)
            {
                bool isBreak = false;
                if (item is Line line)
                {
                    foreach (var curve in allLines)
                    {
                        if (curve.StartPoint.DistanceTo(item.StartPoint) < 5 && curve.EndPoint.DistanceTo(item.EndPoint) < 5)
                        {
                            startLine = item;
                            rmCurve = item;
                            isBreak = true;
                            break;
                        }
                        else if (curve.EndPoint.DistanceTo(item.StartPoint) < 5 && curve.StartPoint.DistanceTo(item.EndPoint) < 5)
                        {
                            startLine = new Line(item.EndPoint, item.StartPoint);
                            rmCurve = item;
                            isBreak = true;
                            break;
                        }
                    }
                }
                if (isBreak)
                    break;
            }
            return startLine;
        }
        List<Curve> GetThisPolyCurves(List<Arc> mainCurves, List<Line> otherCurves,Polyline polyline) 
        {
            var allPoints = IndoorFanCommon.GetPolylinePoints(polyline);
            var plineCurves = new List<Curve>();
            //获取所有起点和终点都在改点集内的线
            foreach (var arc in mainCurves)
            {
                //mainCurve是弧线
                var sp = arc.StartPoint;
                var ep = arc.EndPoint;
                if (allPoints.Any(c => c.DistanceTo(sp) < 5) && allPoints.Any(c => c.DistanceTo(ep) < 5))
                {
                    bool isAdd = true;
                    foreach (var check in plineCurves)
                    {
                        if ((check.StartPoint.DistanceTo(sp) < 5 && check.EndPoint.DistanceTo(ep) < 1) ||
                            (check.EndPoint.DistanceTo(sp) < 5 && check.StartPoint.DistanceTo(ep) < 1))
                            isAdd = false;
                    }
                    if (!isAdd)
                        continue;
                    plineCurves.Add(arc);
                }
            }
            foreach (var line in otherCurves)
            {
                var sp = line.StartPoint;
                var ep = line.EndPoint;
                if (allPoints.Any(c => c.DistanceTo(sp) < 5) && allPoints.Any(c => c.DistanceTo(ep) < 5))
                {
                    bool isAdd = true;
                    foreach (var check in plineCurves)
                    {
                        if ((check.StartPoint.DistanceTo(sp) < 5 && check.EndPoint.DistanceTo(ep) < 1) ||
                            (check.EndPoint.DistanceTo(sp) < 5 && check.StartPoint.DistanceTo(ep) < 1))
                            isAdd = false;
                    }
                    if (!isAdd)
                        continue;
                    plineCurves.Add(line);
                }
            }
            return plineCurves;
        }
        List<Polyline> GetAllAreaRegions(List<Arc> mainCurves, List<Line> otherCurves) 
        {
            //计算每个闭合区域，NTS不支持弧形区域的直接计算，这里将弧打散，计算区域后，再根据点将弧形重新计算进来
            var objs = new DBObjectCollection();
            foreach (var arc in mainCurves)
            {
                //这里是弧线,弧线延长1毫米，这里弧线延长不会导致成圆，这里不考虑这些情况
                var sAngle = arc.StartAngle;
                var eAngle = arc.EndAngle;
                var changeAngle = 180.0 / (Math.PI * arc.Radius);
                objs.Add(new Arc(arc.Center, arc.Normal, arc.Radius, sAngle - changeAngle, eAngle + changeAngle));
            }
            foreach (var curve in otherCurves)
            {
                //这里是直线
                var sp = curve.StartPoint;
                var ep = curve.EndPoint;
                var lineDir = (ep - sp).GetNormal();
                objs.Add(new Line(sp - lineDir.MultiplyBy(1), ep + lineDir.MultiplyBy(1)));
            }
            var spliteAreas = objs.PolygonsEx();
            var allPolylines = new List<Polyline>();
            foreach (var item in spliteAreas) 
            {
                if (item is Polyline polyline)
                    allPolylines.Add(polyline);
            }
            return allPolylines;
        }
        public List<Arc> ArcMergeBySpacing(List<Arc> dirArcs, List<Line> otherLines, double spacing)
        {
            var newArcs = new List<Arc>();
            while (dirArcs.Count > 0)
            {
                dirArcs = dirArcs.OrderBy(c => c.Length).ToList();
                var arc = dirArcs.First();
                dirArcs.Remove(arc);
                var innerNear = ArcNearArc(arc, dirArcs, true, spacing);
                var outNear = ArcNearArc(arc, dirArcs, false, spacing);
                if (innerNear == null && outNear == null)
                {
                    newArcs.Add(arc);
                    continue;
                }
                else
                {
                    var addLines = new List<Arc>();
                    var baseLine = innerNear;
                    if (null == innerNear)
                    {
                        //右侧有距离近的共线
                        baseLine = outNear;
                    }
                    else if (null == outNear)
                    {
                        //左侧有距离近的共线
                        baseLine = innerNear;
                    }
                    else
                    {
                        //两侧都有,处理近的线
                        var innrDis = Math.Abs(arc.Radius - innerNear.Radius);
                        var outDis = Math.Abs(outNear.Radius - arc.Radius);
                        if (innrDis < outDis)
                            baseLine = innerNear;
                        else
                            baseLine = outNear;
                    }
                    var interArc = LinePrjLineInter(arc, baseLine);
                    if (interArc == null) 
                    {
                        newArcs.Add(arc);
                        continue;
                    }
                    var breakLines = ArcBreakByPoints(arc, new List<Point3d> { interArc.StartPoint,interArc.EndPoint});
                    var pt1 = interArc.StartPoint;
                    var pt2 = interArc.EndPoint;
                    var rmLines = new List<Arc>();
                    foreach (var item in breakLines)
                    {
                        if ((item.StartPoint.DistanceTo(pt1) < 1 && item.EndPoint.DistanceTo(pt2) < 1)
                            || (item.StartPoint.DistanceTo(pt2) < 1 && item.EndPoint.DistanceTo(pt2) < 1))
                        {
                            rmLines.Add(item);
                            continue;
                        }
                        addLines.Add(item);
                    }
                    if (addLines.Count > 0)
                        dirArcs.AddRange(addLines);
                    //处理和原来相交的线，并保持连接关系
                    foreach (var rmLine in rmLines)
                    {
                        var interLines = GetInterLines(rmLine, otherLines.Cast<Curve>().ToList());
                        if (interLines.Count < 1)
                            continue;
                        foreach (var inteLine in interLines)
                        {
                            var tempLine = inteLine as Line;
                            var inter = CircleArcUtil.ArcIntersectLineSegment(baseLine, tempLine, out List<Point3d> tempPoints);
                            if (inter > 0)
                                continue;
                            var newLine = LineConnectLine(baseLine, tempLine);
                            otherLines.Remove(tempLine);
                            otherLines.Add(newLine);
                        }
                    }
                }
            }
            return newArcs;
        }
        public List<Line> LineMergeBySpacing(List<Line> dirLines, List<Arc> otherLines, double spacing)
        {
            var newLines = new List<Line>();
            while (dirLines.Count > 0)
            {
                dirLines = dirLines.OrderBy(c => c.Length).ToList();
                var line = dirLines.First();
                dirLines.Remove(line);
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                var leftDir = Vector3d.ZAxis.CrossProduct(lineDir);
                var leftNear = GetDirNearLine(line, dirLines, leftDir, spacing);
                var rightNear = GetDirNearLine(line, dirLines, leftDir.Negate(), spacing);

                if (leftNear == null && rightNear == null)
                {
                    newLines.Add(line);
                    continue;
                }
                else
                {
                    var addLines = new List<Line>();
                    var baseLine = leftNear;
                    if (null == leftNear)
                    {
                        //右侧有距离近的共线
                        baseLine = rightNear;
                    }
                    else if (null == rightNear)
                    {
                        //左侧有距离近的共线
                        baseLine = leftNear;
                    }
                    else
                    {
                        //两侧都有,处理近的线
                        var rightPrjSp = rightNear.StartPoint.PointToLine(line);
                        var leftPrjSp = leftNear.StartPoint.PointToLine(line);
                        if (leftPrjSp.DistanceTo(leftNear.StartPoint) < rightPrjSp.DistanceTo(rightNear.StartPoint))
                            baseLine = leftNear;
                        else
                            baseLine = rightNear;
                    }
                    var interPoints = LinePrjLineInter(line, baseLine);
                    var breakLines = LineBreakByPoints(line, interPoints);
                    var pt1 = interPoints[0];
                    var pt2 = interPoints[1];
                    var rmLines = new List<Line>();
                    foreach (var item in breakLines)
                    {
                        if ((item.StartPoint.DistanceTo(pt1) < 1 && item.EndPoint.DistanceTo(pt2) < 1)
                            || (item.StartPoint.DistanceTo(pt2) < 1 && item.EndPoint.DistanceTo(pt2) < 1))
                        {
                            rmLines.Add(item);
                            continue;
                        }
                        addLines.Add(item);
                    }
                    if (addLines.Count > 0)
                        dirLines.AddRange(addLines);
                    //处理和原来相交的线，并保持连接关系
                    foreach (var rmLine in rmLines)
                    {
                        var interLines = GetInterLines(rmLine, otherLines.Cast<Curve>().ToList());
                        if (interLines.Count < 1)
                            continue;
                        foreach (var inteLine in interLines)
                        {
                            var arc = inteLine as Arc;
                            //var inter = IndoorFanCommon.FindIntersection(baseLine, inteLine, out List<Point3d> tempPoints);
                            //if (inter > 0)
                            //    continue;
                            var newLine = LineConnectLine(baseLine, arc);
                            otherLines.Remove(arc);
                            otherLines.Add(newLine);
                        }
                    }
                }
            }
            return newLines;
        }
        
        List<Point3d> LinePrjLineInter(Line line, Line otherLine)
        {
            var prjSp = otherLine.StartPoint.PointToLine(line);
            var prjEp = otherLine.EndPoint.PointToLine(line);
            IndoorFanCommon.FindIntersection(line, new Line(prjSp, prjEp), out List<Point3d> interPoints);
            return interPoints;
        }
        Arc LinePrjLineInter(Arc arc, Arc otherLine)
        {
            var prjSp = CircleArcUtil.PointToArc(otherLine.StartPoint, arc);
            var prjEp = CircleArcUtil.PointToArc(otherLine.EndPoint, arc);
            var sAngle = Vector3d.XAxis.GetAngleTo((prjSp - arc.Center).GetNormal(), Vector3d.ZAxis);
            var eAngle = Vector3d.XAxis.GetAngleTo((prjEp - arc.Center).GetNormal(), Vector3d.ZAxis);
            var prjArc = new Arc(arc.Center, arc.Normal, arc.Radius, sAngle, eAngle);
            return CircleArcUtil.ArcIntersectArc(arc, prjArc);
        }
        
        Arc LineConnectLine(Line baseLine, Arc targetArc)
        {
            var sp = baseLine.StartPoint;
            var ep = baseLine.EndPoint;
            var prjSp = CircleArcUtil.PointToArc(baseLine.StartPoint, targetArc);
            var prjEp = CircleArcUtil.PointToArc(baseLine.EndPoint, targetArc);
            var linePoints = new List<Point3d>() { targetArc.StartPoint, targetArc.EndPoint };
            if (!linePoints.Any(c => c.DistanceTo(prjSp) < 1))
                linePoints.Add(prjSp);
            if (!linePoints.Any(c => c.DistanceTo(prjEp) < 1))
                linePoints.Add(prjEp);
            var sAngle = double.MaxValue;
            var eAngle = double.MinValue;
            for (int i = 0; i < linePoints.Count - 1; i++)
            {
                var pt = linePoints[i];
                var angle = Vector3d.XAxis.GetAngleTo((pt - targetArc.Center).GetNormal(), Vector3d.ZAxis);
                if (angle < sAngle)
                    sAngle = angle;
                if (angle > eAngle)
                    eAngle = angle;
            }
            return new Arc(targetArc.Center, targetArc.Normal, targetArc.Radius, sAngle, eAngle);
        }
        Line LineConnectLine(Arc baseLine, Line targetLine)
        {
            //这里是两个垂直的线合并后要保持连接关系时进行的处理
            var sp = targetLine.StartPoint;
            var ep = targetLine.EndPoint;
            var prjSp = CircleArcUtil.PointToArc(sp, baseLine);
            var prjEp = CircleArcUtil.PointToArc(ep, baseLine);
            if (prjSp.DistanceTo(sp) > prjEp.DistanceTo(ep))
                return new Line(sp, prjEp);
            return new Line(prjSp, ep);
        }
        public List<Arc> LineConnectToNearLines(List<Arc> extendLines, List<Line> targetLines, double nearDis = 1000)
        {
            var newAxisLines = new List<Arc>();
            foreach (Arc arc in extendLines)
            {
                Point3d spNearPt = arc.StartPoint;
                Point3d epNearPt = arc.EndPoint;
                var arcSp = arc.StartPoint;
                var arcEp = arc.EndPoint;
                foreach (Line line in targetLines)
                {
                    if (!arcSp.PointInLineSegment(line) && !arcEp.PointInLineSegment(line, nearDis))
                        continue;
                    var circle = new Circle(arc.Center, arc.Normal, arc.Radius);
                    var circleInter = circle.CircleIntersectLineSegment(line, out List<Point3d> tempInterPoints);
                    if (tempInterPoints.Count < 2)
                        continue;
                    //这里一般和圆的交点不知一个
                    var pt1 = tempInterPoints[0];
                    var pt2 = tempInterPoints[1];
                    if (!pt1.PointInLineSegment(line, nearDis) && !pt2.PointInLineSegment(line, nearDis))
                        continue;
                    bool pt1InArc = CircleArcUtil.PointRelationArc(pt1, arc, 1, Math.PI * 5 / 180.0) == 1;
                    bool pt2InArc = CircleArcUtil.PointRelationArc(pt2, arc, 1, Math.PI * 5 / 180.0) == 1;
                    if (!pt1InArc && !pt2InArc)
                        continue;
                    if (!pt1InArc)
                    {
                        if (pt1.DistanceTo(arcSp) > pt1.DistanceTo(arcEp))
                        {
                            if (pt1.DistanceTo(arcEp) > epNearPt.DistanceTo(arcEp))
                                epNearPt = pt1;
                        }
                        else
                        {
                            if (pt1.DistanceTo(arcSp) > spNearPt.DistanceTo(arcSp))
                                spNearPt = pt1;
                        }
                    }
                    else 
                    {
                        if (pt2.DistanceTo(arcSp) > pt2.DistanceTo(arcEp))
                        {
                            if (pt2.DistanceTo(arcEp) > epNearPt.DistanceTo(arcEp))
                                epNearPt = pt2;
                        }
                        else
                        {
                            if (pt2.DistanceTo(arcSp) > spNearPt.DistanceTo(arcSp))
                                spNearPt = pt2;
                        }
                    }
                }
                var sAngle = Vector3d.XAxis.GetAngleTo((spNearPt - arc.Center).GetNormal(), Vector3d.ZAxis);
                var eAngle = Vector3d.XAxis.GetAngleTo((epNearPt - arc.Center).GetNormal(), Vector3d.ZAxis);
                newAxisLines.Add(new Arc(arc.Center, arc.Normal, arc.Radius, sAngle, eAngle));
            }
            return newAxisLines;
        }

        public List<Line> LineConnectToNearLines(List<Line> extendLines, List<Arc> targetLines, double nearDis = 1000)
        {
            var newAxisLines = new List<Line>();
            foreach (Line line in extendLines)
            {
                Point3d spNearPt = line.StartPoint;
                Point3d epNearPt = line.EndPoint;
                var lineSp = line.StartPoint;
                var lineEp = line.EndPoint;
                foreach (Arc arc in targetLines)
                {
                    if (CircleArcUtil.PointRelationArc(lineSp,arc) == 1 || CircleArcUtil.PointRelationArc(lineEp, arc) ==1)
                        continue;
                    var circle = new Circle(arc.Center, arc.Normal, arc.Radius);
                    var circleInter = circle.CircleIntersectLineSegment(line, out List<Point3d> tempInterPoints);
                    if (tempInterPoints.Count < 2)
                        continue;
                    var pt1 = tempInterPoints[0];
                    var pt2 = tempInterPoints[1];
                    bool pt1Line = pt1.PointInLineSegment(line, nearDis);
                    bool pt2Line = pt2.PointInLineSegment(line, nearDis);
                    if (!pt1Line && !pt2Line)
                        continue;
                    if (!pt1Line)
                    {
                        if (pt1.DistanceTo(lineSp) > pt1.DistanceTo(lineEp))
                        {
                            if (pt1.DistanceTo(lineEp) > epNearPt.DistanceTo(lineEp))
                                epNearPt = pt1;
                        }
                        else
                        {
                            if (pt1.DistanceTo(lineSp) > spNearPt.DistanceTo(lineSp))
                                spNearPt = pt1;
                        }
                    }
                    else
                    {
                        if (pt2.DistanceTo(lineSp) > pt2.DistanceTo(lineEp))
                        {
                            if (pt2.DistanceTo(lineEp) > epNearPt.DistanceTo(lineEp))
                                epNearPt = pt2;
                        }
                        else
                        {
                            if (pt2.DistanceTo(lineSp) > spNearPt.DistanceTo(lineSp))
                                spNearPt = pt2;
                        }
                    }
                }
                newAxisLines.Add(new Line(lineSp,lineEp));
            }
            return newAxisLines;
        }

        public List<Arc> ParallelLineMerge(List<Arc> targetLines)
        {
            var lines = new List<Arc>();
            while (targetLines.Count > 0)
            {
                var first = targetLines.First();
                targetLines.Remove(first);
                var sp = first.StartPoint;
                var ep = first.EndPoint;
                var dir = (ep - sp).GetNormal();
                while (true)
                {
                    Arc mergeLine = null;
                    foreach (var arc in targetLines)
                    {
                        if (first.Center.DistanceTo(arc.Center) > 1 || Math.Abs(first.Radius-arc.Radius)>1)
                            continue;
                        var toArcSp = CircleArcUtil.PointToArc(sp, arc);
                        var toArcEp = CircleArcUtil.PointToArc(ep, arc);
                        if (CircleArcUtil.PointRelationArc(toArcSp,arc) ==1
                            || CircleArcUtil.PointRelationArc(toArcEp, arc) == 1)
                        {
                            mergeLine = arc;
                            break;
                        }

                    }
                    if (mergeLine == null)
                        break;
                    targetLines.Remove(mergeLine);
                    var tempSp = CircleArcUtil.PointToArc(mergeLine.StartPoint, first);
                    var tempEp = CircleArcUtil.PointToArc(mergeLine.EndPoint, first);
                    var allPoints = new List<Point3d>() { sp, ep, tempEp, tempSp };
                    allPoints = ThPointVectorUtil.PointsOrderByDirection(allPoints, dir, false);
                    sp = allPoints.First();
                    ep = allPoints.Last();
                }
                var sAngle = Vector3d.XAxis.GetAngleTo((sp - first.Center).GetNormal(), Vector3d.ZAxis);
                var eAngle = Vector3d.XAxis.GetAngleTo((ep - first.Center).GetNormal(), Vector3d.ZAxis);
                lines.Add(new Arc(first.Center, first.Normal, first.Radius, sAngle, eAngle));
            }
            return lines;
        }

        public List<Line> ParallelLineMerge(List<Line> targetLines)
        {
            var lines = new List<Line>();
            while (targetLines.Count > 0)
            {
                var first = targetLines.First();
                targetLines.Remove(first);
                var sp = first.StartPoint;
                var ep = first.EndPoint;
                var dir = (ep - sp).GetNormal();
                while (true)
                {
                    Line mergeLine = null;
                    foreach (var line in targetLines)
                    {
                        var lineSp = line.StartPoint;
                        var lineEp = line.EndPoint;
                        var prjSp = lineSp.PointToLine(sp, dir);
                        var prjEp = lineEp.PointToLine(ep, dir);
                        if (prjSp.DistanceTo(lineSp) > 0.00001)
                            continue;
                        if (sp.PointInLineSegment(prjSp, prjEp)
                            || ep.PointInLineSegment(prjSp, prjEp)
                            || prjSp.PointInLineSegment(sp, ep)
                            || prjEp.PointInLineSegment(sp, ep))
                        {
                            mergeLine = line;
                            break;
                        }
                    }
                    if (mergeLine == null)
                        break;
                    targetLines.Remove(mergeLine);
                    var tempSp = mergeLine.StartPoint.PointToLine(sp, dir);
                    var tempEp = mergeLine.EndPoint.PointToLine(ep, dir);
                    var allPoints = new List<Point3d>() { sp, ep, tempEp, tempSp };
                    allPoints = ThPointVectorUtil.PointsOrderByDirection(allPoints, dir, false);
                    sp = allPoints.First();
                    ep = allPoints.Last();
                }
                lines.Add(new Line(sp, ep));
            }
            return lines;
        }

        public List<Arc> LineMergeBySpacingFirstStep(List<Arc> dirArcs, List<Line> otherLines, double spacing)
        {
            var outArcs = new List<Arc>();
            foreach (var arc in dirArcs)
            {
                var innerNear = ArcNearArc(arc, dirArcs, true, spacing);
                var outNear = ArcNearArc(arc, dirArcs, false, spacing);
                if (null == outNear || innerNear == null)
                {
                    outArcs.Add(arc);
                    continue;
                }
            }
            foreach (var item in outArcs)
                dirArcs.Remove(item);
            var newLines = new List<Arc>();
            while (dirArcs.Count > 0)
            {
                dirArcs = dirArcs.OrderBy(c => c.Length).ToList();
                var arc = dirArcs.First();
                dirArcs.Remove(arc);
                var tempLines = new List<Arc>();
                tempLines.AddRange(dirArcs);
                tempLines.AddRange(outArcs);
                var innerNear = ArcNearArc(arc, tempLines, true, spacing);
                var outNear = ArcNearArc(arc, tempLines, false, spacing);
                if (null == innerNear || outNear == null)
                {
                    newLines.Add(arc);
                    continue;
                }
                var leftDis = Math.Abs(innerNear.Radius - arc.Radius);
                var rightDis = Math.Abs(outNear.Radius - arc.Radius);
                if (leftDis > 10000 || rightDis > 10000)
                {
                    newLines.Add(arc);
                    continue;
                }
                var addLines = new List<Arc>();
                //两侧都有,处理近的线
                var baseLine = leftDis < rightDis ? innerNear : outNear;
                var interArc = LinePrjLineInter(arc, baseLine);
                var breakLines = ArcBreakByPoints(arc, new List<Point3d> { interArc.StartPoint,interArc.EndPoint});
                var pt1 = interArc.StartPoint;
                var pt2 = interArc.EndPoint;
                var rmLines = new List<Arc>();
                foreach (var item in breakLines)
                {
                    if ((item.StartPoint.DistanceTo(pt1) < 1 && item.EndPoint.DistanceTo(pt2) < 1)
                        || (item.StartPoint.DistanceTo(pt2) < 1 && item.EndPoint.DistanceTo(pt1) < 1))
                    {
                        rmLines.Add(item);
                        continue;
                    }
                    addLines.Add(item);
                }
                if (addLines.Count > 0)
                    dirArcs.AddRange(addLines);
                if (rmLines.Count > 0)
                {
                    //处理和原来相交的线，并保持连接关系
                    foreach (var rmLine in rmLines)
                    {
                        var interLines = GetInterLines(rmLine, otherLines.Cast<Curve>().ToList());
                        if (interLines.Count < 1)
                            continue;
                        foreach (var inteLine in interLines)
                        {
                            var tempLine = inteLine as Line;
                            var inter = CircleArcUtil.ArcIntersectLineSegment(baseLine, tempLine, out List<Point3d> tempPoints);
                            if (inter > 0)
                                continue;
                            var newLine = LineConnectLine(baseLine, tempLine);
                            otherLines.Remove(tempLine);
                            otherLines.Add(newLine);
                        }
                    }
                }
            }
            newLines.AddRange(outArcs);
            return newLines;
        }
        public List<Line> LineMergeBySpacingFirstStep(List<Line> dirLines, List<Arc> otherLines, double spacing)
        {
            //第一步处理边界处，边界处的线是要保留的
            var outLines = new List<Line>();
            foreach (var line in dirLines)
            {
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                var leftDir = Vector3d.ZAxis.CrossProduct(lineDir);
                var leftNear = GetDirNearLine(line, dirLines, leftDir, spacing);
                var rightNear = GetDirNearLine(line, dirLines, leftDir.Negate(), spacing);
                if (null == leftNear || rightNear == null)
                {
                    outLines.Add(line);
                    continue;
                }
            }
            foreach (var item in outLines)
                dirLines.Remove(item);
            var newLines = new List<Line>();
            while (dirLines.Count > 0)
            {
                dirLines = dirLines.OrderBy(c => c.Length).ToList();
                var line = dirLines.First();
                dirLines.Remove(line);
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                var leftDir = Vector3d.ZAxis.CrossProduct(lineDir);
                var tempLines = new List<Line>();
                tempLines.AddRange(dirLines);
                tempLines.AddRange(outLines);
                var leftNear = GetDirNearLine(line, tempLines, leftDir, spacing);
                var rightNear = GetDirNearLine(line, tempLines, leftDir.Negate(), spacing);
                if (null == leftNear || rightNear == null)
                {
                    newLines.Add(line);
                    continue;
                }
                var rightPrjSp = rightNear.StartPoint.PointToLine(line);
                var leftPrjSp = leftNear.StartPoint.PointToLine(line);
                var leftDis = leftPrjSp.DistanceTo(leftNear.StartPoint);
                var rightDis = rightPrjSp.DistanceTo(rightNear.StartPoint);
                if (leftDis > 10000 || rightDis > 10000)
                {
                    newLines.Add(line);
                    continue;
                }
                var addLines = new List<Line>();
                //两侧都有,处理近的线
                var baseLine = leftDis < rightDis ? leftNear : rightNear;
                var interPoints = LinePrjLineInter(line, baseLine);
                var breakLines = LineBreakByPoints(line, interPoints);
                var pt1 = interPoints[0];
                var pt2 = interPoints[1];
                var rmLines = new List<Line>();
                foreach (var item in breakLines)
                {
                    if ((item.StartPoint.DistanceTo(pt1) < 1 && item.EndPoint.DistanceTo(pt2) < 1)
                        || (item.StartPoint.DistanceTo(pt2) < 1 && item.EndPoint.DistanceTo(pt1) < 1))
                    {
                        rmLines.Add(item);
                        continue;
                    }
                    addLines.Add(item);
                }
                if (addLines.Count > 0)
                    dirLines.AddRange(addLines);
                if (rmLines.Count > 0)
                {
                    //处理和原来相交的线，并保持连接关系
                    foreach (var rmLine in rmLines)
                    {
                        var interLines = GetInterLines(rmLine, otherLines.Cast<Curve>().ToList());
                        if (interLines.Count < 1)
                            continue;
                        foreach (var inteLine in interLines)
                        {
                            var arc = inteLine as Arc;
                            //var inter = IndoorFanCommon.FindIntersection(baseLine, inteLine, out List<Point3d> tempPoints);
                            //if (inter > 0)
                            //    continue;
                            var newLine = LineConnectLine(baseLine, arc);
                            otherLines.Remove(arc);
                            otherLines.Add(newLine);
                        }
                    }
                }
            }
            newLines.AddRange(outLines);
            return newLines;
        }
        List<Curve> GetInterLines(Curve rmLine, List<Curve> targetLines)
        {
            //这里两种线，如果都是线段，垂直
            //传入的如果是线段，圆弧
            var interLines = new List<Curve>();
            foreach (var item in targetLines)
            {
                if (rmLine is Line line)
                {
                    if (item is Line line2)
                    {
                        var inter = IndoorFanCommon.FindIntersection(line, line2, out List<Point3d> interPoints);
                        if (inter > 0)
                            interLines.Add(item);
                    }
                    else if (item is Arc arc)
                    {
                        CircleArcUtil.ArcIntersectLineSegment(arc, line, out List<Point3d> interPoints);
                        if (interPoints.Count == 1)
                            interLines.Add(item);
                    }
                }
                else if (rmLine is Arc arc) 
                {
                    if (item is Line line2)
                    {
                        CircleArcUtil.ArcIntersectLineSegment(arc, line2, out List<Point3d> interPoints);
                        if (interPoints.Count == 1)
                            interLines.Add(item);
                    }
                }
            }
            return interLines;
        }
        Line GetDirNearLine(Line line, List<Line> targetLines, Vector3d vector, double maxDistace)
        {
            Line nearLine = null;
            double neaeDis = double.MaxValue;
            foreach (var item in targetLines)
            {
                var sp = item.StartPoint;
                var ep = item.EndPoint;
                var prjSp = sp.PointToLine(line);
                //这里共轴线的线不会有相交部分
                var thisDis = prjSp.DistanceTo(sp);
                if (thisDis < 0.0001 || thisDis>maxDistace)
                    continue;
                var prjEp = ep.PointToLine(line);
                var thisDir = (sp - prjSp).GetNormal();
                if (thisDir.DotProduct(vector) < 0)
                    continue;
                IndoorFanCommon.FindIntersection(line, new Line(prjSp, prjEp), out List<Point3d> interPoints);
                if (interPoints.Count < 2)
                    continue;
                if (interPoints[0].DistanceTo(interPoints[1]) < 0.01)
                    continue;
                if (thisDis < maxDistace && thisDis < neaeDis)
                {
                    nearLine = item;
                    neaeDis = thisDis;
                }
            }
            return nearLine;
        }
        
        Arc ArcNearArc(Arc arc, List<Arc> targetArcs,bool isInner, double maxDistace)
        {
            Arc nearArc = null;
            double arcSAngle = arc.StartAngle;
            double arcEAngle = arc.EndAngle;
            double neaeDis = double.MaxValue;
            foreach (var item in targetArcs)
            {
                if (item.Center.DistanceTo(arc.Center) > 1 || Math.Abs(item.Radius - arc.Radius) < 1)
                    continue;
                if ((isInner && item.Radius > arc.Radius) || (!isInner && item.Radius < arc.Radius))
                    continue;
                if (arcSAngle >= item.EndAngle || arcEAngle <= item.StartAngle)
                    continue;
                var sp = item.StartPoint;
                //这里共轴线的线不会有相交部分
                var prjSp = CircleArcUtil.PointToArc(sp, arc);
                var thisDis = prjSp.DistanceTo(sp);
                if (thisDis < maxDistace && thisDis < neaeDis)
                {
                    nearArc = item;
                    neaeDis = thisDis;
                }
            }
            return nearArc;
        }
        List<Line> LineBreakByPoints(Line beBreakLine, List<Point3d> points)
        {
            var retLines = new List<Line>();
            var linePoints = new List<Point3d>() { beBreakLine.StartPoint, beBreakLine.EndPoint };
            foreach (var point in points)
            {
                if (linePoints.Any(c => c.DistanceTo(point) < 1))
                    continue;
                if (point.PointInLineSegment(beBreakLine))
                    linePoints.Add(point);
            }
            linePoints = linePoints.OrderBy(c => c.DistanceTo(beBreakLine.StartPoint)).ToList();
            for (int i = 0; i < linePoints.Count - 1; i++)
            {
                var sp = linePoints[i];
                var ep = linePoints[i + 1];
                retLines.Add(new Line(sp, ep));
            }
            return retLines;
        }

        List<Arc> ArcBreakByPoints(Arc beBreakArc, List<Point3d> points)
        {
            var retLines = new List<Arc>();
            var arcPoints = new List<Point3d>() { beBreakArc.StartPoint, beBreakArc.EndPoint };
            foreach (var point in points)
            {
                if (arcPoints.Any(c => c.DistanceTo(point) < 1))
                    continue;
                if (CircleArcUtil.PointRelationArc(point, beBreakArc) ==1)
                    arcPoints.Add(point);
            }
            //对点进行，按照弧形的角度大小排序
            var pointAngle = new Dictionary<Point3d, double>();
            foreach (var point in arcPoints)
            {
                var angle = Vector3d.XAxis.GetAngleTo((point - beBreakArc.Center).GetNormal(), Vector3d.ZAxis);
                pointAngle.Add(point, angle);
            }
            arcPoints = pointAngle.OrderBy(c => c.Value).Select(c => c.Key).ToList();
            for (int i = 0; i < arcPoints.Count - 1; i++)
            {
                var sp = arcPoints[i];
                var ep = arcPoints[i + 1];
                var sAngle = Vector3d.XAxis.GetAngleTo((sp - beBreakArc.Center).GetNormal(), Vector3d.ZAxis);
                var eAngle = Vector3d.XAxis.GetAngleTo((ep - beBreakArc.Center).GetNormal(), Vector3d.ZAxis);
                retLines.Add(new Arc(beBreakArc.Center, beBreakArc.Normal, beBreakArc.Radius, sAngle, eAngle));
            }
            return retLines;
        }

        public List<Line> BreakLineByOtherDirLines(List<Line> beBreakLines, List<Arc> targetLines)
        {
            var lines = new List<Line>();
            while (beBreakLines.Count > 0)
            {
                var line = beBreakLines.First();
                beBreakLines.Remove(line);
                var interPoints = new List<Point3d>();
                foreach (var item in targetLines)
                {
                    CircleArcUtil.ArcIntersectLineSegment(item, line, out List<Point3d> tempPoints);
                    if (tempPoints.Count > 0)
                    {
                        foreach (var point in tempPoints)
                        {
                            if (interPoints.Any(c => c.DistanceTo(point) < 1))
                                continue;
                            interPoints.Add(point);
                        }
                    }
                }
                if (interPoints.Count < 1) 
                    continue;
                interPoints = interPoints.OrderBy(c => c.DistanceTo(line.StartPoint)).ToList();
                for (int i = 0; i < interPoints.Count - 1; i++)
                {
                    var pt1 = interPoints[i];
                    var pt2 = interPoints[i + 1];
                    lines.Add(new Line(pt1, pt2));
                }
            }
            return lines;
        }
        public List<Arc> BreakLineByOtherDirLines(List<Arc> beBreakLines, List<Line> targetLines)
        {
            var lines = new List<Arc>();
            while (beBreakLines.Count > 0)
            {
                var line = beBreakLines.First();
                beBreakLines.Remove(line);
                var interPoints = new List<Point3d>();
                foreach (var item in targetLines)
                {
                    CircleArcUtil.ArcIntersectLineSegment(line, item, out List<Point3d> tempPoints);
                    if (tempPoints.Count > 0)
                    {
                        foreach (var point in tempPoints)
                        {
                            if (interPoints.Any(c => c.DistanceTo(point) < 1))
                                continue;
                            interPoints.Add(point);
                        }
                    }
                }
                //对点进行，按照弧形的角度大小排序
                var pointAngle = new Dictionary<Point3d, double>();
                foreach (var point in interPoints) 
                {
                    var angle = Vector3d.XAxis.GetAngleTo((point - line.Center).GetNormal(), Vector3d.ZAxis);
                    pointAngle.Add(point, angle);
                }
                interPoints = pointAngle.OrderBy(c => c.Value).Select(c => c.Key).ToList();
                if (interPoints.Count > 1)
                {
                    for (int i = 0; i < interPoints.Count - 1; i++)
                    {
                        var pt1 = interPoints[i];
                        var pt2 = interPoints[i + 1];
                        var Angle1 = Vector3d.XAxis.GetAngleTo((pt1 - line.Center).GetNormal(), Vector3d.ZAxis);
                        var Angle2 = Vector3d.XAxis.GetAngleTo((pt2 - line.Center).GetNormal(), Vector3d.ZAxis);
                        var sAngle = Math.Min(Angle1, Angle2);
                        var eAngle = Math.Max(Angle1, Angle2);
                        lines.Add(new Arc(line.Center, line.Normal, line.Radius, sAngle, eAngle));
                    }
                }
            }
            return lines;
        }
    }
}
