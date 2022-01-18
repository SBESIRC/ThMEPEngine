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
    abstract class RoomLayoutFanBase
    {
        protected Polyline _roomPLine;
        protected List<Polyline> _roomInnerPLine;
        protected List<DivisionRoomArea> _roomIntersectAreas;
        protected AreaLayoutGroup _layoutGroup;
        protected double _roomLoad;
        protected double _roomUnitLoad;
        protected Vector3d _UCSXAxis;
        protected Vector3d _UCSYAxis;
        protected Vector3d _groupXVector;
        protected Vector3d _groupYVector;
        protected FanRectangle _fanRectangle;
        //protected string _firstGroupId;
        protected Dictionary<string, Point3d> _allGroupPoints;
        protected int _firstGroupIndex = -1;
        protected Dictionary<string, List<string>> _divisionAreaNearIds;
        protected List<Point3d> _allGroupCenterOrders;
        protected double _midFanSpace = 800.0;
        protected double _minStartDistane = 500;
        protected bool _changeLayoutDir = false;
        //一个区域内有可能会有多个UCS,不进行坐标系转换到XOY平面
        public RoomLayoutFanBase(Dictionary<string, List<string>> divisionAreaNearIds,Vector3d xAxis,Vector3d yAxis)
        {
            _divisionAreaNearIds = new Dictionary<string, List<string>>();
            _roomIntersectAreas = new List<DivisionRoomArea>();
            _allGroupCenterOrders = new List<Point3d>();
            _UCSXAxis = xAxis;
            _UCSYAxis = yAxis;
            foreach (var item in divisionAreaNearIds) 
            {
                _divisionAreaNearIds.Add(item.Key, item.Value);
            }
        }
        public void InitRoomData(Polyline roomOutPLine, List<Polyline> innerPLines, double roomLoad) 
        {
            _roomPLine = roomOutPLine;
            _roomInnerPLine = new List<Polyline>();
            _roomLoad = roomLoad;
            if (null != innerPLines && innerPLines.Count > 0)
                foreach (var item in innerPLines)
                    _roomInnerPLine.Add(item);
            var area = roomOutPLine.Area - _roomInnerPLine.Sum(c => c.Area);
            _roomUnitLoad = roomLoad / area;
        }
        protected void CalcRoomLoad(AreaLayoutGroup layoutGroup, bool isLayoutByVertical = false) 
        {
            _layoutGroup = layoutGroup;
            _roomIntersectAreas.Clear();
            _allGroupCenterOrders.Clear();
            _groupYVector = layoutGroup.FirstDir.Negate();
            _groupXVector = _groupYVector.CrossProduct(Vector3d.ZAxis);
            _allGroupPoints = new Dictionary<string, Point3d>();
            foreach (var item in layoutGroup.GroupDivisionAreas)
                _roomIntersectAreas.Add(item);
            _firstGroupIndex = layoutGroup.OrderGroupIds.IndexOf(layoutGroup.GroupFirstId);
            foreach (var item in layoutGroup.GroupCenterPoints) 
            {
                _allGroupCenterOrders.Add(item.Value);
                _allGroupPoints.Add(item.Key, item.Value);
            }
            foreach (var area in _roomIntersectAreas)
            {
                area.NeedFanCount = (int)Math.Ceiling(area.NeedLoad / _fanRectangle.Load);
                if (!isLayoutByVertical)
                    CalcLayoutArea(area, _fanRectangle, _groupYVector);
                else
                    CalcLayoutAreaByVertical(area, _fanRectangle, _groupYVector);
            }
        }
        protected void LayoutFanVent()
        {
            if (_fanRectangle.MinVentCount < 1 || _fanRectangle.VentRect == null)
                return;
            foreach (var item in _roomIntersectAreas)
            {
                if (null == item || item.FanLayoutAreaResult == null || item.FanLayoutAreaResult.Count < 1)
                    continue;
                foreach (var fanRes in item.FanLayoutAreaResult)
                {
                    foreach (var fanRect in fanRes.FanLayoutResult)
                    {
                        //判断可以放下几个
                        var lengthLine = fanRect.LengthLines.First();
                        var length = lengthLine.Length;
                        var centerPoint = fanRect.CenterPoint;
                        var canLayoutLength = length - _fanRectangle.VentRect.VentMinDistanceToStart - _fanRectangle.VentRect.VentMinDistanceToEnd;
                        int count = _fanRectangle.MaxVentCount;
                        while (true)
                        {
                            if (count < _fanRectangle.MinVentCount)
                                break;
                            var needLength = (count - 1) * _fanRectangle.VentRect.VentMinDistanceToPrevious;
                            if (needLength > canLayoutLength)
                            {
                                count -= 1;
                                continue;
                            }
                            break;
                        }
                        if (count < 1 || count < _fanRectangle.MinVentCount)
                            continue;
                        var startPoint = centerPoint + fanRect.FanDirection.Negate().MultiplyBy(length / 2);
                        if (count == 1)
                        {
                            //一个时放置末尾
                            var ventCenterDisToFan = length - _fanRectangle.VentRect.VentMinDistanceToEnd - _fanRectangle.FanDistanceToStart;
                            ventCenterDisToFan = IndoorFanDistance.DistanceToMultiple(ventCenterDisToFan, IndoorFanDistance.MultipleValue);
                            var ventCenter = startPoint + fanRect.FanDirection.MultiplyBy(ventCenterDisToFan + _fanRectangle.FanDistanceToStart);
                            fanRect.InnerVentRects.Add(new FanInnerVentRect(GetFanVentPolyline(ventCenter, fanRect.FanDirection)));
                        }
                        else
                        {
                            //>=2;开始结尾各一个，中间等分放置
                            var startDis = _fanRectangle.VentRect.VentMinDistanceToStart - _fanRectangle.FanDistanceToStart;
                            startDis = IndoorFanDistance.DistanceToMultiple(startDis, IndoorFanDistance.MultipleValue);
                            var startCenter = startPoint + fanRect.FanDirection.MultiplyBy(startDis + _fanRectangle.FanDistanceToStart);
                            fanRect.InnerVentRects.Add(new FanInnerVentRect(GetFanVentPolyline(startCenter, fanRect.FanDirection)));
                            var dis = canLayoutLength / (count - 1);
                            dis = IndoorFanDistance.DistanceToMultiple(dis, IndoorFanDistance.MultipleValue);
                            var currentPoint = startCenter + fanRect.FanDirection.MultiplyBy(dis);
                            while (currentPoint.DistanceTo(startCenter) < canLayoutLength + 100)
                            {
                                fanRect.InnerVentRects.Add(new FanInnerVentRect(GetFanVentPolyline(currentPoint, fanRect.FanDirection)));
                                currentPoint = currentPoint + fanRect.FanDirection.MultiplyBy(dis);
                                if (fanRect.FanDirection.Length < 0.8)
                                    break;
                            }
                        }
                    }
                }
            }
        }
        protected List<DeleteFan> CheckAndRemoveLayoutFan()
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
                return calcDelFans;
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
            return calcDelFans;
        }
        protected List<string> CheckAndAddLayoutFan() 
        {
            var addFanCellIds = new List<string>();
            var areaLoad = 0.0;
            var ucsArea = 0.0;
            foreach (var item in _roomIntersectAreas)
            {
                ucsArea += item.RealIntersectAreas.Sum(c => c.Area);
            }
            areaLoad = ucsArea * _roomUnitLoad;
            var layoutResultCheck = new LayoutResultCheck(_roomIntersectAreas, areaLoad, _fanRectangle.Load);
            var orderRowIds = _allGroupPoints.Select(c => c.Key).ToList();
            var addFans = layoutResultCheck.RowAddFan(orderRowIds, out List<LayoutRow> ucsRowFans);
            if (addFans.Count != orderRowIds.Count)
                return addFanCellIds;
            for (int i = 0; i < orderRowIds.Count; i++) 
            {
                var addCount = addFans[i];
                if (addCount < 1)
                    continue;
                var rowId = orderRowIds[i];
                var thisRowCells = ucsRowFans.Where(c => c.RowGroupId == rowId).First().RowCells;
                var cellDiff = thisRowCells.ToDictionary(c=>c.CellId,x=>x.CellLayoutDiffNeed);
                while (addCount > 0) 
                {
                    var cellId = cellDiff.OrderByDescending(c=>c.Value).First().Key;
                    addFanCellIds.Add(cellId);
                    foreach (var areaCell in _roomIntersectAreas)
                    {
                        if (areaCell.divisionArea.Uid != cellId)
                            continue;
                        cellDiff[cellId] -= _fanRectangle.Load;
                        areaCell.NeedFanCount += 1;
                        break;
                    }
                    addCount -= 1;
                }
            }
            return addFanCellIds;
        }
        protected Polyline GetFanVentPolyline(Point3d centerPoint, Vector3d fanDir)
        {
            var otherDir = fanDir.CrossProduct(Vector3d.ZAxis);
            var moveOffSetX = otherDir.MultiplyBy(_fanRectangle.VentRect.VentLength / 2);
            var moveOffSetY = fanDir.MultiplyBy(_fanRectangle.VentRect.VentWidth / 2);
            var rectPt1 = centerPoint + moveOffSetY;
            var rectPt2 = centerPoint - moveOffSetY;
            var pt1 = rectPt1 - moveOffSetX;
            var pt1End = rectPt1 + moveOffSetX;
            var pt2 = rectPt2 - moveOffSetX;
            var pt2End = rectPt2 + moveOffSetX;
            Polyline poly = new Polyline();
            poly.Closed = true;
            poly.ColorIndex = 2;
            poly.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(1, pt1End.ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(2, pt2End.ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(3, pt2.ToPoint2D(), 0, 0, 0);
            return poly;
        }
   
        protected Polyline CenterToRect(Point3d centerPoint, Vector3d lengthDir, double length, Vector3d widthDir, double width)
        {
            var pt1 = centerPoint + lengthDir.MultiplyBy(length / 2);
            var pt2 = centerPoint - lengthDir.MultiplyBy(length / 2);
            var moveOffSet = widthDir.MultiplyBy(width / 2);
            var newPt1 = pt1 + moveOffSet;
            var newPt1End = pt1 - moveOffSet;
            var newPt2 = pt2 + moveOffSet;
            var newPt2End = pt2 - moveOffSet;
            Polyline poly = new Polyline();
            poly.Closed = true;
            poly.AddVertexAt(0, newPt1.ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(1, newPt1End.ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(2, newPt2End.ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(3, newPt2.ToPoint2D(), 0, 0, 0);
            return poly;
        }
        protected List<DivisionRoomArea> CalcDivisionAreas(DivisionArea division, List<DivisionRoomArea> targetAreas, Vector3d checkDir)
        {
            var nearAreas = new List<DivisionRoomArea>();
            if (null == targetAreas || targetAreas.Count < 1)
                return nearAreas;
            foreach (var item in targetAreas)
            {
                if (item.divisionArea.Uid.Equals(division.Uid))
                    continue;
                var tempLines = new List<Line>();
                //判断在方向上是否有相邻的区域
                foreach (var curve in division.AreaCurves)
                {
                    if (!(curve is Line))
                        continue;
                    var line = curve as Line;
                    foreach (var targetCurve in item.divisionArea.AreaCurves)
                    {
                        if (!(targetCurve is Line))
                            continue;
                        var targetLine = targetCurve as Line;
                        IndoorFanCommon.FindIntersection(line, targetLine, out List<Point3d> intersecionPoints);
                        if (intersecionPoints == null || intersecionPoints.Count < 2)
                            continue;
                        if (intersecionPoints[0].DistanceTo(intersecionPoints[1]) < 1000)
                            continue;
                        tempLines.Add(line);
                    }
                }
                if (tempLines.Count < 1)
                    continue;
                foreach (var line in tempLines)
                {
                    var prjCenter = division.CenterPoint.PointToLine(line);
                    var tempVector = (division.CenterPoint - prjCenter).GetNormal();
                    if (tempVector.DotProduct(checkDir) < 0.3)
                        continue;
                    nearAreas.Add(item);
                    break;
                }
            }
            return nearAreas;
        }
        protected List<Line> LineTrimByPolylines(List<Line> targetLines, List<Polyline> polylines, bool isOut = false)
        {
            var trimLines = new List<Line>();
            if (null == targetLines || targetLines.Count < 1)
                return trimLines;
            if (null == polylines || polylines.Count < 1)
            {
                trimLines.AddRange(targetLines);
                return trimLines;
            }
            foreach (var item in targetLines)
            {
                var tempInnerLines = new List<Curve>();
                foreach (var pline in polylines)
                {
                    var tempLines = pline.Trim(item, isOut).OfType<Curve>().ToList();
                    if (tempLines.Count < 1)
                        continue;
                    tempInnerLines.AddRange(tempLines);
                }
                if (tempInnerLines.Count < 1)
                    continue;
                foreach (var curve in tempInnerLines)
                {
                    if (curve is Line line)
                        trimLines.Add(line);
                    else if (curve is Polyline pline)
                    {
                        trimLines.AddRange(IndoorFanCommon.GetPolylineCurves(pline).OfType<Line>().ToList());
                    }
                }
            }
            return trimLines;
        }

        protected Dictionary<Point3d, Polyline> CanLayoutArea(List<Polyline> layoutPlines, Point3d startPoint, Point3d endPoint, Vector3d vector, double minLength, double maxLength)
        {
            var polylines = new Dictionary<Point3d, Polyline>();
            var offSet = vector.MultiplyBy(maxLength + 100);
            var startLineSp = startPoint - offSet;
            var startLineEp = startPoint + offSet;
            var endLineSp = endPoint - offSet;
            var endLineEp = endPoint + offSet;
            //线获取房间框线内的
            var startLine = new Line(startLineSp, startLineEp);
            var endLine = new Line(endLineSp, endLineEp);
            var startInnerCurves = LineTrimByPolylines(new List<Line> { startLine }, new List<Polyline> { _roomPLine }, false);
            if (null == startInnerCurves || startInnerCurves.Count < 1)
                return polylines;
            var endInnerCurves = LineTrimByPolylines(new List<Line> { endLine }, new List<Polyline> { _roomPLine }, false);
            if (null == endInnerCurves || endInnerCurves.Count < 1)
                return polylines;
            //获取障碍轮廓线外的
            var spInnerLines = LineTrimByPolylines(startInnerCurves, layoutPlines, false);
            var epInnerLines = LineTrimByPolylines(endInnerCurves, layoutPlines, false);
            spInnerLines = LineTrimByPolylines(spInnerLines, _roomInnerPLine, true);
            epInnerLines = LineTrimByPolylines(epInnerLines, _roomInnerPLine, true);
            if (spInnerLines.Count < 1 || epInnerLines.Count < 1)
                return polylines;
            var lineSeg = new Dictionary<Point3d, Point3d>();
            foreach (var item in spInnerLines)
            {
                var sp = item.StartPoint;
                var ep = item.EndPoint;
                foreach (var target in epInnerLines)
                {
                    var prjSp = sp.PointToLine(target);
                    var prjEp = ep.PointToLine(target);
                    IndoorFanCommon.FindIntersection(new Line(prjSp, prjEp), target, out List<Point3d> tempPoints);
                    if (tempPoints == null || tempPoints.Count < 2)
                        continue;
                    var dis = tempPoints[0].DistanceTo(tempPoints[1]);
                    if (dis < minLength)
                        continue;
                    if (lineSeg.Any(c => c.Key.DistanceTo(tempPoints[0]) < 1))
                        continue;
                    lineSeg.Add(tempPoints[0], tempPoints[1]);
                }
            }
            foreach (var keyValue in lineSeg)
            {
                var pt1 = keyValue.Key;
                var pt2 = keyValue.Value;
                var prjSLine = pt1.PointToLine(startLine);
                var prjELine = pt1.PointToLine(endLine);
                var prj2SLine = pt2.PointToLine(startLine);
                var prj2ELine = pt2.PointToLine(endLine);
                var thisPoints = new List<Point3d> { prjSLine, prjELine, prj2SLine, prj2ELine };
                Polyline poly = new Polyline();
                poly.Closed = true;
                poly.AddVertexAt(0, prjSLine.ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(1, prjELine.ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(2, prj2ELine.ToPoint2D(), 0, 0, 0);
                poly.AddVertexAt(3, prj2SLine.ToPoint2D(), 0, 0, 0);
                var centerPoint = ThPointVectorUtil.PointsAverageValue(thisPoints);
                polylines.Add(centerPoint, poly);
            }
            return polylines;
        }
        protected List<DivisionRoomArea> GetNearDivisionAreas(DivisionArea division, Vector3d checkDir)
        {
            var nearAreas = new List<DivisionRoomArea>();
            var nearKeyValue = _divisionAreaNearIds.Where(c => c.Key == division.Uid).FirstOrDefault().Value;
            if (nearKeyValue == null || nearKeyValue.Count < 1)
                return nearAreas;
            foreach (var item in _roomIntersectAreas)
            {
                if (!nearKeyValue.Any(c => item.divisionArea.Uid == c))
                    continue;
                var centerDir = item.divisionArea.CenterPoint - division.CenterPoint;
                if (centerDir.DotProduct(checkDir) > 0.2)
                    nearAreas.Add(item);
            }
            return nearAreas;
        }
        protected List<DivisionRoomArea> GetNearDivisionAreasByRadius(DivisionArea division, Point3d center, bool dir)
        {
            var nearAreas = new List<DivisionRoomArea>();
            var nearKeyValue = _divisionAreaNearIds.Where(c => c.Key == division.Uid).FirstOrDefault().Value;
            var divisionRadius = division.CenterPoint.DistanceTo(center);
            var divisionAngle = Vector3d.XAxis.GetAngleTo(division.CenterPoint - center, Vector3d.ZAxis);
            if (nearKeyValue == null || nearKeyValue.Count < 1)
                return nearAreas;
            foreach (var item in _roomIntersectAreas)
            {
                if (!nearKeyValue.Any(c => item.divisionArea.Uid == c))
                    continue;
                var centerRadius = item.divisionArea.CenterPoint.DistanceTo(center);
                if (Math.Abs(divisionRadius - centerRadius) > 1000)
                    continue;
                var centerAngle = Vector3d.XAxis.GetAngleTo(item.divisionArea.CenterPoint - center, Vector3d.ZAxis);
                //dir为true，找顺时针（右边）；dir为false，找逆时针（左边）
                if ((dir && centerAngle < divisionAngle) || (!dir && centerAngle > divisionAngle))
                    nearAreas.Add(item);
            }
            return nearAreas;
        }
        protected void CalcLayoutArea(DivisionRoomArea divisionAreaFan, FanRectangle fanRectangle, Vector3d vector,bool calcFanCount =true)
        {
            var fanCount = calcFanCount?(int)Math.Ceiling(divisionAreaFan.NeedLoad / fanRectangle.Load):divisionAreaFan.NeedFanCount;
            divisionAreaFan.NeedFanCount = fanCount;
            if (fanCount < 1)
                return;
            //获取点
            var allPoints = new List<Point3d>();
            foreach (var item in divisionAreaFan.RoomLayoutAreas)
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
            bool isContinue = true;
            int columnCount = fanCount;
            while (isContinue)
            {
                if (columnCount < 1)
                    break;
                var width = columnCount * fanRectangle.Width + (columnCount - 1) * 800;
                if (width < otherDirMaxLength)
                    isContinue = false;
                else
                    columnCount -= 1;
            }
            int rowCount = (int)Math.Ceiling((double)fanCount / columnCount);
            if (columnCount < 1)
                return;
            divisionAreaFan.RowCount = rowCount;
            var spliteAreas = SpliteAreas(divisionAreaFan.RoomLayoutAreas, vector, rowCount);
            //根据行数，等分区域
            for (int i = 0; i < rowCount; i++)
            {
                var dir = i % 2 == 0 ? divisionAreaFan.GroupDir : divisionAreaFan.GroupDir.Negate();
                var areas = spliteAreas[i];
                var fanArealayout = new DivisionLayoutArea(areas);
                fanArealayout.LayoutDir = dir;
                fanArealayout.RowId = i;
                divisionAreaFan.FanLayoutAreaResult.Add(fanArealayout);
            }
        }
        List<List<Polyline>> SpliteAreas(List<Polyline> layoutPolylines, Vector3d dir, int rowCount)
        {
            var rowPolylines = new List<List<Polyline>>();
            var otherDir = dir.CrossProduct(Vector3d.ZAxis);
            if (rowCount < 2)
            {
                rowPolylines.Add(layoutPolylines);
                return rowPolylines;
            }
            var allPoints = new List<Point3d>();
            foreach (var item in layoutPolylines)
            {
                allPoints.AddRange(IndoorFanCommon.GetPolylinePoints(item));
            }
            //根据方向获取分割线位置
            var orderDirPoints = ThPointVectorUtil.PointsOrderByDirection(allPoints, dir, true);
            var orderOhterDirPoints = ThPointVectorUtil.PointsOrderByDirection(allPoints, otherDir, false);
            var maxDis = Math.Abs((orderDirPoints.Last() - orderDirPoints.First()).DotProduct(dir));
            var otherMaxDis = Math.Abs((orderOhterDirPoints.Last() - orderOhterDirPoints.First()).DotProduct(otherDir));
            var moveDis = maxDis / rowCount;
            var origin = orderDirPoints.Last();
            for (int i = 0; i < rowCount; i++)
            {
                var thisRowPlines = new List<Polyline>();
                var SLinePoint = origin + dir.MultiplyBy(moveDis * i);
                if (i > 0)
                    SLinePoint = SLinePoint + dir.MultiplyBy(200);
                var ELinePoint = origin + dir.MultiplyBy(moveDis * (i + 1));
                if (i < rowCount - 1)
                    ELinePoint = ELinePoint - dir.MultiplyBy(200);
                var sLine = new Line(SLinePoint - otherDir.MultiplyBy(otherMaxDis), SLinePoint + otherDir.MultiplyBy(otherMaxDis));
                var eLine = new Line(ELinePoint - otherDir.MultiplyBy(otherMaxDis), ELinePoint + otherDir.MultiplyBy(otherMaxDis));
                var curves = new List<Curve>();
                foreach (var pline in layoutPolylines)
                {
                    var tempSCurves = _roomPLine.Trim(sLine).OfType<Curve>().ToList();
                    var tempECurves = _roomPLine.Trim(eLine).OfType<Curve>().ToList();
                    curves.AddRange(tempSCurves);
                    curves.AddRange(tempECurves);
                    curves.AddRange(IndoorFanCommon.GetPolylineCurves(pline));
                }
                var objs = new DBObjectCollection();
                foreach (var curve in curves)
                {
                    objs.Add(curve);
                }
                var allPolylines = objs.PolygonsEx();
                var rowCenterPoints = new Dictionary<Point3d, Polyline>();
                foreach (Polyline pline in allPolylines)
                {
                    if (null == pline)
                        continue;
                    var centerPoint = IndoorFanCommon.PolylinCenterPoint(pline);
                    var tempDis = Math.Abs((centerPoint - origin).DotProduct(dir));
                    if (tempDis > moveDis * i && tempDis < (moveDis * (i + 1)))
                    {
                        rowCenterPoints.Add(centerPoint,pline);
                        thisRowPlines.Add(pline); 
                    }
                }
                var points = rowCenterPoints.Select(c => c.Key).ToList();
                points = ThPointVectorUtil.PointsOrderByDirection(points, dir, true);
                //var 
                //foreach (var key in points) 
                //{
                //    rowPolylines.Add(rowCenterPoints[key]);
                //}
                rowPolylines.Add(thisRowPlines);
            }
            return rowPolylines;
        }
        protected void CalcLayoutAreaByVertical(DivisionRoomArea divisionAreaFan, FanRectangle fanRectangle, Vector3d yAxis)
        {
            //该分割区域需要布置的风机数
            var fanCount = (int)Math.Ceiling(divisionAreaFan.NeedLoad / fanRectangle.Load);
            divisionAreaFan.NeedFanCount = fanCount;
            if (fanCount < 1)
                return;
            //获取点
            var allPoints = new List<Point3d>();
            foreach (var item in divisionAreaFan.RoomLayoutAreas)
                allPoints.AddRange(IndoorFanCommon.GetPolylinePoints(item));
            if (allPoints.Count < 3)
                return;
            //初始化区域信息（弧形区域至少有一条圆弧）
            var arc = divisionAreaFan.divisionArea.AreaCurves.Where(c => c is Arc).First() as Arc;
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
            var maxAngle = epAngle - spAngle;

            if (dirLength < fanRectangle.Width || maxAngle * innerRadius < fanRectangle.MinLength) 
                return;
            //行数=主方向能排列的最大行数
            int rowCount = Math.Min(fanCount, (int)Math.Floor((dirLength + 800) / (fanRectangle.Width + 800)));
            if (rowCount < 1) return;
            //列数=风机数/行数
            int columnCount = (int)Math.Ceiling((double)fanCount / rowCount);
            divisionAreaFan.ColumnCount = columnCount;
            //根据列数等分区域
            var splitAreas = SpliteAreasByVertical(divisionAreaFan, columnCount);
            for(int i = 0; i < columnCount; i++)
            {
                var dir = i % 2 == 0 ? divisionAreaFan.GroupDir : divisionAreaFan.GroupDir.Negate();
                var areas = splitAreas[i];
                var fanArealayout = new DivisionLayoutArea(areas);
                fanArealayout.LayoutDir = dir;
                fanArealayout.ColumnId = i;
                divisionAreaFan.FanLayoutAreaResult.Add(fanArealayout);
            }
        }
        List<List<Polyline>> SpliteAreasByVertical(DivisionRoomArea divisionArea, int columnCount)
        {
            var columnPolylines = new List<List<Polyline>>();
            if (columnCount < 2)
            {
                columnPolylines.Add(divisionArea.RoomLayoutAreas);
                return columnPolylines;
            }
            //获取点
            var allPoints = new List<Point3d>();
            foreach (var item in divisionArea.RoomLayoutAreas)
            {
                allPoints.AddRange(IndoorFanCommon.GetPolylinePoints(item));
            }
            //初始化区域信息
            var arc = divisionArea.divisionArea.AreaCurves.Where(c => c is Arc).First() as Arc;
            var center = arc.Center;
            var arcNormal = arc.Normal;
            var arcXVector = arc.Ecs.CoordinateSystem3d.Xaxis;
            //获取半径范围
            allPoints = allPoints.OrderBy(c => c.DistanceTo(center)).ToList();
            var innerRadius = allPoints.First().DistanceTo(center);
            var outRadius = allPoints.Last().DistanceTo(center);
            var maxRadius = outRadius - innerRadius;
            //获取角度范围
            var arcOrderPoints = CircleArcUtil.PointOderByArcAngle(allPoints, arc);
            var orderPoints = arcOrderPoints.OrderBy(c => c.Value).Select(c => c.Key).ToList();
            var spAngle = arcXVector.GetAngleTo((orderPoints.First() - center).GetNormal(), arcNormal);
            var epAngle = arcXVector.GetAngleTo((orderPoints.Last() - center).GetNormal(), arcNormal);
            var maxAngle = epAngle - spAngle;
            var disAngle = 200 / innerRadius;//相邻区域间距角
            //角度步长
            var moveAngle = maxAngle / columnCount;
            for(int i = 0; i < columnCount; i++)
            {
                var thisColumnPlines = new List<Polyline>();
                var startVector = arcXVector.RotateBy(spAngle + moveAngle * i + (i > 0 ? disAngle : 0), arcNormal);
                var endVector = arcXVector.RotateBy(spAngle + moveAngle * (i + 1) + (i < columnCount - 1 ? -disAngle : 0), arcNormal);
                //一列的两条直边
                var sLine = new Line(center + startVector.MultiplyBy(innerRadius*0.8), center + startVector.MultiplyBy(outRadius*1.2));
                var eLine = new Line(center + endVector.MultiplyBy(innerRadius*0.8), center + endVector.MultiplyBy(outRadius)*1.2);
                var curves = new List<Curve>();
                foreach (var pline in divisionArea.RoomLayoutAreas)
                {
                    var tempSCurves = _roomPLine.Trim(sLine).OfType<Curve>().ToList();
                    var tempECurves = _roomPLine.Trim(eLine).OfType<Curve>().ToList();
                    curves.AddRange(tempSCurves);
                    curves.AddRange(tempECurves);
                    curves.AddRange(IndoorFanCommon.GetPolylineCurves(pline));
                }
                var objs = new DBObjectCollection();
                foreach (var curve in curves)
                {
                    objs.Add(curve);
                }
                var allPolylines = objs.PolygonsEx();
                foreach (Polyline pline in allPolylines)
                {
                    if (null == pline)
                        continue;
                    var centerPoint = IndoorFanCommon.PolylinCenterPoint(pline);
                    var tempAngle = arcXVector.GetAngleTo((centerPoint - center).GetNormal(), arcNormal);
                    if (tempAngle > spAngle + moveAngle * i && tempAngle < (spAngle + moveAngle * (i + 1))) 
                        thisColumnPlines.Add(pline);
                }
                columnPolylines.Add(thisColumnPlines);
            }
            return columnPolylines;
        }
    }
}
