using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPLighting.ParkingStall.Model;
using ThMEPLighting.ParkingStall.Worker.LightConnect;

namespace ThMEPLighting.ParkingStall.Worker.LightConnectAdjust
{
    class LightAdjustConnectPoint
    {
        Polyline _outPolyline;
        List<Polyline> _innerPolylines;
        List<MaxGroupLight> _maxGroupLights;
        List<Line> _laneLines;
        LightConnectLight _lightConnectLight;
        List<Polyline> _allWalls;
        List<Polyline> _allColumns;
        List<LightBlockReference> _areaLightBlocks;
        double _changeLineMinLength = 100.0;
        double _changeLinePrjLength = 800.0;
        double _lineExtendToCheck = 500.0;//连接线拐角处延长长度
        double _angleLineToConver = Math.PI * 20.0 / 180.0;
        public LightAdjustConnectPoint(List<MaxGroupLight> laneGroupLights, List<LightBlockReference> areaLights, Polyline outPolyline, List<Polyline> innerPolylines) 
        {
            _maxGroupLights = new List<MaxGroupLight>();
            _laneLines = new List<Line>();
            _allWalls = new List<Polyline>();
            _allColumns = new List<Polyline>();
            _outPolyline = outPolyline;
            _innerPolylines = innerPolylines;
            _areaLightBlocks = new List<LightBlockReference>();
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
        public void AdjustMaxGroupConnect()
        {
            _lightConnectLight = new LightConnectLight(_outPolyline, _innerPolylines, _allWalls, _allColumns);
            //第一步：调整连接点
            ChangeConnectPoint(true);
            //第二步：直连交叉处理
            foreach (var maxGroup in _maxGroupLights)
            {
                foreach (var group in maxGroup.LightGroups)
                {
                    foreach (var item in group.LightConnectLines)
                    {
                        if (item.ConnectLines.Count != 1)
                            continue;
                        ConnectChange(item);
                    }
                    if (group.ConnectParent != null)
                    {
                        if (group.ConnectParent.ConnectLines.Count != 1)
                            continue;
                        ConnectChange(group.ConnectParent);
                    }
                }
            }
            //第三步：再次调整连接点，防止线调整后有需要调整的线
            ChangeConnectPoint(false);
        }
        void ChangeConnectPoint(bool adjustStart) 
        {
            foreach (var maxGroup in _maxGroupLights)
            {
                foreach (var group in maxGroup.LightGroups)
                {
                    CheckMoveOneConnect(group.ConnectParent, true);
                    CheckMoveOneConnect(group.ConnectParent, false);
                    foreach (var lightConnect in group.LightConnectLines)
                    {
                        CheckMoveOneConnect(lightConnect, true);
                        CheckMoveOneConnect(lightConnect, false);
                    }
                }
                if (!adjustStart)
                    continue;
                var lightPoint = maxGroup.NearNodeLightPoint;
                var checkLight = _areaLightBlocks.Where(c => c.ConnectPoint2.DistanceTo(lightPoint) < 10).First();
                var nearNode = maxGroup.LightGroups.Where(c => c.NearGroupPoint.DistanceTo(lightPoint) < 5).FirstOrDefault();
                var prjPoint = lightPoint.PointToLine(nearNode.NearLine);
                maxGroup.ConnectLines.Clear();
                var newLines = LineMoveToConnect(lightPoint, lightPoint, new List<Line> { new Line(lightPoint, prjPoint) }, out Point3d newStartPoint, out Point3d newEndPoint);
                if (newLines == null || newLines.Count < 1)
                {
                    maxGroup.ConnectLightPoint = lightPoint;
                    maxGroup.WireTroughLinePoint = prjPoint;
                    maxGroup.ConnectLines.Add(new Line(lightPoint, prjPoint));
                    continue;
                }
                maxGroup.ConnectLightPoint = newStartPoint;
                maxGroup.WireTroughLinePoint = newEndPoint;
                maxGroup.ConnectLines.AddRange(newLines);
            }
        }
        void CheckMoveOneConnect(LightConnectLine lightConnect, bool isStart)
        {
            if (null == lightConnect)
                return;
            var checkPoint = isStart ? lightConnect.StartLightConnectPoint : lightConnect.EndLightConnectPoint;
            var checkLightPoint = isStart ? lightConnect.StartLightPoint : lightConnect.EndLightPoint;
            if (checkLightPoint.DistanceTo(checkPoint) > 10)
                return;
            var checkLight = _areaLightBlocks.Where(c => c.ConnectPoint2.DistanceTo(checkLightPoint) < 10).First();
            var newLines = LineMoveToConnect(checkLightPoint, checkPoint, lightConnect.ConnectLines, out Point3d newStartPoint, out Point3d newEndPoint);
            if (newLines == null || newLines.Count < 1)
                return;
            if (isStart)
                lightConnect.StartLightConnectPoint = newStartPoint;
            else
                lightConnect.EndLightConnectPoint = newStartPoint;
            lightConnect.ConnectLines.Clear();
            lightConnect.ConnectLines.AddRange(newLines);
        }

        List<Line> LineMoveToConnect(Point3d lightPoint,Point3d checkPoint,List<Line> lightConnectLines,out Point3d newStartPoint,out Point3d newEndPoint) 
        {
            newStartPoint =new Point3d();
            newEndPoint = new Point3d();
            var checkLight = _areaLightBlocks.Where(c => c.ConnectPoint2.DistanceTo(lightPoint) < 10).First();
            Line startLine = null;
            var newLines = new List<Line>();
            foreach (var line in lightConnectLines)
            {
                var lineSp = line.StartPoint;
                var lineEp = line.EndPoint;
                if (lineSp.DistanceTo(checkPoint) < 5)
                {
                    startLine = line;
                }
                else if (checkPoint.DistanceTo(lineEp) < 5)
                {
                    startLine = new Line(lineEp, lineSp);
                }
                else 
                {
                    newLines.Add(line);
                }
            }
            if (startLine == null)
                return newLines;
            var lightDir = checkLight.LightVector;
            var startLineDir = (startLine.EndPoint - startLine.StartPoint).GetNormal();
            var nextLine = newLines.Where(c => c.StartPoint.DistanceTo(startLine.EndPoint) < 5 || c.EndPoint.DistanceTo(startLine.EndPoint) < 5).FirstOrDefault();
            var angle = lightDir.GetAngleTo(startLineDir);
            angle %= Math.PI;
            angle = angle > Math.PI / 2 ? (Math.PI - angle) : angle;
            if (angle > _angleLineToConver)
            {
                if (null == nextLine)
                    return newLines;
                var nextSp = nextLine.StartPoint;
                var nextEp = nextLine.EndPoint;
                if (startLine.EndPoint.DistanceTo(nextEp) < 1)
                {
                    nextSp = nextLine.EndPoint;
                    nextEp = nextLine.StartPoint;
                }
                var nextDir = (nextEp - nextSp).GetNormal();
                var dot = nextDir.DotProduct(lightDir);
                var startMove = dot > 0? (checkLight.ConnectPoint2 - checkLight.ConnectPoint1): (checkLight.ConnectPoint2 - checkLight.ConnectPoint3);
                var endMove = nextDir.MultiplyBy(Math.Abs(startMove.DotProduct(nextDir)));
                newStartPoint = startLine.StartPoint + startMove;
                newEndPoint = startLine.EndPoint + endMove;
                newLines.Add(new Line(newStartPoint, newEndPoint));
                newLines.Remove(nextLine);
                newLines.Add(new Line(newEndPoint, nextEp));
            }
            else 
            {
                //角度小，进行平移，终点可能会有其它线，要注意对终点的影响
                var startMoveOffset = lightDir.DotProduct(startLineDir) > 0 ? (checkLight.ConnectPoint3 - checkLight.ConnectPoint2) : (checkLight.ConnectPoint1 - checkLight.ConnectPoint2);
                newStartPoint = startLine.StartPoint + startMoveOffset;
                newEndPoint = startLine.EndPoint;
                if (nextLine != null)
                {
                    var nextSp = nextLine.StartPoint;
                    var nextEp = nextLine.EndPoint;
                    if (startLine.EndPoint.DistanceTo(nextEp) < 1)
                    {
                        nextSp = nextLine.EndPoint;
                        nextEp = nextLine.StartPoint;
                    }
                    var nextDir = (nextEp - nextSp).GetNormal();
                    var dot = nextDir.DotProduct(startMoveOffset);
                    if (Math.Abs(dot) > 1)
                    {
                        newLines.Remove(nextLine);
                        newEndPoint = nextSp + nextDir.MultiplyBy(dot);
                        newLines.Add(new Line(newEndPoint, nextEp));
                    }
                }
                newLines.Add(new Line(newStartPoint, newEndPoint));
            }
            return newLines;
        }
        void ConnectChange(LightConnectLine lightConnect) 
        {
            if (lightConnect.ConnectLines.Count != 1)
                return;
            var otherLines = new List<Line>();
            var firstLine = lightConnect.ConnectLines.First();
            var startCrossCount = CrossLaneLineCount(lightConnect.ConnectLines);
            //if (firstLine.Length < 5000)
            //    return;
            if (firstLine.StartPoint.DistanceTo(lightConnect.StartLightConnectPoint) > 5)
                firstLine = new Line(firstLine.StartPoint, firstLine.EndPoint);
            var firstDir = (firstLine.EndPoint - firstLine.EndPoint).GetNormal();
            var startLight = _areaLightBlocks.Where(c => c.ConnectPoint2.DistanceTo(lightConnect.StartLightPoint) < 1).First();
            var endLight = _areaLightBlocks.Where(c => c.ConnectPoint2.DistanceTo(lightConnect.EndLightPoint) < 1).First();
            var startLines = LightAllConnectLines(lightConnect.StartLightConnectPoint, true);
            var endLines =  LightAllConnectLines(lightConnect.EndLightConnectPoint, true);

            var startPointLines = new List<Line>();
            var endPointLines = new List<Line>();
            foreach (var line in startLines)
            {
                if (line.StartPoint.DistanceTo(lightConnect.StartLightConnectPoint) < 1)
                    startPointLines.Add(line);
                else if (line.EndPoint.DistanceTo(lightConnect.StartLightConnectPoint) < 1)
                    startPointLines.Add(new Line(line.EndPoint, line.StartPoint));
            }
            foreach (var line in endLines)
            {
                if (line.StartPoint.DistanceTo(lightConnect.EndLightConnectPoint) < 1)
                    endPointLines.Add(line);
                else if (line.EndPoint.DistanceTo(lightConnect.EndLightConnectPoint) < 1)
                    endPointLines.Add(new Line(line.EndPoint, line.StartPoint));
            }

            var firstLightDir = startLight.LightVector;
            var endLightDir = endLight.LightVector;
            var lightDirDot = firstLightDir.DotProduct(endLightDir);
            if (Math.Abs(lightDirDot) > 0.3 && Math.Abs(lightDirDot) < 0.8)
                return;

            bool endIsMain = endPointLines.Count> startPointLines.Count;
            var rmLines = new List<Line>();
            var newLines = new List<Line>();

            var mainLightDir = firstLightDir;
            bool haveMainDir = endPointLines.Count != startPointLines.Count;
            if (haveMainDir) 
            {
                Vector3d centerVector = new Vector3d();
                if (endPointLines.Count > startPointLines.Count)
                {
                    foreach (var line in endPointLines) 
                    {
                        var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                        if (centerVector.Length < 0.1)
                            centerVector = lineDir;
                        else
                            centerVector += lineDir;
                    }
                }
                else 
                {
                    foreach (var line in startPointLines)
                    {
                        var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                        if (centerVector.Length < 0.1)
                            centerVector = lineDir;
                        else
                            centerVector += lineDir;
                    }
                }
                centerVector = centerVector.GetNormal();
                var lightDir = endIsMain ? endLightDir: firstLightDir;
                var lightLeftDir = Vector3d.ZAxis.CrossProduct(lightDir);
                var angle = lightDir.GetAngleTo(centerVector);
                angle %= Math.PI;
                angle = angle > Math.PI / 2 ? Math.PI - angle : angle;
                if (angle > Math.PI / 4)
                    mainLightDir = lightLeftDir;
                else
                    mainLightDir = lightDir;

            }
            var lightLeft = Vector3d.ZAxis.CrossProduct(mainLightDir);
            var lineVector = firstLine.EndPoint - firstLine.StartPoint;
            var dotDir = lineVector.DotProduct(mainLightDir);
            var dotLeftDir = lineVector.DotProduct(lightLeft);
            bool isChange = false;
            var min = Math.Min(Math.Abs(dotDir),Math.Abs(dotLeftDir));
            isChange = min > _changeLinePrjLength;
            if (!isChange && min > _changeLineMinLength)
            {
                //进一步判断是否需要转换
                double minAngle = double.MaxValue;
                if (endIsMain && endPointLines.Count > 0)
                {
                    for (int i = 0; i < endPointLines.Count; i++) 
                    {
                        var tempSLine = endPointLines[i];
                        var startDir = (tempSLine.EndPoint - tempSLine.StartPoint).GetNormal();
                        for (int j = i + 1; j < endPointLines.Count; j++) 
                        {
                            var tempELine = endPointLines[j];
                            var endDir = (tempELine.EndPoint - tempELine.StartPoint).GetNormal();
                            var angle = startDir.GetAngleTo(endDir);
                            if (angle < minAngle)
                                minAngle = angle;
                        }
                    }
                }
                else if (!endIsMain && startPointLines.Count > 0) 
                {
                    for (int i = 0; i < startPointLines.Count; i++)
                    {
                        var tempSLine = startPointLines[i];
                        var startDir = (tempSLine.EndPoint - tempSLine.StartPoint).GetNormal();
                        for (int j = i + 1; j < startPointLines.Count; j++)
                        {
                            var tempELine = startPointLines[j];
                            var endDir = (tempELine.EndPoint - tempELine.StartPoint).GetNormal();
                            var angle = startDir.GetAngleTo(endDir);
                            if (angle < minAngle)
                                minAngle = angle;
                        }
                    }
                }
                isChange = minAngle < Math.PI * 80.0 / 180.0;
            }
            if (isChange) 
            {
                var mainDir = Math.Abs(dotLeftDir) > Math.Abs(dotDir) ? mainLightDir : lightLeft;
                if (haveMainDir)
                {
                    mainDir = mainLightDir;
                    if (endIsMain)
                        firstLine = new Line(firstLine.EndPoint, firstLine.StartPoint);
                }
                else
                {
                    var lightDotLight = firstLightDir.DotProduct(endLightDir);
                    if (Math.Abs(lightDirDot) < 0.2)
                    {
                        //认为两个灯垂直，沿着灯的方向进行修正连线
                        mainDir = firstLightDir;
                    }
                }
                for (int i = 0; i < 2; i++)
                {
                    Point3d prjPoint = new Point3d();
                    newLines.Clear();
                    prjPoint = firstLine.StartPoint.PointToFace(firstLine.EndPoint, mainDir);
                    newLines.Add(new Line(firstLine.StartPoint, prjPoint));
                    newLines.Add(new Line(prjPoint, firstLine.EndPoint));
                    bool isCross = false;
                    foreach (var line in newLines)
                    {
                        if (isCross)
                            break;
                        isCross = _lightConnectLight.CrossObstacleLine(line);
                    }
                    if (!isCross)
                    {
                        var newCrossLineCount = CrossLaneLineCount(newLines);
                        isCross = newCrossLineCount > startCrossCount;
                    }
                    if (!isCross)
                    {
                        rmLines.Add(firstLine);
                        break;
                    }
                    mainDir = Vector3d.ZAxis.CrossProduct(mainDir);
                    newLines.Clear();
                }
            }
            if (rmLines.Count < 1)
                return;
            var tempLines = new List<Line>();
            foreach (var line in lightConnect.ConnectLines) 
            {
                if (rmLines.Any(c => (c.StartPoint.DistanceTo(line.StartPoint) < 1 && c.EndPoint.DistanceTo(line.EndPoint) < 1) || (c.StartPoint.DistanceTo(line.EndPoint) < 1 && c.EndPoint.DistanceTo(line.StartPoint) < 1)))
                    continue;
                tempLines.Add(line);
            }
            lightConnect.ConnectLines.Clear();
            tempLines.AddRange(newLines);
            lightConnect.ConnectLines.AddRange(tempLines);
        }
        List<Line> LightAllConnectLines(Point3d lightPoint,bool isConnectPoint) 
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
                var maxGroupPoint = isConnectPoint ? maxGroupLight.ConnectLightPoint : maxGroupLight.NearNodeLightPoint;
                if (maxGroupPoint.DistanceTo(lightPoint) < 10)
                    lightLines.AddRange(maxGroupLight.ConnectLines);
            }
            return lightLines;
        }
        int CrossLaneLineCount(List<Line> connectLines)
        {
            int count = 0;
            if (null == connectLines || connectLines.Count < 1 || _laneLines == null || _laneLines.Count < 1)
                return count;
            foreach (var line in connectLines)
            {
                var lineDir = (line.EndPoint - line.StartPoint).GetNormal();
                
                bool endInOtherLine = false;
                bool startInOtherLine = false;
                foreach (var item in connectLines) 
                {
                    if (item.StartPoint.DistanceTo(line.StartPoint) < 5 && item.EndPoint.DistanceTo(line.EndPoint) < 5)
                        continue;
                    if (item.StartPoint.DistanceTo(line.StartPoint) < 5 || item.EndPoint.DistanceTo(line.StartPoint)<5)
                        startInOtherLine = true;
                    if (item.StartPoint.DistanceTo(line.EndPoint) < 5 || item.EndPoint.DistanceTo(line.EndPoint) < 5)
                        endInOtherLine = true;
                }
                var newSp = line.StartPoint - lineDir.MultiplyBy(startInOtherLine ? _lineExtendToCheck : 0);
                var newEp =line.EndPoint + lineDir.MultiplyBy(endInOtherLine ? _lineExtendToCheck : 0);
                var newCheckLine = new Line(newSp,newEp);
                foreach (var checkLine in _laneLines)
                {
                    if (newCheckLine.LineIsIntersection(checkLine))
                        count += 1;
                }
            }
            return count;
        }
    }
}
