using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightConnectAdjust
{
    class LightAdjustConnectLine
    {
        Polyline _outPolyline;
        List<Polyline> _innerPolylines;
        List<MaxGroupLight> _maxGroupLights;
        List<Line> _laneLines;
        List<Polyline> _allWalls;
        List<Polyline> _allColumns;
        List<LightBlockReference> _areaLightBlocks;
        double _offSetMaxSpace = 200.0;//当没有后续线时该线的偏移值
        double _offSetMinSpace = 150.0;//当后续有线时的偏移值
        public LightAdjustConnectLine(List<MaxGroupLight> laneGroupLights, List<LightBlockReference> areaLights, Polyline outPolyline, List<Polyline> innerPolylines)
        {
            _areaLightBlocks = new List<LightBlockReference>();
            _maxGroupLights = new List<MaxGroupLight>();
            _laneLines = new List<Line>();
            _allWalls = new List<Polyline>();
            _allColumns = new List<Polyline>();
            _outPolyline = outPolyline;
            _innerPolylines = innerPolylines;
            if (null != laneGroupLights && laneGroupLights.Count > 0)
            {
                foreach (var item in laneGroupLights)
                {
                    if (null == item)
                        continue;
                    _maxGroupLights.Add(item);
                }
            }
            if (null != areaLights && areaLights.Count > 0)
            {
                foreach (var item in areaLights)
                {
                    if (null == item)
                        continue;
                    _areaLightBlocks.Add(item);
                }
            }
        }
        public void InitData(List<Polyline> obstacleWalls, List<Polyline> obstacleColumns, List<Line> laneLines)
        {
            _allWalls.Clear();
            _allColumns.Clear();
            _laneLines.Clear();
            double minArea = 100;
            if (null != obstacleWalls && obstacleWalls.Count > 0)
            {
                foreach (var item in obstacleWalls)
                {
                    if (null == item || item.Area < minArea)
                        continue;
                    _allWalls.Add((Polyline)item.Clone());
                }
            }
            if (null != obstacleColumns && obstacleColumns.Count > 0)
            {
                foreach (var item in obstacleColumns)
                {
                    if (null == item || item.Area < minArea)
                        continue;
                    _allColumns.Add((Polyline)item.Clone());
                }
            }
            if (null != laneLines && laneLines.Count > 0)
            {
                foreach (var item in laneLines)
                {
                    if (item == null || item.Length < 10)
                        continue;
                    _laneLines.Add(item);
                }
            }
        }
        public List<MaxGroupLight> AdjustMaxGroupConnect()
        {
            if (_areaLightBlocks == null || _areaLightBlocks.Count < 1)
                return _maxGroupLights;
            foreach (var light in _areaLightBlocks)
            {
                var checkPoints = new List<Point3d> { light.ConnectPoint1, light.ConnectPoint2, light.ConnectPoint3 };
                var lightDir = light.LightVector;
                var leftDir = Vector3d.ZAxis.CrossProduct(lightDir);
                foreach (var maxGroup in _maxGroupLights)
                {
                    var lines = LightAllConnectLines(light.ConnectPoint2, false);
                    if (lines ==null || lines.Count < 1)
                        continue;
                    foreach (var point in checkPoints)
                    {
                        var startLines = new List<Line>();
                        var otherLines = new List<Line>();
                        foreach (var line in lines)
                        {
                            if (line.StartPoint.DistanceTo(point) < 5)
                                startLines.Add((Line)line.Clone());
                            else if (line.EndPoint.DistanceTo(point) < 5)
                                startLines.Add(new Line(line.EndPoint, line.StartPoint));
                            else
                                otherLines.Add((Line)line.Clone());
                        }
                        if (startLines.Count < 2)
                            continue;
                        startLines = startLines.OrderBy(c => c.Length).ToList();
                        while (startLines.Count > 0)
                        {
                            var changeLines = LightConnectChange(startLines, otherLines, lightDir);
                            if (null == changeLines || changeLines.Count < 1)
                                continue;
                            MaxGroupChangeLines(maxGroup, point, changeLines);
                        }
                    }
                }
            }
            return _maxGroupLights;
        }
        private List<LightConnectLineChange> LightConnectChange(List<Line> startLines,List<Line> otherLines,Vector3d lightDir) 
        {
            var tempLightDir = lightDir;
            var retChange = new List<LightConnectLineChange>();
            var leftDir = Vector3d.ZAxis.CrossProduct(lightDir);
            var first = startLines.First();
            var firstDir = (first.EndPoint - first.StartPoint).GetNormal();
            if (Math.Abs(firstDir.DotProduct(leftDir)) > 0.4)
            { 
                leftDir = lightDir; 
                tempLightDir = Vector3d.ZAxis.CrossProduct(lightDir);
            }
            startLines.Remove(first);
            var nearLines = new List<Line>();
            var notNearLines = new List<Line>();
            foreach (var line in startLines)
            {
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                if (line.EndPoint.DistanceTo(first.StartPoint) < 1)
                    lineDir = lineDir.Negate();
                var dotDir = firstDir.DotProduct(lineDir);
                if (dotDir < 0 || Math.Abs(dotDir) < 0.2) 
                {
                    notNearLines.Add(line);
                    continue;
                }
                var angle = lineDir.GetAngleTo(firstDir);
                angle %= Math.PI * 2;
                angle %= Math.PI;
                if (angle > (Math.PI * 10.0 / 180.0)) 
                {
                    notNearLines.Add(line);
                    continue;
                }
                if (line.StartPoint.DistanceTo(first.StartPoint) < 5)
                    nearLines.Add(line);
                else
                    nearLines.Add(new Line(line.EndPoint, line.StartPoint));
            }
            if (nearLines.Count < 1)
                return retChange;
            
            var newStartLines = new List<Line>();
            
            startLines.Clear();
            startLines.AddRange(notNearLines);
            nearLines.Add(first);
            nearLines = nearLines.OrderBy(c => c.Length).ToList();
            int maxCount = nearLines.Count;
            List<Vector3d> hisDirs = new List<Vector3d>();
            //这里线不会太多，最多两个，太多说明连接点选择不合理
            while (nearLines.Count > 1) 
            {
                var startLine = nearLines[0];
                var nearLine = nearLines[1];
                Line startNextLine = null;
                Line nearLineNextLine = null;
                foreach (var line in otherLines)
                {
                    if (startNextLine == null && line.StartPoint.DistanceTo(startLine.EndPoint) < 5)
                        startNextLine = line;
                    else if (startNextLine == null && line.EndPoint.DistanceTo(startLine.EndPoint) < 5)
                        startNextLine = new Line(line.EndPoint, line.StartPoint);
                    else if (nearLineNextLine == null && line.StartPoint.DistanceTo(nearLine.EndPoint) < 5)
                        nearLineNextLine = line;
                    else if (nearLineNextLine == null && line.EndPoint.DistanceTo(nearLine.EndPoint) < 5)
                        nearLineNextLine = new Line(line.EndPoint, line.StartPoint);
                }
                if (startNextLine != null)
                {
                    var firstRm = LineChangeResult(first, startNextLine, nearLineNextLine,maxCount, ref hisDirs, leftDir);
                    if(null != firstRm)
                        retChange.Add(firstRm);
                }
                else if (nearLineNextLine == null)
                {
                    //两个都没有后续的线，这里需要将短的线进行偏移
                    var offDis = _offSetMaxSpace;
                    var lineLeftDir = Vector3d.ZAxis.CrossProduct(firstDir);
                    var endMoveOffset = lineLeftDir.MultiplyBy(offDis);
                    var startMoveOffset = lineLeftDir.MultiplyBy(offDis) + firstDir.MultiplyBy(offDis);
                    var startSp = startLine.StartPoint;
                    var startEp = startLine.EndPoint;
                    var firstRm = new LightConnectLineChange(startLine, null);
                    firstRm.AddLines.Add(new Line(startSp + startMoveOffset, startEp + endMoveOffset));
                    firstRm.AddLines.Add(new Line(startSp, startSp + startMoveOffset));
                    retChange.Add(firstRm);
                }
                if (nearLineNextLine != null)
                {
                    var nearRm = LineChangeResult(nearLine, nearLineNextLine, startNextLine, maxCount, ref hisDirs, leftDir);
                    if (null != nearRm)
                        retChange.Add(nearRm);
                }
                nearLines.Remove(startLine);
            }
            return retChange;
        }

        private void MaxGroupChangeLines(MaxGroupLight maxGroupLight, Point3d checkPoint, List<LightConnectLineChange> lineChanges) 
        {
            foreach (var group in maxGroupLight.LightGroups)
            {
                foreach (var item in group.LightConnectLines)
                {
                    if (item.StartLightConnectPoint.DistanceTo(checkPoint) < 10 || item.EndLightConnectPoint.DistanceTo(checkPoint) < 10)
                    {
                        var lines = LightConnectLineChangeLines(item.ConnectLines, lineChanges);
                        item.ConnectLines.Clear();
                        if (null != lines && lines.Count > 0)
                            item.ConnectLines.AddRange(lines);
                    }
                }
                if (group.ConnectParent != null)
                {
                    if (group.ConnectParent.StartLightConnectPoint.DistanceTo(checkPoint) < 10 || group.ConnectParent.EndLightConnectPoint.DistanceTo(checkPoint) < 10)
                    {
                        var lines = LightConnectLineChangeLines(group.ConnectParent.ConnectLines, lineChanges);
                        group.ConnectParent.ConnectLines.Clear();
                        if (null != lines && lines.Count > 0)
                            group.ConnectParent.ConnectLines.AddRange(lines);
                    }
                }
            }
            if (maxGroupLight.ConnectLines.Count < 1 || maxGroupLight.ConnectLightPoint.DistanceTo(checkPoint) > 10)
                return;
            var addLines = LightConnectLineChangeLines(maxGroupLight.ConnectLines, lineChanges);
            maxGroupLight.ConnectLines.Clear();
            if (null == addLines || addLines.Count < 1)
                return;
            var nearNode = maxGroupLight.LightGroups.Where(c => c.NearGroupPoint.DistanceTo(maxGroupLight.NearNodeLightPoint) < 5).FirstOrDefault();
            var mainLine = nearNode.NearLine;
            var allPoints = new List<Point3d>();
            foreach (var line in addLines) 
            {
                var sp = line.StartPoint;
                var ep = line.EndPoint;
                if (!allPoints.Any(c => c.DistanceTo(sp) < 5))
                    allPoints.Add(sp);
                if (!allPoints.Any(c => c.DistanceTo(ep) < 5))
                    allPoints.Add(ep);
            }
            double nearDis = double.MaxValue;
            Point3d nearPoint = allPoints.OrderBy(c=>c.DistanceTo(maxGroupLight.WireTroughLinePoint)).First();
            foreach (var point in allPoints) 
            {
                var prj=point.PointToLine(mainLine);
                var dis = prj.DistanceTo(point);
                if (dis < nearDis)
                {
                    nearDis = dis;
                    nearPoint = point;
                }
            }
            maxGroupLight.WireTroughLinePoint = nearPoint;
            maxGroupLight.ConnectLines.Clear();
            maxGroupLight.ConnectLines.AddRange(addLines);
        }
        private List<Line> LightConnectLineChangeLines(List<Line> lightLines, List<LightConnectLineChange> lineChanges) 
        {
            var copyLines = new List<Line>();
            lightLines.ForEach(c => copyLines.Add((Line)c.Clone()));
            if (null == lineChanges || lineChanges.Count < 1)
                return copyLines;
            bool inChange = false;
            var tempLines = new List<Line>();
            foreach (var change in lineChanges)
            {
                if (inChange)
                    break;
                tempLines.Clear();
                int hisCount = change.RmNextLine != null ? 2 : 1;
                foreach (var line in copyLines)
                {
                    if ((change.RmLine.StartPoint.DistanceTo(line.StartPoint) < 1 && change.RmLine.EndPoint.DistanceTo(line.EndPoint) < 1) ||
                        (change.RmLine.StartPoint.DistanceTo(line.EndPoint) < 1 && change.RmLine.EndPoint.DistanceTo(line.StartPoint) < 1))
                    {
                        continue;
                    }
                    else if (null != change.RmNextLine)
                    {
                        if ((change.RmNextLine.StartPoint.DistanceTo(line.StartPoint) < 1 && change.RmNextLine.EndPoint.DistanceTo(line.EndPoint) < 1) ||
                            (change.RmNextLine.StartPoint.DistanceTo(line.EndPoint) < 1 && change.RmNextLine.EndPoint.DistanceTo(line.StartPoint) < 1))
                        {
                            continue;
                        }
                        else 
                        {
                            tempLines.Add(line);
                        }
                    }
                    else
                    {
                        tempLines.Add(line);
                    }
                }
                inChange = hisCount == (copyLines.Count-tempLines.Count);
                if (inChange)
                    tempLines.AddRange(change.AddLines);
            }
            if (!inChange)
                return copyLines;
            return tempLines;
        }
        List<Line> LightAllConnectLines(Point3d lightPoint, bool isConnectPoint)
        {
            var lightLines = new List<Line>();
            foreach (var maxGroupLight in _maxGroupLights)
            {
                foreach (var group in maxGroupLight.LightGroups)
                {
                    foreach (var item in group.LightConnectLines)
                    {
                        var checkSp = isConnectPoint ? item.StartLightConnectPoint : item.StartLightPoint;
                        var checkEp = isConnectPoint ? item.EndLightConnectPoint : item.EndLightPoint;
                        if (checkSp.DistanceTo(lightPoint) < 10 || checkEp.DistanceTo(lightPoint) < 10)
                            lightLines.AddRange(item.ConnectLines);
                    }
                    if (group.ConnectParent == null)
                        continue;
                    var checkGroupSp = isConnectPoint ? group.ConnectParent.StartLightConnectPoint : group.ConnectParent.StartLightPoint;
                    var checkGroupEp = isConnectPoint ? group.ConnectParent.EndLightConnectPoint : group.ConnectParent.EndLightPoint;
                    if (checkGroupSp.DistanceTo(lightPoint) < 10 || checkGroupEp.DistanceTo(lightPoint) < 10)
                        lightLines.AddRange(group.ConnectParent.ConnectLines);
                }
                if (maxGroupLight.ConnectLines.Count < 1)
                    continue;
                var checkPoint = isConnectPoint ? maxGroupLight.ConnectLightPoint : maxGroupLight.NearNodeLightPoint;
                if (checkPoint.DistanceTo(lightPoint) < 10)
                    lightLines.AddRange(maxGroupLight.ConnectLines);
            }
            return lightLines;
        }

        LightConnectLineChange LineChangeResult(Line startLine,Line nextLine,Line nearNextLine,int maxCount,ref List<Vector3d> hisDirs,Vector3d leftDir) 
        {
            var startDir = (startLine.EndPoint - startLine.StartPoint).GetNormal();
            var startNextDir = (nextLine.EndPoint - nextLine.StartPoint).GetNormal();
            var tempLightDir = leftDir.CrossProduct(Vector3d.ZAxis);
            bool dirInHis = false;
            foreach (var dir in hisDirs)
            {
                if (dirInHis)
                    break;
                dirInHis = startNextDir.DotProduct(dir) > 0.5;
            }
            if (dirInHis)
            {
                if (maxCount < 3)
                    return null;
                startNextDir = startNextDir.Negate();
                dirInHis = false;
                foreach (var dir in hisDirs)
                {
                    if (dirInHis)
                        break;
                    dirInHis = startNextDir.DotProduct(dir) > 0.5;
                }
                if (dirInHis && maxCount>2)
                    return null;
            }
            hisDirs.Add(startNextDir);
            var offDis = _offSetMinSpace;
            if (null == nearNextLine)
                offDis = _offSetMaxSpace;
            var dotStartNext = leftDir.MultiplyBy(offDis).DotProduct(startNextDir);
            var endMoveOffset = leftDir.MultiplyBy(dotStartNext);
            var startMoveOffset = leftDir.MultiplyBy(dotStartNext) + startDir.MultiplyBy(Math.Abs(tempLightDir.MultiplyBy(offDis).DotProduct(startDir)));
            var startSp = startLine.StartPoint;
            var startEp = startLine.EndPoint;
            var firstRm = new LightConnectLineChange(startLine, nextLine);
            firstRm.AddLines.Add(new Line(startSp + startMoveOffset, startEp + endMoveOffset));
            firstRm.AddLines.Add(new Line(startSp, startSp + startMoveOffset));
            if(null != nextLine)
                firstRm.AddLines.Add(new Line(nextLine.StartPoint + endMoveOffset, nextLine.EndPoint));
            return firstRm;
        }
    }
    class LightConnectLineChange 
    {
        public Line RmLine { get; }
        public Line RmNextLine { get; }
        public List<Line> AddLines { get; }
        public LightConnectLineChange(Line rmLine,Line rmNextLine) 
        {
            //灯上连接点移除线记录，防止移除后加入的数据错乱，这里记录两个线，nextLine可能为null,
            //主要是防止一个灯上连接另外两个灯时出现第一根线共线，后续添加时出现错乱问题
            this.RmLine = rmLine;
            this.RmNextLine = rmNextLine;
            this.AddLines = new List<Line>();
        }
    }
}
