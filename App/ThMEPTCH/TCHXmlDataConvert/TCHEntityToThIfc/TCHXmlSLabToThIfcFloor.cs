using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.TCHXmlModels.TCHBaseModels;
using ThMEPTCH.TCHXmlModels.TCHEntityModels;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPTCH.Model;
using GeometryExtensions;

namespace ThMEPTCH.TCHXmlDataConvert.TCHEntityToThIfc
{
    [TCHConvertAttribute("天正楼板转中间数据")]
    class TCHXmlSLabToThIfcFloor : TCHConvertBase
    {
        double ArcChord = 500;
        public TCHXmlSLabToThIfcFloor() 
        {
            AcceptTCHEntityTypes.Add(typeof(TCH_SLab));
        }
        public override List<object> ConvertToBuidingElement()
        {
            var thIfcObjs = new List<object>();
            if (null == TCHXmlEntities || TCHXmlEntities.Count < 1)
                return thIfcObjs;
            foreach (var item in TCHXmlEntities)
            {
                if (item is TCH_SLab tch_SLab)
                {
                    var pointStrings = tch_SLab.GetAllPoints();
                    var allPoints = GetAllFloorPoints(pointStrings);
                    allPoints = allPoints.OrderBy(c => c.Order).ToList();
                    var allCurves = PointsGetTargetCurves(allPoints);
                    var floorThick = tch_SLab.Flr_thick.GetDoubleValue();
                    var extVector = floorThick < 0 ? Vector3d.ZAxis : Vector3d.ZAxis.Negate();

                    var curves = new DBObjectCollection();
                    var tempLines = new List<Line>();
                    bool haveArc = false;
                    foreach (var curve in allCurves)
                    {
                        if (curve is Line line)
                        {
                            tempLines.Add(line);
                        }
                        else if (curve is Arc arc) 
                        {
                            haveArc = true;
                            var expObjs = new DBObjectCollection();
                            var outArcLines = arc.TessellateArcWithChord(ArcChord);
                            outArcLines.Explode(expObjs);
                            foreach (var obj in expObjs)
                            {
                                tempLines.Add(obj as Line);
                            }
                        }
                    }
                    foreach (var line in tempLines)
                    {
                        var eLine = line.ExtendLine(1);
                        curves.Add(eLine);
                    }
                    var pLines = curves.PolygonsEx();
                    if (pLines == null || pLines.Count < 1)
                    {
                        continue;
                    }
                    if (!haveArc) 
                    {
                        foreach (var curve in pLines)
                        {
                            if (curve is Polyline polyline)
                            {
                                var newPolygon = ThMPolygonTool.CreateMPolygon(polyline, new List<Curve>());
                                ThTCHSlab slab = new ThTCHSlab(newPolygon, Math.Abs(floorThick), extVector);
                                thIfcObjs.Add(slab);
                            }
                            else if (curve is MPolygon polygon)
                            {
                                ThTCHSlab slab = new ThTCHSlab(polygon, Math.Abs(floorThick), extVector);
                                thIfcObjs.Add(slab);
                            }
                        }
                        continue;
                    }
                    //再次还原圆弧
                    foreach (var curve in pLines) 
                    {
                        if (curve is Polyline polyline)
                        {
                            var tempCurves = GetThisPolyCurves(polyline, allCurves);
                            var newPLine = GetNewPolyline(polyline, tempCurves);
                            if (null != newPLine)
                            {
                                var newPolygon = ThMPolygonTool.CreateMPolygon(newPLine, new List<Curve>());
                                ThTCHSlab slab = new ThTCHSlab(newPolygon, Math.Abs(floorThick), extVector);
                                thIfcObjs.Add(slab);
                            }
                        }
                        else if (curve is MPolygon polygon) 
                        {
                            var tempAllLoops = polygon.Loops();
                            var allNewLoops = new List<Polyline>();
                            foreach (var pline in tempAllLoops) 
                            {
                                var tempCurves = GetThisPolyCurves(pline, allCurves);
                                var newPLine = GetNewPolyline(pline, tempCurves);
                                if (null != newPLine)
                                {
                                    allNewLoops.Add(newPLine);
                                }
                            }
                            if (allNewLoops.Count < 2)
                                continue;
                            allNewLoops = allNewLoops.OrderByDescending(c => c.Area).ToList();
                            var max = allNewLoops.First();
                            allNewLoops.RemoveAt(0);
                            var newPolygon = ThMPolygonTool.CreateMPolygon(max, allNewLoops.Cast<Curve>().ToList());
                            ThTCHSlab slab = new ThTCHSlab(newPolygon, Math.Abs(floorThick), extVector);
                            thIfcObjs.Add(slab);
                        }
                    }
                }
            }
            return thIfcObjs;
        }
        List<Curve> GetThisPolyCurves(Polyline polyline, List<Curve> targetCurves)
        {
            var allPoints = polyline.VerticesEx(ArcChord).OfType<Point3d>().ToList();
            var plineCurves = new List<Curve>();
            //获取所有起点和终点都在改点集内的线
            foreach (var curve in targetCurves)
            {
                var sp = curve.StartPoint;
                var ep = curve.EndPoint;
                sp = new Point3d(sp.X, sp.Y, 0);
                ep = new Point3d(ep.X, ep.Y, 0);
                if (allPoints.Any(c => c.DistanceTo(sp) < 5) && allPoints.Any(c => c.DistanceTo(ep) < 5))
                {
                    bool isAdd = true;
                    foreach (var check in plineCurves)
                    {
                        if ((check.StartPoint.DistanceTo(sp) < 5 && check.EndPoint.DistanceTo(ep) < 1) ||
                            (check.EndPoint.DistanceTo(sp) < 5 && check.StartPoint.DistanceTo(ep) < 1))
                            isAdd = false;
                    }
                    if (!isAdd)
                        continue;
                    plineCurves.Add(curve);
                }
            }
            return plineCurves;
        }
        Polyline GetNewPolyline(Polyline oldPolyline,List<Curve> targetCurves) 
        {
            var startLine = CalcFirstCurve(oldPolyline, targetCurves, out Curve rmCurve);
            targetCurves.Remove(rmCurve);
            var segments = new PolylineSegmentCollection();
            if (startLine is Line sLine)
                segments.Add(new PolylineSegment(sLine.StartPoint.ToPoint2D(), sLine.EndPoint.ToPoint2D()));
            else if (startLine is Arc sArc)
                segments.Add(new PolylineSegment(sArc.EndPoint.ToPoint2D(), sArc.StartPoint.ToPoint2D(), sArc.BulgeFromCurve(sArc.IsClockWise())));
            var startPoint = startLine.StartPoint;
            var endPoint = startLine.EndPoint;
            while (targetCurves.Count > 0)
            {
                var currentSp = endPoint;
                Curve currentLine = null;
                foreach (var curve in targetCurves)
                {
                    if (curve.StartPoint.DistanceTo(currentSp) < 5 || curve.EndPoint.DistanceTo(currentSp) < 5)
                    {
                        currentLine = curve;
                        break;
                    }
                }
                if (currentLine == null)
                {
                    break;
                }
                targetCurves.Remove(currentLine);
                bool isReverse = currentLine.EndPoint.DistanceTo(currentSp) < 5;
                var sPoint = currentLine.StartPoint;
                var ePoint = currentLine.EndPoint;
                if (isReverse)
                {
                    sPoint = currentLine.EndPoint;
                    ePoint = currentLine.StartPoint;
                }
                endPoint = ePoint;
                if (currentLine is Line)
                {
                    segments.Add(new PolylineSegment(sPoint.ToPoint2D(), ePoint.ToPoint2D()));
                }
                else if (currentLine is Arc arc)
                {
                    var bulge = arc.BulgeFromCurve(arc.IsClockWise());
                    bulge = isReverse ? -bulge : bulge;
                    segments.Add(new PolylineSegment(sPoint.ToPoint2D(), ePoint.ToPoint2D(), bulge));
                }
            }
            var temp = segments.Join(new Tolerance(2, 2));
            var newPLine = temp.First().ToPolyline();
            var outPL = newPLine.GetOffsetCurves(1);
            foreach (var item in outPL)
            {
                if (item is Polyline pl)
                {
                    var innerPL = pl.GetOffsetCurves(-1);
                    foreach (var newPl in innerPL)
                    {
                        newPLine = newPl as Polyline;
                    }
                }
            }
            return newPLine;
        }
        Curve CalcFirstCurve(Polyline polyline, List<Curve> targetCurves, out Curve rmCurve)
        {
            var allCurves = new List<Curve>();
            var expObjs = new DBObjectCollection();
            polyline.Explode(expObjs);
            foreach (var obj in expObjs)
            {
                allCurves.Add(obj as Curve);
            }
            var startLine = targetCurves.First();
            rmCurve = targetCurves.First();
            foreach (var item in targetCurves)
            {
                var sp = item.StartPoint;
                var ep = item.EndPoint;
                sp = new Point3d(sp.X, sp.Y, 0);
                ep = new Point3d(ep.X, ep.Y, 0);
                bool isBreak = false;
                if (item is Line line)
                {
                    foreach (var curve in allCurves)
                    {
                        if (curve.StartPoint.DistanceTo(sp) < 5 && curve.EndPoint.DistanceTo(ep) < 5)
                        {
                            startLine = item;
                            rmCurve = item;
                            isBreak = true;
                            break;
                        }
                        else if (curve.EndPoint.DistanceTo(sp) < 5 && curve.StartPoint.DistanceTo(ep) < 5)
                        {
                            startLine = new Line(item.EndPoint, item.StartPoint);
                            rmCurve = item;
                            isBreak = true;
                            break;
                        }
                    }
                }
                if (isBreak)
                    break;
            }
            return startLine;
        }
        List<Curve> PointsGetTargetCurves(List<FloorPoint> allPoints)
        {
            List<Curve> allCurves = new List<Curve>();
            if (null == allPoints || allPoints.Count < 1)
                return allCurves;
            for (int i = 0; i < allPoints.Count; i++)
            {
                var spNode = allPoints[i];
                var epNode = allPoints[0];
                if (i + 1 < allPoints.Count)
                {
                    epNode = allPoints[i + 1];
                }
                var sp = spNode.Point;
                var ep = epNode.Point;
                if (sp.DistanceTo(ep) < 1)
                    continue;
                var angle = spNode.Angle;
                if (Math.Abs(angle - 0) > 0.0001)
                {
                    var xAxis = (ep - sp).GetNormal();
                    var yAxis = Vector3d.ZAxis.CrossProduct(xAxis);
                    var length = sp.DistanceTo(ep);
                    var centerPt = sp + xAxis.MultiplyBy(length / 2);
                    var radius = length / (2 * Math.Sin(angle / 2));
                    var moveDis = (length / 2) / Math.Tan(angle / 2);
                    var arcCenter = centerPt + yAxis.MultiplyBy(moveDis);
                    var sDir = (sp - arcCenter).GetNormal();
                    var sAngle = Vector3d.XAxis.GetAngleTo(sDir, Vector3d.ZAxis);
                    var eAngle = sAngle + angle;
                    var arc = new Arc(arcCenter, Vector3d.ZAxis, radius, sAngle, eAngle);
                    allCurves.Add(arc);
                }
                else
                {
                    allCurves.Add(new Line(sp, ep));
                }
            }
            return allCurves;
        }
        List<FloorPoint> GetAllFloorPoints(List<XmlString> pointStrings) 
        {
            var floorPoints = new List<FloorPoint>();
            if (null == pointStrings || pointStrings.Count < 1)
                return floorPoints;
            Dictionary<int, string> pointDic = new Dictionary<int, string>();
            Dictionary<int, string> angleDic = new Dictionary<int, string>();
            Dictionary<int, string> showDic = new Dictionary<int, string>();
            foreach (var node in pointStrings)
            {
                var nodeName = node.name;
                if (string.IsNullOrEmpty(nodeName))
                    continue;
                var numStr = System.Text.RegularExpressions.Regex.Replace(nodeName, @"[^0-9]+", "");
                if (string.IsNullOrEmpty(numStr))
                    continue;
                var intNum = 0;
                int.TryParse(numStr, out intNum);
                if (nodeName.Contains("点"))
                {
                    if (!pointDic.Any(c => c.Key == intNum))
                        pointDic.Add(intNum, node.value);
                }
                else if (nodeName.Contains("角"))
                {
                    if (!angleDic.Any(c => c.Key == intNum))
                        angleDic.Add(intNum, node.value);
                }
                else
                {
                    if (!showDic.Any(c => c.Key == intNum))
                        showDic.Add(intNum, node.value);
                }
            }
            
            foreach (var keyValue in pointDic)
            {
                var ptIndex = keyValue.Key;
                var ptValue = keyValue.Value;
                FloorPoint floorPoint = new FloorPoint();
                floorPoint.Order = ptIndex;
                floorPoint.Point = XmConvertCommon.StringToPoint3d(ptValue);

                var angleStr = angleDic.Where(c => c.Key == ptIndex).FirstOrDefault().Value;
                if (!string.IsNullOrEmpty(angleStr))
                {
                    floorPoint.Angle = XmConvertCommon.StringToDouble(angleStr);
                }
                var showStr = showDic.Where(c => c.Key == ptIndex).FirstOrDefault().Value;
                if (string.IsNullOrEmpty(showStr))
                {
                    floorPoint.IsShow = XmConvertCommon.StringToInt(showStr);
                }
                floorPoints.Add(floorPoint);
            }
            return floorPoints;
        }
    }
    class FloorPoint
    {
        public int Order { get; set; }
        public Point3d  Point{ get; set; }
        public double Angle { get; set; }
        public int IsShow { get; set; }
    }
}
