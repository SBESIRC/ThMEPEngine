using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Algorithm.GraphDomain;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.LightConnect
{
    class LightGroupByRoute
    {
        List<GraphRoute> _graphRoutes;
        List<Line> _graphLines;
        List<List<LightGroup>> _lightGroups;
        List<Line> _trunkingLines;
        List<MaxGroupLight> _maxGroupLights;
        List<Line> _notCrossLines = new List<Line>();
        public LightGroupByRoute(List<List<LightGroup>> targetGroups, List<GraphRoute> graphRoutes, List<Line> graphLines, List<Line> trunkingLines) 
        {
            _graphRoutes = new List<GraphRoute>();
            _graphLines = new List<Line>();
            _lightGroups = new List<List<LightGroup>>();
            _trunkingLines = new List<Line>();
            _maxGroupLights = new List<MaxGroupLight>();
            if (null != trunkingLines && trunkingLines.Count > 0) 
            {
                foreach (var item in trunkingLines) 
                {
                    if (null == item)
                        continue;
                    _trunkingLines.Add(item);
                }
            }
            if (null != targetGroups && targetGroups.Count > 0)
            {
                foreach (var item in targetGroups)
                {
                    if (null == item || item.Count < 1)
                        continue;
                    _lightGroups.Add(item);
                }
            }
            if (null != graphRoutes && graphRoutes.Count > 0)
            {
                foreach (var item in graphRoutes)
                {
                    if (null == item)
                        continue;
                    _graphRoutes.Add(item);
                }
            }
            if (null != graphLines && graphLines.Count > 0)
            {
                foreach (var item in graphLines)
                {
                    if (null == item)
                        continue;
                    _graphLines.Add(item);
                }
            }
        }
        public void InitPolylines(Polyline outPolyine,List<Polyline> innerPolylines) 
        {
            _notCrossLines.Clear();
            _notCrossLines.AddRange(LightConnectUtil.TransPolylineToLine(outPolyine));
            if (null != innerPolylines && innerPolylines.Count > 0)
            {
                foreach (var pline in innerPolylines)
                    _notCrossLines.AddRange(LightConnectUtil.TransPolylineToLine(pline));
            }
        }
        public List<MaxGroupLight> GroupMergeConveter(double mergeMaxDis)
        {
            var laneGroupLights = new List<MaxGroupLight>();
            ConverToMaxGroupLight(mergeMaxDis);
            if (null != _maxGroupLights && _maxGroupLights.Count > 0)
            {
                _maxGroupLights.ForEach(c => laneGroupLights.Add(c));
            }
            return laneGroupLights;
        }
        void ConverToMaxGroupLight(double maxInnerDis) 
        {
            _maxGroupLights.Clear();
            if (null == _lightGroups || _lightGroups.Count < 1)
                return;
            foreach (var groups in _lightGroups)
            {
                var mergeConverter = GroupInnerMerge(groups, maxInnerDis);
                var nearNode = mergeConverter.OrderBy(c => c.NearRouteDisToEnd).FirstOrDefault();
                var laneGroupLight = new MaxGroupLight(nearNode.NearGroupPoint, nearNode.NearRouteDisToEnd);
                laneGroupLight.NearRoutePoint = nearNode.NearRoutePoint;
                laneGroupLight.LightGroups.AddRange(mergeConverter);
                _maxGroupLights.Add(laneGroupLight);
            }
        }
        List<LightDirGroup> GroupInnerMerge(List<LightGroup> lightGroups,double maxInnerDis)
        {
            var subLightGroups = GroupConver(lightGroups);
            //优先连接可以直连分组
            var retGroups = new List<LightDirGroup>();
            while (subLightGroups.Count > 0)
            {
                subLightGroups = subLightGroups.OrderBy(c => c.LightPoints.Count).ToList();
                var first = subLightGroups[0];
                subLightGroups.Remove(first);
                if (first.LightPoints.Count < 2)
                {
                    retGroups.Add(first);
                    continue;
                }
                int connectI = -1;
                double nearDis = double.MaxValue;
                for (int i = 0; i < subLightGroups.Count; i++)
                {
                    var second = subLightGroups[i];
                    //判断平行
                    if (!LightConnectUtil.GroupDirIsParallel(first.LineDir, second.LineDir,10))
                        continue;
                    var nearFirstPoint = LightConnectUtil.GetGroupNearPoint(first.LightPoints, second.LightPoints, out Point3d nearSecondPoint);
                    var prjPoint = nearSecondPoint.PointToLine(nearFirstPoint, first.LineDir);
                    if (prjPoint.DistanceTo(nearSecondPoint) > 2000)
                        continue;
                    double dis = nearFirstPoint.DistanceTo(nearSecondPoint);
                    if (dis > maxInnerDis)
                        continue;
                    if (dis < nearDis)
                    {
                        nearDis = dis;
                        connectI = i;
                    }
                }
                if (connectI > -1)
                {
                    var merge = subLightGroups[connectI];
                    subLightGroups.Remove(merge);

                    if (merge.NearRouteDisToEnd < first.NearRouteDisToEnd)
                    {
                        merge.LightPoints.AddRange(first.LightPoints);
                        subLightGroups.Add(merge);
                    }
                    else 
                    {
                        first.LightPoints.AddRange(merge.LightPoints);
                        subLightGroups.Add(first);
                    }
                }
                else
                {
                    retGroups.Add(first);
                }
            }
            return retGroups;
        }
        List<LightDirGroup> GroupConver(List<LightGroup> lightGroups)
        {
            var subLightGroups = new List<LightDirGroup>();
            foreach (var item in lightGroups)
            {
                if (item == null || item.GroupPoints == null || item.GroupPoints.Count < 1)
                    continue;
                var addGroups = GetGroupDirLight(item);
                if (null == addGroups || addGroups.Count < 1)
                    continue;
                subLightGroups.AddRange(addGroups);
            }
            return subLightGroups;
        }
        List<LightDirGroup> GetGroupDirLight(LightGroup group)
        {
            var retGroups = new List<LightDirGroup>();
            foreach (var xGroup in group.InnerXGroups)
            {
                double nearDis = double.MaxValue;
                Point3d nearRoutePoint = new Point3d();
                Point3d groupPoint = new Point3d();
                Line nearLine = null;
                var points = new List<Point3d>();
                foreach (var point in xGroup)
                {
                    points.Add(point);
                    var nearLines = GetTrunkingLine(point);
                    if (null == nearLines || nearLines.Count < 1)
                        continue;
                    foreach (var line in nearLines)
                    {
                        if (CheckLineCrossOutLine(point, line))
                            continue;
                        var routePoint = NearRouteNode(point, line, out double disToEnd);
                        if (disToEnd < nearDis)
                        {
                            nearDis = disToEnd;
                            nearRoutePoint = routePoint;
                            groupPoint = point;
                            nearLine = line;
                        }
                    }
                }
                points = ThPointVectorUtil.PointsOrderByDirection(points, group.XAxis, false).ToList();
                retGroups.Add(new LightDirGroup(points, group.XAxis,groupPoint,nearRoutePoint,nearLine, nearDis));
            }
            foreach (var yGroup in group.InnerYGroups)
            {
                double nearDis = double.MaxValue;
                Point3d nearRoutePoint = new Point3d();
                Point3d groupPoint = new Point3d();
                Line nearLine = null;
                var points = new List<Point3d>();
                foreach (var point in yGroup)
                {
                    points.Add(point);
                    var nearLines = GetTrunkingLine(point);
                    if (null == nearLines || nearLines.Count < 1)
                        continue;
                    foreach (var line in nearLines)
                    {
                        if (CheckLineCrossOutLine(point, line))
                            continue;
                        var routePoint = NearRouteNode(point, line, out double disToEnd);
                        if (disToEnd < nearDis)
                        {
                            nearDis = disToEnd;
                            nearRoutePoint = routePoint;
                            groupPoint = point;
                            nearLine = line;
                        }
                    }
                }
                points = ThPointVectorUtil.PointsOrderByDirection(points, group.YAxis, false).ToList();
                retGroups.Add(new LightDirGroup(points, group.YAxis, groupPoint, nearRoutePoint, nearLine, nearDis));
            }
            return retGroups;
        }
        List<Line> GetTrunkingLine(Point3d lightPoint) 
        {
            List<Line> nearLines =new List<Line>();
            foreach (var line in _trunkingLines) 
            {
                var prjPoint = lightPoint.PointToLine(line);
                if (!prjPoint.PointInLineSegment(line))
                    continue;
                var checkLine = new Line(lightPoint, prjPoint);
                int insterCount = 0;
                foreach (var tLine in _trunkingLines) 
                {
                    if (checkLine.LineIsIntersection(tLine))
                        insterCount += 1;
                }
                if (insterCount > 1)
                    continue;
                nearLines.Add(line);
            }
            return nearLines;
        }
        Point3d NearRouteNode(Point3d point,Line line, out double disToEnd)
        {
            var lineRoutes = new List<GraphRoute>();
            var linePoints = new List<Point3d>();
            foreach (var route in _graphRoutes)
            {
                if (null == route)
                    continue;
                var routeStartPoint = (Point3d)route.currentNode.GraphNode;
                if (!routeStartPoint.PointInLineSegment(line, 400, 400))
                    continue;
                linePoints.Add(routeStartPoint);
                lineRoutes.Add(route);
            }
            GraphRoute nearRoute = null;
            double nearDis = double.MaxValue;
            var prjPoint = point.PointToLine(line);
            linePoints = ThPointVectorUtil.PointsOrderByLine(linePoints, line).ToList();
            lineRoutes = lineRoutes.OrderByDescending(c => c.weightToStart).ToList();
            //获取最近的两个点判断用哪一个
            linePoints = linePoints.OrderBy(c => c.DistanceTo(prjPoint)).ToList();
            int i = 0;
            foreach (var item in linePoints) 
            {
                if (i > 1)
                    break;
                var route = lineRoutes.Where(c => ((Point3d)c.currentNode.GraphNode).DistanceTo(item) < 10).FirstOrDefault();
                var dis = prjPoint.DistanceTo(point) + route.weightToStart+ prjPoint.DistanceTo((Point3d)route.currentNode.GraphNode);
                if (dis < nearDis) 
                {
                    nearDis = dis;
                    nearRoute = route;
                }
                i += 1;
            }
            disToEnd = nearDis;
            return (Point3d)nearRoute.currentNode.GraphNode;
        }

        bool CheckLineCrossOutLine(Point3d point,Line laneLine) 
        {
            if (null == _notCrossLines || _notCrossLines.Count < 1)
                return false;
            bool isCorss = false;
            var prjPoint = point.PointToLine(laneLine);
            var tempLine = new Line(point, prjPoint);
            foreach (var check in _notCrossLines)
            {
                if (isCorss)
                    break;
                isCorss = tempLine.LineIsIntersection(check);
            }
            return isCorss;
        }
    }
}
