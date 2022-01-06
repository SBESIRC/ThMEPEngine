using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPHVAC.IndoorFanLayout.Models;

namespace ThMEPHVAC.IndoorFanLayout.Business
{
    class RectAreaLayoutFan : RoomLayoutFanBase
    {
        public RectAreaLayoutFan(Dictionary<string, List<string>> divisionAreaNearIds, Vector3d xAxis, Vector3d yAxis)
            :base(divisionAreaNearIds, xAxis,yAxis)
        {
            _roomIntersectAreas = new List<DivisionRoomArea>();
        }
        public List<DivisionRoomArea> GetRectangle(AreaLayoutGroup layoutGroup, FanRectangle fanRectangle)
        {
            _roomIntersectAreas.Clear();
            if (layoutGroup.IsArcGroup)
                return _roomIntersectAreas;
            _fanRectangle = fanRectangle;
            CalcRoomLoad(layoutGroup);
            if (_roomIntersectAreas.Count < 1)
                return _roomIntersectAreas;
            LayoutFanRectFirstStep(true);
            //排布完成后，进行检查删除逻辑
            CheckAndRemoveLayoutFan();
            //判断是否需要更改方向
            if (_changeLayoutDir) 
            {
                foreach (var areaCell in _roomIntersectAreas) 
                {
                    foreach (var item in areaCell.FanLayoutAreaResult) 
                    {
                        foreach (var fan in item.FanLayoutResult)
                            fan.FanDirection = fan.FanDirection.Negate();
                    }
                }
            }
            while (true)
            {
                bool haveChange = false;
                foreach (var item in _roomIntersectAreas)
                {
                    if (item.FanLayoutAreaResult == null || item.FanLayoutAreaResult.Count < 1)
                        continue;
                    haveChange = CheckAndAlignmentFan(item, _groupYVector.Negate());
                    if (haveChange)
                        break;
                }
                if (!haveChange)
                    break;
            }
            //根据分割排布后的矩形，计算实际排布的矩形大小
            AdjustmentFanRectSize();
            //排布内部的小风口
            LayoutFanVent();
            //根据风口，进行调整对齐，并调整风机长度
            var allGroupIds = _allGroupPoints.Select(c => c.Key).ToList();
            foreach (var groupId in allGroupIds)
            {
                var thisGroupAreaFans = _roomIntersectAreas.Where(c => c.GroupId == groupId).ToList();
                if (thisGroupAreaFans.Count < 1)
                    continue;
                var rowCount = thisGroupAreaFans.Max(c => c.RowCount);
                var changeFanIds = new List<string>();
                for (int i = 0; i < rowCount;)
                {
                    var thisRowFans = new List<FanLayoutRect>();
                    var thisRowVents = new List<FanInnerVentRect>();
                    foreach (var item in thisGroupAreaFans) 
                    {
                        if (item.FanLayoutAreaResult == null || item.FanLayoutAreaResult.Count < 1)
                            continue;
                        var tempFans = item.FanLayoutAreaResult.Where(c => c.RowId == i).ToList();
                        foreach (var fan in tempFans)
                        {
                            thisRowFans.AddRange(fan.FanLayoutResult);
                            thisRowVents.AddRange(fan.FanLayoutResult.SelectMany(c => c.InnerVentRects));
                        }
                    }
                    if (thisRowFans.Count < 2)
                    {
                        i += 1;
                        continue;
                    }
                    thisRowFans = thisRowFans.OrderBy(c => c.InnerVentRects.Count).ToList();
                    string changeId = "";
                    Point3d newPoint = new Point3d();
                    foreach (var item in thisRowFans) 
                    {
                        if (item.InnerVentRects.Count<1 || item.InnerVentRects.Count > 1)
                            continue;
                        var thisVent = item.InnerVentRects.First();
                        var startPoint = item.CenterPoint - item.FanDirection.MultiplyBy(item.Length / 2);
                        var minPoint = startPoint + item.FanDirection.MultiplyBy(_fanRectangle.VentRect.VentMinDistanceToStart);
                        var maxPoint = startPoint + item.FanDirection.MultiplyBy(item.Length) - item.FanDirection.MultiplyBy(_fanRectangle.VentRect.VentMinDistanceToEnd);
                        var otherFanVents = thisRowVents.Where(c => c.VentId != thisVent.VentId).ToList();
                        
                        foreach (var vent in otherFanVents) 
                        {
                            var prjPoint = vent.CenterPoint.PointToFace(item.CenterPoint, _groupXVector);
                            if (prjPoint.DistanceTo(thisVent.CenterPoint) < 1)
                                continue;
                            if (prjPoint.PointInLineSegment(minPoint, minPoint))
                            {
                                changeId = thisVent.VentId;
                                newPoint = prjPoint;
                                break;
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(changeId))
                    {
                        i += 1;
                        continue;
                    }
                    foreach (var item in _roomIntersectAreas)
                    {
                        if (item.GroupId != groupId)
                            continue;
                        foreach (var fanAreas in item.FanLayoutAreaResult)
                        {
                            foreach (var fan in fanAreas.FanLayoutResult)
                            {
                                if (fan.InnerVentRects == null || fan.InnerVentRects.Count < 1)
                                    continue;
                                var rm = fan.InnerVentRects.Where(c => c.VentId == changeId).FirstOrDefault();
                                if (null == rm)
                                    continue;
                                fan.InnerVentRects.Remove(rm);
                                fan.InnerVentRects.Add(new FanInnerVentRect(GetFanVentPolyline(newPoint, fan.FanDirection)));
                                if (!changeFanIds.Any(c => c == fan.FanId))
                                    changeFanIds.Add(fan.FanId);
                                break;
                            }
                        }
                    }
                }

                if (changeFanIds.Count < 1)
                    continue;
                //对齐后，根据风口位置调整风机长度
                foreach (var layoutArea in _roomIntersectAreas) 
                {
                    if (layoutArea.GroupId != groupId)
                        continue;
                    foreach (var layoutFan in layoutArea.FanLayoutAreaResult) 
                    {
                        if (layoutFan.FanLayoutResult.Count < 1)
                            continue;
                        foreach (var fanId in changeFanIds) 
                        {
                            var rm = layoutFan.FanLayoutResult.Where(c => c.FanId == fanId).FirstOrDefault();
                            if (rm == null)
                                continue;
                            layoutFan.FanLayoutResult.Remove(rm);
                            var center = rm.CenterPoint;
                            var start = center - rm.FanDirection.MultiplyBy(rm.Length / 2);
                            var newEnd = rm.InnerVentRects.Last().CenterPoint + rm.FanDirection.MultiplyBy(_fanRectangle.VentRect.VentMinDistanceToEnd);
                            var newCenter = ThPointVectorUtil.PointsAverageValue(new List<Point3d> { start, newEnd });
                            var poly = CenterToRect(newCenter, rm.FanDirection, start.DistanceTo(newEnd), _groupXVector, rm.Width);
                            var newFan = new FanLayoutRect(poly, _fanRectangle.Width, rm.LengthDirctor);
                            newFan.FanDirection = rm.FanDirection;
                            newFan.InnerVentRects.AddRange(rm.InnerVentRects);
                            layoutFan.FanLayoutResult.Add(newFan);
                        }
                        
                    }
                }
            }
            return _roomIntersectAreas;
        }
        bool CheckAndAlignmentFan(DivisionRoomArea divisionAreaFan, Vector3d checkDir)
        {
            var dirNearFans = GetDirNearFans(divisionAreaFan, checkDir);
            var otherDirNearFans = GetDirNearFans(divisionAreaFan, checkDir.Negate());
            var nearFans = new List<FanLayoutRect>();
            if (dirNearFans.Count <= otherDirNearFans.Count)
                nearFans.AddRange(otherDirNearFans);
            else
                nearFans.AddRange(dirNearFans);
            bool haveChange = false;
            for (int i = 0; i < divisionAreaFan.RowCount; i++)
            {
                var thisRow = divisionAreaFan.FanLayoutAreaResult.Where(c => c.RowId == i).FirstOrDefault();
                if (thisRow == null || thisRow.FanLayoutResult ==null || thisRow.FanLayoutResult.Count<1)
                    continue;
                if (nearFans.Count() < thisRow.FanLayoutResult.Count)
                    continue;
                var thisDir = thisRow.FanLayoutResult.First().FanDirection;
                var thisRowFanCount = thisRow.FanLayoutResult.Count;
                var calcResult = CalcFanRectangle(thisRow.LayoutAreas, nearFans, _fanRectangle, checkDir, thisRowFanCount);
                if (calcResult.Count != thisRowFanCount)
                    return false;
                //检查新的位置和旧的位置是否有不同
                var oldFanCenters = new List<Point3d>();
                foreach (var item in thisRow.FanLayoutResult)
                {
                    oldFanCenters.Add(item.CenterPoint);
                }
                var newFanCenters = new List<Point3d>();
                foreach (var item in calcResult)
                {
                    newFanCenters.Add(IndoorFanCommon.PolylinCenterPoint(item));
                }

                foreach (var point in newFanCenters)
                {
                    if (oldFanCenters.Any(c => c.DistanceTo(point) < 5))
                        continue;
                    haveChange = true;
                    break;
                }
                if (haveChange)
                {
                    thisRow.FanLayoutResult.Clear();
                    foreach (var pline in calcResult)
                    {
                        var fanPline = new FanLayoutRect(pline,_fanRectangle.Width, checkDir);
                        fanPline.FanDirection = thisDir;
                        thisRow.FanLayoutResult.Add(fanPline);
                    }
                }
            }
            return haveChange;
        }
        void CheckAndRemoveLayoutFan() 
        {
            //计算需要多少台时要根据当前UCS面积计算需要，如果有多个UCS时不能以整个房间的负荷作为计算
            var areaLoad = 0.0;
            var ucsArea = 0.0;
            foreach (var item in _roomIntersectAreas) 
            {
                ucsArea += item.RealIntersectAreas.Sum(c => c.Area);
            }
            areaLoad = ucsArea * _roomUnitLoad;
            var layoutResultCheck = new LayoutResultCheck(_roomIntersectAreas, areaLoad, _fanRectangle.Load);
            var calcDelFans = layoutResultCheck.GetDeleteFanByRow();
            if (calcDelFans.Count < 1)
                return;
            foreach (var areaCell in _roomIntersectAreas) 
            {
                if (!calcDelFans.Any(c => c.CellId == areaCell.divisionArea.Uid))
                    continue;
                int delCount = 0;
                foreach (var item in areaCell.FanLayoutAreaResult) 
                {
                    var delFans = new List<FanLayoutRect>();
                    foreach (var fan in item.FanLayoutResult) 
                    {
                        if (calcDelFans.Any(c => c.FanId == fan.FanId))
                            delFans.Add(fan);
                    }
                    if (delFans.Count < 1)
                        continue;
                    delCount += delFans.Count;
                    foreach (var del in delFans)
                        item.FanLayoutResult.Remove(del);
                }
                if (delCount < 1)
                    continue;
                areaCell.NeedFanCount -= delCount;
            }
            //有删除重新进行排布计算
            foreach (var area in _roomIntersectAreas)
            {
                if (!calcDelFans.Any(c => c.CellId == area.divisionArea.Uid))
                    continue;
                area.FanLayoutAreaResult.Clear();
                CalcLayoutArea(area, _fanRectangle, _groupYVector, false);
                OneDivisionAreaCalcFanRectangle(area, _fanRectangle, _groupYVector.Negate());
            }
            //LayoutFanRectFirstStep(false);
        }
        List<FanLayoutRect> GetDirNearFans(DivisionRoomArea divisionAreaFan, Vector3d checkDir) 
        {
            var nearAreas = GetNearDivisionAreas(divisionAreaFan.divisionArea, checkDir);
            var nearFans = new List<FanLayoutRect>();
            foreach (var fanLayoutArea in nearAreas)
            {
                if (fanLayoutArea == null || fanLayoutArea.FanLayoutAreaResult == null || fanLayoutArea.FanLayoutAreaResult.Count < 1)
                    continue;
                foreach (var item in fanLayoutArea.FanLayoutAreaResult)
                {
                    if (item.FanLayoutResult == null || item.FanLayoutResult.Count < 1)
                        continue;
                    nearFans.AddRange(item.FanLayoutResult);
                }
            }
            return nearFans;
        }

        void LayoutFanRectFirstStep(bool checkLoad)
        {
            _changeLayoutDir = false;
            _roomIntersectAreas = _roomIntersectAreas.OrderByDescending(c => c.RealIntersectAreas.Sum(x => x.Area)).ToList();
            for (int j = _firstGroupIndex; j >= 0; j--)
            {
                CalcRowAreaLayoutFans(j, checkLoad);
            }
            for (int j = _firstGroupIndex + 1; j < _allGroupCenterOrders.Count; j++)
            {
                CalcRowAreaLayoutFans(j, checkLoad);
            }
        }
        void CalcRowAreaLayoutFans(int rowNum,bool checkLoad) 
        {
            var orientation = _groupYVector.Negate();
            var curretnPoint = _allGroupCenterOrders[rowNum];
            var currentGroupId = _allGroupPoints.Where(c => c.Value.DistanceTo(curretnPoint) < 1).First().Key;
            int layoutCount = 0;
            double rowNeedLoad = 0.0;
            foreach (var item in _roomIntersectAreas)
            {
                if (item.GroupId != currentGroupId)
                    continue;
                rowNeedLoad += item.NeedLoad;
                OneDivisionAreaCalcFanRectangle(item, _fanRectangle, orientation);
                layoutCount += item.FanLayoutAreaResult.Sum(c => c.FanLayoutResult.Count);
            }
            var readLoad = layoutCount * _fanRectangle.Load;
            if (checkLoad && rowNeedLoad > readLoad)
            {
                var needCount = (int)Math.Ceiling(rowNeedLoad / _fanRectangle.Load);
                var addCount = needCount - layoutCount;
                layoutCount = 0;
                //该行负荷不够，清除该行的历史，修改区域所需风机个数，重新进行排布
                foreach (var area in _roomIntersectAreas)
                {
                    if (area.GroupId != currentGroupId)
                        continue;
                    if (addCount < 1)
                        break;
                    int thisCount = area.FanLayoutAreaResult.Sum(c => c.FanLayoutResult.Count);
                    if (thisCount > 0 && addCount > 0)
                    {
                        area.NeedFanCount += 1;
                        addCount -= 1;
                    }
                    if (Math.Abs(area.divisionArea.AreaPolyline.Area - 33847790) < 10) 
                    {
                    
                    }
                    area.FanLayoutAreaResult.Clear();
                    CalcLayoutArea(area, _fanRectangle, _groupYVector,false);
                    OneDivisionAreaCalcFanRectangle(area, _fanRectangle, orientation);
                    layoutCount += area.FanLayoutAreaResult.Sum(c => c.FanLayoutResult.Count);
                }
            }
            if (rowNum == _firstGroupIndex)
                _changeLayoutDir = layoutCount < 1;
        }
        void AdjustmentFanRectSize()
        {
            var allGroupIds = _allGroupPoints.Select(c => c.Key).ToList();
            foreach (var groupId in allGroupIds)
            {
                var thisGroupAreaFans = _roomIntersectAreas.Where(c => c.GroupId == groupId).ToList();
                if (thisGroupAreaFans.Count < 1)
                    continue;
                //根据最长长度进行对齐调整
                var maxRowCount = thisGroupAreaFans.Max(c => c.RowCount);
                var hisFans = new List<string>();
                for (int i = 0; i < maxRowCount; i++) 
                {
                    var thisGroupFans = new List<FanLayoutRect>();
                    foreach (var area in thisGroupAreaFans)
                    {

                        foreach (var fanRes in area.FanLayoutAreaResult) 
                        {
                            if(fanRes.RowId == i)
                                thisGroupFans.AddRange(fanRes.FanLayoutResult);
                        }
                    }
                    var newValues = FanRectangleAlignment(thisGroupFans, _fanRectangle.MaxLength);
                    if (null == newValues || newValues.Count < 1)
                        continue;
                    foreach (var fanPline in _roomIntersectAreas)
                    {
                        if (hisFans.Count == newValues.Count)
                            break;
                        foreach (var item in fanPline.FanLayoutAreaResult)
                        {
                            foreach (var keyValue in newValues)
                            {
                                if (hisFans.Any(c => c == keyValue.Key))
                                    continue;
                                var tempValue = item.FanLayoutResult.Where(c => c.FanId == keyValue.Key).FirstOrDefault();
                                if (tempValue == null)
                                    continue;
                                item.FanLayoutResult.Remove(tempValue);
                                item.FanLayoutResult.Add(keyValue.Value);
                                hisFans.Add(keyValue.Key);
                            }
                        }
                    }
                }
            }
        }
        Dictionary<string, FanLayoutRect> FanRectangleAlignment(List<FanLayoutRect> targetFanLayout, double maxLength)
        {
            var retRes = new Dictionary<string, FanLayoutRect>();
            //step1 先将共线长度>=最大长度的优先对齐
            var overLengthFans = new List<FanLayoutRect>();
            var otherLengthFans = new List<FanLayoutRect>();
            foreach (var item in targetFanLayout)
            {
                if (item.Length >= maxLength)
                    overLengthFans.Add(item);
                else
                    otherLengthFans.Add(item);
            }
            Line overLengthBaseLine = null;
            Line otherLengthBaseLine = null;
            if (overLengthFans.Count > 0)
            {
                var lineInterCount = GetIntersectionLine(overLengthFans, maxLength);
                overLengthBaseLine = lineInterCount.Where(c => c.Value > 0).OrderByDescending(c => c.Value).FirstOrDefault().Key;
                var changeRes = ChangeFanLengthByLine(overLengthBaseLine, overLengthFans);
                if (changeRes.Count > 0)
                {
                    //超长的风机长度处理
                    foreach (var keyValue in changeRes)
                    {
                        if (keyValue.Value.Length > maxLength)
                        {
                            
                            var centerPoint = keyValue.Value.CenterPoint;
                            var dir = keyValue.Value.LengthDirctor;
                            var poly = CenterToRect(centerPoint, dir, maxLength, _groupXVector, keyValue.Value.Width);
                            var newFan = new FanLayoutRect(poly,_fanRectangle.Width, keyValue.Value.LengthDirctor);
                            newFan.FanDirection = keyValue.Value.FanDirection;
                            retRes.Add(keyValue.Key, newFan);
                        }
                        else
                        {
                            retRes.Add(keyValue.Key, keyValue.Value);
                        }
                    }
                    overLengthBaseLine = retRes.First().Value.LengthLines.First();
                }
            }
            if (otherLengthFans.Count > 0)
            {
                var lineInterCount = GetIntersectionLine(otherLengthFans, -1);
                otherLengthBaseLine = lineInterCount.First().Key;
                int baseLineCount = lineInterCount.First().Value;
                double weight = baseLineCount * otherLengthBaseLine.Length;
                foreach (var keyValue in lineInterCount)
                {
                    var line = keyValue.Key;
                    var count = keyValue.Value;
                    var tempWeight = line.Length * count;
                    if (tempWeight > weight)
                    {
                        otherLengthBaseLine = line;
                        baseLineCount = count;
                        weight = tempWeight;
                    }
                }
                var changeRes = ChangeFanLengthByLine(otherLengthBaseLine, otherLengthFans);
                foreach (var keyValue in changeRes)
                {
                    retRes.Add(keyValue.Key, keyValue.Value);
                }
            }
            //return retRes;
            var notChanges = targetFanLayout.Where(c => !retRes.Any(x => x.Key == c.FanId)).ToList();
            //step2 将其它共线少的位置调整到风机的相应位置
            if (null != overLengthBaseLine)
            {
                var changeFans = new Dictionary<string, FanLayoutRect>();
                foreach (var keyValue in retRes)
                {
                    var thisFanRect = keyValue.Value;
                    var newFan = ChangeFanLengthByBaseLine(overLengthBaseLine, keyValue.Value);
                    if (null == newFan)
                        continue;
                    changeFans.Add(keyValue.Key, newFan);
                }
                foreach (var keyValue in changeFans)
                {
                    retRes[keyValue.Key] = keyValue.Value;
                }
                foreach (var item in notChanges)
                {
                    var newFan = ChangeFanLengthByBaseLine(overLengthBaseLine, item);
                    if (null == newFan)
                        continue;
                    retRes.Add(item.FanId, newFan);
                }
            }
            else if (null != otherLengthBaseLine)
            {
                var changeFans = new Dictionary<string, FanLayoutRect>();
                notChanges = targetFanLayout.Where(c => !retRes.Any(x => x.Key == c.FanId)).ToList();
                foreach (var keyValue in retRes)
                {
                    var thisFanRect = keyValue.Value;
                    var newFan = ChangeFanLengthByBaseLine(otherLengthBaseLine, keyValue.Value);
                    if (null == newFan)
                        continue;
                    changeFans.Add(keyValue.Key, newFan);
                }
                foreach (var keyValue in changeFans)
                {
                    retRes[keyValue.Key] = keyValue.Value;
                }
                foreach (var item in notChanges)
                {
                    var newFan = ChangeFanLengthByBaseLine(otherLengthBaseLine, item);
                    if (null == newFan)
                        continue;
                    retRes.Add(item.FanId, newFan);
                }
            }
            return retRes;
        }
        
        FanLayoutRect ChangeFanLengthByBaseLine(Line baseLine, FanLayoutRect fanLayoutRect)
        {
            var thisLine = fanLayoutRect.LengthLines.First();
            var interLine = LineIntersectionFanRect(baseLine, fanLayoutRect);
            if (null == interLine || interLine.Length < 2000)
                return null;
            if (Math.Abs(thisLine.Length - interLine.Length) < 5)
                return null;
            var centerPoint = fanLayoutRect.CenterPoint;
            var otherDir = fanLayoutRect.LengthDirctor.CrossProduct(Vector3d.ZAxis);
            var newCenter = ThPointVectorUtil.PointsAverageValue(new List<Point3d> { interLine.StartPoint, interLine.EndPoint });
            newCenter = newCenter.PointToFace(centerPoint, otherDir);
            var poly =CenterToRect(newCenter, fanLayoutRect.LengthDirctor, interLine.Length, otherDir, fanLayoutRect.Width);
            var newFan = new FanLayoutRect(poly, _fanRectangle.Width,fanLayoutRect.LengthDirctor);
            newFan.FanDirection = fanLayoutRect.FanDirection;
            return newFan;
        }
        Dictionary<string, FanLayoutRect> ChangeFanLengthByLine(Line standardLine, List<FanLayoutRect> targetFanLayout)
        {
            var retRes = new Dictionary<string, FanLayoutRect>();
            if (null == standardLine || targetFanLayout == null || targetFanLayout.Count < 1)
                return retRes;
            var tempSp = standardLine.StartPoint;
            var tempEp = standardLine.EndPoint;
            for (int j = 0; j < targetFanLayout.Count; j++)
            {
                var secondFan = targetFanLayout[j];
                var secondLine = secondFan.LengthLines.First();
                var prjSp = tempSp.PointToLine(secondLine);
                var prjEp = tempEp.PointToLine(secondLine);
                IndoorFanCommon.FindIntersection(new Line(prjSp, prjEp), secondLine, out List<Point3d> interPoints);
                if (interPoints == null || interPoints.Count < 2)
                    continue;
                var dis = interPoints[0].DistanceTo(interPoints[1]);
                if (Math.Abs(standardLine.Length - dis) > 10)
                    continue;
                var centerPoint = secondFan.CenterPoint;
                var otherDir = secondFan.LengthDirctor.CrossProduct(Vector3d.ZAxis);
                var newCenter = ThPointVectorUtil.PointsAverageValue(new List<Point3d> { interPoints[0], interPoints[1] });
                newCenter = newCenter.PointToFace(centerPoint, otherDir);
                var poly = CenterToRect(newCenter, secondFan.LengthDirctor, dis, otherDir, secondFan.Width);
                var newFan = new FanLayoutRect(poly, _fanRectangle.Width, secondFan.LengthDirctor);
                newFan.FanDirection = secondFan.FanDirection;
                retRes.Add(secondFan.FanId, newFan);
            }
            return retRes;
        }
        Line LineIntersectionFanRect(Line baseLine, FanLayoutRect fanLayoutRect)
        {
            var tempSp = baseLine.StartPoint;
            var tempEp = baseLine.EndPoint;
            var secondLine = fanLayoutRect.LengthLines.First();
            var prjSp = tempSp.PointToLine(secondLine);
            var prjEp = tempEp.PointToLine(secondLine);
            IndoorFanCommon.FindIntersection(new Line(prjSp, prjEp), secondLine, out List<Point3d> interPoints);
            if (interPoints == null || interPoints.Count < 2)
                return null;
            return new Line(interPoints[0], interPoints[1]);
        }
        Dictionary<Line, int> GetIntersectionLine(List<FanLayoutRect> targetFanLayout, double length = -1)
        {
            var lineInterCount = new Dictionary<Line, int>();
            for (int i = 0; i < targetFanLayout.Count; i++)
            {
                var firstFan = targetFanLayout[i];
                var intCount = 1;
                var firstLine = firstFan.LengthLines.First();
                Point3d? interPointSp = null;
                Point3d? interPointEp = null;
                var checkLength = length > 0 ? length : firstLine.Length;
                if (length < 0)
                {
                    interPointSp = firstLine.StartPoint;
                    interPointEp = firstLine.EndPoint;
                }
                for (int j = 0; j < targetFanLayout.Count; j++)
                {
                    if (i == j)
                        continue;
                    var secondFan = targetFanLayout[j];
                    var secondLine = secondFan.LengthLines.First();
                    var prjSp = ((interPointSp != null && interPointSp.HasValue) ? interPointSp.Value : firstLine.StartPoint).PointToLine(secondLine);
                    var prjEp = ((interPointEp != null && interPointEp.HasValue) ? interPointEp.Value : firstLine.EndPoint).PointToLine(secondLine);
                    IndoorFanCommon.FindIntersection(new Line(prjSp, prjEp), secondLine, out List<Point3d> interPoints);
                    if (interPoints == null || interPoints.Count < 2)
                        continue;
                    var dis = interPoints[0].DistanceTo(interPoints[1]);
                    if (dis - checkLength < 0 && Math.Abs(checkLength - dis) > 10)
                        continue;
                    if (length > 0)
                    {
                        interPointSp = interPoints[0];
                        interPointEp = interPoints[1];
                    }
                    intCount += 1;
                }
                if (interPointSp == null || !interPointSp.HasValue)
                    lineInterCount.Add(firstLine, intCount);
                else
                    lineInterCount.Add(new Line(interPointSp.Value, interPointEp.Value), intCount);
            }
            return lineInterCount;

        }
        

        void OneDivisionAreaCalcFanRectangle(DivisionRoomArea divisionArea, FanRectangle fanRectangle, Vector3d vector)
        {
            var fanCount = divisionArea.NeedFanCount;
            if (fanCount < 1)
                return;
            //获取点
            var allPoints = new List<Point3d>();
            foreach (var item in divisionArea.RoomLayoutAreas)
            {
                allPoints.AddRange(IndoorFanCommon.GetPolylinePoints(item));
            }
            if (allPoints.Count < 3)
                return;
            var otherDir = _groupXVector;
            var orderDirPoints = ThPointVectorUtil.PointsOrderByDirection(allPoints, vector, false);
            var orderOtherDirPoints = ThPointVectorUtil.PointsOrderByDirection(allPoints, otherDir, false);
            var dirLength = (orderDirPoints.Last() - orderDirPoints.First()).DotProduct(vector);
            var otherDirMaxLength = (orderOtherDirPoints.Last() - orderOtherDirPoints.First()).DotProduct(otherDir);
            if (dirLength < fanRectangle.MinLength || otherDirMaxLength < fanRectangle.Width)
                return;
            int columnCount = fanCount;
            int rowCount = divisionArea.FanLayoutAreaResult.Count;
            if (columnCount < 1)
                return;
            var nearAreas = GetNearDivisionAreas(divisionArea.divisionArea, vector);
            var nearFans = new List<FanLayoutRect>();
            foreach (var fan in nearAreas)
            {
                if (fan.FanLayoutAreaResult == null || fan.FanLayoutAreaResult.Count < 1)
                    continue;
                foreach (var item in fan.FanLayoutAreaResult)
                {
                    if (item.FanLayoutResult == null || item.FanLayoutResult.Count < 1)
                        continue;
                    nearFans.AddRange(item.FanLayoutResult);
                }
            }

            var count = fanCount / rowCount;
            var tempCount = fanCount % rowCount;
            //根据行数，等分区域
            for (int i = 0; i < rowCount; i++)
            {
                var layoutDivision = divisionArea.FanLayoutAreaResult.Where(c => c.RowId == i).First();
                if (layoutDivision == null || layoutDivision.LayoutAreas == null || layoutDivision.LayoutAreas.Count() < 1)
                {
                    tempCount += count;
                    continue;
                }
                var thisRowCount = count;
                if (tempCount > 0)
                {
                    thisRowCount += 1;
                    tempCount -= 1;
                }
                //根据相邻区域的进行对齐排布
                var calcResult = CalcFanRectangle(layoutDivision.LayoutAreas, nearFans, fanRectangle, vector, thisRowCount);
                //对齐后，如果排布的个数不够，进行部分对齐进行排布
                //还是不够时进行等分排布
                if (calcResult.Count != thisRowCount)
                    calcResult = CalcFanRectangle(layoutDivision.LayoutAreas, fanRectangle, vector, thisRowCount, false);
                //最后进行最小间距排布
                if (calcResult.Count != thisRowCount)
                    calcResult = CalcFanRectangle(layoutDivision.LayoutAreas, fanRectangle, vector, thisRowCount, true);
                var thisRowFans = new List<FanLayoutRect>();
                tempCount += thisRowCount - calcResult.Count;
                foreach (var pline in calcResult)
                {
                    var fanPline = new FanLayoutRect(pline, _fanRectangle.Width, vector);
                    fanPline.FanDirection = layoutDivision.LayoutDir;
                    thisRowFans.Add(fanPline);
                }
                nearFans.Clear();
                nearFans.AddRange(thisRowFans);
                layoutDivision.FanLayoutResult.AddRange(thisRowFans);
            }
        }
        
        List<Polyline> CalcFanRectangle(List<Polyline> layoutPolylines, List<FanLayoutRect> nearLayoutFans, FanRectangle fanRectangle, Vector3d vector, int fanCount)
        {
            var tempPloylines = new Dictionary<Point3d,Polyline>();
            if (null == nearLayoutFans || nearLayoutFans.Count < 1)
                return tempPloylines.Select(c=>c.Value).ToList();
            var nearFanPoints = new List<Point3d>();
            foreach (var item in nearLayoutFans)
            {
                nearFanPoints.Add(item.CenterPoint);
            }
            if (nearFanPoints.Count < fanCount)
                return tempPloylines.Select(c => c.Value).ToList();
            var allPoints = new List<Point3d>();
            foreach (var item in layoutPolylines)
            {
                allPoints.AddRange(IndoorFanCommon.GetPolylinePoints(item));
            }
            var otherDir = Vector3d.ZAxis.CrossProduct(vector);
            var orderDirPoints = ThPointVectorUtil.PointsOrderByDirection(allPoints, vector, false);
            var orderOtherDirPoints = ThPointVectorUtil.PointsOrderByDirection(allPoints, otherDir, false);
            var dirLength = (orderDirPoints.Last() - orderDirPoints.First()).DotProduct(vector);
            var otherDirMaxLength = (orderOtherDirPoints.Last() - orderOtherDirPoints.First()).DotProduct(otherDir);

            var tempOtherDir = (orderOtherDirPoints.Last() - orderOtherDirPoints.First()).GetNormal();
            tempOtherDir = tempOtherDir.DotProduct(otherDir) > 0 ? otherDir : otherDir.Negate();
            nearFanPoints = ThPointVectorUtil.PointsOrderByDirection(nearFanPoints, otherDir, false);
            var origin = orderOtherDirPoints.First();
            var hisPoints = new List<Point3d>();
            //如果相邻的地方风机个数多，进行两侧对齐
            if (fanCount < nearFanPoints.Count)
            {
                for (int i = 0; i < fanCount; i++)
                {
                    var isStart = i % 2 == 0;
                    if (isStart)
                    {
                        for (int j = 0; j < nearFanPoints.Count; j++)
                        {
                            var point = nearFanPoints[j];
                            if (hisPoints.Any(c => c.DistanceTo(point) < 1))
                                continue;
                            var startPoint = point.PointToFace(origin, vector);
                            startPoint -= tempOtherDir.MultiplyBy(fanRectangle.Width / 2);
                            var endPoint = startPoint + tempOtherDir.MultiplyBy(fanRectangle.Width);
                            var tempPlines = CanLayoutArea(layoutPolylines, startPoint, endPoint, vector, fanRectangle.MinLength, dirLength);
                            if (tempPlines == null || tempPlines.Count < 1)
                                continue;
                            hisPoints.Add(point);
                            bool isAdd = true;
                            foreach (var item in tempPlines) 
                            {
                                if (!isAdd)
                                    break;
                                foreach (var keyValue in tempPloylines) 
                                {
                                    if(Math.Abs((item .Key - keyValue.Key).DotProduct(otherDir))<fanRectangle.MinLength)
                                    {
                                        isAdd = false;
                                        break;
                                    }
                                }
                            }
                            if (!isAdd)
                                continue;
                            foreach (var item in tempPlines)
                                tempPloylines.Add(item.Key, item.Value);
                            break;
                        }
                    }
                    else
                    {
                        for (int j = nearFanPoints.Count - 1; j >= 0; j--)
                        {
                            var point = nearFanPoints[j];
                            if (hisPoints.Any(c => c.DistanceTo(point) < 1))
                                continue;
                            var startPoint = point.PointToFace(origin, vector);
                            startPoint -= tempOtherDir.MultiplyBy(fanRectangle.Width / 2);
                            var endPoint = startPoint + tempOtherDir.MultiplyBy(fanRectangle.Width);
                            var tempPlines = CanLayoutArea(layoutPolylines, startPoint, endPoint, vector, fanRectangle.MinLength, dirLength);
                            if (tempPlines == null || tempPlines.Count < 1)
                                continue;
                            hisPoints.Add(point);
                            bool isAdd = true;
                            foreach (var item in tempPlines)
                            {
                                if (!isAdd)
                                    break;
                                foreach (var keyValue in tempPloylines)
                                {
                                    if (Math.Abs((item.Key - keyValue.Key).DotProduct(otherDir)) < fanRectangle.MinLength)
                                    {
                                        isAdd = false;
                                        break;
                                    }
                                }
                            }
                            if (!isAdd)
                                continue;
                            foreach (var item in tempPlines)
                                tempPloylines.Add(item.Key, item.Value);
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < fanCount; i++)
                {
                    double maxLength = double.MinValue;
                    var thisPLines = new Dictionary<Point3d,Polyline>();
                    var targetPoint = new Point3d();
                    foreach (var point in nearFanPoints)
                    {
                        if (hisPoints.Any(c => c.DistanceTo(point) < 1))
                            continue;
                        var startPoint = point.PointToFace(origin, vector);
                        startPoint -= tempOtherDir.MultiplyBy(fanRectangle.Width / 2);
                        var endPoint = startPoint + tempOtherDir.MultiplyBy(fanRectangle.Width);
                        var tempPlines = CanLayoutArea(layoutPolylines, startPoint, endPoint, vector, fanRectangle.MinLength, dirLength);
                        if (tempPlines == null || tempPlines.Count < 1)
                            continue;
                        var thisMaxLength = double.MinValue;
                        bool isAdd = true;
                        foreach (var pline in tempPlines)
                        {
                            if (!isAdd)
                                break;
                            double length = pline.Value.Area / fanRectangle.Width;
                            if (thisMaxLength < length)
                                thisMaxLength = length;
                            if (tempPloylines.Any(c=>Math.Abs((c.Key - pline.Key).DotProduct(otherDir)) < fanRectangle.MinLength))
                            {
                                hisPoints.Add(point);
                                isAdd = false;
                                break;
                            }
                        }
                        if (!isAdd)
                            continue;
                        if (maxLength < thisMaxLength)
                        {
                            targetPoint = point;
                            maxLength = thisMaxLength;
                            thisPLines.Clear();
                            foreach (var pline in tempPlines)
                                thisPLines.Add(pline.Key,pline.Value);
                        }
                    }
                    if (thisPLines.Count > 0)
                    {
                        hisPoints.Add(targetPoint);
                        foreach (var pline in thisPLines)
                            tempPloylines.Add(pline.Key, pline.Value);
                    }
                }
            }
            return tempPloylines.Select(c => c.Value).ToList();
        }
        List<Polyline> CalcFanRectangle(List<Polyline> layoutPolylines, FanRectangle fanRectangle, Vector3d vector, int fanCount, bool isMinSapce)
        {
            var allPoints = new List<Point3d>();
            foreach (var item in layoutPolylines)
            {
                allPoints.AddRange(IndoorFanCommon.GetPolylinePoints(item));
            }
            var otherDir = Vector3d.ZAxis.CrossProduct(vector);
            var orderDirPoints = ThPointVectorUtil.PointsOrderByDirection(allPoints, vector, false);
            var orderOtherDirPoints = ThPointVectorUtil.PointsOrderByDirection(allPoints, otherDir, false);
            var dirLength = (orderDirPoints.Last() - orderDirPoints.First()).DotProduct(vector);
            var otherDirMaxLength = (orderOtherDirPoints.Last() - orderOtherDirPoints.First()).DotProduct(otherDir);

            var tempOtherDir = (orderOtherDirPoints.Last() - orderOtherDirPoints.First()).GetNormal();
            tempOtherDir = tempOtherDir.DotProduct(otherDir) > 0 ? otherDir : otherDir.Negate();

            var startSpace = isMinSapce ? 0 : (otherDirMaxLength / (fanCount * 2) - fanRectangle.Width / 2);
            //startSpace = startSpace > 500 ? startSpace : 500;
            var minSpace = fanRectangle.Width + 800;
            var space = otherDirMaxLength / fanCount;
            space = isMinSapce ? minSpace : (space < minSpace ? minSpace : space);
            var origin = orderOtherDirPoints.First() + tempOtherDir.MultiplyBy(startSpace);
            var startPoint = origin;
            var tempPloylines = new List<Polyline>();
            while (true)
            {
                var endPoint = startPoint + tempOtherDir.MultiplyBy(fanRectangle.Width);
                if (startPoint.DistanceTo(origin) > otherDirMaxLength || endPoint.DistanceTo(origin) > otherDirMaxLength)
                    break;
                var tempPlines = CanLayoutArea(layoutPolylines, startPoint, endPoint, vector, fanRectangle.MinLength, dirLength);
                if (tempPlines == null || tempPlines.Count < 1)
                    startPoint += tempOtherDir.MultiplyBy(100);
                else
                {
                    startPoint += tempOtherDir.MultiplyBy(space);
                    tempPloylines.AddRange(tempPlines.Select(c=>c.Value).ToList());
                }
            }
            return tempPloylines;
        }
    }
    
}
