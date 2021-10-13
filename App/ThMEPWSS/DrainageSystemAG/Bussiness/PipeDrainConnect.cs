using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.Common;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    /// <summary>
    /// 立管和地漏点位连线
    /// </summary>
    class PipeDrainConnect
    {
        private double _pipeConnectLineCornerRange = 200;//连线拐角处最大垂直距离
        private double _minConnectLineCornerRange = Math.Sqrt(100.0*100.0/2.0);//拐角连线最短距离
        private double _pointInAxisExtend = 10;
        private double _minLineLength = 5;
        private List<Point3d> _pointInXAxis;
        private List<Point3d> _pointInYAxis;
        private Point3d _centerPipePoint;
        private double _centerNoBayDis;
        private Dictionary<Point3d,double> _drainagePointNoBays;
        private Polyline _maxPolyLine;
        public PipeDrainConnect(Polyline outPolyline,Point3d pipeCenter,double noBayPipeDis, Dictionary<Point3d,double> connectPoints) 
        {
            _maxPolyLine = outPolyline;
            _centerPipePoint = pipeCenter;
            _centerNoBayDis = noBayPipeDis;
            _pointInXAxis = new List<Point3d>();
            _pointInYAxis = new List<Point3d>();
            _drainagePointNoBays = new Dictionary<Point3d, double>();
            if (null != connectPoints && connectPoints.Count > 0)
            {
                foreach (var keyValue in connectPoints)
                {
                    if (null == keyValue.Key )
                        continue;
                    var point = keyValue.Key;
                    if (_drainagePointNoBays.Any(c => c.Key.DistanceTo(point) < 1))
                        continue;
                    _drainagePointNoBays.Add(point,keyValue.Value);
                }    
            }
        }
        public List<Line> PipeDrainConnectByMainAxis(Vector3d xAxis)
        {
            var yAxis = Vector3d.ZAxis.CrossProduct(xAxis);
            List<Line> resLines = new List<Line>();
            if (null == _centerPipePoint || null == _drainagePointNoBays || _drainagePointNoBays.Count < 1)
                return resLines;
            var lines = _ConnectLineWithxAxisMianAxis(xAxis, yAxis);
            if (lines != null && lines.Count > 0)
                resLines.AddRange(lines);
            return resLines;
        }
        List<Line> _ConnectLineWithxAxisMianAxis(Vector3d xAxis, Vector3d yAxis) 
        {
            _XYAxisPoints(xAxis, yAxis);
            var resLines = new List<Line>();

            //轴线上的点认为可以直接连接
            var xAxisPoints = new List<Point3d>();
            var yAxisPoints = new List<Point3d>();
            _pointInXAxis.ForEach(c => { if (c != null) xAxisPoints.Add(c); });
            _pointInYAxis.ForEach(c => { if (c != null) yAxisPoints.Add(c); });

            //轴上有可以直接连接的点,先将直连的点位直接连接到主线上，也作为主线，再将其它数据点位连接到这些主线上
            var pointGroups = new List<List<Point3d>>();
            var tempPoints = new List<Point3d>();
            foreach (var keyValue in _drainagePointNoBays) 
                tempPoints.Add(keyValue.Key);
            pointGroups = PointGroupByYAxis(tempPoints,yAxis);

            //同轴线的先直线连接
            pointGroups = pointGroups.OrderByDescending(c => c.Count).ToList();
            var notConnectPoints = new List<Point3d>();
            var xMainLines = new List<Line>();
            var yMainLines = new List<Line>();
            var groupLines = new List<Line>();
            //连y轴可以上的线
            foreach (var group in pointGroups)
            {
                if (group.Count < 2)
                {
                    notConnectPoints.Add(group.First());
                    continue;
                }
                var pointInXMainLine = _CheckPointsInLine(_centerPipePoint, xAxis, group);
                var prjPoint = group.First().PointToLine(_centerPipePoint, xAxis);
                var pointOrders = PointVectorUtil.PointsOrderByDirection(group, yAxis, prjPoint);
                var maxPoints = pointOrders.Where(c => c.Value >= -0.001).OrderBy(c => c.Value).Select(c => c.Key).ToList();
                var minPoints = pointOrders.Where(c => c.Value <= 0.0001).OrderByDescending(c => c.Value).Select(c => c.Key).ToList();
                if (maxPoints.Count > 0)
                {
                    for (int i = maxPoints.Count - 1; i > 0; i--)
                    {
                        var addLine = new Line(maxPoints[i], maxPoints[i - 1]);
                        groupLines.Add(addLine);
                        if (pointInXMainLine)
                            xMainLines.Add(addLine);
                    }
                    if (!pointInXMainLine)
                        notConnectPoints.Add(maxPoints.First());
                }
                if (minPoints.Count > 0)
                {
                    for (int i = minPoints.Count - 1; i > 0; i--)
                    {
                        var addLine = new Line(minPoints[i], minPoints[i - 1]);
                        groupLines.Add(addLine);
                        if (pointInXMainLine)
                            xMainLines.Add(addLine);
                    }
                    if (!pointInXMainLine)
                        notConnectPoints.Add(minPoints.First());
                }
            }
            if (groupLines.Count > 0)
                resLines.AddRange(groupLines);

            //优先排远的数据
            notConnectPoints = notConnectPoints.OrderBy(c => c.DistanceTo(_centerPipePoint)).ToList();
            foreach (var point in notConnectPoints)
            {
                //这里不考虑轴上的点
                var prjxAxisPoint = point.PointToLine(_centerPipePoint, xAxis);
                var prjyAxisPoint = point.PointToLine(_centerPipePoint, yAxis);
                var xConnectPoint = GetConnectPoint(prjxAxisPoint, _centerPipePoint, xAxisPoints);
                var yConnectPoint = GetConnectPoint(prjyAxisPoint, _centerPipePoint, yAxisPoints);
                var xPrjDisToPoint = prjxAxisPoint.DistanceTo(point);
                var yPrjDisToPoint = prjyAxisPoint.DistanceTo(point);
                var xPrjDisToConnect = prjxAxisPoint.DistanceTo(xConnectPoint);
                var yPrjDisToConnect = prjyAxisPoint.DistanceTo(yConnectPoint);
                var tempXAxis = (_centerPipePoint - prjxAxisPoint).GetNormal();
                var tempYAxis = (prjxAxisPoint - point).GetNormal();

                //判断是否在主线点范围内
                var inyAxis = _CheckPrjPointInMainLine(point, yAxisPoints, yAxis);
                var inxAxis = _CheckPrjPointInMainLine(point, xAxisPoints, xAxis);
                if (null != _maxPolyLine) 
                {
                    if (!_maxPolyLine.Contains(prjxAxisPoint))
                        inyAxis = true;
                    else if (!_maxPolyLine.Contains(prjyAxisPoint))
                        inxAxis = true; 
                }
                var startNoBayDis = GetPointNoBayDistance(point);
                if (inyAxis && inxAxis)
                {
                    //离那个轴近使用那个
                    if ((xPrjDisToConnect + xPrjDisToPoint) > (yPrjDisToConnect + yPrjDisToPoint))
                    {
                        //连接到Y轴
                        var pipeNoBayDis = GetPointNoBayDistance(yConnectPoint);
                        var cornerDis = Math.Min(yPrjDisToConnect- pipeNoBayDis, yPrjDisToPoint- startNoBayDis);
                        if (cornerDis < _minConnectLineCornerRange) 
                        {
                            bool connet = yPrjDisToConnect < startNoBayDis || yPrjDisToPoint < pipeNoBayDis;
                            if (connet)
                            {
                                //距离太小不弯折，直接连接
                                resLines.Add(new Line(point, yConnectPoint));
                                continue;
                            }
                            cornerDis = Math.Min(yPrjDisToConnect, yPrjDisToPoint);
                        }
                        cornerDis = Math.Min(_pipeConnectLineCornerRange, cornerDis);
                        var createPoint = prjyAxisPoint + tempXAxis.MultiplyBy(cornerDis);
                        var pointY = prjyAxisPoint - tempYAxis.MultiplyBy(cornerDis);
                        resLines.Add(new Line(pointY, createPoint));
                        if (point.DistanceTo(pointY) > _minLineLength)
                            resLines.Add(new Line(point, pointY));
                        if (yAxisPoints.Any(c => c.DistanceTo(createPoint) < 1))
                            continue;
                        yAxisPoints.Add(createPoint);
                    }
                    else
                    {
                        //连接到X轴
                        var pipeNoBayDis = GetPointNoBayDistance(xConnectPoint);
                        var cornerDis = Math.Min(xPrjDisToConnect - pipeNoBayDis, xPrjDisToPoint - startNoBayDis);
                        if (cornerDis < _minConnectLineCornerRange)
                        {
                            bool connet = xPrjDisToConnect < startNoBayDis || xPrjDisToPoint < pipeNoBayDis;
                            if (connet)
                            {
                                //距离太小不弯折，直接连接
                                resLines.Add(new Line(point, xConnectPoint));
                                continue;
                            }
                            cornerDis = Math.Min(xPrjDisToConnect, xPrjDisToPoint);
                        }
                        cornerDis = Math.Min(_pipeConnectLineCornerRange, cornerDis);
                        var createPoint = prjxAxisPoint + tempXAxis.MultiplyBy(cornerDis);
                        var pointX = prjxAxisPoint - tempYAxis.MultiplyBy(cornerDis);
                        resLines.Add(new Line(pointX, createPoint));
                        if (point.DistanceTo(pointX) > _minLineLength) 
                        {
                            var line = new Line(point, pointX);
                            resLines.Add(line);
                            groupLines.Add(line);
                        }
                            
                        if (xAxisPoints.Any(c => c.DistanceTo(createPoint) < 1))
                            continue;
                        xAxisPoints.Add(createPoint);
                    }
                }
                else if (inyAxis)
                {
                    //连接到Y轴
                    var pipeNoBayDis = GetPointNoBayDistance(yConnectPoint);
                    var cornerDis = Math.Min(yPrjDisToConnect - pipeNoBayDis, yPrjDisToPoint - startNoBayDis);
                    if (cornerDis < _minConnectLineCornerRange)
                    {
                        //距离太小不弯折，直接连接
                        bool connet = yPrjDisToConnect < startNoBayDis || yPrjDisToPoint < pipeNoBayDis;
                        if (connet)
                        {
                            //距离太小不弯折，直接连接
                            resLines.Add(new Line(point, yConnectPoint));
                            continue;
                        }
                        cornerDis = Math.Min(yPrjDisToConnect, yPrjDisToPoint);
                    }
                    cornerDis = Math.Min(_pipeConnectLineCornerRange, cornerDis);
                    var createPoint = prjyAxisPoint + tempYAxis.MultiplyBy(cornerDis);
                    var pointY = prjyAxisPoint - tempXAxis.MultiplyBy(cornerDis);
                    resLines.Add(new Line(pointY, createPoint));
                    if (point.DistanceTo(pointY) > _minLineLength)
                        resLines.Add(new Line(point, pointY));
                    if (yAxisPoints.Any(c => c.DistanceTo(createPoint) < _minLineLength))
                        continue;
                    yAxisPoints.Add(createPoint);
                }
                else
                {
                    //这里优先连接到x轴上
                    //进一步判断是否可以连接到其它副线
                    var connToAssistLine = _ConnectToAssistLine(point, prjxAxisPoint, groupLines, out List<Line> addLines);
                    if (connToAssistLine)
                    {
                        if (addLines.Count > 0)
                            resLines.AddRange(addLines);
                        continue;
                    }
                    var pipeNoBayDis = GetPointNoBayDistance(xConnectPoint);
                    var cornerDis = Math.Min(xPrjDisToConnect - pipeNoBayDis, xPrjDisToPoint - startNoBayDis);
                    if (cornerDis < _minConnectLineCornerRange)
                    {
                        //距离太小不弯折，直接连接
                        bool connet = xPrjDisToConnect<startNoBayDis || xPrjDisToPoint<pipeNoBayDis;
                        if (connet)
                        {
                            //距离太小不弯折，直接连接
                            resLines.Add(new Line(point, xConnectPoint));
                            continue;
                        }
                        cornerDis = Math.Min(xPrjDisToConnect, xPrjDisToPoint);
                    }
                    cornerDis = Math.Min(_pipeConnectLineCornerRange, cornerDis);
                    var createPoint = prjxAxisPoint + tempXAxis.MultiplyBy(cornerDis);
                    var pointX = prjxAxisPoint - tempYAxis.MultiplyBy(cornerDis);
                    resLines.Add(new Line(pointX, createPoint));
                    if (point.DistanceTo(pointX) > _minLineLength)
                        resLines.Add(new Line(point, pointX));
                    if (xAxisPoints.Any(c => c.DistanceTo(createPoint) < _minLineLength))
                        continue;
                    xAxisPoints.Add(createPoint);

                }
            }
            //主线的计算，这里没有考虑线的方向
            if (xAxisPoints.Count > 1)
            {
                //x轴上有点，计算x轴的线
                var disOrder = PointVectorUtil.PointsOrderByDirection(xAxisPoints, xAxis, false, _centerPipePoint);
                for (int i = 0; i < disOrder.Count - 1; i++)
                    resLines.Add(new Line(disOrder[i], disOrder[i + 1]));
            }
            if (yAxisPoints.Count > 1)
            {
                //y轴上有点，计算y轴的线
                var disOrder = PointVectorUtil.PointsOrderByDirection(yAxisPoints, yAxis, false, _centerPipePoint);
                for (int i = 0; i < disOrder.Count - 1; i++)
                    resLines.Add(new Line(disOrder[i], disOrder[i + 1]));
            }
            return resLines;
        }
        Point3d GetConnectPoint(Point3d prjPoint,Point3d centerPoint,List<Point3d> targetPoints) 
        {
            if (null == targetPoints || targetPoints.Count < 1)
                return centerPoint;
            var line = new Line(prjPoint, centerPoint);
            List<Point3d> poins = new List<Point3d>();
            foreach (var item in targetPoints) 
            {
                if (PointVectorUtil.PointInLineSegment(item, line))
                    poins.Add(item);
            }
            if (poins.Count < 1)
                return centerPoint;
            return poins.OrderBy(c => c.DistanceTo(prjPoint)).First();
        }
        double GetPointNoBayDistance(Point3d point) 
        {
            var dis = 0.0;
            if (point.DistanceTo(_centerPipePoint) < 1)
            {
                dis = _centerNoBayDis;
            }
            else
            {
                foreach (var keyValue in _drainagePointNoBays) 
                {
                    if (keyValue.Key.DistanceTo(point) < 1)
                    {
                        dis = keyValue.Value;
                        break;
                    }
                }
            }
            return dis;
        }
        List<List<Point3d>> PointGroupByYAxis(List<Point3d> points,Vector3d yAxis) 
        {
            var pointGroups = new List<List<Point3d>>();
            var tempPoints = new List<Point3d>();
            foreach (var point in points)
                tempPoints.Add(point);
            while (tempPoints.Count > 0)
            {
                var basePoint = tempPoints.FirstOrDefault();
                List<Point3d> groupPoints = new List<Point3d>() { basePoint };
                foreach (var point in tempPoints)
                {
                    if (groupPoints.Any(c => c.DistanceTo(point) < 1))
                        continue;
                    if (!point.PointInLine(basePoint, yAxis))
                        continue;
                    groupPoints.Add(point);
                }
                pointGroups.Add(groupPoints);
                tempPoints = tempPoints.Where(c => !groupPoints.Any(x => x.DistanceTo(c) < 1)).ToList();
            }
            return pointGroups;
        }
        bool _ConnectToAssistLine(Point3d point,Point3d prjXPoint,List<Line> assistLines,out List<Line> addLines) 
        {
            bool conntAssist = false;
            addLines = new List<Line>();
            var nearLine = _NearMainLine(point, assistLines);
            if (null == nearLine)
                return conntAssist;
            var prjLinePoint = point.PointToLine(nearLine);
            var xDis = prjLinePoint.DistanceTo(point);
            if (xDis > prjXPoint.DistanceTo(point))
                return conntAssist;
            var lineSp = nearLine.StartPoint;
            var lineEp = nearLine.EndPoint;
            var tempY = (prjXPoint - point).GetNormal();
            var tempX = (point - prjLinePoint).GetNormal();
            var listPoints = new List<Point3d> { lineSp, lineEp };
            var orderPoints = PointVectorUtil.PointsOrderByDirection(listPoints, tempY, prjLinePoint);
            var nearAxisPoint = orderPoints.OrderBy(c => c.Value).Where(c => c.Value > 5).FirstOrDefault().Key;
            if (null == nearAxisPoint)
                return conntAssist;
            var yDis = prjLinePoint.DistanceTo(nearAxisPoint);
            var cornerDis = Math.Min(xDis, yDis);
            cornerDis = Math.Min(_pipeConnectLineCornerRange, cornerDis);
            var createPoint = prjLinePoint + tempY.MultiplyBy(cornerDis);
            var pointY = prjLinePoint + tempX.MultiplyBy(cornerDis);
            addLines.Add(new Line(pointY, createPoint));
            //连接到副线，进一步计算接入点
            if (!PointVectorUtil.PointInLineSegment(prjLinePoint, nearLine))
            {
                addLines.Add(new Line(createPoint, nearAxisPoint));
            }
            if (point.DistanceTo(pointY) > 5)
                addLines.Add(new Line(point, pointY));
            return true;
        }
        void _XYAxisPoints(Vector3d xAxis, Vector3d yAxis) 
        {
            _ClearData();
            _pointInXAxis.Add(_centerPipePoint);
            _pointInYAxis.Add(_centerPipePoint);
            foreach (var keyValue in _drainagePointNoBays)
            {
                var point = keyValue.Key;
                var prjXAxis = point.PointToLine(_centerPipePoint, xAxis);
                var prjYAxis = point.PointToLine(_centerPipePoint, yAxis);
                if (prjXAxis.DistanceTo(point) < _pointInAxisExtend)
                {
                    //离X轴很近,直接使用投影点
                    if (_pointInXAxis.Any(c => c.DistanceTo(prjXAxis) < _pointInAxisExtend))
                        continue;
                    _pointInXAxis.Add(prjXAxis);
                }
                else if (prjYAxis.DistanceTo(point) < _pointInAxisExtend)
                {
                    //离Y轴很近,直接使用投影点
                    if (_pointInYAxis.Any(c => c.DistanceTo(prjYAxis) < _pointInAxisExtend))
                        continue;
                    _pointInYAxis.Add(prjYAxis);
                }
            }
        }
        void _ClearData()
        {
            if (null == _pointInXAxis)
                _pointInXAxis = new List<Point3d>();
            _pointInXAxis.Clear();
            if (null == _pointInYAxis)
                _pointInYAxis = new List<Point3d>();
            _pointInYAxis.Clear();
        }
        bool _CheckPrjPointInMainLine(Point3d prjPoint,List<Point3d> mainAxisPoints,Vector3d mainAxis) 
        {
            if (null == mainAxisPoints || mainAxisPoints.Count < 2)
                return false;
            var orderPoints = PointVectorUtil.PointsOrderByDirection(mainAxisPoints, mainAxis,false,new Point3d());
            return prjPoint.PointInLineSegment(orderPoints.First(),orderPoints.Last());
        }
        bool _CheckPointsInLine(Point3d linePoint, Vector3d lineDirection, List<Point3d> checkPoints) 
        {
            bool inLine = false;
            if (null == checkPoints || checkPoints.Count < 2)
                return inLine;
            foreach (var point in checkPoints)
            {
                if (inLine)
                    break;
                inLine = point.PointInLine(linePoint, lineDirection);
            }
            return inLine;
        }
        Line _NearMainLine(Point3d point,List<Line> targetLines) 
        {
            Line line = null;
            if (null == targetLines || targetLines.Count < 1)
                return line;
            double dis = double.MaxValue;
            foreach (var li in targetLines) 
            {
                var closePoint = li.GetClosestPointTo(point, false);
                var liDis = closePoint.DistanceTo(point);
                if (liDis < dis) 
                {
                    line = li;
                    dis = liDis;
                }
            }
            return line;
        }
    }
}
