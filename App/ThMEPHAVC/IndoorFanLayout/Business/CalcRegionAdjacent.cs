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
    class CalcRegionAdjacent
    {
        List<DivisionArea> _divisionAreas;
        ThCADCoreNTSSpatialIndex _areaSpatialIndex;
        Dictionary<Polyline, DivisionArea> _areaPLine;
        public CalcRegionAdjacent(List<DivisionArea> divisionAreas) 
        {
            _divisionAreas = new List<DivisionArea>();
            _areaPLine = new Dictionary<Polyline, DivisionArea>();
            if (null == divisionAreas || divisionAreas.Count < 1)
                return;
            var areaObjs = new DBObjectCollection();
            foreach (var item in divisionAreas)
            {
                if (item.AreaPolyline.Area < 100)
                    continue;
                _areaPLine.Add(item.AreaPolyline, item);
                areaObjs.Add(item.AreaPolyline);
                _divisionAreas.Add(item); 
            }
            _areaSpatialIndex = new ThCADCoreNTSSpatialIndex(areaObjs);
        }
        public Dictionary<string, List<string>> GetDivisionAdjacent() 
        {
            //区域的相邻关系，按照当前UCS的坐标系进行计算，计算上下左右四个方向的邻居
            //弧形区域按照内外弧方向,计算四个方向的邻居
            var resAdjacent = new Dictionary<string, List<string>>();
            if (null == _divisionAreas || _divisionAreas.Count < 1)
                return resAdjacent;
            for (int i = 0; i < _divisionAreas.Count; i++)
            {
                var area = _divisionAreas[i];
                var xVector = Vector3d.XAxis;
                var yVector = Vector3d.YAxis;
                if (area.IsArc)
                {
                    yVector = (area.CenterPoint - area.ArcCenterPoint).GetNormal();
                }
                else 
                {
                    yVector = area.XVector;
                }
                var buffArea = area.AreaPolyline.Buffer(10)[0] as Polyline;
                var checkAreas = new List<DivisionArea>();
                var interPLines = _areaSpatialIndex.SelectCrossingPolygon(buffArea);
                foreach (var item in interPLines)
                {
                    var pl = item as Polyline;
                    var addArea = _areaPLine[pl];
                    checkAreas.Add(addArea);
                }

                xVector = yVector.CrossProduct(Vector3d.ZAxis);
                var xAxis = area.XVector;
                var curretnPoint = area.CenterPoint;
                var currentId = area.Uid;
                var nearIds = new List<string>();
                foreach (var item in checkAreas)
                {
                    if (item.Uid == currentId || item.IsArc != area.IsArc)
                        continue;
                    if (nearIds.Any(c => c == item.Uid))
                        continue;
                    if (CheckDivisionAreas(area, item, yVector, true,false))
                        nearIds.Add(item.Uid);
                    else if(CheckDivisionAreas(area,item,xVector,true,true))
                         nearIds.Add(item.Uid);
                }
                if (nearIds.Count > 0)
                    resAdjacent.Add(area.Uid, nearIds);

            }
            return resAdjacent;
        }

        bool CheckDivisionAreas(DivisionArea division, DivisionArea checkArea, Vector3d checkDir,bool isTwoDir,bool isVertical)
        {
            if (division.IsArc != checkArea.IsArc || division.Uid == checkArea.Uid)
                return false;
            if (division.CenterPoint.DistanceTo(checkArea.CenterPoint) > 15000)
                return false;
            //先判断是否可能相交，再进行详细的判断相邻关系
            if (!CheckAreaCanInster(division, checkArea))
                return false;
            //判断在方向上是否有相邻的区域
            if (division.IsArc)
            {
                return CheckArcDivisionAreas(division, checkArea, checkDir,isTwoDir, isVertical);
            }
            else 
            {
                var tempLines = new List<Line>();
                foreach (var curve in division.AreaCurves)
                {
                    if (!(curve is Line))
                        continue;
                    var line = curve as Line;
                    foreach (var targetCurve in checkArea.AreaCurves)
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
                    return false;
                foreach (var line in tempLines)
                {
                    var prjCenter = division.CenterPoint.PointToLine(line);
                    var tempVector = (division.CenterPoint - prjCenter).GetNormal();
                    var dot = tempVector.DotProduct(checkDir);
                    if (isTwoDir)
                        dot = Math.Abs(dot);
                    if (dot < 0.3)
                        continue;
                    return true;
                }
            }
            return false;
        }
        private bool CheckArcDivisionAreas(DivisionArea division, DivisionArea checkArea, Vector3d checkDir, bool isTwoDir,bool isVertical)
        {
            //弧形区域计算扇形部分是否有共线,且共圆心,判断是极轴方向还是
            if (division.ArcCenterPoint.DistanceTo(checkArea.ArcCenterPoint) > 10)
                return false;
            if (isVertical)
            {
                //计算区域中心-圆心的距离接近
                if (Math.Abs(division.CenterPoint.DistanceTo(division.ArcCenterPoint) - checkArea.CenterPoint.DistanceTo(checkArea.ArcCenterPoint)) > 1000)
                    return false;
                //再检查是否相交
                var largerDivision = division.AreaPolyline.ToNTSPolygon().Buffer(100);
                if (largerDivision.Intersects(checkArea.AreaPolyline.ToNTSPolygon()))
                    return true;
            }
            else
            {
                var tempArcs = new List<Arc>();
                foreach (var curve in division.AreaCurves)
                {
                    if (curve is Line)
                        continue;
                    var arc = curve as Arc;
                    var dir = (division.CenterPoint - arc.Center).GetNormal();
                    foreach (var targetCurve in checkArea.AreaCurves)
                    {
                        if (targetCurve is Line)
                            continue;
                        var targetArc = targetCurve as Arc;
                        var interArc = CircleArcUtil.ArcIntersectArc(arc, targetArc);
                        if (interArc == null || interArc.Length < 10)
                            continue;
                        tempArcs.Add(arc);
                    }
                }
                if (tempArcs.Count < 1)
                    return false;
                foreach (var arc in tempArcs)
                {
                    var prjCenter = CircleArcUtil.PointToArc(division.CenterPoint, arc);
                    var tempVector = (division.CenterPoint - prjCenter).GetNormal();
                    var dot = tempVector.DotProduct(checkDir);
                    if (isTwoDir)
                        dot = Math.Abs(dot);
                    if (dot < 0.3)
                        continue;
                    return true;
                }
            }
            return false;
        }
        bool CheckAreaCanInster(DivisionArea division, DivisionArea checkArea) 
        {
            var firstAllPoints = IndoorFanCommon.GetPolylinePoints(division.AreaPolyline);
            var checkAllPoints = IndoorFanCommon.GetPolylinePoints(division.AreaPolyline);
            var firstMinx = firstAllPoints.Min(c => c.X);
            var firstMaxx = firstAllPoints.Max(c => c.X);
            var checkMinx = checkAllPoints.Min(c => c.X);
            var checkMaxx = checkAllPoints.Max(c => c.X);
            if ((firstMaxx < checkMinx && Math.Abs(firstMaxx-checkMinx)>1 )
                || (firstMinx> checkMaxx && Math.Abs(firstMinx - checkMaxx) > 1))
                return false;
            var firstMiny = firstAllPoints.Min(c => c.Y);
            var firstMaxy = firstAllPoints.Max(c => c.Y);
            var checkMiny = checkAllPoints.Min(c => c.Y);
            var checkMaxy = checkAllPoints.Max(c => c.Y);
            if ((firstMaxy < checkMiny && Math.Abs(firstMaxy - checkMiny) > 1)
                || (firstMiny > checkMaxy && Math.Abs(firstMiny - checkMaxy) > 1))
                return false;
            return true;
        }
    }
}
