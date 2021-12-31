using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPHVAC.IndoorFanLayout.Models;

namespace ThMEPHVAC.IndoorFanLayout.Business
{
    class AxisLineSimpleRegion
    {
        public AxisLineSimpleRegion() 
        {
        
        }
        public List<DivisionArea> AxisRegionResult(AxisGroupResult axisGroup, double mergeSpacing) 
        {
            var results = new List<DivisionArea>();
            if (axisGroup == null || axisGroup.IsArc)
                return results;

            //step1 合并线，这里只考虑平行且共线的线进行合并
            var step2OldLine1 = axisGroup.MainCurves;
            var step2OldLine2 = axisGroup.OtherLines;
            var step2AxisLines1 = ParallelLineMerge(step2OldLine1.Cast<Line>().ToList());
            var step2AxisLines2 = ParallelLineMerge(step2OldLine2.Cast<Line>().ToList());
            axisGroup.MainCurves.Clear();
            axisGroup.OtherLines.Clear();
            axisGroup.MainCurves.AddRange(step2AxisLines1);
            axisGroup.OtherLines.AddRange(step2AxisLines2);

            //step2 处理垂直线没有相交上的问题,只会进行延长操作，不会进行缩短操作。
            var step1OldLine1 = axisGroup.MainCurves;
            var step1OldLine2 = axisGroup.OtherLines;
            var step1AxisLines1 = LineConnectToNearLines(step1OldLine1.Cast<Line>().ToList(), step1OldLine2.Cast<Line>().ToList(), 6000);
            var step1AxisLines2 = LineConnectToNearLines(step1OldLine2.Cast<Line>().ToList(), step1OldLine1.Cast<Line>().ToList(), 6000);
            axisGroup.MainCurves.Clear();
            axisGroup.OtherLines.Clear();
            axisGroup.MainCurves.AddRange(step1AxisLines1);
            axisGroup.OtherLines.AddRange(step1AxisLines2);

            //step3 对平行线进行合并，并保持连接关系
            var firstLine = axisGroup.MainCurves.First();
            var lineDir = (firstLine.StartPoint - firstLine.EndPoint).GetNormal();
            var otherDir = Vector3d.ZAxis.CrossProduct(lineDir);

            var mainLines = axisGroup.MainCurves.Cast<Line>().ToList();
            var otherLines = axisGroup.OtherLines.Cast<Line>().ToList();
            var firstStep = LineMergeBySpacingFirstStep(mainLines, otherLines, mergeSpacing);
            var lastMainCurves = LineMergeBySpacing(firstStep, otherLines, mergeSpacing);
            otherLines = BreakLineByOtherDirLines(otherLines, lastMainCurves);
            var secondStep = LineMergeBySpacingFirstStep(otherLines, lastMainCurves, mergeSpacing);
            var lastOtherCures = LineMergeBySpacing(secondStep, lastMainCurves, mergeSpacing);
            lastMainCurves = BreakLineByOtherDirLines(lastMainCurves, lastOtherCures);
            lastOtherCures = BreakLineByOtherDirLines(lastOtherCures, lastMainCurves);

           

            //var testArea = new DivisionArea(axisGroup.IsArc, new Polyline());
            //testArea.AreaCurves.AddRange(lastOtherCures);
            //testArea.AreaCurves.AddRange(lastMainCurves);
            //results.Add(testArea);
            var objs = new DBObjectCollection();
            foreach (var curve in lastOtherCures)
            {
                var sp = curve.StartPoint;
                var ep = curve.EndPoint;
                var dir = (ep - sp).GetNormal();
                objs.Add(new Line(sp-dir.MultiplyBy(5), ep+dir.MultiplyBy(5)));
            }
            foreach (var curve in lastMainCurves)
            {
                var sp = curve.StartPoint;
                var ep = curve.EndPoint;
                var dir = (ep - sp).GetNormal();
                objs.Add(new Line(sp - dir.MultiplyBy(5), ep + dir.MultiplyBy(5)));
            }
            var allPolygons = objs.PolygonsEx();
            foreach (Polyline polyline in allPolygons)
            {
                var division = new DivisionArea(axisGroup.IsArc, polyline);
                var otherDirDot = otherDir.DotProduct(Vector3d.YAxis);
                var lineDirDot = lineDir.DotProduct(Vector3d.YAxis);
                if (Math.Abs(otherDirDot) > Math.Abs(lineDirDot))
                {
                    division.XVector = otherDirDot > 0 ? otherDir.Negate() : otherDir;
                }
                else
                {
                    division.XVector = lineDirDot > 0 ? lineDir.Negate() : lineDir;
                }
                results.Add(division);
            }
            return results;
        }

