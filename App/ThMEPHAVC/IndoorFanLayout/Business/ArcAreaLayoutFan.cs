using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPHVAC.IndoorFanLayout.Models;

namespace ThMEPHVAC.IndoorFanLayout.Business
{
    class ArcAreaLayoutFan : RoomLayoutFanBase
    {
        public ArcAreaLayoutFan(Dictionary<string, List<string>> divisionAreaNearIds, Vector3d xAxis, Vector3d yAxis) 
            : base(divisionAreaNearIds, xAxis, yAxis)
        {
        }
        public List<DivisionRoomArea> GetRectangle(AreaLayoutGroup layoutGroup, FanRectangle fanRectangle) 
        {
            _roomIntersectAreas.Clear();
            if (!layoutGroup.IsArcGroup)
                return _roomIntersectAreas;
            _fanRectangle = fanRectangle;
            CalcRoomLoad(layoutGroup);
            if (_roomIntersectAreas.Count < 1)
                return _roomIntersectAreas;
           
            LayoutFanRectFirstStep();
            //排布完成后，进行检查添加和删除逻辑
            CheckDeleteAddFan();
            CheckChangeLayoutDir();
            int maxCount = _roomIntersectAreas.Count * 2;
            while (true)
            {
                if (maxCount < 0)
                    break;
                bool haveChange = false;
                foreach (var item in _roomIntersectAreas)
                {
                    if (item.FanLayoutAreaResult == null || item.FanLayoutAreaResult.Count < 1)
                        continue;
                    haveChange = CheckAndAlignmentFan(item, true);
                    if (haveChange)
                        break;
                    haveChange = CheckAndAlignmentFan(item, false);
                    if (haveChange)
                        break;
                }
                maxCount -= 1;
                if (!haveChange)
                    break;
            }
            AdjustmentFanRectSize();
            //排布内部的小风口
            LayoutFanVent();
            AlignmentFanVent();
            return _roomIntersectAreas;
        }
        void CheckDeleteAddFan() 
        {
            var delFans = CheckAndRemoveLayoutFan();
            if (delFans.Count > 0)
            {
                //有删除重新进行排布计算
                foreach (var area in _roomIntersectAreas)
                {
                    //continue;
                    if (!delFans.Any(c => c.CellId == area.divisionArea.Uid))
                        continue;
                    area.FanLayoutAreaResult.Clear();
                    CalcLayoutArea(area, _fanRectangle, _groupYVector, false);
                    OneDivisionAreaCalcFanRectangle(area, _fanRectangle);
                }
            }
            else
            {
                var addFanCellIds = CheckAndAddLayoutFan();
                if (addFanCellIds.Count > 0)
                {
                    //需要添加 重新进行排布计算
                    foreach (var area in _roomIntersectAreas)
                    {
                        //continue;
                        if (!addFanCellIds.Any(c => c == area.divisionArea.Uid))
                            continue;
                        area.FanLayoutAreaResult.Clear();
                        CalcLayoutArea(area, _fanRectangle, _groupYVector, false);
                        OneDivisionAreaCalcFanRectangle(area, _fanRectangle);
                    }
                }
            }
        }
        void AlignmentFanVent() 
        {
            //扇形区域根据风口，进行调整对齐，并调整风机长度
            var allGroupIds = _allGroupPoints.Select(c => c.Key).ToList();
            foreach (var groupId in allGroupIds)
            {
                var thisGroupAreaFans = _roomIntersectAreas.Where(c => c.GroupId == groupId && c.divisionArea.IsArc).ToList();
                if (thisGroupAreaFans.Count < 1)
                    continue;
                //获取同圆心的弧形区域
                while (thisGroupAreaFans.Count > 0)
                {
                    var first = thisGroupAreaFans.First();
                    thisGroupAreaFans.Remove(first);
                    var arc = first.divisionArea.AreaCurves.OfType<Arc>().First();
                    var thisArcAreas = new List<DivisionRoomArea>();
                    foreach (var item in thisGroupAreaFans)
                    {
                        var thisArc = item.divisionArea.AreaCurves.OfType<Arc>().First();
                        if (arc.Center.DistanceTo(thisArc.Center) > 1)
                            continue;
                        thisArcAreas.Add(item);
                    }
                    foreach (var item in thisArcAreas)
                        thisGroupAreaFans.Remove(item);
                    thisArcAreas.Add(first);
                    ArcAreaAlignmentFanVent(arc.Center, groupId, thisArcAreas);
                }
                
            }
        }
        void ArcAreaAlignmentFanVent(Point3d arcCener,string groupId,List<DivisionRoomArea> thisArcAreas) 
        {
            var rowCount = thisArcAreas.Max(c => c.RowCount);
            var changeFanIds = new List<string>();
            for (int i = 0; i < rowCount;)
            {
                var thisRowFans = new List<FanLayoutRect>();
                var thisRowVents = new List<FanInnerVentRect>();
                foreach (var item in thisArcAreas)
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
                    if (!string.IsNullOrEmpty(changeId))
                        break;
                    if (item.InnerVentRects.Count !=1)
                        continue;
                    if (changeFanIds.Any(c => c == item.FanId))
                        continue;
                    var thisVent = item.InnerVentRects.First();
                    var startPoint = item.CenterPoint - item.FanDirection.MultiplyBy(item.Length / 2);
                    var dir = (item.CenterPoint - arcCener).GetNormal();
                    var minPoint = startPoint + item.FanDirection.MultiplyBy(_fanRectangle.VentRect.VentMinDistanceToStart);
                    var maxPoint = startPoint + item.FanDirection.MultiplyBy(item.Length) - item.FanDirection.MultiplyBy(_fanRectangle.VentRect.VentMinDistanceToEnd);
                    var radius1 = minPoint.DistanceTo(arcCener);
                    var radius2 = maxPoint.DistanceTo(arcCener);
                    var minRadius = radius1;
                    var maxRadius = radius2;
                    var thisRadius = thisVent.CenterPoint.DistanceTo(arcCener);
                    if(radius1>radius2)
                    {
                        minRadius = radius2;
                        maxRadius = radius1;
                    }
                    var otherFanVents = thisRowVents.Where(c => c.VentId != thisVent.VentId).ToList();
                    foreach (var vent in otherFanVents)
                    {
                        var dis = vent.CenterPoint.DistanceTo(arcCener);
                        if (Math.Abs(dis- thisRadius) < 1)
                            continue;
                        if (dis<= maxRadius && dis>=minRadius)
                        {
                            changeId = thisVent.VentId;
                            newPoint = arcCener + dir.MultiplyBy(dis);
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
                return;
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
                        var otherDir = Vector3d.ZAxis.CrossProduct(rm.FanDirection);
                        var start = center - rm.FanDirection.MultiplyBy(rm.Length / 2);
                        var newEnd = rm.InnerVentRects.Last().CenterPoint + rm.FanDirection.MultiplyBy(_fanRectangle.VentRect.VentMinDistanceToEnd);
                        var newCenter = ThPointVectorUtil.PointsAverageValue(new List<Point3d> { start, newEnd });
                        var poly = CenterToRect(newCenter, rm.FanDirection, start.DistanceTo(newEnd), otherDir, rm.Width);
                        var newFan = new FanLayoutRect(poly, _fanRectangle.Width, rm.LengthDirctor);
                        newFan.FanDirection = rm.FanDirection;
                        newFan.InnerVentRects.AddRange(rm.InnerVentRects);
                        layoutFan.FanLayoutResult.Add(newFan);
                    }

                }
            }
        }
        bool CheckAndAlignmentFan(DivisionRoomArea divisionAreaFan, bool isInner)
        {
            var arc = divisionAreaFan.divisionArea.AreaCurves.OfType<Arc>().First();
            var outDir = (divisionAreaFan.divisionArea.CenterPoint - arc.Center).GetNormal();
            var checkDir = isInner ? outDir.Negate() : outDir;
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
            bool haveChange = false;
            for (int i = 0; i < divisionAreaFan.RowCount; i++)
            {
                var thisRow = divisionAreaFan.FanLayoutAreaResult.Where(c => c.RowId == i).FirstOrDefault();
                if (thisRow == null || thisRow.FanLayoutResult == null || thisRow.FanLayoutResult.Count < 1)
                    continue;
                if (nearFans.Count() < thisRow.FanLayoutResult.Count)
                    continue;
                var thisDir = thisRow.FanLayoutResult.First().FanDirection;
                var thisRowFanCount = thisRow.FanLayoutResult.Count;
                var calcResult = CalcFanRectangle(divisionAreaFan.divisionArea, thisRow.LayoutAreas, nearFans, _fanRectangle, thisRowFanCount);
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
                        var center = IndoorFanCommon.PolylinCenterPoint(pline);
                        var thisFanDir = (center - arc.Center).GetNormal();
                        if (thisFanDir.DotProduct(thisDir) < 0)
                            thisFanDir = thisFanDir.Negate();
                        var fanPline = new FanLayoutRect(pline, _fanRectangle.Width, thisFanDir);
                        fanPline.FanDirection = thisFanDir;
                        thisRow.FanLayoutResult.Add(fanPline);
                    }
                }
            }
            return haveChange;
        }
        void LayoutFanRectFirstStep()
        {
            _changeLayoutDir = false;
            for (int j = _firstGroupIndex; j >= 0; j--)
            {
                var curretnPoint = _allGroupCenterOrders[j];
                var currentGroupId = _allGroupPoints.Where(c => c.Value.DistanceTo(curretnPoint) < 1).First().Key;
                int layoutCount = 0;
                foreach (var item in _roomIntersectAreas)
                {
                    if (item.GroupId != currentGroupId)
                        continue;
                    OneDivisionAreaCalcFanRectangle(item, _fanRectangle);
                    layoutCount += item.FanLayoutAreaResult.Sum(c => c.FanLayoutResult.Count);
                }
                if (j == _firstGroupIndex)
                    _changeLayoutDir = layoutCount < 1;
            }
            for (int j = _firstGroupIndex + 1; j < _allGroupCenterOrders.Count; j++)
            {
                var curretnPoint = _allGroupCenterOrders[j];
                var currentGroupId = _allGroupPoints.Where(c => c.Value.DistanceTo(curretnPoint) < 1).First().Key;
                foreach (var item in _roomIntersectAreas)
                {
                    if (item.GroupId != currentGroupId)
                        continue;
                    OneDivisionAreaCalcFanRectangle(item, _fanRectangle);
                }
            }
            
        }
        void OneDivisionAreaCalcFanRectangle(DivisionRoomArea divisionArea, FanRectangle fanRectangle)
        {
            var fanCount = divisionArea.NeedFanCount;
            if (fanCount < 1)
                return;
            var allPoints = new List<Point3d>();
            foreach (var item in divisionArea.RoomLayoutAreas)
            {
                allPoints.AddRange(IndoorFanCommon.GetPolylinePoints(item));
            }
            if (allPoints.Count < 3)
                return;
            
            //扇形区域一般是一个外弧，一个内弧，两个边线,即使不是标准的，也必须要有一段圆弧
            var arc = divisionArea.divisionArea.AreaCurves.Where(c => c is Arc).First() as Arc;
            var center = arc.Center;
            var arcNormal = arc.Normal;
            var arcXVector = arc.Ecs.CoordinateSystem3d.Xaxis;
            var outVector = (arc.EndPoint - center).GetNormal();
            allPoints = allPoints.OrderBy(c => c.DistanceTo(center)).ToList();
            var innerRadius = allPoints.First().DistanceTo(center);
            var outRadius = allPoints.Last().DistanceTo(center);
            double dirLength = outRadius - innerRadius;
            var arcOrderPoints = CircleArcUtil.PointOderByArcAngle(allPoints, arc);
            var orderPoints = arcOrderPoints.OrderBy(c => c.Value).Select(c => c.Key).ToList();
            var sp = orderPoints.First();
            var ep = orderPoints.Last();
            var spVector = (sp - arc.Center).GetNormal();
            var epVector = (ep - arc.Center).GetNormal();
            var spAngle = arcXVector.GetAngleTo(spVector, arcNormal);
            var epAngle = arcXVector.GetAngleTo(epVector, arcNormal);
            var otherDirLength = (epAngle-spAngle)*innerRadius;
            if (dirLength < fanRectangle.MinLength || otherDirLength < fanRectangle.Width)
                return;
            int columnCount = fanCount;
            int rowCount = divisionArea.FanLayoutAreaResult.Count;
            if (columnCount < 1)
                return;
            var vector = divisionArea.GroupDir.DotProduct(outVector) > 0 ? outVector : outVector.Negate();
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
                var calcResult = new List<Polyline>();
                //根据相邻区域的进行对齐排布
                calcResult = CalcFanRectangle(divisionArea.divisionArea, layoutDivision.LayoutAreas, nearFans, fanRectangle, thisRowCount);
                //对齐后，如果排布的个数不够，进行部分对齐进行排布
                //还是不够时进行等分排布
                if (calcResult.Count != thisRowCount)
                    calcResult = CalcFanRectangle(divisionArea.divisionArea, layoutDivision.LayoutAreas, fanRectangle, thisRowCount, false);
                //最后进行最小间距排布
                if (calcResult.Count != thisRowCount)
                    calcResult = CalcFanRectangle(divisionArea.divisionArea,layoutDivision.LayoutAreas, fanRectangle, thisRowCount, true);
                var thisRowFans = new List<FanLayoutRect>();
                tempCount += thisRowCount - calcResult.Count;
                foreach (var pline in calcResult)
                {
                    var allLines = IndoorFanCommon.GetPolylineCurves(pline);
                    var lengthLine = allLines.OrderByDescending(c => c.GetLength()).First();
                    var lengthDir = (lengthLine.EndPoint - lengthLine.StartPoint).GetNormal();
                    var fanDir = layoutDivision.LayoutDir.Length>0.5?( lengthDir.DotProduct(layoutDivision.LayoutDir) > 0 ? lengthDir : lengthDir.Negate()): layoutDivision.LayoutDir;
                    var fanPline = new FanLayoutRect(pline, _fanRectangle.Width, lengthDir);
                    fanPline.FanDirection = fanDir;
                    thisRowFans.Add(fanPline);
                }
                nearFans.Clear();
                nearFans.AddRange(thisRowFans);
                layoutDivision.FanLayoutResult.AddRange(thisRowFans);
            }
        }

        List<Polyline> CalcFanRectangle(DivisionArea divisionArea,List<Polyline> layoutPolylines, FanRectangle fanRectangle, int fanCount, bool isMinSapce)
        {
            //扇形区域一般是一个外弧，一个内弧，两个边线,即使不是标准的，也必须要有一段圆弧
            var allPoints = new List<Point3d>();
            foreach (var item in layoutPolylines)
            {
                allPoints.AddRange(IndoorFanCommon.GetPolylinePoints(item));
            }

            var arcCurves = divisionArea.AreaCurves.Where(c => c is Arc).First() as Arc;
            var center = arcCurves.Center;
            var arcNormal = arcCurves.Normal;
            var arcXVector = arcCurves.Ecs.CoordinateSystem3d.Xaxis;
            allPoints = allPoints.OrderBy(c => c.DistanceTo(center)).ToList();
            var layoutInnerRadius = allPoints.First().DistanceTo(center);
            var outRadius = allPoints.Last().DistanceTo(center);
            double maxLength = outRadius - layoutInnerRadius;
            var arcOrderPoints = CircleArcUtil.PointOderByArcAngle(allPoints, arcCurves);
            var orderPoints = arcOrderPoints.OrderBy(c => c.Value).Select(c => c.Key).ToList();
            var sp = orderPoints.First();
            var ep = orderPoints.Last();
            var spVector = (sp - arcCurves.Center).GetNormal();
            var epVector = (ep - arcCurves.Center).GetNormal();
            var spAngle = arcXVector.GetAngleTo(spVector, arcNormal);
            var epAngle = arcXVector.GetAngleTo(epVector, arcNormal);

            var totalAngle = epAngle - spAngle;
            var startAngle = isMinSapce ? Math.PI * 5 / 180 : (totalAngle / (fanCount * 2));
            var angleSpace = totalAngle / fanCount;
            var innerCircle = new Circle(center, arcNormal, layoutInnerRadius);

            var tempPloylines = new List<Polyline>();
            while (true)
            {
                if (startAngle > totalAngle)
                    break;
                var startVector = arcXVector.RotateBy(startAngle+ spAngle, arcNormal);
                var startCenterPoint = center + startVector.MultiplyBy(layoutInnerRadius);
                var otherDir = arcNormal.CrossProduct(startVector);
                var startPoint = startCenterPoint - otherDir.MultiplyBy(fanRectangle.Width / 2);
                var endPoint = startCenterPoint + otherDir.MultiplyBy(fanRectangle.Width / 2);

                var tempPlines = CanLayoutArea(layoutPolylines, startPoint, endPoint, startVector, fanRectangle.MinLength, maxLength);
                if (tempPlines == null || tempPlines.Count < 1)
                    startAngle += Math.PI * 1 / 180;
                else
                {
                    startAngle += angleSpace;
                    tempPloylines.AddRange(tempPlines.Select(c => c.Value).ToList());
                }
            }
            return tempPloylines;
        }
  
        List<Polyline> CalcFanRectangle(DivisionArea divisionArea, List<Polyline> layoutPolylines, List<FanLayoutRect> nearLayoutFans, FanRectangle fanRectangle, int fanCount)
        {
            //弧形区域，每个风机的朝向都是不同的
            var tempPloylines = new Dictionary<Point3d, Polyline>();
            if (null == nearLayoutFans || nearLayoutFans.Count < 1)
                return tempPloylines.Select(c => c.Value).ToList();
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


            var arcCurves = divisionArea.AreaCurves.Where(c => c is Arc).First() as Arc;
            var center = arcCurves.Center;
            var arcNormal = arcCurves.Normal;
            var arcXVector = arcCurves.Ecs.CoordinateSystem3d.Xaxis;
            allPoints = allPoints.OrderBy(c => c.DistanceTo(center)).ToList();
            var layoutInnerRadius = allPoints.First().DistanceTo(center);
            var outRadius = allPoints.Last().DistanceTo(center);
            double dirMaxLength = outRadius - layoutInnerRadius;
            var arcOrderPoints = CircleArcUtil.PointOderByArcAngle(allPoints, arcCurves);
            var orderPoints = arcOrderPoints.OrderBy(c => c.Value).Select(c => c.Key).ToList();
            var sp = orderPoints.First();
            var ep = orderPoints.Last();
            var spVector = (sp - arcCurves.Center).GetNormal();
            var epVector = (ep - arcCurves.Center).GetNormal();
            var spAngle = arcXVector.GetAngleTo(spVector, arcNormal);
            var epAngle = arcXVector.GetAngleTo(epVector, arcNormal);

            var totalAngle = epAngle - spAngle;
            var angleSpace = totalAngle / fanCount;
            var innerCircle = new Circle(center, arcNormal, layoutInnerRadius);

            var nearFanOrders = CircleArcUtil.PointOderByArcAngle(nearFanPoints, arcCurves);
            nearFanPoints = nearFanOrders.OrderBy(c => c.Value).Select(c => c.Key).ToList();
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
                            var fanNormal = (point - center).GetNormal();
                            var startPoint =   CircleArcUtil.PointToCircle(point,innerCircle);
                            var sideDir = arcNormal.CrossProduct(fanNormal);
                            startPoint -= sideDir.MultiplyBy(fanRectangle.Width / 2);
                            var endPoint = startPoint + sideDir.MultiplyBy(fanRectangle.Width);
                            var tempPlines = CanLayoutArea(layoutPolylines, startPoint, endPoint, fanNormal, fanRectangle.MinLength, dirMaxLength);
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
                                    if (Math.Abs((item.Key - keyValue.Key).DotProduct(sideDir)) < fanRectangle.MinLength)
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
                            var fanNormal = (point - center).GetNormal();
                            var startPoint = CircleArcUtil.PointToCircle(point, innerCircle);
                            var sideDir = arcNormal.CrossProduct(fanNormal);
                            startPoint -= sideDir.MultiplyBy(fanRectangle.Width / 2);
                            var endPoint = startPoint + sideDir.MultiplyBy(fanRectangle.Width);
                            var tempPlines = CanLayoutArea(layoutPolylines, startPoint, endPoint, fanNormal, fanRectangle.MinLength, dirMaxLength);
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
                                    if (Math.Abs((item.Key - keyValue.Key).DotProduct(sideDir)) < fanRectangle.MinLength)
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
                    var thisPLines = new Dictionary<Point3d, Polyline>();
                    var targetPoint = new Point3d();
                    foreach (var point in nearFanPoints)
                    {
                        if (hisPoints.Any(c => c.DistanceTo(point) < 1))
                            continue;
                        var fanNormal = (point - center).GetNormal();
                        var startPoint = CircleArcUtil.PointToCircle(point, innerCircle);
                        var sideDir = arcNormal.CrossProduct(fanNormal);
                        startPoint -= sideDir.MultiplyBy(fanRectangle.Width / 2);
                        var endPoint = startPoint + sideDir.MultiplyBy(fanRectangle.Width);
                        var tempPlines = CanLayoutArea(layoutPolylines, startPoint, endPoint, fanNormal, fanRectangle.MinLength, maxLength);
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
                            if (tempPloylines.Any(c => Math.Abs((c.Key - pline.Key).DotProduct(sideDir)) < fanRectangle.MinLength))
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
                                thisPLines.Add(pline.Key, pline.Value);
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

        void AdjustmentFanRectSize()
        {
            var allGroupIds = _allGroupPoints.Select(c => c.Key).ToList();
            foreach (var groupId in allGroupIds)
            {
                var thisGroupAreaFans = _roomIntersectAreas.Where(c => c.GroupId == groupId && c.divisionArea.IsArc).ToList();
                if (thisGroupAreaFans.Count < 1)
                    continue;
                
                //获取同圆心的弧形区域
                while (thisGroupAreaFans.Count > 0) 
                {
                    var first = thisGroupAreaFans.First();
                    thisGroupAreaFans.Remove(first);
                    var arc = first.divisionArea.AreaCurves.OfType<Arc>().First();
                    var thisArcAreas = new List<DivisionRoomArea>();
                    foreach (var item in thisGroupAreaFans) 
                    {
                        var thisArc =item.divisionArea.AreaCurves.OfType<Arc>().First();
                        if (arc.Center.DistanceTo(thisArc.Center) > 1)
                            continue;
                        thisArcAreas.Add(item);
                    }
                    foreach (var item in thisArcAreas)
                        thisGroupAreaFans.Remove(item);
                    thisArcAreas.Add(first);
                    CalaAlignment(thisArcAreas, arc.Center);
                }
            }
        }
        void CalaAlignment(List<DivisionRoomArea> targetAreas,Point3d arcCenter) 
        {
            //根据最长长度进行对齐调整
            var maxRowCount = targetAreas.Max(c => c.RowCount);
            var hisFans = new List<string>();
            for (int i = 0; i < maxRowCount; i++)
            {
                var thisGroupFans = new List<FanLayoutRect>();
                foreach (var area in targetAreas)
                {
                    foreach (var fanRes in area.FanLayoutAreaResult)
                    {
                        if (fanRes.RowId == i)
                            thisGroupFans.AddRange(fanRes.FanLayoutResult);
                    }
                }
                if (thisGroupFans.Count == 8) 
                {
                
                }
                var newValues = FanRectangleAlignment(thisGroupFans, arcCenter, _fanRectangle.MaxLength);
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
        Dictionary<string, FanLayoutRect> FanRectangleAlignment(List<FanLayoutRect> targetFanLayout, Point3d arcCenter, double maxLength)
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
                var lineInterCount = GetIntersectionLine(arcCenter,overLengthFans, maxLength);
                var maxCount = lineInterCount.Max(c => c.Value);
                if (maxCount > 2) 
                {
                    var thisCountLines = lineInterCount.Where(c => c.Value == maxCount).Select(c => c.Key).ToList();
                    overLengthBaseLine = thisCountLines.OrderBy(c => c.Length).First();
                }
                else
                    overLengthBaseLine = lineInterCount.Where(c => c.Value > 0).OrderByDescending(c => c.Value).FirstOrDefault().Key;
                var testLines = lineInterCount.Select(c => c.Key).ToList();
                var length = testLines.Select(c => c.Length).ToList();
                var changeRes = ChangeFanLengthByLine(arcCenter, overLengthBaseLine, overLengthFans);
                if (changeRes.Count > 0)
                {
                    //超长的风机长度处理
                    foreach (var keyValue in changeRes)
                    {
                        if (keyValue.Value.Length > maxLength)
                        {
                            var centerPoint = keyValue.Value.CenterPoint;
                            var dir = keyValue.Value.LengthDirctor;
                            var otherDir = Vector3d.ZAxis.CrossProduct(dir);
                            var poly = CenterToRect(centerPoint, dir, maxLength, otherDir, keyValue.Value.Width);
                            var newFan = new FanLayoutRect(poly, _fanRectangle.Width, keyValue.Value.LengthDirctor);
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
                var lineInterCount = GetIntersectionLine(arcCenter, otherLengthFans, -1);
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
                var changeRes = ChangeFanLengthByLine(arcCenter, otherLengthBaseLine, otherLengthFans);
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
                    var newFan = ChangeFanLengthByBaseLine(arcCenter,overLengthBaseLine, keyValue.Value);
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
                    var newFan = ChangeFanLengthByBaseLine(arcCenter, overLengthBaseLine, item);
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
                    var newFan = ChangeFanLengthByBaseLine(arcCenter,otherLengthBaseLine, keyValue.Value);
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
                    var newFan = ChangeFanLengthByBaseLine(arcCenter,otherLengthBaseLine, item);
                    if (null == newFan)
                        continue;
                    retRes.Add(item.FanId, newFan);
                }
            }
            return retRes;
        }
        Dictionary<Line, int> GetIntersectionLine(Point3d arcCenter,List<FanLayoutRect> targetFanLayout, double length = -1)
        {
            var lineInterCount = new Dictionary<Line, int>();
            for (int i = 0; i < targetFanLayout.Count; i++)
            {
                var firstFan = targetFanLayout[i];
                var intCount = 1;
                var firstLine = firstFan.LengthLines.First();
                Line interLine = null;
                for (int j = 0; j < targetFanLayout.Count; j++)
                {
                    if (i == j)
                        continue;
                    var line = LineIntersectionFanRect(arcCenter, firstLine, targetFanLayout[j], length);
                    if (null == line)
                        continue;
                    interLine = line;
                    intCount += 1;
                }
                interLine = interLine == null ? firstLine : interLine;
                lineInterCount.Add(interLine, intCount);
            }
            return lineInterCount;

        }

        Dictionary<string, FanLayoutRect> ChangeFanLengthByLine(Point3d arcCenter,Line standardLine, List<FanLayoutRect> targetFanLayout)
        {
            var retRes = new Dictionary<string, FanLayoutRect>();
            if (null == standardLine || targetFanLayout == null || targetFanLayout.Count < 1)
                return retRes;

            var radius1 = standardLine.StartPoint.DistanceTo(arcCenter);
            var radius2 = standardLine.EndPoint.DistanceTo(arcCenter);
            double interInnerRadius = radius1;
            double interOutRadius = radius2;
            if (radius1 > radius2)
            {
                interInnerRadius = radius2;
                interOutRadius = radius1;
            }
            var centerRadius = (radius1 + radius2) / 2;
            var centerCircle = new Circle(arcCenter, Vector3d.ZAxis, centerRadius);
            for (int j = 0; j < targetFanLayout.Count; j++)
            {
                var secondFan = targetFanLayout[j];
                var secondLine = secondFan.LengthLines.First();
                var thisRadius1 = secondLine.StartPoint.DistanceTo(arcCenter);
                var thisRadius2 = secondLine.EndPoint.DistanceTo(arcCenter);
                double thisInnerRadius = thisRadius1;
                double thisOutRadius = thisRadius2;
                if (thisRadius1 > thisRadius2)
                {
                    thisInnerRadius = thisRadius2;
                    thisOutRadius = thisRadius1;
                }
                if (thisInnerRadius > interOutRadius || thisOutRadius < interInnerRadius)
                    continue;
                var innerRadius = Math.Max(thisInnerRadius, interInnerRadius);
                var outRadius = Math.Min(thisOutRadius, interOutRadius);
                var dis = outRadius - innerRadius;
                if (standardLine.Length > dis && Math.Abs(standardLine.Length - dis) > 10)
                    continue;
                var otherDir = secondFan.FanDirection.CrossProduct(Vector3d.ZAxis);
                var newCenter = CircleArcUtil.PointToCircle(secondFan.CenterPoint, centerCircle);
                var poly = CenterToRect(newCenter, secondFan.FanDirection, dis, otherDir, secondFan.Width);
                var newFan = new FanLayoutRect(poly, _fanRectangle.Width, secondFan.FanDirection);
                newFan.FanDirection = secondFan.FanDirection;
                retRes.Add(secondFan.FanId, newFan);
            }
            return retRes;
        }

        FanLayoutRect ChangeFanLengthByBaseLine(Point3d arcCenter,Line baseLine, FanLayoutRect fanLayoutRect)
        {
            var thisLine = fanLayoutRect.LengthLines.First();
            var interLine = LineIntersectionFanRect(arcCenter,baseLine, fanLayoutRect,-1);
            if (null == interLine || interLine.Length < 2000)
                return null;
            if (Math.Abs(thisLine.Length - interLine.Length) < 5)
                return null;

            var radius1 = interLine.StartPoint.DistanceTo(arcCenter);
            var radius2 = interLine.EndPoint.DistanceTo(arcCenter);
            var centerRadius = (radius1 + radius2) / 2;
            var centerCircle = new Circle(arcCenter, Vector3d.ZAxis, centerRadius);

            var otherDir = fanLayoutRect.FanDirection.CrossProduct(Vector3d.ZAxis);
            var newCenter = CircleArcUtil.PointToCircle(fanLayoutRect.CenterPoint, centerCircle);
            var poly = CenterToRect(newCenter, fanLayoutRect.FanDirection, interLine.Length, otherDir, fanLayoutRect.Width);
            var newFan = new FanLayoutRect(poly, _fanRectangle.Width, fanLayoutRect.FanDirection);
            newFan.FanDirection = fanLayoutRect.FanDirection;
            return newFan;
        }
        Line LineIntersectionFanRect(Point3d arcCenter,Line baseLine, FanLayoutRect fanLayoutRect,double maxLength)
        {
            var radius1 = baseLine.StartPoint.DistanceTo(arcCenter);
            var radius2 = baseLine.EndPoint.DistanceTo(arcCenter);
            double interInnerRadius = radius1;
            double interOutRadius = radius2;
            if (radius1 > radius2)
            {
                interInnerRadius = radius2;
                interOutRadius = radius1;
            }
            double checkLength = maxLength > 0 ? maxLength : baseLine.Length;
            var secondLine = fanLayoutRect.LengthLines.First();
            var thisRadius1 = secondLine.StartPoint.DistanceTo(arcCenter);
            var thisRadius2 = secondLine.EndPoint.DistanceTo(arcCenter);
            double thisInnerRadius = thisRadius1;
            double thisOutRadius = thisRadius2;
            if (thisRadius1 > thisRadius2)
            {
                thisInnerRadius = thisRadius2;
                thisOutRadius = thisRadius1;
            }
            if (thisInnerRadius > interOutRadius || thisOutRadius < interInnerRadius)
                return null;
            var innerRadius = Math.Max(thisInnerRadius, interInnerRadius);
            var outRadius = Math.Min(thisOutRadius, interOutRadius);
            var dis = outRadius - innerRadius;
            if (dis - checkLength < 0 && Math.Abs(checkLength - dis) > 10)
                return null;
            var innerCircle = new Circle(arcCenter, Vector3d.ZAxis, innerRadius);
            CircleArcUtil.CircleIntersectLineSegment(innerCircle, baseLine, out List<Point3d> innerPoints);
            var outCircle = new Circle(arcCenter, Vector3d.ZAxis, outRadius);
            CircleArcUtil.CircleIntersectLineSegment(outCircle, baseLine, out List<Point3d> outPoints);
            if (innerPoints.Count == 1 && outPoints.Count == 1)
                return new Line(innerPoints.First(), outPoints.First());
            return null;
        }
    }
}
