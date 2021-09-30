using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightConnect
{
    class LightGroupConnect
    {
        Polyline _outPolyline;
        List<Polyline> _innerPolylines;
        List<MaxGroupLight> _maxGroupLights;
        LightConnectLight _lightConnectLight;
        double _maxGroupConnectDis = 15000;
        List<Line> _laneLines;
        List<Polyline> _allWalls;
        List<Polyline> _allColumns;
        List<LightBlockReference> _areaLightBlocks;
        public LightGroupConnect(List<MaxGroupLight> laneGroupLights, List<LightBlockReference> areaLights, Polyline outPolyline, List<Polyline> innerPolylines)
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
        public List<MaxGroupLight> CalcGroupConnect()
        {
            _lightConnectLight = new LightConnectLight(_outPolyline, _innerPolylines, _allWalls, _allColumns);
            InitMaxGroup();
            var retGroups = new List<MaxGroupLight>();
            foreach (var maxGroup in _maxGroupLights)
            {
                //先处理可以直接连接的分组
                var hisGroups = CalcGroupConnectFirstStep(maxGroup);
                Point3d nearPoint = new Point3d();
                Point3d currentPoint = new Point3d();
                var targetGroups = new List<LightDirGroup>();
                targetGroups.AddRange(maxGroup.LightGroups);
                targetGroups = targetGroups.Where(c => !hisGroups.Any(x => x.GroupId.Equals(c.GroupId))).ToList();
                while (targetGroups.Count > 0)
                {
                    var currentGroup = hisGroups.Last();
                    var nearGroup = GetNearGroup(targetGroups, hisGroups, out nearPoint, out currentPoint, out currentGroup, true);
                    if (nearGroup == null)
                    {
                        //进入到这里可能会导致连接出现错乱
                        nearGroup = GetNearGroup(targetGroups, hisGroups, out nearPoint, out currentPoint, out currentGroup, false);
                        if(null == nearGroup)
                        {
                            nearGroup = targetGroups.First();
                            nearPoint = LightConnectUtil.GetGroupNearPoint(nearGroup.LightPoints, currentGroup.LightPoints, out currentPoint);
                        }
                    }
                    //检查是否已经和历史的节点有连接
                    string hisGroupId = ConnectHisGroup(nearGroup, hisGroups);
                    bool isConnect = !string.IsNullOrEmpty(hisGroupId);
                    if (null == currentGroup)
                    {
                        targetGroups.Remove(targetGroups.First());
                        continue;
                    }
                    if (!isConnect)
                    {
                        var dir = nearGroup.LineDir;
                        if (null != _areaLightBlocks && _areaLightBlocks.Count > 0)
                        {
                            var light = _areaLightBlocks.Where(c => c.ConnectPoint2.DistanceTo(nearPoint) < 100).FirstOrDefault();
                            if (null != light)
                                dir = light.LightVector;
                        }
                        var connect = CalcLightConnectLines(nearPoint, currentPoint);
                        nearGroup.ConnectParent = connect;
                    }
                    else
                    {
                        //有直连的，不需要计算连接线
                        nearPoint = LightConnectUtil.GetGroupNearPoint(nearGroup.LightPoints, currentGroup.LightPoints, out currentPoint);
                        nearGroup.ConnectParent = new LightConnectLine(nearPoint, currentPoint, new List<Line>());
                    }
                    nearGroup.ParentId = currentGroup.GroupId;
                    nearGroup.ConnectParentPoint = nearPoint;
                    nearGroup.ParentConnectPoint = currentPoint;
                    hisGroups.Add(nearGroup);
                    //检查有和改分组共点的处理为已连接
                    foreach (var item in targetGroups)
                    {
                        if (item.GroupId.Equals(nearGroup.GroupId))
                            continue;
                        if (item.ParentId.Equals(nearGroup.GroupId))
                        {
                            hisGroups.Add(item);
                        }
                        else
                        {
                            var firstPoint = LightConnectUtil.GetGroupNearPoint(item.LightPoints, nearGroup.LightPoints, out Point3d secondPoint);
                            if (firstPoint.DistanceTo(secondPoint) < 10)
                            {
                                item.ParentId = nearGroup.GroupId;
                                item.ConnectParentPoint = firstPoint;
                                item.ParentConnectPoint = firstPoint;
                                hisGroups.Add(item);
                            }
                        }
                    }
                    targetGroups = targetGroups.Where(c => !hisGroups.Any(x => x.GroupId.Equals(c.GroupId))).ToList();
                }
                var newGroup = new MaxGroupLight(maxGroup.NearNodeLightPoint, maxGroup.DistanctToStartPoint);
                newGroup.NearRoutePoint = maxGroup.NearRoutePoint;
                newGroup.LightGroups.AddRange(hisGroups);
                retGroups.Add(newGroup);
            }

            return retGroups;
        }
        List<LightDirGroup> CalcGroupConnectFirstStep(MaxGroupLight maxGroup)
        {
            var startGroup = maxGroup.LightGroups.Where(c => c.ParentId == "0").FirstOrDefault();
            var hisGroups = new List<LightDirGroup>();
            var targetGroups = new List<LightDirGroup>();
            Point3d nearPoint = new Point3d();
            Point3d currentPoint = new Point3d();

            //先处理可以直接连接的分组
            targetGroups.AddRange(maxGroup.LightGroups);
            hisGroups.Add(startGroup);
            var currentGroup = startGroup;
            var notConnectIds = new List<string>();
            notConnectIds.Add(startGroup.GroupId);
            while (true)
            {
                targetGroups = targetGroups.Where(c => !notConnectIds.Any(x => x.Equals(c.GroupId))).ToList();
                if (targetGroups.Count < 1)
                    break;
                var nearGroup = GetNearGroup(currentGroup, targetGroups, out currentPoint, out nearPoint);
                notConnectIds.Add(nearGroup.GroupId);
                if (currentPoint.DistanceTo(nearPoint) > _maxGroupConnectDis)
                    break;
                if (currentPoint.DistanceTo(nearPoint) < 10)
                {
                    //直连
                    nearGroup.ParentId = currentGroup.GroupId;
                    nearGroup.ConnectParentPoint = nearPoint;
                    nearGroup.ParentConnectPoint = currentPoint;
                    nearGroup.ConnectParent = new LightConnectLine(nearPoint, currentPoint, new List<Line>());
                    hisGroups.Add(nearGroup);
                    continue;
                }
                nearPoint = GroupConnectPoint(currentGroup, nearGroup, out currentPoint);
                if (currentPoint.DistanceTo(nearPoint) > _maxGroupConnectDis)
                    continue;
                if (CrossLaneLineCount(new List<Line> { new Line(nearPoint, currentPoint) }) > 0)
                    continue;
                var unCheckIds = new List<string>();
                unCheckIds.Add(startGroup.GroupId);
                unCheckIds.Add(nearGroup.GroupId);
                if (CheckCrossGroup(nearPoint, currentPoint, null, maxGroup.LightGroups))
                {
                    continue;
                }
                var dir = nearGroup.LineDir;
                if (null != _areaLightBlocks && _areaLightBlocks.Count > 0)
                {
                    var light = _areaLightBlocks.Where(c => c.ConnectPoint2.DistanceTo(nearPoint) < 100).FirstOrDefault();
                    if (null != light)
                        dir = light.LightVector;
                }
                var connect = CalcLightConnectLines(nearPoint, currentPoint);
                var connectLines = connect.ConnectLines;
                if (CrossLaneLineCount(connectLines) > 1)
                    continue;
                nearGroup.ParentId = currentGroup.GroupId;
                nearGroup.ConnectParentPoint = nearPoint;
                nearGroup.ParentConnectPoint = currentPoint;
                nearGroup.ConnectParent = connect;
                hisGroups.Add(nearGroup);
            }
            return hisGroups;
        }
        LightDirGroup GetNearGroup(LightDirGroup group, List<LightDirGroup> targetGroups, out Point3d groupPoint, out Point3d nearGroupPoint)
        {
            double nearDis = double.MaxValue;
            LightDirGroup nearGroup = null;
            groupPoint = new Point3d();
            nearGroupPoint = new Point3d();
            foreach (var checkGroup in targetGroups)
            {
                var firstPoint = GroupConnectPoint(group, checkGroup, out Point3d secondPoint);
                var dis = firstPoint.DistanceTo(secondPoint);
                if (dis < nearDis)
                {
                    nearGroup = checkGroup;
                    groupPoint = firstPoint;
                    nearGroupPoint = secondPoint;
                    nearDis = dis;
                }
            }
            return nearGroup;
        }
        LightDirGroup GetNearGroup(List<LightDirGroup> targetGroups, List<LightDirGroup> hisGroups, out Point3d groupPoint, out Point3d hisGroupPoint, out LightDirGroup hisGroup, bool checkCross)
        {
            hisGroup = hisGroups.Last();
            LightDirGroup nearGroup = null;
            double nearDis = double.MaxValue;
            groupPoint = new Point3d();
            hisGroupPoint = new Point3d();
            List<LightDirGroup> allDirGroups = new List<LightDirGroup>();
            allDirGroups.AddRange(targetGroups);
            allDirGroups.AddRange(hisGroups);
            foreach (var group in targetGroups)
            {
                //根据当前接入点，获取下一个分组的接入点
                var connectInfos = GetHisGroupConnect(group, hisGroups, allDirGroups, checkCross);
                if (connectInfos == null || connectInfos.Count < 1)
                    continue;
                int minCross = connectInfos.Min(c => c.CorssLineCount);
                var minCrossConnects = connectInfos.Where(c => c.CorssLineCount == minCross).ToList().OrderBy(c => c.DistanceToEnd).ToList();
                var currentConnect = minCrossConnects.First();
                if (minCross < 1)
                {
                    var count1Connects = connectInfos.Where(c => c.CorssLineCount == 1).ToList().OrderBy(c => c.DistanceToEnd).ToList();
                    if (count1Connects.Count > 0)
                    {
                        double dis = minCrossConnects.First().DistanceToEnd - count1Connects.First().DistanceToEnd;
                        if (dis > 5000)
                            currentConnect = count1Connects.First();
                    }
                    else if (minCrossConnects.Count > 0)
                    {
                        //多个时判断用那个
                        for (int i = 1; i < minCrossConnects.Count; i++)
                        {
                            var temp = minCrossConnects[i];
                            if (Math.Abs(temp.DistanceToEnd - currentConnect.DistanceToEnd) > 1000)
                                continue;
                            var curNearDis = currentConnect.FirstPoint.DistanceTo(currentConnect.SecondPoint);
                            var tempNearDis = temp.FirstPoint.DistanceTo(temp.SecondPoint);
                            if (tempNearDis < curNearDis)
                                currentConnect = minCrossConnects[i];

                        }
                    }
                }
                else if(minCrossConnects.Count>1)
                {
                    //获取方向和最短方向差不多，且接近平行的
                    var connectDir =(currentConnect.SecondPoint - currentConnect.FirstPoint).GetNormal();
                    for (int i = 1; i < minCrossConnects.Count; i++) 
                    {
                        var tempConnect = minCrossConnects[i];
                        var tempDir = (tempConnect.SecondPoint - tempConnect.FirstPoint).GetNormal();
                        if (tempDir.DotProduct(connectDir) < 0)
                            continue;
                        var grouDirDot = tempDir.DotProduct(group.LineDir);
                        if (Math.Abs(grouDirDot) > 0.9 || Math.Abs(grouDirDot) < 0.1)
                        {
                            //有连接垂直或平行的
                            currentConnect = tempConnect;
                            break;
                        }
                    }
                }
                var thisDis = currentConnect.DistanceToEnd;
                if (thisDis > nearDis)
                    continue;
                nearDis = thisDis;
                groupPoint = currentConnect.FirstPoint;
                hisGroupPoint = currentConnect.SecondPoint;
                nearGroup = group;
                hisGroup = hisGroups.Where(c => c.GroupId.Equals(currentConnect.SecondGroupId)).FirstOrDefault();
            }
            return nearGroup;
        }
        List<GroupConnect> GetHisGroupConnect(LightDirGroup group, List<LightDirGroup> lightDirGroups, List<LightDirGroup> allDirLightGroups, bool checkCross)
        {
            var hisGroup = new List<GroupConnect>();
            var connectIds = new List<string>();
            foreach (var tempGroup in allDirLightGroups)
            {
                var tempFirst = LightConnectUtil.GetGroupNearPoint(group.LightPoints, tempGroup.LightPoints, out Point3d tempScond);
                if (tempFirst.DistanceTo(tempScond) < 10)
                    connectIds.Add(tempGroup.GroupId);
            }
            var unCheckIds = new List<string>();
            foreach (var targetGroup in lightDirGroups)
            {
                var firstPoint = LightConnectUtil.GetGroupNearPoint(group.LightPoints, targetGroup.LightPoints, out Point3d secondPoint);
                if (firstPoint.DistanceTo(secondPoint) < 10 || firstPoint.DistanceTo(secondPoint) > _maxGroupConnectDis)
                    continue;
                firstPoint = GroupConnectPoint(targetGroup, group, out secondPoint);
                //这里不在考虑已有直连的组
                var targetConnectIds = new List<string>();
                foreach (var tempGroup in allDirLightGroups)
                {
                    var tempFirst = LightConnectUtil.GetGroupNearPoint(targetGroup.LightPoints, tempGroup.LightPoints, out Point3d tempScond);
                    if (tempFirst.DistanceTo(tempScond) < 10)
                        targetConnectIds.Add(tempGroup.GroupId);
                }
                unCheckIds.Clear();
                unCheckIds.AddRange(connectIds);
                unCheckIds.AddRange(targetConnectIds);
                if (checkCross && CheckCrossGroup(firstPoint, secondPoint, unCheckIds, allDirLightGroups))
                    continue;
                var dis = ConnectPointDistanceToStart(targetGroup, secondPoint, lightDirGroups);
                var dir = group.LineDir;
                if (null != _areaLightBlocks && _areaLightBlocks.Count > 0)
                {
                    var light = _areaLightBlocks.Where(c => c.ConnectPoint2.DistanceTo(firstPoint) < 100).FirstOrDefault();
                    if (null != light)
                        dir = light.LightVector;
                }
                var lightConnect = CalcLightConnectLines(firstPoint, secondPoint);
                var connectLines = lightConnect.ConnectLines;
                dis += connectLines.Sum(c => c.Length);
                int count = CrossLaneLineCount(connectLines);
                var connect = new GroupConnect(group.GroupId, targetGroup.GroupId, firstPoint, secondPoint, count, dis);
                connect.ConnectLines.AddRange(connectLines);
                hisGroup.Add(connect);
                unCheckIds.Clear();
            }
            return hisGroup;
        }
        double ConnectPointDistanceToStart(LightDirGroup startGroup, Point3d checkPoint, List<LightDirGroup> hisLightDirGroups)
        {
            //这里就不计算精确值
            var currentGroup = startGroup;
            var dis = currentGroup.ConnectParentPoint.DistanceTo(checkPoint);
            string parentId = currentGroup.ParentId;
            while (!string.IsNullOrEmpty(parentId) && parentId != "0")
            {
                dis += currentGroup.ConnectParent != null ? currentGroup.ConnectParent.ConnectLines.Sum(c => c.Length) : 0;
                var parentGroup = hisLightDirGroups.Where(c => c.GroupId.Equals(parentId)).FirstOrDefault();
                if (null == parentGroup)
                    break;
                dis += parentGroup.PointDisToConnectPoint(currentGroup.ParentConnectPoint);
                parentId = parentGroup.ParentId;
                currentGroup = parentGroup;
            }
            return dis;
        }
        string ConnectHisGroup(LightDirGroup group, List<LightDirGroup> lightDirGroups)
        {
            if (group == null)
                return "";
            foreach (var targetGroup in lightDirGroups)
            {
                var firstPoint = LightConnectUtil.GetGroupNearPoint(group.LightPoints, targetGroup.LightPoints, out Point3d secondPoint);
                if (firstPoint.DistanceTo(secondPoint) < 10)
                    return targetGroup.GroupId;
            }
            return "";
        }
        void InitMaxGroup()
        {
            foreach (var maxGroup in _maxGroupLights)
            {
                var point = maxGroup.NearNodeLightPoint;
                foreach (var group in maxGroup.LightGroups)
                {
                    if (group == null)
                        continue;
                    string pid = group.NearGroupPoint.DistanceTo(point) < 10 ? "0" : "";
                    group.ParentId = pid;
                    group.ParentConnectPoint = new Point3d();
                    group.ConnectParentPoint = string.IsNullOrEmpty(pid) ? new Point3d() : point;
                    group.LightConnectLines.Clear();
                    //这里先将一个小分组内的连线连接好
                    var orderPoints = ThPointVectorUtil.PointsOrderByDirection(group.LightPoints, group.LineDir, false).ToList();
                    for (int i = 0; i < orderPoints.Count - 1; i++)
                    {
                        var startPoint = orderPoints[i];
                        var endPoint = orderPoints[i + 1];
                        group.LightConnectLines.Add(CalcLightConnectLines(startPoint, endPoint));
                    }
                }
            }
        }
        List<Line> CalcGroupConnectLines(Point3d startPoint, Point3d endPoint, Vector3d xDir)
        {
            var lines = _lightConnectLight.PointConnectLines(startPoint, endPoint, xDir);
            return lines;
        }
        LightConnectLine CalcLightConnectLines(Point3d startPoint, Point3d endPoint)
        {
            LightConnectLine lightConnect = null;
            var startLight = _areaLightBlocks.Where(c => c.ConnectPoint2.DistanceTo(startPoint) < 100).FirstOrDefault();
            var endLight = _areaLightBlocks.Where(c => c.ConnectPoint2.DistanceTo(endPoint) < 100).FirstOrDefault();
            var connectDir = (endLight.ConnectPoint2 - startLight.ConnectPoint2).GetNormal();

            var startDir = startLight.LightVector;
            var endDir = endLight.LightVector;
            var dotDir = startDir.DotProduct(endDir);
            var startPoints = new List<Point3d>
            {
                startLight.ConnectPoint1,
                startLight.ConnectPoint2,
                startLight.ConnectPoint3
            };
            var endPoints = new List<Point3d>
            {
                endLight.ConnectPoint1,
                endLight.ConnectPoint2,
                endLight.ConnectPoint3
            };
            if (Math.Abs(dotDir) > 0.7)
            {
                //起点终点等方向和灯接近平行
                var dotConnect = connectDir.DotProduct(startDir);
                if (Math.Abs(dotConnect) > 0.7)
                {
                    //连接方向和灯的方向接近平行
                    var firstPoint = LightConnectUtil.NearPoint(startPoints, endPoints, out Point3d secondPoint);
                    if (_lightConnectLight.CrossObstacleLine(firstPoint, secondPoint))
                    {
                        firstPoint = startPoint;
                        secondPoint = endPoint;
                    }
                    var lines = CalcGroupConnectLines(firstPoint, secondPoint, startDir);
                    lightConnect = new LightConnectLine(startPoint, endPoint, lines);
                    lightConnect.StartLightConnectPoint = firstPoint;
                    lightConnect.EndLightConnectPoint = secondPoint;
                }
                else if (Math.Abs(dotConnect) > 0.3)
                {
                    lightConnect = LightGetConnect(startLight, endLight, true);
                }
                else
                {
                    //灯方向和连接方向接近垂直
                    var dirDot = Math.Abs((endPoint - startPoint).DotProduct(startDir));
                    if (dirDot < 20)
                    {
                        //灯方向和连接方向接近垂直，灯位置接近，根据点进行判断
                        var firstPoint = startPoint;
                        var secondPoint = endPoint;
                        if (_lightConnectLight.CrossObstacleLine(firstPoint, secondPoint))
                        {
                            //先判断中间点
                            firstPoint = startLight.ConnectPoint1;
                            secondPoint = endPoints.OrderBy(c => c.DistanceTo(firstPoint)).First();
                            if (_lightConnectLight.CrossObstacleLine(firstPoint, secondPoint))
                            {
                                firstPoint = startLight.ConnectPoint3;
                                secondPoint = endPoints.OrderBy(c => c.DistanceTo(firstPoint)).First();
                                if (_lightConnectLight.CrossObstacleLine(firstPoint, secondPoint))
                                {
                                    //都不符合
                                    firstPoint = startPoint;
                                    secondPoint = endPoint;
                                }
                            }
                        }
                        var lines = CalcGroupConnectLines(firstPoint, secondPoint, startDir);
                        lightConnect = new LightConnectLine(startPoint, endPoint, lines);
                        lightConnect.StartLightConnectPoint = firstPoint;
                        lightConnect.EndLightConnectPoint = secondPoint;
                    }
                    else
                    {
                        lightConnect = LightGetConnect(startLight, endLight, false);
                    }
                }
            }
            else
            {
                //两个灯的方向不一致
                var firstPoint = LightConnectUtil.GetGroupNearPoint(startPoints, endPoints, out Point3d secondPoint);
                if (_lightConnectLight.CrossObstacleLine(firstPoint, secondPoint))
                {
                    firstPoint = startPoint;
                    secondPoint = endPoint;
                }
                var lines = CalcGroupConnectLines(firstPoint, secondPoint, startDir);
                lightConnect = new LightConnectLine(startPoint, endPoint, lines);
                lightConnect.StartLightConnectPoint = firstPoint;
                lightConnect.EndLightConnectPoint = secondPoint;
            }
            return lightConnect;
        }
        LightConnectLine LightGetConnect(LightBlockReference startLight, LightBlockReference endLight, bool isNearPoint)
        {
            var startDir = startLight.LightVector;
            var startPoint = startLight.ConnectPoint2;
            var endPoint = endLight.ConnectPoint2;
            var startPoints = new List<Point3d>
            {
                startLight.ConnectPoint1,
                startLight.ConnectPoint2,
                startLight.ConnectPoint3
            };
            var endPoints = new List<Point3d>
            {
                endLight.ConnectPoint1,
                endLight.ConnectPoint2,
                endLight.ConnectPoint3
            };
            LightConnectLine lightConnect = null;
            //先判断中间点，在判断其余两个点
            var firstPoint = startLight.ConnectPoint2;
            var secondPoint = isNearPoint ? endPoints.OrderBy(c => c.DistanceTo(firstPoint)).First() : endLight.ConnectPoint2;
            if (_lightConnectLight.CrossObstacleLine(firstPoint, secondPoint))
            {
                //不能直连，进一步判断另外两个点
                var sPoint = startLight.ConnectPoint1;
                var ePoint = endPoints.OrderBy(c => c.DistanceTo(sPoint)).First();
                if (_lightConnectLight.CrossObstacleLine(sPoint, endPoint))
                {
                    var eSPoint = startLight.ConnectPoint2;
                    var eEPoint = endPoints.OrderBy(c => c.DistanceTo(eSPoint)).First();
                    if (_lightConnectLight.CrossObstacleLine(eSPoint, eEPoint))
                    {
                        //没有满足的直接使用中心点的连线
                        var midLines = CalcGroupConnectLines(startPoint, endPoint, startDir);
                        lightConnect = new LightConnectLine(startPoint, endPoint, midLines);
                        lightConnect.StartLightConnectPoint = startPoint;
                        lightConnect.EndLightConnectPoint = endPoint;
                    }
                    else
                    {
                        lightConnect = new LightConnectLine(startPoint, endPoint, new List<Line> { new Line(eSPoint, eEPoint) });
                        lightConnect.StartLightConnectPoint = eSPoint;
                        lightConnect.EndLightConnectPoint = eEPoint;
                    }
                }
                else
                {
                    lightConnect = new LightConnectLine(startPoint, endPoint, new List<Line> { new Line(sPoint, ePoint) });
                    lightConnect.StartLightConnectPoint = sPoint;
                    lightConnect.EndLightConnectPoint = ePoint;
                }
            }
            else
            {
                lightConnect = new LightConnectLine(startPoint, endPoint, new List<Line> { new Line(firstPoint, secondPoint) });
                lightConnect.StartLightConnectPoint = firstPoint;
                lightConnect.EndLightConnectPoint = secondPoint;
            }
            return lightConnect;
        }
        bool CheckCrossGroup(Point3d startPoint, Point3d endPoint, List<string> unCheckIds, List<LightDirGroup> checkGroups)
        {
            var dir = (endPoint - startPoint).GetNormal();
            var line = new Line(startPoint, endPoint);
            bool isCross = false;
            foreach (var group in checkGroups)
            {
                if (isCross)
                    break;
                if (null == group || (unCheckIds != null && unCheckIds.Any(c => c.Equals(group.GroupId))))
                    continue;
                for (int i = 0; i < group.LightPoints.Count; i++)
                {
                    if (isCross)
                        break;
                    var point = group.LightPoints[i];
                    if (point.PointInLineSegment(line, 100))
                    {
                        isCross = true;
                        break;
                    }
                    for (int j = i + 1; j < group.LightPoints.Count; j++)
                    {
                        if (isCross)
                            break;
                        var checkLine = new Line(point, group.LightPoints[j]);
                        if (LightConnectUtil.GroupDirIsParallel(dir, checkLine.LineDirection().GetNormal(), 15))
                            continue;
                        isCross = checkLine.LineIsIntersection(line);
                    }
                }
            }
            return isCross;
        }
        Point3d GroupConnectPoint(LightDirGroup hisGroup, LightDirGroup newGroup, out Point3d hisGroupoint)
        {
            bool isParallel = LightConnectUtil.GroupDirIsParallel(hisGroup.LineDir, newGroup.LineDir, 10);
            var hisGroupBasePoint = hisGroup.ConnectParentPoint;
            var nearPoint = NearConnectPoint(hisGroup, newGroup, isParallel);
            if (hisGroup.LightPoints.Count < 2)
                hisGroupoint = hisGroup.LightPoints.First();
            else
            {
                var orderPoints = hisGroup.LightPoints.OrderBy(c => c.DistanceTo(nearPoint)).ToList();
                var first = orderPoints[0];
                var second = orderPoints[1];
                hisGroupoint = first;
                if (isParallel)
                {
                    var nearDir = (first - nearPoint).GetNormal();
                    if (Math.Abs(nearDir.DotProduct(hisGroup.LineDir)) > 0.5)
                        hisGroupoint = first;
                    else if (first.DistanceTo(hisGroupBasePoint) < 10)
                        hisGroupoint = first;
                    else if (second.DistanceTo(hisGroupBasePoint) < 10)
                        hisGroupoint = second;
                    else if (first.DistanceTo(hisGroupBasePoint) > second.DistanceTo(hisGroupBasePoint))
                        hisGroupoint = second;
                }
            }
            if (hisGroup.LightPoints.Count < 2 || newGroup.LightPoints.Count < 2) 
            {
                if (!HaveCanConnectLine(hisGroup.LightPoints, newGroup.LightPoints))
                    nearPoint = CorrectGroupConnectPoint(hisGroup, newGroup, nearPoint, ref hisGroupoint);
               
                newGroup.ConnectParentPoint = nearPoint;
                return nearPoint;
            }
            var firstDir = newGroup.LineDir;
            var secondDir = hisGroup.LineDir;
            var angle = firstDir.GetAngleTo(secondDir);
            angle = angle > Math.PI / 2.0 ? Math.PI - angle : angle;
            var connectDir = (hisGroupoint - nearPoint).GetNormal();
            if (angle < Math.PI * 10.0 / 180.0 && Math.Abs(connectDir.DotProduct(firstDir))<0.8)
            {
                //如果两个分组方向平行，且连接方向和本身接近平行时，获取的两个点可能不合适，
                //有些计算出来的数据可能会穿框线
                var tempLine = new Line(nearPoint, hisGroupoint);
                if (_lightConnectLight.IsCrossOutPolyline(tempLine) ||  _lightConnectLight.IsCrossInnerPolyline(tempLine))
                {
                    var tempPoints = new List<Point3d>();
                    tempPoints.AddRange(newGroup.LightPoints);
                    tempPoints = tempPoints.OrderBy(c => c.DistanceTo(hisGroupBasePoint)).ToList();
                    //后面跑A*后会和其它点有重复，交叉问题，这种要每个点和其它点跑A*搜索，进行修正最后两个点
                    var dir = (nearPoint - hisGroupoint).GetNormal();
                    Point3d tempFirst = new Point3d();
                    Point3d tempSecond = new Point3d();
                    for (int i = 0; i < newGroup.LightPoints.Count; i++)
                    {
                        var point1 = newGroup.LightPoints[i];
                        var point2 = hisGroup.LightPoints.OrderBy(c => c.DistanceTo(point1)).First();
                        tempLine = new Line(point1, point2);
                        if (_lightConnectLight.IsCrossOutPolyline(tempLine) || _lightConnectLight.IsCrossInnerPolyline(tempLine))
                            continue;
                        tempFirst = point1;
                        tempSecond = point2;
                        break;
                    }
                    if (tempFirst.DistanceTo(tempSecond) > 10)
                    {
                        nearPoint = tempFirst;
                        hisGroupoint = tempSecond;
                    }
                    else if (newGroup.LightPoints.Count > 1 && hisGroup.LightPoints.Count > 1)
                    {
                        nearPoint = CorrectGroupConnectPoint(hisGroup, newGroup, nearPoint, ref hisGroupoint);
                        
                    }
                }
            }
            newGroup.ConnectParentPoint = nearPoint;
            return nearPoint;
        }
        Point3d NearConnectPoint(LightDirGroup connectGroup, LightDirGroup newGroup, bool isParall)
        {
            var hisGroupBasePoint = connectGroup.ConnectParentPoint;
            var orderPoints = newGroup.LightPoints.OrderBy(c => c.DistanceTo(hisGroupBasePoint)).ToList();
            //判断最近的两个点距离，在一定范围内，选择靠中间的点
            if (orderPoints.Count < 2)
                return orderPoints.FirstOrDefault();
            if (!isParall)
                return orderPoints.FirstOrDefault();
            var first = orderPoints[0];
            var second = orderPoints[1];
            var firstDis = first.DistanceTo(hisGroupBasePoint);
            var secondDis = second.DistanceTo(hisGroupBasePoint);
            var nearDir = (first - hisGroupBasePoint).GetNormal();
            if (Math.Abs(nearDir.DotProduct(connectGroup.LineDir)) < 0.1)
            {
                return first;
            }
            else if (first.DistanceTo(hisGroupBasePoint) < 10)
            {
                return first;
            }
            else if (second.DistanceTo(hisGroupBasePoint) < 10)
            {
                return second;
            }
            else if (Math.Abs(firstDis - secondDis) < 1000)
            {
                //判断那个接近中间
                int mid = newGroup.LightPoints.Count / 2;
                var midPoint = newGroup.LightPoints[mid];
                if (first.DistanceTo(midPoint) < second.DistanceTo(midPoint))
                    return first;
                return second;
            }
            else
            {
                var hisGroupDir = connectGroup.LineDir;
                var newGroupDir = newGroup.LineDir;
                var firstDir = (first - hisGroupBasePoint).GetNormal();
                var secondDir = (second - hisGroupBasePoint).GetNormal();
                var firstAngle = firstDir.GetAngleTo(hisGroupDir) % Math.PI;
                firstAngle = firstAngle > Math.PI / 2 ? Math.PI - firstAngle : firstAngle;
                var secondAngle = secondDir.GetAngleTo(hisGroupDir) % Math.PI;
                secondAngle = secondAngle > Math.PI / 2 ? Math.PI - secondAngle : secondAngle;
                if (Math.Abs(firstAngle - secondAngle) < 5)
                    return orderPoints.FirstOrDefault();
                if (Math.Abs(hisGroupDir.DotProduct(newGroupDir)) > 0.7)
                {
                    //两个平行，获取和方向角度大的灯
                    if (firstAngle >= secondAngle)
                        return first;
                    return second;
                }
                else
                {
                    //接近垂直，获取和方向角度小的灯
                    if (firstAngle <= secondAngle)
                        return first;
                    return second;
                }
            }
        }
        Point3d PointChangeToNext(Point3d basePoint,List<Point3d> checkPoints,List<Line> checkLines,out bool isChange) 
        {
            isChange = false;
            Point3d tempPoint = basePoint;
            foreach (var point in checkPoints)
            {
                if (point.DistanceTo(tempPoint) < 10)
                    continue;
                foreach (var line in checkLines)
                {
                    if (isChange)
                        break;
                    var prjPoint = point.PointToLine(line);
                    if (!prjPoint.PointInLineSegment(line, 100))
                        continue;
                    if (prjPoint.DistanceTo(point) < 200)
                        continue;
                    if (_lightConnectLight.IsCrossOutPolyline(new Line(point, prjPoint)))
                        continue;
                    isChange = true;
                }
                if (isChange)
                {
                    tempPoint = point;
                    break;
                }
            }
            return tempPoint;
        }
        int CrossLaneLineCount(List<Line> connectLines)
        {
            int count = 0;
            if (null == connectLines || connectLines.Count < 1 || _laneLines == null || _laneLines.Count < 1)
                return count;
            foreach (var line in connectLines)
            {
                foreach (var checkLine in _laneLines)
                {
                    if (line.LineIsIntersection(checkLine))
                        count += 1;
                }
            }
            return count;
        }

        Point3d CorrectGroupConnectPoint(LightDirGroup hisGroup, LightDirGroup newGroup,Point3d nearPoint,ref Point3d hisGroupoint) 
        {
            //没有可以直接连接的需要对分组中的每个点进行跑A星进行修正
            var tempNewGroupPoint = nearPoint;
            var tempHisGroupPoint = hisGroupoint;
            int maxCount = Math.Max(newGroup.LightPoints.Count, hisGroup.LightPoints.Count);//防止死循环
            bool newIsChange = true;
            bool hisIsChange = true;
            while ((newIsChange || hisIsChange) && maxCount > 0)
            {
                var lightConnect = CalcLightConnectLines(tempNewGroupPoint, tempHisGroupPoint);
                var lines = lightConnect.ConnectLines;
                var thisDirLines = new List<Line>();
                foreach (var line in lines)
                {
                    var tempAngle = line.CurveDirection().GetAngleTo(newGroup.LineDir);
                    tempAngle = tempAngle > Math.PI / 2.0 ? Math.PI - tempAngle : tempAngle;
                    if (tempAngle > Math.PI * 10 / 180.0)
                        continue;
                    thisDirLines.Add(line);
                }
                if (thisDirLines.Count < 1)
                    break;
                newIsChange = false;
                hisIsChange = false;
                tempNewGroupPoint = PointChangeToNext(tempNewGroupPoint, newGroup.LightPoints, thisDirLines, out newIsChange);
                tempHisGroupPoint = PointChangeToNext(tempHisGroupPoint, hisGroup.LightPoints, thisDirLines, out hisIsChange);
                maxCount -= 1;
            }
            if (tempNewGroupPoint.DistanceTo(tempHisGroupPoint) > 10)
            {
                nearPoint = tempNewGroupPoint;
                hisGroupoint = tempHisGroupPoint;
            }
            return nearPoint;
            
        }
        bool HaveCanConnectLine(List<Point3d> groupPoints,List<Point3d> checkGroupPoints) 
        {
            bool isCross = true;
            for (int i = 0; i < groupPoints.Count; i++)
            {
                var point1 = groupPoints[i];
                var point2 = checkGroupPoints.OrderBy(c => c.DistanceTo(point1)).First();
                var tempLine = new Line(point1, point2);
                if (_lightConnectLight.IsCrossOutPolyline(tempLine) || _lightConnectLight.IsCrossInnerPolyline(tempLine))
                    continue;
                isCross = false;
                break;
            }
            return !isCross;
        }
    }
    class GroupConnect
    {
        public string FirstGroupId { get; }
        public string SecondGroupId { get; }
        public Point3d FirstPoint { get; }
        public Point3d SecondPoint { get; }
        public List<Line> ConnectLines { get; }
        public int CorssLineCount { get; }
        public double DistanceToEnd { get; }
        public GroupConnect(string firstGroupId, string secondGroupId, Point3d firstPoint, Point3d secondPoint, int count, double disToEnd)
        {
            this.FirstGroupId = firstGroupId;
            this.SecondGroupId = secondGroupId;
            this.FirstPoint = firstPoint;
            this.SecondPoint = secondPoint;
            this.ConnectLines = new List<Line>();
            this.CorssLineCount = count;
            this.DistanceToEnd = disToEnd;
        }
    }
}
