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
    class LightInnerGroupConnect
    {
        Polyline _outPolyline;
        List<Polyline> _innerPolylines;
        List<Line> _innerLines;
        public LightInnerGroupConnect(Polyline outPolyline, List<Polyline> innerPolylines) 
        {
            _outPolyline = outPolyline;
            _innerPolylines = new List<Polyline>();
            _innerLines = new List<Line>();
            if (null != innerPolylines && innerPolylines.Count > 0) 
            {
                foreach(var item in innerPolylines) 
                {
                    if (null == item || item.Area < 100)
                        continue;
                    _innerPolylines.Add(item);
                }
                var liens = ThMEPLineExtension.ExplodeCurves(innerPolylines.ToCollection()).Where(c => c is Line).Cast<Line>().ToList();
                _innerLines.AddRange(liens);
            }
        }
        public List<LightGroup> GroupInnerConnect(List<List<Point3d>> groups,List<LightBlockReference> ligthBlocks,double outExtDistance=2000) 
        {
            var retGroups = new List<LightGroup>();
            foreach (var group in groups) 
            {
                if (group == null || group.Count < 1)
                    continue;
                var thisGroupBlocks = GroupLightBlock(group, ligthBlocks);
                if (thisGroupBlocks == null || thisGroupBlocks.Count < 1)
                    continue;
                //根据灯确定XY轴，
                var lightXAxis = GroupLightXYAxis(thisGroupBlocks, out Vector3d lightYAxis);
                //根据XY轴，灯的点位确定主线方向
                var xAxis = GroupXYAxis(group, lightXAxis, lightYAxis, out Vector3d yAxis);
                var lightGroup = new LightGroup(group, xAxis, yAxis);
                lightGroup.BlockReferences.AddRange(thisGroupBlocks);

                //根据灯的主线连接各个灯,先考虑直连
                var xGroups = new List<List<Point3d>>();
                var yGroups = new List<List<Point3d>>();
                var tempPoints = new List<Point3d>();
                tempPoints.AddRange(group);
                bool isDes = true;
                while (tempPoints.Count > 0)
                {
                    tempPoints = ThPointVectorUtil.PointsOrderByDirection(tempPoints, yAxis, isDes).ToList();
                    var basePoint = tempPoints.FirstOrDefault();
                    var thisLinePoints = GetLinePoints(basePoint, tempPoints, xAxis, outExtDistance);
                    tempPoints = tempPoints.Where(c => !thisLinePoints.Any(x => x.DistanceTo(c) < 1)).ToList();
                    thisLinePoints = ThPointVectorUtil.PointsOrderByDirection(thisLinePoints, xAxis, false).ToList();
                    xGroups.Add(thisLinePoints);
                    isDes = false;
                }
                foreach (var item in xGroups) 
                {
                    if (item.Count < 1)
                        continue;
                    if (item.Count > 1) 
                    {
                        lightGroup.InnerXGroups.Add(item);
                        continue;
                    }
                    tempPoints.Add(item.First());
                }
                while (tempPoints.Count > 0)
                {
                    var basePoint = tempPoints.FirstOrDefault();
                    var thisLinePoints = GetLinePoints(basePoint, group, yAxis, outExtDistance);
                    tempPoints = tempPoints.Where(c => !thisLinePoints.Any(x => x.DistanceTo(c) < 1)).ToList();
                    yGroups.Add(thisLinePoints);
                }
                foreach (var item in yGroups) 
                {
                    if (item == null || item.Count < 1)
                        continue;
                    lightGroup.InnerYGroups.Add(item);
                }
                retGroups.Add(lightGroup);
            }
            return retGroups;
        }
        List<LightBlockReference> GroupLightBlock(List<Point3d> groupPoints, List<LightBlockReference> ligthBlocks) 
        {
            var retBlocks = new List<LightBlockReference>();
            if (null == groupPoints || groupPoints.Count < 1 || ligthBlocks == null || ligthBlocks.Count < 1)
                return retBlocks;
            foreach(var item in groupPoints) 
            {
                foreach(var block in ligthBlocks) 
                {
                    if (item.DistanceTo(block.LightPosition2d) < 10)
                        retBlocks.Add(block);
                }
            }
            return retBlocks;
        }
        Vector3d GroupLightXYAxis(List<LightBlockReference> ligthBlocks,out Vector3d yAxis) 
        {
            //x轴为灯的长轴方向，y轴为灯的短轴方向，有些有多个方向的以多的为准
            var vectorCounts = new Dictionary<Vector3d, int>();
            foreach (var item in ligthBlocks) 
            {
                if (null == item)
                    continue;
                var vectorX = item.LightVector;
                bool isAdd = true;
                Vector3d key = new Vector3d();
                foreach (var keyValue in vectorCounts)
                {
                    var dot = keyValue.Key.DotProduct(vectorX);
                    if (Math.Abs(dot) > 0.999)
                    {
                        isAdd = false;
                        key = keyValue.Key;
                        break;
                    }
                }
                if (!isAdd)
                {
                    vectorCounts[key] += 1;
                }
                else 
                {
                    vectorCounts.Add(vectorX, 1);
                }
            }
            var xAxis = vectorCounts.OrderByDescending(c => c.Value).First().Key;
            yAxis = xAxis.CrossProduct(Vector3d.ZAxis).GetNormal();
            return xAxis;
        }

        Vector3d GroupXYAxis(List<Point3d> groupPoints, Vector3d lightXAxis,Vector3d lightYAxis,out Vector3d yAxis) 
        {
            var origin = groupPoints.FirstOrDefault();
            var orderX = ThPointVectorUtil.PointsOrderByDirection(groupPoints, lightXAxis,true, origin);
            var orderY = ThPointVectorUtil.PointsOrderByDirection(groupPoints, lightYAxis, true, origin);
            var prjXStart = orderX.First().PointToLine(origin, lightXAxis);
            var prjXEnd = orderX.Last().PointToLine(origin, lightXAxis);
            var prjYStart = orderY.First().PointToLine(origin, lightYAxis);
            var prjYEnd = orderY.Last().PointToLine(origin, lightYAxis);
            var xLength = (prjXEnd - prjXStart).Length;
            var yLength = (prjYStart - prjYEnd).Length;
            var xAxis = lightXAxis;
            if (xLength <= yLength)
                xAxis = lightYAxis;
            yAxis = xAxis.CrossProduct(Vector3d.ZAxis);
            return xAxis;
        }

        List<Point3d> GetLinePoints(Point3d basePoint, List<Point3d> allPoints, Vector3d orderDir, double outTolerance)
        {
            var retPoints = new List<Point3d>();
            retPoints.Add(basePoint);
            var checkPoint = basePoint;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     
            var tempPoints = new List<Point3d>();
            tempPoints.AddRange(allPoints);
            
            var hisPoints = new List<Point3d>();
            while (true) 
            {
                tempPoints = tempPoints.Where(c => !retPoints.Any(x => x.DistanceTo(c) < 1)).ToList();
                if(tempPoints ==null || tempPoints.Count<1)
                    break;
                tempPoints = ThPointVectorUtil.PointsOrderByDirection(tempPoints, orderDir, false);
                int startCount = retPoints.Count;
                foreach (var point in tempPoints)
                {
                    var prjPoint = point.PointToLine(checkPoint, orderDir);
                    if (prjPoint.DistanceTo(point) > outTolerance)
                        continue;
                    if (retPoints.Count > 0)
                    {
                        var nearPoint = retPoints.OrderBy(c => c.DistanceTo(point)).First();
                        var test = ThPointVectorUtil.PointsOrderByDirection(retPoints, orderDir, checkPoint);
                        if (nearPoint.DistanceTo(point) > 10000)
                        {
                            continue;
                        }
                    }
                    var line = new Line(checkPoint, point);
                    if (_innerLines != null && _innerLines.Any(c => c.LineIsIntersection(line)))
                        continue;
                    retPoints.Add(point);
                    checkPoint = point;
                }
                if (startCount == retPoints.Count)
                    break;
            }
           
            retPoints = ThPointVectorUtil.PointsOrderByDirection(retPoints, orderDir, false);
            return retPoints;
        }
    }
}
