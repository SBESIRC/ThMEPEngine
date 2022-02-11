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
    class ArcAreaLayoutFanByVertical : RoomLayoutFanBase
    {
        public ArcAreaLayoutFanByVertical(Dictionary<string, List<string>> divisionAreaNearIds, Vector3d xAxis, Vector3d yAxis)
            : base(divisionAreaNearIds, xAxis, yAxis)
        {
        }
        public List<DivisionRoomArea> GetRectangle(AreaLayoutGroup layoutGroup, FanRectangle fanRectangle)
        {
            _roomIntersectAreas.Clear();
            if (!layoutGroup.IsArcGroup)
                return _roomIntersectAreas;
            _fanRectangle = fanRectangle;
            CalcRoomLoad(layoutGroup, true);
            if (_roomIntersectAreas.Count < 1)
                return _roomIntersectAreas;
            //初始排布风机外框
            LayoutFanRectFirstStep();
            //检查删除和增加风机
            CheckDeleteAddFan();
            //检查风机的排布方向是否需要修正
            CheckChangeLayoutDir();
            //周向对齐调整
            AdjustFanRectByRadius();
            //径向对齐调整
            AdjustFanRectByAngle();
            //排布风机风口
            LayoutFanVent();
            //调整风机风口
            AlignmentFanVent();
            return _roomIntersectAreas;
        }
        private void LayoutFanRectFirstStep()
        {
            _changeLayoutDir = false;
            //按照逆时针方向每列布置
            for (int j = 0; j < _allGroupCenterOrders.Count; j++)
            {
                int layoutCount = 0;
                var currentPoint = _allGroupCenterOrders[j];
                var currentGroupId = _allGroupPoints.Where(c => c.Value.DistanceTo(currentPoint) < 1).First().Key;
                foreach (var item in _roomIntersectAreas)
                {
                    if (item.GroupId != currentGroupId)
                        continue;
                    OneDivisionAreaCalcFanRectangle(item, _fanRectangle, true);
                    layoutCount += item.FanLayoutAreaResult.Sum(c => c.FanLayoutResult.Count);
                }
                if (j == _firstGroupIndex)
                    _changeLayoutDir = layoutCount < 1;
            }
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
                    OneDivisionAreaCalcFanRectangle(area, _fanRectangle,true);
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
                        OneDivisionAreaCalcFanRectangle(area, _fanRectangle, true);
                    }
                }
            }
        }
        private void AdjustFanRectByRadius()
        {
            //按照顺时针方向检查每列的对齐情况
            for (int j = _allGroupCenterOrders.Count - 1; j >= 0; j--)
            {
                var currentPoint = _allGroupCenterOrders[j];
                var currentGroupId = _allGroupPoints.Where(c => c.Value.DistanceTo(currentPoint) < 1).First().Key;
                foreach (var item in _roomIntersectAreas)
                {
                    if (item.GroupId == currentGroupId)
                        continue;
                    //忽略不需要布置的分割区域
                    if (item.FanLayoutAreaResult == null || item.FanLayoutAreaResult.Count < 1)
                        continue;
                    //两侧分割区域的布置结果
                    var rightNearFans = getNearFans(item.divisionArea, true);
                    var leftNearFans = getNearFans(item.divisionArea, false);
                    //忽略按照右边对齐的分割区域
                    if (rightNearFans.Count >= leftNearFans.Count)
                        continue;
                    //还没有正确对齐的分割区域重新布置
                    for (int i = 0; i < item.ColumnCount; i++)
                    {
                        var thisColumn = item.FanLayoutAreaResult.Where(c => c.ColumnId == i).FirstOrDefault();
                        if (thisColumn == null || thisColumn.FanLayoutResult == null || thisColumn.FanLayoutResult.Count < 1)
                            continue;
                        thisColumn.FanLayoutResult.Clear();
                    }
                    OneDivisionAreaCalcFanRectangle(item, _fanRectangle, false);
                }
            }
        }
        private void AdjustFanRectByAngle()
        {
            var AllGroupIds = _allGroupPoints.Select(c => c.Key).ToList();
            foreach (var groupId in AllGroupIds)
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
                    //每个组分别进行对齐
                    GroupAlignment(thisArcAreas, arc.Center);
                }
            }
        }
        private void AlignmentFanVent()
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
        //测试函数
        public List<Curve> getPolyline()
        {
            List<Curve> tmpList = new List<Curve>();
            int index = 0;
            ////查看分组结果
            //Dictionary<string, int> groupdic = new Dictionary<string, int>();
            //foreach (var roomIntersectArea in _roomIntersectAreas)
            //{
            //    var poly = roomIntersectArea.divisionArea.AreaPolyline;
            //    if (groupdic.ContainsKey(roomIntersectArea.GroupId) == false)
            //        groupdic.Add(roomIntersectArea.GroupId, index++);
            //    poly.ColorIndex = groupdic[roomIntersectArea.GroupId] % 8;
            //    tmpList.Add(poly);
            //}
            ////查看相邻结果
            foreach (var roomArea in _roomIntersectAreas)
            {
                index = (index + 1) % 8;
                var division = roomArea.divisionArea;
                var originPoint = division.CenterPoint;
                var nearAreas = GetNearDivisionAreasByRadius(division, division.ArcCenterPoint, true);
                if (nearAreas != null && nearAreas.Count >= 1)
                {
                    foreach (var item in nearAreas)
                    {
                        var targetPoint = item.divisionArea.CenterPoint;
                        var targetDir = (targetPoint - originPoint).GetNormal();
                        Polyline poly = new Polyline();
                        poly.Closed = true;
                        poly.ColorIndex = index;
                        poly.AddVertexAt(0, originPoint.ToPoint2D(), 0, 0, 0);
                        poly.AddVertexAt(1, (targetPoint + 1000 * targetDir.RotateBy(-Math.PI / 2, Vector3d.ZAxis)).ToPoint2D(), 0, 0, 0);
                        poly.AddVertexAt(2, (targetPoint + 1000 * targetDir.RotateBy(Math.PI / 2, Vector3d.ZAxis)).ToPoint2D(), 0, 0, 0);
                        tmpList.Add(poly);
                    }

                }
            }
            //查看分割区域
            //foreach (var area in _roomIntersectAreas)
            //{
            //    index = (index + 1) % 8;
            //    foreach (var area1 in area.FanLayoutAreaResult)
            //    {
            //        Point3d center = Point3d.Origin;
            //        foreach (var area2 in area1.LayoutAreas)
            //        {
            //            var areadb = area2;
            //            areadb.ColorIndex = index;
            //            tmpList.Add(area2);
            //            if (center == Point3d.Origin)
            //            {
            //                center = area2.GetCentroidPoint();
            //            }
            //        }
            //        var c = new Circle(center, Vector3d.ZAxis, 300);
            //        c.ColorIndex = index;
            //        tmpList.Add(c);
            //        Line line = new Line(center, center + area1.LayoutDir.MultiplyBy(1000));
            //        line.ColorIndex = index;
            //        tmpList.Add(line);
            //    }
            //}

            return tmpList;
        }

        List<FanLayoutRect> getNearFans(DivisionArea divisionArea, bool isRight)
        {
            var nearAreas = GetNearDivisionAreasByRadius(divisionArea, divisionArea.ArcCenterPoint, isRight);
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
            return nearFans;
        }
        //单个分割区域的排布
        private void OneDivisionAreaCalcFanRectangle(DivisionRoomArea divisionArea, FanRectangle fanRectangle, bool isAlignByRight)
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

            //初始化区域信息
            var arc = divisionArea.divisionArea.AreaCurves.Where(c => c is Arc).First() as Arc;
            var center = arc.Center;
            var arcNormal = arc.Normal;
            var arcXVector = arc.Ecs.CoordinateSystem3d.Xaxis;
            //获取半径范围
            allPoints = allPoints.OrderBy(c => c.DistanceTo(center)).ToList();
            var innerRadius = allPoints.First().DistanceTo(center);
            var outRadius = allPoints.Last().DistanceTo(center);
            var dirLength = outRadius - innerRadius;
            //获取角度范围
            var arcOrderPoints = CircleArcUtil.PointOderByArcAngle(allPoints, arc);
            var orderPoints = arcOrderPoints.OrderBy(c => c.Value).Select(c => c.Key).ToList();
            var spAngle = arcXVector.GetAngleTo((orderPoints.First() - center).GetNormal(), arcNormal);
            var epAngle = arcXVector.GetAngleTo((orderPoints.Last() - center).GetNormal(), arcNormal);
            var otherDirLength = (epAngle - spAngle) * innerRadius;//内弧长
            if (dirLength < fanRectangle.Width || otherDirLength < fanRectangle.MinLength)
                return;
            int columnCount = divisionArea.ColumnCount;
            if (columnCount < 1)
                return;
            //初始寻找顺时针方向的分割区域的布置结果
            var nearFans = getNearFans(divisionArea.divisionArea, isAlignByRight);

            var count = fanCount / columnCount;
            var tempCount = fanCount % columnCount;
            //根据列等分区域
            for (int i = 0; i < columnCount; i++)
            {
                var layoutDivision = divisionArea.FanLayoutAreaResult.Where(c => c.ColumnId == i).First();
                if (layoutDivision == null || layoutDivision.LayoutAreas == null || layoutDivision.LayoutAreas.Count() < 1)
                {
                    tempCount += count;
                    continue;
                }
                var thisColumnCount = count;
                if (tempCount > 0)
                {
                    thisColumnCount += 1;
                    tempCount -= 1;
                }
                var calcResult1 = new List<Polyline>();
                var calcResult2 = new List<Polyline>();
                var calcResult3 = new List<Polyline>();
                //根据相邻区域的进行对齐排布
                calcResult1 = CalcFanRectangle(divisionArea.divisionArea, layoutDivision.LayoutAreas, nearFans, fanRectangle, thisColumnCount);
                //还是不够时进行等分排布
                if (calcResult1.Count != thisColumnCount)
                    calcResult2 = CalcFanRectangle(divisionArea.divisionArea, layoutDivision.LayoutAreas, fanRectangle, thisColumnCount, false);
                //最后进行最小间距排布
                if (calcResult2.Count != thisColumnCount) 
                    calcResult3 = CalcFanRectangle(divisionArea.divisionArea, layoutDivision.LayoutAreas, fanRectangle, thisColumnCount, true);
                List<Polyline> calcResult = GetCalcResult(calcResult1, calcResult2, calcResult3);
                var thisColumnFans = new List<FanLayoutRect>();
                tempCount += thisColumnCount - calcResult.Count;
                var xVector = layoutDivision.LayoutDir;
                foreach (var pline in calcResult)
                {
                    var allLines = IndoorFanCommon.GetPolylineCurves(pline);
                    var lengthLine = allLines.OrderByDescending(c => c.GetLength()).First();
                    var lengthDir = (lengthLine.EndPoint - lengthLine.StartPoint).GetNormal();

                    var fanDir = xVector.Length > 0.5 ? (lengthDir.DotProduct(xVector) > 0 ? lengthDir : lengthDir.Negate()) : xVector;
                    var fanPline = new FanLayoutRect(pline, _fanRectangle.Width, lengthDir);
                    fanPline.FanDirection = fanDir;
                    thisColumnFans.Add(fanPline);
                }
                nearFans.Clear();
                nearFans.AddRange(thisColumnFans);
                layoutDivision.FanLayoutResult.AddRange(thisColumnFans);
            }
        }

        //相邻区域风机外框的对齐排布
        List<Polyline> CalcFanRectangle(DivisionArea divisionArea, List<Polyline> layoutPolylines, List<FanLayoutRect> nearLayoutFans, FanRectangle fanRectangle, int fanCount)
        {
            var tempPloylines = new Dictionary<Point3d, Polyline>();
            //如果没有邻居或者邻居排布数量小于当前列,那么不进行对齐排布
            if (null == nearLayoutFans || nearLayoutFans.Count < fanCount)
                return tempPloylines.Select(c => c.Value).ToList();
            int ans = 0;
            if (fanCount == 2)
                ans = 1;
            //获取邻居风机的排布中心
            var nearFanPoints = nearLayoutFans.Select(o => o.CenterPoint).ToList();

            //获取点
            var allPoints = new List<Point3d>();
            foreach (var item in layoutPolylines)
                allPoints.AddRange(IndoorFanCommon.GetPolylinePoints(item));
            //初始化区域信息
            var arc = divisionArea.AreaCurves.Where(c => c is Arc).First() as Arc;
            var center = arc.Center;
            var arcNormal = arc.Normal;
            var arcXVector = arc.Ecs.CoordinateSystem3d.Xaxis;
            //获取半径范围
            allPoints = allPoints.OrderBy(c => c.DistanceTo(center)).ToList();
            var innerRadius = allPoints.First().DistanceTo(center);
            var outRadius = allPoints.Last().DistanceTo(center);
            var dirLength = outRadius - innerRadius;
            //获取角度范围
            var arcOrderPoints = CircleArcUtil.PointOderByArcAngle(allPoints, arc);
            var orderPoints = arcOrderPoints.OrderBy(c => c.Value).Select(c => c.Key).ToList();
            var spAngle = arcXVector.GetAngleTo((orderPoints.First() - center).GetNormal(), arcNormal);
            var epAngle = arcXVector.GetAngleTo((orderPoints.Last() - center).GetNormal(), arcNormal);
            var otherDirLength = (epAngle - spAngle) * innerRadius;//内弧长
            var midAngle = (spAngle + epAngle) / 2;
            var vector = arcXVector.RotateBy(midAngle, arcNormal).GetNormal();//中心线
            nearFanPoints = nearFanPoints.OrderBy(o => o.DistanceTo(center)).ToList();
            //已使用过的邻居风机
            var hisPoints = new List<Point3d>();
            //如果邻居排布数量大于当前列所需要的排布数量
            if (nearFanPoints.Count > fanCount)
            {
                for (int i = 0; i < fanCount; i++)
                {
                    if (i % 2 == 0)//从下往上加
                    {
                        for (int j = 0; j < nearFanPoints.Count; j++)
                        {
                            var point = nearFanPoints[j];
                            if (hisPoints.Any(c => c.DistanceTo(point) < 1))
                                continue;
                            var fanRadius = point.DistanceTo(center);
                            var centerPoint = center + vector.MultiplyBy(fanRadius);
                            var startPoint = centerPoint - vector.MultiplyBy(fanRectangle.Width / 2);
                            var endPoint = centerPoint + vector.MultiplyBy(fanRectangle.Width / 2);
                            var tempPlines = CanLayoutArea(layoutPolylines, startPoint, endPoint, vector.RotateBy(Math.PI / 2, arcNormal), fanRectangle.MinLength, otherDirLength);
                            if (tempPlines == null || tempPlines.Count < 1)
                                continue;
                            hisPoints.Add(point);
                            bool isAdd = true;
                            foreach (var item in tempPlines)
                            {
                                if (!isAdd) break;
                                foreach (var keyValue in tempPloylines)
                                {
                                    if (item.Key.DistanceTo(keyValue.Key) < fanRectangle.Width + 800)
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
                    else//从上往下加
                    {
                        for (int j = nearFanPoints.Count - 1; j >= 0; j--)
                        {
                            var point = nearFanPoints[j];
                            if (hisPoints.Any(c => c.DistanceTo(point) < 1))
                                continue;
                            var fanRadius = point.DistanceTo(center);
                            var centerPoint = center + vector.MultiplyBy(fanRadius);
                            var startPoint = centerPoint - vector.MultiplyBy(fanRectangle.Width / 2);
                            var endPoint = centerPoint + vector.MultiplyBy(fanRectangle.Width / 2);
                            var tempPlines = CanLayoutArea(layoutPolylines, startPoint, endPoint, vector.RotateBy(Math.PI / 2, arcNormal), fanRectangle.MinLength, otherDirLength);
                            if (tempPlines == null || tempPlines.Count < 1)
                                continue;
                            hisPoints.Add(point);
                            bool isAdd = true;
                            foreach (var item in tempPlines)
                            {
                                if (!isAdd) break;
                                foreach (var keyValue in tempPloylines)
                                {
                                    if (item.Key.DistanceTo(keyValue.Key) < fanRectangle.Width + 800)
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
                        var fanRadius = point.DistanceTo(center);
                        var centerPoint = center + vector.MultiplyBy(fanRadius);
                        var startPoint = centerPoint - vector.MultiplyBy(fanRectangle.Width / 2);
                        var endPoint = centerPoint + vector.MultiplyBy(fanRectangle.Width / 2);
                        var tempPlines = CanLayoutArea(layoutPolylines, startPoint, endPoint, vector.RotateBy(Math.PI / 2, arcNormal), fanRectangle.MinLength, otherDirLength);
                        if (tempPlines == null || tempPlines.Count < 1)
                            continue;
                        var thisMaxLength = double.MinValue;
                        bool isAdd = true;
                        foreach (var item in tempPlines)
                        {
                            if (!isAdd)
                                break;
                            double length = item.Value.Area / fanRectangle.Width;
                            if (thisMaxLength < length)
                                thisMaxLength = length;
                            foreach (var keyValue in tempPloylines)
                            {
                                if (item.Key.DistanceTo(keyValue.Key) < fanRectangle.Width + 800)
                                {
                                    isAdd = false;
                                    break;
                                }
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
        //排布风机外框线
        List<Polyline> CalcFanRectangle(DivisionArea divisionArea, List<Polyline> layoutPolylines, FanRectangle fanRectangle, int fanCount, bool isMinSapce)
        {
            var allPoints = new List<Point3d>();
            foreach (var item in layoutPolylines)
            {
                allPoints.AddRange(IndoorFanCommon.GetPolylinePoints(item));
            }
            //初始化区域信息
            var arc = divisionArea.AreaCurves.Where(c => c is Arc).First() as Arc;
            var center = arc.Center;
            var arcNormal = arc.Normal;
            var arcXVector = arc.Ecs.CoordinateSystem3d.Xaxis;
            //获取半径范围
            allPoints = allPoints.OrderBy(c => c.DistanceTo(center)).ToList();
            var innerRadius = allPoints.First().DistanceTo(center);
            var outRadius = allPoints.Last().DistanceTo(center);
            var dirLength = outRadius - innerRadius;
            //获取角度范围
            var arcOrderPoints = CircleArcUtil.PointOderByArcAngle(allPoints, arc);
            var orderPoints = arcOrderPoints.OrderBy(c => c.Value).Select(c => c.Key).ToList();
            var spAngle = arcXVector.GetAngleTo((orderPoints.First() - center).GetNormal(), arcNormal);
            var epAngle = arcXVector.GetAngleTo((orderPoints.Last() - center).GetNormal(), arcNormal);
            var otherDirLength = (epAngle - spAngle) * innerRadius;//内弧长
            var midAngle = (spAngle + epAngle) / 2;
            var vector = arcXVector.RotateBy(midAngle, arcNormal).GetNormal();//中心线
            var startDist = innerRadius + fanRectangle.Width / 2 + (isMinSapce ? 400 : (dirLength - fanRectangle.Width * fanCount) / (fanCount * 2));
            var stepDist = Math.Max(600 + fanRectangle.Width, dirLength / fanCount);

            var tempPolylines = new List<Polyline>();
            while (true)
            {
                if (startDist > outRadius)
                    break;
                var centerPoint = center + vector.MultiplyBy(startDist);
                var startPoint = centerPoint - vector.MultiplyBy(fanRectangle.Width / 2);
                var endPoint = centerPoint + vector.MultiplyBy(fanRectangle.Width / 2);
                var tempPlines = CanLayoutArea(layoutPolylines, startPoint, endPoint, vector.RotateBy(Math.PI / 2, arcNormal), fanRectangle.MinLength, otherDirLength);
                if (tempPlines == null || tempPlines.Count < 1)
                    startDist += 50;
                else
                {
                    startDist += stepDist;
                    tempPolylines.AddRange(tempPlines.Select(c => c.Value).ToList());
                }
            }
            return tempPolylines;
        }
        //每列分割区域的径向对齐
        private void GroupAlignment(List<DivisionRoomArea> targetAreas, Point3d arcCenter)
        {
            var maxColumnCount = targetAreas.Max(c => c.ColumnCount);
            for (int i = 0; i < maxColumnCount; i++)
            {
                //获取每列分割区域的每个分组的风机
                var thisGroupFans1 = new List<FanLayoutRect>();//单风口的风机
                var thisGroupFans2 = new List<FanLayoutRect>();//二风口的风机
                foreach (var area in targetAreas)
                {
                    foreach (var fanRes in area.FanLayoutAreaResult)
                    {
                        //找到该列的风机
                        if (fanRes.ColumnId == i)
                        {
                            foreach(var fan in fanRes.FanLayoutResult)
                            {
                                //判断可以放下几个
                                var canLayoutLength = fan.Length - _fanRectangle.VentRect.VentMinDistanceToStart - _fanRectangle.VentRect.VentMinDistanceToEnd;
                                int count = Math.Min(_fanRectangle.MaxVentCount, (int)Math.Ceiling(canLayoutLength / _fanRectangle.VentRect.VentMinDistanceToPrevious));
                                if (count == 1)
                                    thisGroupFans1.Add(fan);
                                else if (count == 2)
                                    thisGroupFans2.Add(fan);
                            }
                        }
                    }
                }
                //分别对齐两种风机
                AlignFans(thisGroupFans1, arcCenter);
                AlignFans(thisGroupFans2, arcCenter);
                //短风机对齐长风机
            }
        }

        private void AlignFans(List<FanLayoutRect> thisGroupFans,Point3d arcCenter)
        {
            //风机数小于2，不需要对齐
            if (thisGroupFans.Count <= 1) return;
            List<bool> mask = new List<bool>(thisGroupFans.Count);
            //计算每列分割区域的每个分组的角度范围
            double maxSAngle = double.MinValue;
            double minEAngle = double.MaxValue;
            double minRadius = double.MaxValue;
            var maxLength = thisGroupFans.Max(o => o.Length);
            Point3d minCenter = Point3d.Origin;
            foreach (var fan in thisGroupFans)
            {
                var lenLine = fan.LengthLines.First();
                if (lenLine.Length < maxLength * 0.6)
                {
                    mask.Add(false);
                    continue;
                }
                var angle1 = Vector3d.XAxis.GetAngleTo(lenLine.StartPoint - arcCenter, Vector3d.ZAxis);
                var angle2 = Vector3d.XAxis.GetAngleTo(lenLine.EndPoint - arcCenter, Vector3d.ZAxis);
                double sAngle = Math.Min(angle1, angle2);
                double eAngle = Math.Max(angle1, angle2);
                //去掉没有交集的
                if (sAngle > minEAngle || eAngle < maxSAngle)
                {
                    mask.Add(false);
                    continue;
                }
                //计算需要对齐的距离圆弧最近的风机
                if (fan.CenterPoint.DistanceTo(arcCenter) < minRadius)
                {
                    minRadius = fan.CenterPoint.DistanceTo(arcCenter);
                    minCenter = fan.CenterPoint;
                }
                maxSAngle = Math.Max(maxSAngle, sAngle);
                minEAngle = Math.Min(minEAngle, eAngle);
                mask.Add(true);
            }
            //起始角度线
            var sVector = Vector3d.XAxis.RotateBy(maxSAngle, Vector3d.ZAxis).GetNormal();
            var sPoint = arcCenter + sVector.MultiplyBy(minRadius * 2);
            var sLine = new Line(arcCenter, sPoint);
            //终止角度线
            var eVector = Vector3d.XAxis.RotateBy(minEAngle, Vector3d.ZAxis).GetNormal();
            var ePoint = arcCenter + eVector.MultiplyBy(minRadius * 2);
            var eLine = new Line(arcCenter, ePoint);
            //过最近中心点的横线
            var centerLineVector = (minCenter - arcCenter).GetNormal().RotateBy(Math.PI / 2, Vector3d.ZAxis);
            var centerLine = new Line(minCenter + centerLineVector.MultiplyBy(minRadius), minCenter - centerLineVector.MultiplyBy(minRadius));
            //计算交点
            sPoint = ThCADCoreNTSLineExtension.Intersection(centerLine, sLine);
            ePoint = ThCADCoreNTSLineExtension.Intersection(centerLine, eLine);
            sVector = sPoint - minCenter;
            eVector = ePoint - minCenter;
            //如果超长，那么等比缩放
            if (sVector.Length + eVector.Length > _fanRectangle.MaxLength)
            {
                var k = _fanRectangle.MaxLength / (sVector.Length + eVector.Length);
                sVector = sVector.MultiplyBy(k);
                eVector = eVector.MultiplyBy(k);
            }
            centerLineVector = (minCenter - arcCenter).GetNormal();
            //新的中心线
            centerLine.Dispose();
            centerLine = new Line(arcCenter, arcCenter + centerLineVector.MultiplyBy(minRadius * 10));
            for(int i=0;i<thisGroupFans.Count;i++)
            {
                if (!mask[i]) continue;
                var fan = thisGroupFans[i];
                var thisCenterLineVector = (fan.CenterPoint - arcCenter).GetNormal().RotateBy(Math.PI / 2, Vector3d.ZAxis);
                var thisCenterLine = new Line(fan.CenterPoint + thisCenterLineVector.MultiplyBy(minRadius), fan.CenterPoint - thisCenterLineVector.MultiplyBy(minRadius));
                var center = ThCADCoreNTSLineExtension.Intersection(centerLine, thisCenterLine);
                var fanDir = fan.FanDirection;
                Polyline poly = new Polyline();
                poly.Closed = true;
                poly.AddVertexAt(0, (center + eVector - centerLineVector.MultiplyBy(_fanRectangle.Width / 2)).ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(0, (center + sVector - centerLineVector.MultiplyBy(_fanRectangle.Width / 2)).ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(0, (center + sVector + centerLineVector.MultiplyBy(_fanRectangle.Width / 2)).ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(0, (center + eVector + centerLineVector.MultiplyBy(_fanRectangle.Width / 2)).ToPoint2D(), 0, 0, 0);
                foreach (var fanPline in _roomIntersectAreas)
                {
                    foreach (var item in fanPline.FanLayoutAreaResult)
                    {
                        var tempValue = item.FanLayoutResult.Where(c => c.FanId == fan.FanId).FirstOrDefault();
                        if (tempValue == null)
                            continue;
                        item.FanLayoutResult.Remove(tempValue);
                        var tempFan = new FanLayoutRect(poly, _fanRectangle.Width, thisCenterLineVector);
                        tempFan.FanDirection = fanDir;
                        item.FanLayoutResult.Add(tempFan);
                    }
                }
                thisCenterLine.Dispose();
            }
        }
        //每列分割区域风口的径向对齐
        private void ArcAreaAlignmentFanVent(Point3d arcCener, string groupId, List<DivisionRoomArea> thisArcAreas)
        {
            var columnCount = thisArcAreas.Max(c => c.ColumnCount);
            var changeFanIds = new List<string>();
            for (int i = 0; i < columnCount;)
            {
                var thisColumnFans = new List<FanLayoutRect>();//当前列的风机
                var thisColumnVents = new List<FanInnerVentRect>();//当前列的风口
                foreach (var item in thisArcAreas)
                {
                    if (item.FanLayoutAreaResult == null || item.FanLayoutAreaResult.Count < 1)
                        continue;
                    var tempFans = item.FanLayoutAreaResult.Where(c => c.ColumnId == i).ToList();
                    foreach (var fan in tempFans)
                    {
                        thisColumnFans.AddRange(fan.FanLayoutResult);
                        thisColumnVents.AddRange(fan.FanLayoutResult.SelectMany(c => c.InnerVentRects));
                    }
                }
                if (thisColumnFans.Count < 2)
                {
                    i += 1;
                    continue;
                }
                var minCenter = thisColumnFans.Select(o => o.CenterPoint).OrderBy(o => o.DistanceTo(arcCener)).First();
                var centerAngle = Vector3d.YAxis.GetAngleTo(minCenter - arcCener, Vector3d.ZAxis);
                thisColumnFans = thisColumnFans.OrderBy(c => c.InnerVentRects.Count).ToList();
                string changeId = "";
                Point3d newPoint = new Point3d();
                foreach (var item in thisColumnFans)
                {
                    if (!string.IsNullOrEmpty(changeId))
                        break;
                    //只计算一个风口的风机
                    if (item.InnerVentRects.Count != 1)
                        continue;
                    if (changeFanIds.Any(c => c == item.FanId))
                        continue;
                    var thisVent = item.InnerVentRects.First();
                    //计算可以布置的坐标范围和当前风口的坐标
                    var startPoint = item.CenterPoint - item.FanDirection.MultiplyBy(item.Length / 2);
                    var minPoint = startPoint + item.FanDirection.MultiplyBy(_fanRectangle.VentRect.VentMinDistanceToStart);
                    var maxPoint = startPoint + item.FanDirection.MultiplyBy(item.Length) - item.FanDirection.MultiplyBy(_fanRectangle.VentRect.VentMinDistanceToEnd);
                    //转换坐标系
                    minPoint = minPoint.RotateBy(-centerAngle, Vector3d.ZAxis, arcCener);
                    maxPoint = maxPoint.RotateBy(-centerAngle, Vector3d.ZAxis, arcCener);
                    var thisPoint = thisVent.CenterPoint.RotateBy(-centerAngle, Vector3d.ZAxis, arcCener);
                    var minDis = Math.Min(minPoint.X, maxPoint.X);
                    var maxDis = Math.Max(minPoint.X, maxPoint.X);
                    var thisDis = thisPoint.X;
                    //当前列的其他风口
                    var otherFanVents = thisColumnVents.Where(c => c.VentId != thisVent.VentId).ToList();
                    //如果存在某个风口在坐标范围内，那么将当前风口的位置对齐到该风口
                    foreach (var vent in otherFanVents)
                    {
                        var ventCenter = vent.CenterPoint.RotateBy(-centerAngle, Vector3d.ZAxis, arcCener);
                        var dis = ventCenter.X;
                        if (Math.Abs(dis - thisDis) < 1)
                            continue;
                        if (dis <= maxDis && dis >= minDis)
                        {
                            changeId = thisVent.VentId;
                            newPoint = new Point3d(dis, thisPoint.Y, 0).RotateBy(centerAngle, Vector3d.ZAxis, arcCener);
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
        }
        
        private List<Polyline> GetCalcResult(List<Polyline> calcResult1, List<Polyline> calcResult2, List<Polyline> calcResult3)
        {
            var calcResultCount = Math.Max(calcResult1.Count, Math.Max(calcResult2.Count, calcResult3.Count));
            if (calcResult1.Count == calcResultCount)
            {
                DisposeCalcResult(calcResult2);
                DisposeCalcResult(calcResult3);
                return calcResult1;
            }
            else if (calcResult2.Count == calcResultCount)
            {
                DisposeCalcResult(calcResult1);
                DisposeCalcResult(calcResult3);
                return calcResult2;
            }
            else
            {
                DisposeCalcResult(calcResult1);
                DisposeCalcResult(calcResult2);
                return calcResult3;
            }
        }
        private void DisposeCalcResult(List<Polyline> calcResult)
        {
            foreach (var poly in calcResult) poly.Dispose();
        }
    }
}
