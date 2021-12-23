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
        public CalcRegionAdjacent(List<DivisionArea> divisionAreas) 
        {
            _divisionAreas = new List<DivisionArea>();
            if (null == divisionAreas || divisionAreas.Count < 1)
                return;
            foreach (var item in divisionAreas)
                _divisionAreas.Add(item);
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
                xVector = yVector.CrossProduct(Vector3d.ZAxis);
                var xAxis = area.XVector;
                var curretnPoint = area.CenterPoint;
                var currentId = area.Uid;
                var dirNearGroups = new List<DivisionArea>();
                var dirNegateGroups = new List<DivisionArea>();
                var nearIds = new List<string>();
                foreach (var item in _divisionAreas)
                {
                    if (item.Uid == currentId || item.IsArc != area.IsArc)
                        continue;
                    if (nearIds.Any(c => c == item.Uid))
                        continue;
                    if (CheckDivisionAreas(area, item, yVector, true))
                        nearIds.Add(item.Uid);
                    else if(CheckDivisionAreas(area,item,xVector,true))
                         nearIds.Add(item.Uid);
                }
                if (nearIds.Count > 0)
                    resAdjacent.Add(area.Uid, nearIds);

            }
            return resAdjacent;
        }

        bool CheckDivisionAreas(DivisionArea division, DivisionArea checkArea, Vector3d checkDir,bool isTwoDir)
        {
            if (division.IsArc != checkArea.IsArc || division.Uid == checkArea.Uid)
                return false;
            if (division.CenterPoint.DistanceTo(checkArea.CenterPoint) > 11000)
                return false;
            //先判断是否可能相交，再进行详细的判断相邻关系
            if (!CheckAreaCanInster(division, checkArea))
                return false;
            //判断在方向上是否有相邻的区域
            if (division.IsArc)
            {
                //弧形区域计算扇形部分是否有共线,且共圆心
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
                        if (interArc == null || interArc.Length<10)
                            continue;
                        tempArcs.Add(arc);
                    }
                }
                if (tempArcs.Count < 1)
                    return false;
                foreach (var arc in tempArcs)
                {
                    var prjCenter = CircleArcUtil.PointToArc(division.CenterPoint,arc);
                    var tempVector = (division.CenterPoint - prjCenter).GetNormal();
                    var dot = tempVector.DotProduct(checkDir);
                    if (isTwoDir)
                        dot = Math.Abs(dot);
                    if (dot < 0.3)
                        continue;
                    return true;
                }
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
        public Dictionary<string, List<string>> GetDivisionAdjacentByVertical(Vector3d xVector)
        {
            var resAdjacent = new Dictionary<string, List<string>>();
            if (null == _divisionAreas || _divisionAreas.Count < 1)
                return resAdjacent;
            for (int i = 0; i < _divisionAreas.Count; i++)
            {
                var area = _divisionAreas[i];
                var currentPoint = area.CenterPoint;
                var currentId = area.Uid;
                var nearIds = new List<string>();
                foreach (var item in _divisionAreas)
                {
                    if (item.Uid == currentId || item.IsArc != area.IsArc)
                        continue;
                    if (nearIds.Any(c => c == item.Uid))
                        continue;
                    //矩形区域按照x轴分组
                    if (!area.IsArc && CheckDivisionAreas(area, item, xVector, true))
                        nearIds.Add(item.Uid);
                    //弧形区域按照周向分组
                    else if (area.IsArc && CheckDivisionAreasByVertical(area, item, true))
                        nearIds.Add(item.Uid);
                }
                if (nearIds.Count > 0)
                    resAdjacent.Add(area.Uid, nearIds);
            }
            return resAdjacent;
        }

        private bool CheckDivisionAreasByVertical(DivisionArea division, DivisionArea checkArea, bool isTwoDir)
        {
            if (division.CenterPoint.DistanceTo(checkArea.CenterPoint) > 10000)
                return false;
            //先判断是否可能相交，再进行详细的判断相邻关系
            if (!CheckAreaCanInster(division, checkArea))
                return false;
            //计算圆弧部分是否有共心,且区域中心-圆心的距离接近
            var tempArcs = new List<Arc>();
            var arc = division.AreaCurves.Where(c => c is Arc).First() as Arc;
            var checkarc = checkArea.AreaCurves.Where(c => c is Arc).First() as Arc;
            if (arc.Center.DistanceTo(checkarc.Center) > 1)
                return false;
            if (Math.Abs(division.CenterPoint.DistanceTo(arc.Center) - checkArea.CenterPoint.DistanceTo(arc.Center)) > 1000) 
                return false;
            //再检查是否相交
            var largerDivision = division.AreaPolyline.ToNTSPolygon().Buffer(100);
            if (largerDivision.Intersects(checkArea.AreaPolyline.ToNTSPolygon()))
                return true;
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
