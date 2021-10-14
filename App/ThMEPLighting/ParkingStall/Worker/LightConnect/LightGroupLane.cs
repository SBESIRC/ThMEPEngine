using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightConnect
{
    class LightGroupLane
    {
        Polyline _outPolyline;
        List<Polyline> _innerPolylines;
        List<Line> _innerLines;
        List<LightGroup> _lightGroups;
        List<List<LightGroup>> _groupLightGroups;
        List<Line> _laneLines;
        int _maxCount = 25;
        double _connectGroupMaxDis = 15000;
        public LightGroupLane(Polyline outPolyline,List<Polyline> innerPolylines,List<LightGroup> lightGroups, List<Line> laneLines,int count) 
        {
            _outPolyline = outPolyline;
            _innerPolylines = new List<Polyline>();
            _innerLines = new List<Line>();
            _groupLightGroups = new List<List<LightGroup>>();
            _maxCount = count;
            
            if (null != innerPolylines && innerPolylines.Count > 0)
            {
                foreach (var item in innerPolylines)
                {
                    if (null == item || item.Area < 100)
                        continue;
                    _innerPolylines.Add(item);
                }
                var liens = ThMEPLineExtension.ExplodeCurves(innerPolylines.ToCollection()).Where(c => c is Line).Cast<Line>().ToList();
                _innerLines.AddRange(liens);
            }

            _laneLines = new List<Line>();
            if (null != laneLines && laneLines.Count > 0)
                laneLines.ForEach(c => _laneLines.Add(c));

            _lightGroups = new List<LightGroup>();
            if (null != lightGroups && lightGroups.Count > 0)
                lightGroups.ForEach(c => _lightGroups.Add(c));
            _lightGroups = _lightGroups.OrderBy(c => c.GroupCount).ToList();
            foreach (var item in _lightGroups)
            {
                _groupLightGroups.Add(new List<LightGroup> { item });
            }
        }
        public List<List<LightGroup>> FirstStepGroup()
        {
            var allLines = new List<Line>();
            _laneLines.ForEach(c => allLines.Add(c));
            allLines.AddRange(_innerLines);
            var liens = ThMEPLineExtension.ExplodeCurves((new List<Polyline> { _outPolyline }).ToCollection()).Where(c => c is Line).Cast<Line>().ToList();
            allLines.AddRange(liens);
            //先将不穿任何线的点聚在一起
            var retLightGroups = ConnectFirstNotCrossLines(_groupLightGroups, allLines,false,0);
            retLightGroups = retLightGroups.OrderBy(c => c.Sum(x => x.GroupCount)).ToList();
            //再将不穿车道线的点聚在一起，且中心点不穿框线
            retLightGroups = ConnectFirstNotCrossLines(retLightGroups, _laneLines, false,0);
            //再将最近点不穿任何车道线，且点数少的连接
            allLines.Clear();
            _laneLines.ForEach(c => allLines.Add(c));
            retLightGroups = ConnectFirstNotCrossLines(retLightGroups, allLines, true,2);
            return retLightGroups;
        }
        List<List<LightGroup>> ConnectFirstNotCrossLines(List<List<LightGroup>> targetGroups,List<Line> notCrossLines, bool isDirectConn,int minCount) 
        {
            var retLightGroups = new List<List<LightGroup>>();
            var newLightGroup =new List<List<LightGroup>>();
            targetGroups.ForEach(c => newLightGroup.Add(c));
            while (newLightGroup.Count > 0)
            {
                newLightGroup = newLightGroup.OrderBy(c => c.Sum(x => x.GroupCount)).ToList();
                var firstGroup = newLightGroup[0];
                var firstPoints = new List<Point3d>();
                var firstList = new List<LightGroup>();
                foreach (var item in firstGroup)
                {
                    firstPoints.AddRange(item.GroupPoints);
                    firstList.Add(item);
                }
                var firstCenterPoint = ThPointVectorUtil.PointsAverageValue(firstPoints);
                bool isAdd = false;
                for (int i = 1; i < newLightGroup.Count; i++)
                {
                    var secondGroup = newLightGroup[i];
                    var secondPoints = new List<Point3d>();
                    var secondList = new List<LightGroup>();
                    foreach (var item in secondGroup)
                    {
                        secondPoints.AddRange(item.GroupPoints);
                        secondList.Add(item);
                    }
                    if ((firstPoints.Count + secondPoints.Count) > _maxCount)
                        continue;
                    var firstNear = LightConnectUtil.GetGroupNearPoint(firstPoints, secondPoints, out Point3d secondNear);
                    var line = new Line(firstNear, secondNear);
                    if (line.Length > _connectGroupMaxDis)
                        continue;
                    var secondCenterPoint = ThPointVectorUtil.PointsAverageValue(secondPoints);
                    if(!isDirectConn)
                        line = new Line(firstCenterPoint, secondCenterPoint);
                    if (notCrossLines.Any(c => c.LineIsIntersection(line)))
                        continue;
                    isAdd = true;
                    
                    foreach (var first in firstGroup)
                    {
                        if (isDirectConn && isAdd && firstPoints.Count <= minCount)
                            break;
                        if (!isAdd)
                            break;
                        foreach (var second in secondGroup)
                        {
                            firstNear = LightConnectUtil.GetGroupNearPoint(first.GroupPoints, second.GroupPoints, out secondNear);
                            line = new Line(firstNear, secondNear);
                            if (notCrossLines.Any(c => c.LineIsIntersection(line)))
                            {
                                isAdd = false;
                                break;
                            }
                        }
                    }
                    if (!isAdd)
                        continue;
                    firstList.AddRange(secondList);
                    newLightGroup.Remove(secondGroup);
                    break;
                }
                newLightGroup.Remove(firstGroup);
                if (isAdd)
                    newLightGroup.Add(firstList);
                else
                    retLightGroups.Add(firstList);
            }
            return retLightGroups;
        }
        public List<List<LightGroup>> SecondStepGroup(List<List<LightGroup>> lightGroups)
        {
            var retLightGroups = new List<List<LightGroup>>();
            var newLightGroup = new List<List<LightGroup>>();
            lightGroups.ForEach(c => newLightGroup.Add(c));
            while (newLightGroup.Count > 0)
            {
                newLightGroup = newLightGroup.OrderBy(c => c.Sum(x => x.GroupCount)).ToList();
                var firstGroup = newLightGroup[0];
                var firstPoints = new List<Point3d>();
                var firstList = new List<LightGroup>();
                foreach (var item in firstGroup)
                {
                    firstPoints.AddRange(item.GroupPoints);
                    firstList.Add(item);
                }
                var firstCenterPoint = ThPointVectorUtil.PointsAverageValue(firstPoints);
                var nearDis = double.MaxValue;
                var centerNearDis = double.MaxValue;
                int centerNearIndex = -1;
                int nearIndex = -1;
                for (int i = 1; i < newLightGroup.Count; i++)
                {
                    var secondGroup = newLightGroup[i];
                    var secondPoints = new List<Point3d>();
                    var secondList = new List<LightGroup>();
                    foreach (var item in secondGroup)
                    {
                        secondPoints.AddRange(item.GroupPoints);
                        secondList.Add(item);
                    }
                    if ((firstPoints.Count + secondPoints.Count) > _maxCount)
                        continue;
                    var secondCenterPoint = ThPointVectorUtil.PointsAverageValue(secondPoints);
                    var firstPoint = LightConnectUtil.GetGroupNearPoint(firstPoints, secondPoints, out Point3d secondNear);
                    var line = new Line(firstPoint, secondNear);
                    if (line.Length > _connectGroupMaxDis)
                        continue;
                    if (line.Length < nearDis) 
                    {
                        nearIndex = i;
                        nearDis = line.Length;
                    }
                    double centerDis = firstCenterPoint.DistanceTo(secondCenterPoint);
                    if (centerDis < centerNearDis)
                    {
                        centerNearIndex = i;
                        centerNearDis = centerDis;
                    }
                }
                int addIndex = -1;
                var mergeList = new List<LightGroup>();
                if (centerNearIndex == nearIndex)
                {
                    if (centerNearIndex != -1) 
                    {
                        //中心点和最近点指向同一个分组
                        addIndex = centerNearIndex;
                    }
                }
                else if (nearIndex != -1 && centerNearIndex != -1)
                {
                    //中心点和最近点指向不同一个分组，判断中心点近和最近点近那个更合适
                    var checkFirst = newLightGroup[nearIndex];
                    var checkSecond = newLightGroup[centerNearIndex];
                    int firstCount = checkFirst.Sum(c => c.GroupCount);
                    int secondCount = checkSecond.Sum(c => c.GroupCount);
                    int minCount = Math.Min(firstCount,secondCount);
                    int firstWeight = 0;
                    int secondWeight = 0;
                    if (minCount < 8)
                    {
                        firstWeight = firstCount < secondCount ? 1 : -1;
                    }
                    else 
                    {
                        firstWeight = firstCount - secondCount;
                        secondWeight = secondCount - firstCount;
                    }
                    addIndex = firstWeight > secondWeight ? nearIndex : centerNearIndex;
                }
                else
                {
                    //中心点或最近点有一个没有
                    addIndex = centerNearIndex == -1 ? nearIndex : centerNearIndex;
                }
                if (addIndex != -1)
                {
                    mergeList = newLightGroup[addIndex];
                    newLightGroup.Remove(mergeList);
                    firstList.AddRange(mergeList);
                }
                newLightGroup.Remove(firstGroup);
                if (mergeList.Count > 0)
                    newLightGroup.Add(firstList);
                else
                    retLightGroups.Add(firstList);
            }
            return retLightGroups;
        }
    }
}
