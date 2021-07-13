using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Common;

namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    /// <summary>
    /// 立管和地漏点位连线
    /// </summary>
    class PipeDrainConnect
    {
        private double _pipeConnectLineCornerRange = 200;//连线拐角处最大距离
        private List<Point3d> _pointInXAxis;
        private List<Point3d> _pointInYAxis;
        private Point3d _centerPipePoint;
        private List<Point3d> _drainagePoints;
        public PipeDrainConnect(Point3d pipeCenter,List<Point3d> connectPoints) 
        {
            _centerPipePoint = pipeCenter;
            _pointInXAxis = new List<Point3d>();
            _pointInYAxis = new List<Point3d>();
            _drainagePoints = new List<Point3d>();
            if (null != connectPoints && connectPoints.Count > 0)
            {
                foreach (var point in connectPoints)
                {
                    if (null == point || _drainagePoints.Any(c=>c.DistanceTo(point)<1))
                        continue;
                    _drainagePoints.Add(point);
                }    
            }
        }
        public List<Line> CenterConnect(Vector3d xAxis)
        {
            var yAxis = Vector3d.ZAxis.CrossProduct(xAxis);
            var cos45 = Math.Cos(Math.PI / 4);
            var xPoints = new List<Point3d>();
            var yPoints = new List<Point3d>();
            List<Line> lines = new List<Line>();
            foreach (var point in _drainagePoints)
            {
                var prjXAxis = point.PointToLine(_centerPipePoint, xAxis);
                var prjYAxis = point.PointToLine(_centerPipePoint, yAxis);
                var disToXAxis = prjXAxis.DistanceTo(point);
                var disToYAxis = prjYAxis.DistanceTo(point);
                var xDis = prjXAxis.DistanceTo(_centerPipePoint) + disToXAxis;
                var yDis = prjYAxis.DistanceTo(_centerPipePoint) + disToYAxis;
                var tempYAxis = (_centerPipePoint - prjYAxis).GetNormal();
                var tempXAxis = (_centerPipePoint - prjXAxis).GetNormal();
                var dir = (_centerPipePoint - point).GetNormal();
                var dot = dir.DotProduct(xAxis);
                if (disToXAxis < 10)
                {
                    //离X轴很近,直接使用投影点
                    if (xPoints.Any(c => c.DistanceTo(prjXAxis) < 1))
                        continue;
                    xPoints.Add(prjXAxis);
                }
                else if (disToYAxis < 10)
                {
                    //离Y轴很近,直接使用投影点
                    if (yPoints.Any(c => c.DistanceTo(prjYAxis) < 1))
                        continue;
                    yPoints.Add(prjYAxis);
                }
                else if (xDis <= yDis)
                {
                    //点连到X轴上，判断是否需要45度角连接
                    if (Math.Abs(dot) > cos45)
                    {
                        //45度连接,计算角度连接点
                        var createPoint = prjXAxis + tempXAxis.MultiplyBy(disToXAxis);
                        if (disToXAxis > _pipeConnectLineCornerRange)
                        {
                            createPoint = prjXAxis + tempXAxis.MultiplyBy(_pipeConnectLineCornerRange);
                            var point1 = prjXAxis - tempYAxis.MultiplyBy(_pipeConnectLineCornerRange);
                            lines.Add(new Line(point1, createPoint));
                            lines.Add(new Line(point, point1));
                        }
                        else
                        {
                            lines.Add(new Line(point, createPoint));
                        }
                        if (xPoints.Any(c => c.DistanceTo(createPoint) < 1))
                            continue;
                        xPoints.Add(createPoint);
                    }
                    else
                    {
                        //和X轴角度>45度,直接连接立管
                        lines.Add(new Line(point, _centerPipePoint));
                        if (xPoints.Any(c => c.DistanceTo(_centerPipePoint) < 1))
                            continue;
                        xPoints.Add(_centerPipePoint);
                    }
                }
                else
                {
                    //点连到Y轴上
                    if (Math.Abs(dot) > cos45)
                    {
                        //和Y轴角度>45度，直接连接立管
                        lines.Add(new Line(point, _centerPipePoint));
                        if (yPoints.Any(c => c.DistanceTo(_centerPipePoint) < 1))
                            continue;
                        yPoints.Add(_centerPipePoint);
                    }
                    else
                    {
                        //45度连接
                        var createPoint = prjYAxis + tempYAxis.MultiplyBy(disToYAxis);
                        if (disToYAxis > _pipeConnectLineCornerRange)
                        {
                            createPoint = prjYAxis + tempYAxis.MultiplyBy(_pipeConnectLineCornerRange);
                            var point1 = prjYAxis - tempXAxis.MultiplyBy(_pipeConnectLineCornerRange);
                            lines.Add(new Line(point1, createPoint));
                            lines.Add(new Line(point, point1));
                        }
                        else
                        {
                            lines.Add(new Line(point, createPoint));
                        }
                        if (yPoints.Any(c => c.DistanceTo(createPoint) < 1))
                            continue;
                        yPoints.Add(createPoint);
                    }
                }
            }
            //主线的计算，这里没有考虑线的方向
            if (yPoints.Count > 0)
            {
                if (!yPoints.Any(c => c.IsEqualTo(_centerPipePoint, new Tolerance(1, 1))))
                    yPoints.Add(_centerPipePoint);
                //y轴上有点，计算y轴的线
                var disOrder = PointVectorUtil.PointsOrderByDirection(yPoints, yAxis, false, _centerPipePoint);
                for (int i = 0; i < disOrder.Count - 1; i++)
                {
                    lines.Add(new Line(disOrder[i], disOrder[i + 1]));
                }

            }
            if (xPoints.Count > 0)
            {
                if (!xPoints.Any(c => c.IsEqualTo(_centerPipePoint, new Tolerance(1, 1))))
                    xPoints.Add(_centerPipePoint);
                //x轴上有点，计算x轴的线
                var disOrder = PointVectorUtil.PointsOrderByDirection(xPoints, xAxis, false, _centerPipePoint);
                for (int i = 0; i < disOrder.Count - 1; i++)
                {
                    lines.Add(new Line(disOrder[i], disOrder[i + 1]));
                }
            }
            return lines;
        }
        public List<Line> PipeDrainConnectByMainAxis(Vector3d xAxis)
        {
            var yAxis = Vector3d.ZAxis.CrossProduct(xAxis);
            List<Line> resLines = new List<Line>();
            if (null == _centerPipePoint || null == _drainagePoints || _drainagePoints.Count < 1)
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
            _drainagePoints.ForEach(c => { if (null != c) tempPoints.Add(c); });
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
            notConnectPoints = notConnectPoints.OrderByDescending(c => c.DistanceTo(_centerPipePoint)).ToList();
            foreach (var point in notConnectPoints)
            {
                //这里不考虑轴上的点
                var prjxAxisPoint = point.PointToLine(_centerPipePoint, xAxis);
                var prjyAxisPoint = point.PointToLine(_centerPipePoint, yAxis);
                var prjDisToPoint = prjxAxisPoint.DistanceTo(point);
                var prjDisToCenter = prjxAxisPoint.DistanceTo(_centerPipePoint);
                var tempXAxis = (_centerPipePoint - prjxAxisPoint).GetNormal();
                var tempYAxis = (prjxAxisPoint - point).GetNormal();

                //判断是否在主线点范围内
                var inyAxis = _CheckPrjPointInMainLine(point, yAxisPoints, yAxis);
                var inxAxis = _CheckPrjPointInMainLine(point, xAxisPoints, xAxis);
               
                if (inyAxis && inxAxis)
                {
                    //离那个轴近使用那个
                    if (prjDisToPoint > prjDisToCenter)
                    {
                        //连接到Y轴
                        var cornerDis = Math.Min(prjDisToPoint, prjDisToCenter);
                        cornerDis = Math.Min(_pipeConnectLineCornerRange, cornerDis);
                        var createPoint = prjyAxisPoint - tempXAxis.MultiplyBy(cornerDis);
                        var pointY = prjyAxisPoint + tempYAxis.MultiplyBy(cornerDis);
                        resLines.Add(new Line(pointY, createPoint));
                        if (point.DistanceTo(pointY) > 5)
                            resLines.Add(new Line(point, pointY));
                        if (yAxisPoints.Any(c => c.DistanceTo(createPoint) < 1))
                            continue;
                        yAxisPoints.Add(createPoint);
                    }
                    else
                    {
                        //连接到X轴
                        var cornerDis = Math.Min(prjDisToPoint, prjDisToCenter);
                        cornerDis = Math.Min(_pipeConnectLineCornerRange, cornerDis);
                        var createPoint = prjxAxisPoint + tempXAxis.MultiplyBy(cornerDis);
                        var pointX = prjxAxisPoint - tempYAxis.MultiplyBy(cornerDis);
                        resLines.Add(new Line(pointX, createPoint));
                        if (point.DistanceTo(pointX) > 5) 
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
                    var cornerDis = Math.Min(prjDisToPoint, prjDisToCenter);
                    cornerDis = Math.Min(_pipeConnectLineCornerRange, cornerDis);
                    var createPoint = prjyAxisPoint + tempXAxis.MultiplyBy(cornerDis);
                    var pointY = prjyAxisPoint - tempYAxis.MultiplyBy(cornerDis);
                    resLines.Add(new Line(pointY, createPoint));
                    if (point.DistanceTo(pointY) > 5)
                        resLines.Add(new Line(point, pointY));
                    if (yAxisPoints.Any(c => c.DistanceTo(createPoint) < 1))
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
                    var cornerDis = Math.Min(prjDisToPoint, prjDisToCenter);
                    cornerDis = Math.Min(_pipeConnectLineCornerRange, cornerDis);
                    var createPoint = prjxAxisPoint + tempXAxis.MultiplyBy(cornerDis);
                    var pointX = prjxAxisPoint - tempYAxis.MultiplyBy(cornerDis);
                    resLines.Add(new Line(pointX, createPoint));
                    if (point.DistanceTo(pointX) > 5)
                        resLines.Add(new Line(point, pointX));
                    if (xAxisPoints.Any(c => c.DistanceTo(createPoint) < 1))
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
            foreach (var point in _drainagePoints)
            {
                var prjXAxis = point.PointToLine(_centerPipePoint, xAxis);
                var prjYAxis = point.PointToLine(_centerPipePoint, yAxis);
                if (prjXAxis.DistanceTo(point) < 5)
                {
                    //离X轴很近,直接使用投影点
                    if (_pointInXAxis.Any(c => c.DistanceTo(prjXAxis) < 1))
                        continue;
                    _pointInXAxis.Add(prjXAxis);
                }
                else if (prjYAxis.DistanceTo(point) < 5)
                {
                    //离Y轴很近,直接使用投影点
                    if (_pointInYAxis.Any(c => c.DistanceTo(prjYAxis) < 1))
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