        public List<Line> LineConnectToNearLines(List<Line> extendLines, List<Line> targetLines, double nearDis = 1000)
        {
            var newAxisLines = new List<Line>();
            foreach (Line line in extendLines)
            {
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                var lineSp = line.StartPoint;
                var lineEp = line.EndPoint;
                Line spNearLine = null;
                Line epNearLine = null;
                double spNearDis = double.MaxValue;
                double epNearDis = double.MaxValue;
                foreach (Line item in targetLines)
                {
                    var prjPoint = lineSp.PointToLine(item);
                    if (!prjPoint.PointInLineSegment(item,nearDis))
                        continue;
                    if (prjPoint.DistanceTo(lineSp) < nearDis)
                    {
                        if ((prjPoint - lineSp).DotProduct(lineDir) > 0)
                            continue;
                        var tempDis = prjPoint.DistanceTo(lineSp);
                        if (tempDis < spNearDis)
                        {
                            spNearDis = tempDis;
                            spNearLine = item;
                        }
                    }
                    else if (prjPoint.DistanceTo(lineEp) < nearDis)
                    {
                        if ((lineEp - prjPoint).DotProduct(lineDir) > 0)
                            continue;
                        var tempDis = prjPoint.DistanceTo(lineEp);
                        if (tempDis < epNearDis)
                        {
                            epNearDis = tempDis;
                            epNearLine = item;
                        }
                    }
                }
                if (spNearLine != null)
                {
                    lineSp = lineSp.PointToLine(spNearLine);
                    //if ((lineSp - line.StartPoint).GetNormal().DotProduct(lineDir) > 0) 
                    //    lineSp = line.StartPoint;
                }
                if (epNearLine != null)
                {
                    lineEp = lineEp.PointToLine(epNearLine);
                    //if ((lineEp - line.EndPoint).GetNormal().DotProduct(lineDir) < 0)
                    //    lineEp = line.EndPoint;
                }
                newAxisLines.Add(new Line(lineSp, lineEp));
            }
            return newAxisLines;
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

        public List<Line> LineMergeBySpacingFirstStep(List<Line> dirLines, List<Line> otherLines, double spacing)
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
                        var interLines = GetInterLines(rmLine, otherLines);
                        if (interLines.Count < 1)
                            continue;
                        foreach (var inteLine in interLines)
                        {
                            var inter = IndoorFanCommon.FindIntersection(baseLine, inteLine, out List<Point3d> tempPoints);
                            if (inter > 0)
                                continue;
                            var newLine = LineConnectLine(baseLine, inteLine);
                            otherLines.Remove(inteLine);
                            otherLines.Add(newLine);
                        }
                    }
                }
            }
            newLines.AddRange(outLines);
            return newLines;
        }
        public List<Line> LineMergeBySpacing(List<Line> dirLines, List<Line> otherLines, double spacing)
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
                        var interLines = GetInterLines(rmLine, otherLines);
                        if (interLines.Count < 1)
                            continue;
                        foreach (var inteLine in interLines)
                        {
                            var inter = IndoorFanCommon.FindIntersection(baseLine, inteLine, out List<Point3d> tempPoints);
                            if (inter > 0)
                                continue;
                            var newLine = LineConnectLine(baseLine, inteLine);
                            otherLines.Remove(inteLine);
                            otherLines.Add(newLine);
                        }
                    }
                }
            }
            return newLines;
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
                if (thisDis < 0.0001)
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
        List<Point3d> LinePrjLineInter(Line line, Line otherLine)
        {
            var prjSp = otherLine.StartPoint.PointToLine(line);
            var prjEp = otherLine.EndPoint.PointToLine(line);
            IndoorFanCommon.FindIntersection(line, new Line(prjSp, prjEp), out List<Point3d> interPoints);
            return interPoints;
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
        List<Line> GetInterLines(Line rmLine, List<Line> targetLines)
        {
            var interLines = new List<Line>();
            foreach (var item in targetLines)
            {
                var inter = IndoorFanCommon.FindIntersection(rmLine, item, out List<Point3d> interPoints);
                if (inter > 0)
                    interLines.Add(item);
            }
            return interLines;
        }
        Line LineConnectLine(Line baseLine, Line targetLine)
        {
            //这里是两个垂直的线合并后要保持连接关系时进行的处理
            var sp = targetLine.StartPoint;
            var ep = targetLine.EndPoint;
            var prjSp = sp.PointToLine(baseLine);
            var prjEp = ep.PointToLine(baseLine);
            if (prjSp.DistanceTo(sp) > prjEp.DistanceTo(ep))
                return new Line(sp, prjEp);
            return new Line(prjSp, ep);
        }
        public List<Line> BreakLineByOtherDirLines(List<Line> beBreakLines, List<Line> targetLines)
        {
            //这里一般没有平行的线，不考虑合并的问题
            //一根线，移除也只是移除两头的线段，不会有中间的移除
            var lines = new List<Line>();
            while (beBreakLines.Count > 0)
            {
                var line = beBreakLines.First();
                beBreakLines.Remove(line);
                var interPoints = new List<Point3d>();
                foreach (var item in targetLines)
                {
                    var inter = IndoorFanCommon.FindIntersection(line, item, out List<Point3d> tempPoints);
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
                if (interPoints.Count > 1)
                {

                    var linePoints = interPoints.OrderBy(c => c.DistanceTo(line.StartPoint)).ToList();
                    lines.Add(new Line(linePoints.First(), linePoints.Last()));
                }
            }
            return lines;
        }
    }
}
