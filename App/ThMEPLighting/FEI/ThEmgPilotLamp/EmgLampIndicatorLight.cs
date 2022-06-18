using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Algorithm.DijkstraAlgorithm;
using ThMEPEngineCore.CAD;
using ThMEPLighting.ServiceModels;

namespace ThMEPLighting.FEI.ThEmgPilotLamp
{
    class EmgLampIndicatorLight
    {
        private List<Polyline> _targetColums;
        private List<Polyline> _targetWalls;
        private IndicatorLight _targetInfo;
        private List<LightLayout> _ligthLayouts;
        private Vector3d _normal = Vector3d.ZAxis;
        private Polyline _maxPolyline;
        private List<Polyline> _innerPolylines;
        private double _lightSpace = 10000;//灯具最大间距
        private double _lightOffset = 800;//单方向灯具偏移距离
        private double _lightDeleteMaxSpace = 10000;
        private double _lightDeleteMaxAngle = 30;
        private double _wallLightMergeAngle = 45;
        private double _pointInLineDistance = 1500;
        private double _delLightDirAngleToHostLight = 30;
        //删除对向指示灯的最大间距
        private double _delOpSideHostLightMaxDis = 2500;
        public List<LineGraphNode> _wallGraphNodes;//壁装的在线的那一侧
        private List<GraphNode> _hostLightNodes;
        private bool _isHostFirst=false;
        private List<Line> _mainLines;
        private EmgWallLight _emgWallLight;
        Dictionary<LineGraphNode, List<LightLayout>> _lineWallLights = new Dictionary<LineGraphNode, List<LightLayout>>();
        public EmgLampIndicatorLight(Polyline outPolyline,List<Polyline> innerPolylines,List<Polyline> columns, List<Polyline> walls, IndicatorLight indicator)
        {
            this._lightSpace = ThEmgLightService.Instance.MaxLightSpace;
            this._lightOffset = ThEmgLightService.Instance.HostLightMoveOffSet;
            this._lightDeleteMaxSpace = ThEmgLightService.Instance.MaxDeleteDistance;
            this._lightDeleteMaxAngle = ThEmgLightService.Instance.MaxDeleteAngle;

            _maxPolyline = outPolyline;
            _targetInfo = new IndicatorLight();
            _targetColums = new List<Polyline>();
            _targetWalls = new List<Polyline>();
            _ligthLayouts = new List<LightLayout>();
            _wallGraphNodes = new List<LineGraphNode>();
            _hostLightNodes = new List<GraphNode>();
            _innerPolylines = new List<Polyline>();
            if (null != columns && columns.Count > 0)
            {
                foreach (var item in columns)
                {
                    if (null == item)
                        continue;
                    _targetColums.Add(item);
                }
            }
            if (null != walls && walls.Count > 0)
            {
                foreach (var item in walls)
                {
                    if (null == item)
                        continue;
                    _targetWalls.Add(item);
                }
            }
            foreach (var item in innerPolylines) 
            {
                _innerPolylines.Add(item);
            }
            _targetInfo = indicator;
            _emgWallLight = new EmgWallLight(outPolyline, _innerPolylines, _targetInfo, _targetColums, _targetWalls, _wallLightMergeAngle, _lightSpace);

            var objs = new DBObjectCollection();
            _targetInfo.mainLines.ForEach(x => objs.Add(x));
            _targetInfo.assistLines.ForEach(x => objs.Add(x));
            _mainLines = ThFEILineExtension.LineSimplifier(objs, 500, 20.0, 2.0, Math.PI * 15 / 180.0).Cast<Line>().ToList();

        }
        public List<LightLayout> CalcLayout(bool isHostFirst)
        {
            _isHostFirst = isHostFirst;
            _ligthLayouts.Clear();
            _wallGraphNodes.Clear();
            _hostLightNodes.Clear();

            //主要，辅助疏散路径（壁装）上的点的信息获取判断
            CalcMainLayout(isHostFirst);

            //主要线到出口处使用吊装
            CalcExitLayout();

            //拐角处的吊装判断
            CalcMainCornerHost(3500);

            RemoveCornerHost();

            //辅助疏散路径（吊装计算）
            CalcAssitHostLayout();

            //对结果进行检查移除多余的节点
            CheckAndRemove(Math.PI* _lightDeleteMaxAngle / 180, _lightDeleteMaxSpace, true);

            //移除离出口近的额壁装灯
            CheckAndRemoveNearExitWallLight(2500);

            //检查壁装间距，以及添加吊装灯
            ChcekWallLightAddHostingLight();
            return _ligthLayouts;
        }


        /// <summary>
        /// 主要疏散路径 - 壁装(或吊装)  辅助疏散路径 - 壁装(或吊装) 指示灯布置计算
        /// </summary>
        private void CalcMainLayout(bool isHostFirst)
        {
            //对线进行合并，这里要考虑连续拐弯的情况，整个一个线上的排布一线的同一侧
            _wallGraphNodes = _emgWallLight.GetLineWallGraphNodes();
            if (!isHostFirst)
            {
                foreach (var lineInfo in _wallGraphNodes)
                {
                    if (lineInfo.line.Length < 4500)
                        continue;
                    //根据线获取获取排布侧的墙柱
                    var dir = lineInfo.lineDir;
                    var leftDir = dir.RotateBy(Math.PI / 2, _normal).GetNormal();
                    var lineLights = lineInfo.leftWallLayouLight;
                    if (lineInfo.layoutLineSide.DotProduct(leftDir)<0.5)
                        lineLights = lineInfo.rightWallLayoutLight;
                    var addLights = new List<LightLayout>();
                    foreach (var light in lineLights) 
                    {
                        if (light == null)
                            continue;
                        if (_ligthLayouts.Any(c => !c.isHoisting && c.linePoint.DistanceTo(light.linePoint) < 2500 && Math.Abs(c.directionSide.DotProduct(light.directionSide)) > 0.3 && Math.Abs(c.direction.DotProduct(light.direction)) > 0.3))
                            continue;
                        addLights.Add(light);
                        _ligthLayouts.Add(light);
                    }
                    _lineWallLights.Add(lineInfo, addLights);
                }
            }
            else 
            {
                var objs = new DBObjectCollection();
                _targetInfo.mainLines.ForEach(x => objs.Add(x));
                _targetInfo.assistLines.ForEach(x => objs.Add(x));
                List<Curve> curves = ThFEILineExtension.LineSimplifier(objs, 500, 20.0, 2.0, Math.PI * 15 / 180.0).Cast<Curve>().ToList();
                GetLightLayoutPlanB(curves);
            }
        }
        /// <summary>
        /// 主要疏散路径 - 吊装 指示灯布置计算
        /// </summary>
        private void CalcExitLayout()
        {
            //主疏散路径，一般是车道中线到出口的最后一段路线，这里用吊装指示.这里吊灯使用双面
            var objs = new DBObjectCollection();
            _targetInfo.exitLines.ForEach(x => objs.Add(x));
           // List<Curve> exitLines = ThMEPLineExtension.ExplodeCurves(objs);
            List<Line> curves = ThFEILineExtension.LineSimplifier(objs, 500, 200.0, 200.0, Math.PI * 15 / 180.0).Cast<Line>().ToList();
            curves = curves.Where(c => c.Length > 500).ToList();
            var lineAllNodes = GetHostLineNodes(curves.Cast<Curve>().ToList(), out Dictionary<Line, List<NodeDirection>> lineTwoExits, out Dictionary<Line, List<NodeDirection>> noNodeLines,out Dictionary<Line, List<NodeDirection>> lineDirNotEixtDir);
            _hostLightNodes.AddRange(lineAllNodes.Select(c => c.graphNode));
            List<GraphNode> hisNodes = new List<GraphNode>();
            ///      双向疏散灯
            /// E  -----+--------E
            ///         |
            ///         |
            ///         |
            if (lineTwoExits != null && lineTwoExits.Count > 0)
            {
                //通往两侧的疏散口，中间疏散点加入吊装双向指示灯
                foreach (var item in lineTwoExits)
                {
                    if (item.Value == null || item.Value.Count < 1)
                        continue;
                    Vector3d exitDir = (item.Key.EndPoint - item.Key.StartPoint).GetNormal();
                    var leftDir = exitDir.RotateBy(Math.PI / 2, _normal).GetNormal();
                    foreach (var node in item.Value)
                    {
                        if (null == node || node.graphNode.isExit)
                            continue;
                        if (hisNodes.Any(c => c.nodePoint.IsEqualTo(node.graphNode.nodePoint, new Tolerance(1, 1))))
                            continue;
                        if (_ligthLayouts.Any(c => c.linePoint.DistanceTo(node.graphNode.nodePoint) < 1000))
                            continue;
                        var light = new LightLayout(node.graphNode.nodePoint, node.graphNode.nodePoint, null, leftDir, exitDir, leftDir, node.graphNode, true);
                        light.isTwoExitDir = true;
                        _ligthLayouts.Add(light);
                        hisNodes.Add(node.graphNode);
                    }
                }
            }
            
            //优先从距离出口最远的点进行开始排布
            LineNodeToHostLight(curves.Cast<Curve>().ToList(), lineAllNodes, ref hisNodes, false,true);

            //疏散口可能没有用到，这边加一步判断，加入相应的线
            CheckAddExitHostLight(lineDirNotEixtDir, ref hisNodes);
            //检查是否有第一个指示灯
            //所在点在壁装指示灯上的都属于第一个指示灯
            if (_ligthLayouts == null || _ligthLayouts.Count < 1)
                return;
            foreach (var light in _ligthLayouts) 
            {
                if (null == light || !light.isHoisting || light.isCheckDelete)
                    continue;
                bool inWallLightLine = false;
                foreach (var lineNode in _wallGraphNodes) 
                {
                    if (inWallLightLine)
                        break;
                    if (lineNode == null || lineNode.nodeDirections == null || lineNode.nodeDirections.Count < 1)
                        continue;
                    inWallLightLine = lineNode.nodeDirections.Any(c => c.graphNode.nodePoint.DistanceTo(light.linePoint) < 1000);
                }
                light.isCheckDelete = inWallLightLine;
            }
        }
        private void CheckAddExitHostLight(Dictionary<Line, List<NodeDirection>> lineDirNotEixtDir, ref List<GraphNode> hisNodes) 
        {
            if (null == lineDirNotEixtDir || lineDirNotEixtDir.Count < 1)
                return;
            var otherLineObj = new DBObjectCollection();
            _targetInfo.mainLines.ForEach(c => otherLineObj.Add(c));
            _targetInfo.assistLines.ForEach(c => otherLineObj.Add(c));
            var otherLines = ThFEILineExtension.LineSimplifier(otherLineObj, 50, 20.0, 2.0, Math.PI / 180.0);

            var objs = new DBObjectCollection();
            _targetInfo.exitLines.ForEach(x => objs.Add(x));
            var allLines = ThFEILineExtension.LineSimplifier(objs, 50, 20.0, 2.0, Math.PI / 180.0);
            allLines = allLines.Select(y =>
            {
                var dir = (y.EndPoint - y.StartPoint).GetNormal();
                return new Line(y.StartPoint - dir * 1, y.EndPoint + dir * 1);
            }).ToList();
            var objs1 = new DBObjectCollection();
            allLines.ForEach(x => objs1.Add(x));
            var nodeGeo = objs1.ToNTSNodedLineStrings();
            if (null == nodeGeo)
                return;
            List<Line> dijkstraLines = nodeGeo.ToDbObjects()
                .SelectMany(x =>
                {
                    DBObjectCollection entitySet = new DBObjectCollection();
                    (x as Polyline).Explode(entitySet);
                    return entitySet.Cast<Line>().ToList();
                })
                .Where(x => x.Length > 5)
                .ToList();
            var exitNodes = _targetInfo.allNodes.Where(c => c.isExit).ToList();
            List<Point3d> notCreatePoints = new List<Point3d>();
            foreach (var item in lineDirNotEixtDir) 
            {
                if (item.Value == null || item.Value.Count < 1)
                    continue;
                foreach (var node in item.Value) 
                {
                    foreach (var exit in exitNodes)
                    {
                        var _dijkstra = new DijkstraAlgorithm(dijkstraLines.Cast<Curve>().ToList());
                        var sNode = _dijkstra.nodes.Where(c => c.NodePt.DistanceTo(node.graphNode.nodePoint) < 10).FirstOrDefault();
                        var eNode = _dijkstra.nodes.Where(c => c.NodePt.DistanceTo(exit.nodePoint) < 10).FirstOrDefault();
                        if (null == sNode || eNode == null)
                            continue;
                        var routePts = _dijkstra.FindingMinPath(sNode.NodePt, eNode.NodePt);
                        if (null == routePts || routePts.Count < 2)
                            continue;
                        if (routePts.LastOrDefault().DistanceTo(node.graphNode.nodePoint) > 10)
                            continue;
                        routePts.Reverse();
                        var exitDir = (routePts[1] - routePts[0]).GetNormal();
                        var dot = exitDir.DotProduct(node.outDirection);
                        if (dot > 0.3)
                            continue;
                        var interPoints = new List<Point3d>();
                        for (int i = 0; i < routePts.Count - 1; i++)
                        {
                            //if (PointInLaneLine(routePts[i],1000))
                            //count += 1;
                            var tempLine = new Line(routePts[i], routePts[i + 1]);
                            foreach (var line in otherLines)
                            {
                                var prjPt = EmgPilotLampUtil.PointToLine(tempLine.StartPoint, line);
                                var dis = prjPt.DistanceTo(line.StartPoint) + prjPt.DistanceTo(line.EndPoint);
                                var dis2 = prjPt.DistanceTo(tempLine.StartPoint) + prjPt.DistanceTo(tempLine.EndPoint);
                                if (dis > (line.Length + 20))
                                    continue;
                                if (dis2 > (tempLine.Length + 20))
                                    continue;
                                if (interPoints.Any(c => c.DistanceTo(prjPt) < 100))
                                    continue;
                                interPoints.Add(prjPt);
                            }
                        }
                        if (interPoints.Count > 1)
                            continue;
                        var route = EmgPilotLampUtil.InitRouteByPoints(_targetInfo.allNodes,routePts);

                        AddHostLightByRoute(allLines.Cast<Curve>().ToList(), route, ref hisNodes, false, ref notCreatePoints,false);
                    }
                }
            }
        }
        /// <summary>
        /// 计算主要疏散路径，壁装拐角处的吊装判断和生成
        /// </summary>
        private void CalcMainCornerHost(double minHostLightDis=2500)
        {
            if (null == _wallGraphNodes || _wallGraphNodes.Count < 1)
                return;
            var wallLight = _ligthLayouts.Where(c => !c.isHoisting).ToList();

            ///获取需要布置的点，并记录线排布在那一侧
            var nodeSideDirs = new Dictionary<NodeDirection,Vector3d>();
            foreach (var lineNodes in _wallGraphNodes) 
            {
                if (null == lineNodes.nodeDirections || lineNodes.nodeDirections.Count < 1)
                    continue;
                if (lineNodes.line.Length < 5000)
                    continue;
                //如果该线上灯一个也没有不进行处理
                List<LightLayout> lineLight = new List<LightLayout>();
                foreach (var light in _ligthLayouts) 
                {
                    var dot = light.direction.DotProduct(lineNodes.lineDir);
                    if (Math.Abs(dot) < 0.5)
                        continue;
                    if (lineNodes.nodeDirections.Any(c => c.graphNode.nodePoint.DistanceTo(light.nearNode.nodePoint) < 1500))
                        lineLight.Add(light);
                }
                if (lineLight.Count < 1)
                    continue;
                foreach (var item in lineNodes.nodeDirections) 
                {
                    if (item.inDirection == null || item.inDirection.Count < 1)
                        continue;
                    var dot = item.outDirection.DotProduct(lineNodes.lineDir);
                    if (Math.Abs(dot) < 0.3)
                        continue;
                    if (nodeSideDirs.Any(c => c.Key.graphNode.nodePoint.IsEqualTo(item.graphNode.nodePoint)))
                        continue;
                    nodeSideDirs.Add(item, lineNodes.layoutLineSide);
                }
            }
            nodeSideDirs = nodeSideDirs.OrderBy(c => c.Key.distanceToExit).ToDictionary(c => c.Key, c => c.Value);
            foreach (var item in nodeSideDirs) 
            {
                if (_ligthLayouts.Any(c => c.isHoisting && c.linePoint.DistanceTo(item.Key.nodePointInLine) < 1000))
                    continue;
                Point3d createPoint;
                bool isAdd = CheckAddHosting(item.Key, item.Value, out bool isTwoSide, out createPoint);
                //进一步判断是否有指向本方向的非平行壁装，如果没有不进行添加
                if (isAdd && !_isHostFirst)
                {
                    var checkLights = wallLight.Where(c => c.nearNode != null && c.nearNode.nodePoint.DistanceTo(item.Key.graphNode.nodePoint)<2500).ToList();
                    isAdd = false;
                    foreach (var wLight in checkLights)
                    {
                        if (isAdd)
                            break;
                        if (wallLight == null)
                            continue;
                        var dotDir = wLight.direction.DotProduct(item.Key.outDirection);
                        if (Math.Abs(dotDir) > 0.6)
                            //和疏散方向平行，不需要进一步判断
                            continue;
                        var dotSideDir = wLight.direction.DotProduct(item.Value);
                        if (dotSideDir < 0.1)
                            isAdd = true;
                    }
                }
                if (!isAdd)
                    continue;
                var moveVect = GetHostMoveVector(item.Key.graphNode);
                createPoint = item.Key.graphNode.nodePoint + moveVect;
                if (_ligthLayouts.Any(c => c.isHoisting && c.linePoint.DistanceTo(c.pointInOutSide)< minHostLightDis && c.linePoint.DistanceTo(createPoint) < minHostLightDis))
                    continue;
                var light = new LightLayout(item.Key.nodePointInLine, createPoint, null,item.Value,item.Key.outDirection,item.Value,item.Key.graphNode, true);
                light.isTwoSide = isTwoSide;
                _ligthLayouts.Add(light);
            }
        }

        private void RemoveCornerHost()
        {
            //拐角处如果是线路上的第一个拐点，如果该处有吊灯，需要删除
            //获取主疏散路径上的节点，并获取每个节点到相应出口的距离，
            //按照距离排序后，按照最远的节点进行遍历，如果已经生成相应的吊灯，则需要删除
            if (null == _targetInfo || _targetInfo.mainLines == null || _targetInfo.mainLines.Count < 1)
                return;
            var objs = new DBObjectCollection();
            _targetInfo.mainLines.ForEach(x => objs.Add(x));
            List<Curve> mainLines = ThFEILineExtension.ExplodeCurves(objs);
            if (null == mainLines || mainLines.Count < 1)
                return;
            var allLineNodes = GetHostLineNodes(mainLines, out Dictionary<Line, List<NodeDirection>> lineTwoExits, out Dictionary<Line, List<NodeDirection>> noNodeLines,out Dictionary < Line, List < NodeDirection >> lineDirNotEixtDir);
            if (allLineNodes == null || allLineNodes.Count < 1)
                return;
            allLineNodes = allLineNodes.OrderByDescending(c => c.distanceToExit).ToList();
            List<GraphNode> hisNodes = new List<GraphNode>();
            List<GraphNode> delNodeLight = new List<GraphNode>();
            foreach (var node in allLineNodes)
            {
                if (hisNodes.Any(c => c.nodePoint.IsEqualTo(node.graphNode.nodePoint)))
                    continue;
                hisNodes.Add(node.graphNode);
                if (_hostLightNodes.Any(c => c.nodePoint.IsEqualTo(node.graphNode.nodePoint, new Tolerance(1, 1))))
                    continue;
                List<GraphRoute> routes = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes, node.graphNode, true);
                if (null == routes || routes.Count < 1)
                    continue;
                var route = routes.First();
                while (route != null && route.nextRoute != null)
                {
                    if (hisNodes.Any(c => c.nodePoint.IsEqualTo(route.node.nodePoint)))
                    {
                        route = route.nextRoute;
                        continue;
                    }
                    hisNodes.Add(route.node);
                    if (_hostLightNodes.Any(c => c.nodePoint.IsEqualTo(node.graphNode.nodePoint, new Tolerance(1, 1))))
                    {
                        route = route.nextRoute;
                        continue;
                    }
                    //判断该点是否有灯
                    var light = _ligthLayouts.Where(c => c.isHoisting && c.nearNode != null && c.nearNode.nodePoint.IsEqualTo(route.node.nodePoint, new Tolerance(1, 1))).FirstOrDefault();
                    if (light != null && route.node.nodePoint.IsEqualTo(node.graphNode.nodePoint, new Tolerance(1, 1)))
                        //是第一个灯，删除
                        delNodeLight.Add(node.graphNode);
                }
            }

            _ligthLayouts = _ligthLayouts.Where(c => !c.isHoisting || (c.isHoisting && c.nearNode != null && !delNodeLight.Any(x => x.nodePoint.IsEqualTo(c.nearNode.nodePoint)))).ToList();
            //return;
            //移除距离近，且方向接近相反的吊装指示灯
            //方向相反，间距在一定范围内移除一个距离疏散口远的吊装灯
            List<Point3d> delHostLightPoints = new List<Point3d>();
            var wallLights = _ligthLayouts.Where(c => !c.isHoisting).ToList();
            var hostLights = _ligthLayouts.Where(c => c.isHoisting).ToList();
            hostLights = _ligthLayouts.Where(c => c.isHoisting).ToList();
            foreach (var light in hostLights)
            {
                if (delHostLightPoints.Any(c => c.IsEqualTo(light.linePoint, new Tolerance(1, 1))) || light.nearNode.nodePoint.DistanceTo(light.pointInOutSide) > 2000)
                    continue;
                LightLayout nearLight = null;
                foreach (var item in hostLights)
                {
                    if (nearLight != null)
                        break;
                    if (delHostLightPoints.Any(c => c.IsEqualTo(item.linePoint, new Tolerance(1, 1))) || item.nearNode.nodePoint.DistanceTo(item.pointInOutSide) > 2000)
                        continue;
                    if (light.nearNode.nodePoint.IsEqualTo(item.nearNode.nodePoint, new Tolerance(1, 1)))
                        continue;
                    var dis = light.nearNode.nodePoint.DistanceTo(item.nearNode.nodePoint);
                    var dot = light.direction.DotProduct(item.direction);
                    if (dis > _delOpSideHostLightMaxDis || dot > -0.5)
                        continue;
                    nearLight = item;
                }
                if (null != nearLight)
                {
                    var route = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes, light.nearNode, true).FirstOrDefault();
                    var nearRoute = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes, nearLight.nearNode, true).FirstOrDefault();
                    var routeDisExit = GraphUtils.GetRouteDisToEnd(route);
                    var nearRouteDisExit = GraphUtils.GetRouteDisToEnd(nearRoute);
                    if (routeDisExit > nearRouteDisExit)
                        delHostLightPoints.Add(light.linePoint);
                    else
                        delHostLightPoints.Add(nearLight.linePoint);
                }
            }
            _ligthLayouts = _ligthLayouts.Where(c => !c.isHoisting || (c.isHoisting && c.nearNode != null && !delHostLightPoints.Any(x => x.IsEqualTo(c.linePoint, new Tolerance(1, 1))))).ToList();

            //return;

            if (_isHostFirst)
                return;
            ///删除第一个没有
            hostLights = _ligthLayouts.Where(c => c.isHoisting).ToList();
            delHostLightPoints.Clear();
            foreach (var light in hostLights)
            {
                if (!light.isHoisting || !light.canDelete || !light.isCheckDelete || light.nearNode.nodePoint.DistanceTo(light.pointInOutSide) > 2000)
                    continue;
                //判断壁装是否有指向该方向的
                var thisNodeWallLight = wallLights.Where(c => c.nearNode.nodePoint.IsEqualTo(light.nearNode.nodePoint, new Tolerance(1, 1))).ToList();
                if (thisNodeWallLight.Any(c => !c.direction.IsParallelToEx(light.direction)))
                    //有非平行的壁装指示灯，不需要删除
                    continue;
                //进一步判断是否吊装的第一个节点
                var routes = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes, light.nearNode, false);
                var inOtherRoute = false;
                foreach (var route in routes)
                {
                    if (inOtherRoute)
                        break;
                    if (route.node.nodePoint.IsEqualTo(light.nearNode.nodePoint, new Tolerance(1, 1)))
                        continue;
                    foreach (var node in _hostLightNodes)
                    {
                        if (inOtherRoute)
                            break;
                        if (node.nodePoint.IsEqualTo(light.nearNode.nodePoint, new Tolerance(1, 1)))
                            continue;
                        inOtherRoute = GraphUtils.NodeInRoute(route, node);
                        if (!inOtherRoute)
                            continue;
                        GraphNode pNode = null;
                        var tempRoute = route;
                        while (tempRoute.nextRoute != null) 
                        {
                            if (pNode != null)
                                break;
                            if (tempRoute.nextRoute.node.nodePoint.IsEqualTo(light.nearNode.nodePoint, new Tolerance(1, 1)))
                                pNode = tempRoute.node;
                            tempRoute = tempRoute.nextRoute;
                        }
                        if (pNode != null && pNode.nodePoint.DistanceTo(light.nearNode.nodePoint) < 2500)
                            inOtherRoute = false;

                    }
                }
                if (inOtherRoute)
                    continue;
                delHostLightPoints.Add(light.linePoint);
            }
            _ligthLayouts = _ligthLayouts.Where(c => !c.isHoisting || (c.isHoisting && c.nearNode != null && !delHostLightPoints.Any(x => x.IsEqualTo(c.linePoint, new Tolerance(1, 1))))).ToList();

        }
        /// <summary>
        /// 辅助疏散路径 - 吊装指示灯布置计算
        /// </summary>
        private void CalcAssitHostLayout()
        {
            var objs = new DBObjectCollection();
            _targetInfo.assistHostLines.ForEach(x => objs.Add(x));
            List<Curve> assistHost = ThFEILineExtension.ExplodeCurves(objs);
            if (null == assistHost || assistHost.Count < 1)
                return;
            //获取这些线上的节点，优先排布距离出口处距离远的节点
            var lineAllNodes = GetHostLineNodes(assistHost, out Dictionary<Line, List<NodeDirection>> lineTwoExits, out Dictionary<Line, List<NodeDirection>> noNodeLines, out Dictionary<Line, List<NodeDirection>> lineDirNotEixtDir);
            List<GraphNode> hisNodes = new List<GraphNode>();
            LineNodeToHostLight(assistHost, lineAllNodes, ref hisNodes, true);
        }

        
        /// <summary>
        /// 吊装线上的节点信息获取
        /// </summary>
        /// <param name="hostCurves"></param>
        /// <param name="lineTwoExits"></param>
        /// <param name="noNodeLines"></param>
        /// <returns></returns>
        private List<NodeDirection> GetHostLineNodes(List<Curve> hostCurves,
            out Dictionary<Line, List<NodeDirection>> lineTwoExits,
            out Dictionary<Line, List<NodeDirection>> lineGraphNodes,
            out Dictionary<Line,List<NodeDirection>> lineNotExitDirNode) 
        {
            //获取这些线上的节点，优先排布距离出口处距离远的节点
            var lineAllNodes = new List<NodeDirection>();
            lineTwoExits = new Dictionary<Line, List<NodeDirection>>();
            lineGraphNodes = new Dictionary<Line, List<NodeDirection>>();
            lineNotExitDirNode = new Dictionary<Line, List<NodeDirection>>();
            foreach (var line in hostCurves)
            {
                var liNodes = new List<NodeDirection>();
                Point3d sp = line.StartPoint;
                var dir = (line.EndPoint - line.StartPoint).GetNormal();
                var leftDir = dir.RotateBy(Math.PI / 2, _normal).GetNormal();
                var lineNodes = GetLineNodes(line as Line,2000);
                var unLineDirNodes =new List<NodeDirection>();
                if (lineNodes.Count < 1)
                    continue;
                int exitCount = 0;
                foreach (var item in lineNodes)
                {
                    if (item == null || item.nodeDirections == null || item.nodeDirections.Count < 1)
                        continue;
                    foreach (var node in item.nodeDirections)
                    {
                        if (node == null)
                            continue;
                        
                        if (node.graphNode.isExit)
                            exitCount += 1;
                        if (lineAllNodes.Any(c => c.graphNode.nodePoint.IsEqualTo(node.graphNode.nodePoint, new Tolerance(1, 1))))
                            continue;
                        List<GraphRoute> routes = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes, node.graphNode, true);
                        if (null == routes || routes.Count < 1)
                            continue;
                        var route = routes.First();
                        //因为考虑精度，会将线附近的其它点获取到，这里需要将不符合要求的点过滤掉
                        var exitDir = (route.nextRoute.node.nodePoint - route.node.nodePoint).GetNormal();
                        double angle = exitDir.GetAngleTo(dir);
                        var routePoint = route.node.nodePoint;
                        if (routePoint.DistanceTo(sp) < 10)
                        {
                            var dot = exitDir.DotProduct(dir);
                            if (dot < 0.9)
                            {
                                unLineDirNodes.Add(node);
                                continue;
                            }
                        }
                        else if (routePoint.DistanceTo(line.EndPoint) < 10) 
                        {
                            var dot = exitDir.DotProduct(dir.Negate());
                            if (dot < 0.9)
                            {
                                //var newNode = new NodeDirection(node.nodePointInLine, null, 0, dir.Negate(), node.graphNode);
                                unLineDirNodes.Add(node);
                                continue;
                            }
                        }
                        else
                        {
                            angle %= Math.PI;
                            if (angle > Math.PI / 18 && angle < (Math.PI - Math.PI / 18))
                            {
                                unLineDirNodes.Add(node);
                                continue;
                            }
                        }
                        var nodeInfo = new NodeDirection(node.nodePointInLine, null, GraphUtils.GetRouteDisToEnd(route), node.outDirection, node.graphNode);
                        nodeInfo.inDirection.AddRange(node.inDirection);
                        liNodes.Add(nodeInfo);
                        lineAllNodes.Add(nodeInfo);
                    }
                }
                lineGraphNodes.Add(line as Line, liNodes);
                if (exitCount > 1 && liNodes.Count > 0)
                    lineTwoExits.Add(line as Line, liNodes);
                if (unLineDirNodes != null && unLineDirNodes.Count > 0)
                    lineNotExitDirNode.Add(line as Line, unLineDirNodes);
            }
            return lineAllNodes;
        }

        /// <summary>
        /// 吊装节点信息，在相应的位置布置相应的灯具
        /// </summary>
        /// <param name="hostLines"></param>
        /// <param name="lineAllNodes"></param>
        /// <param name="hisNodes"></param>
        /// <param name="unHostLineExit"></param>
        private void LineNodeToHostLight(List<Curve> hostLines,List<NodeDirection> lineAllNodes,ref List<GraphNode> hisNodes,bool unHostLineExit, bool checkIsEndLine = false) 
        {
            lineAllNodes = lineAllNodes.OrderByDescending(c => c.distanceToExit).ToList();
            List<Point3d> notCreatePoints = new List<Point3d>();
            foreach (var node in lineAllNodes)
            {
                if (hisNodes.Any(c => c.nodePoint.IsEqualTo(node.graphNode.nodePoint, new Tolerance(1, 1))))
                    continue;
                if (!PointInLaneLine(node.graphNode.nodePoint, _pointInLineDistance))
                    continue;
                var route = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes, node.graphNode, true).FirstOrDefault();
                AddHostLightByRoute(hostLines, route, ref hisNodes, unHostLineExit, ref notCreatePoints, true, checkIsEndLine);
            }
        }
        private void AddHostLightByRoute(List<Curve> hostLines, GraphRoute route, ref List<GraphNode> hisNodes, bool unHostLineExit,ref List<Point3d> notCreatePoints,bool canDel =true,bool checkIsEndLine=false) 
        {
            bool isFirst = true;
            if (null == route)
                return;
            Line pLine = null;
            Point3d? pPoint = null;
            while (route != null && route.nextRoute != null)
            {
                if (hisNodes.Any(c => c.nodePoint.IsEqualTo(route.node.nodePoint, new Tolerance(1, 1))))
                    break;
                if (checkIsEndLine && !CheckMergeHostLine(route, _mainLines, ref notCreatePoints, ref hisNodes))
                    break;
                //获取线,出口方向
                var sNode = route.node;
                var eNode = route.nextRoute.node;
                var dir = (eNode.nodePoint - sNode.nodePoint).GetNormal();
                bool isExitLine = hostLines.Any(c => c != null && EmgPilotLampUtil.LineIsCollinear(c.StartPoint, c.EndPoint, sNode.nodePoint + dir.MultiplyBy(10), eNode.nodePoint - dir.MultiplyBy(10),5,1000,15));
                if (isFirst && !isExitLine)
                    break;
                hisNodes.Add(route.node);
                if (!isExitLine)
                {
                    route = route.nextRoute;
                    while (route.nextRoute != null)
                    {
                        eNode = route.node;
                        var nextDir = (route.nextRoute.node.nodePoint - route.node.nodePoint).GetNormal();
                        if (!nextDir.IsParallelToEx(dir))
                            break;
                        route = route.nextRoute;
                    }
                }
                else
                    route = route.nextRoute;
                if (canDel) 
                {
                    //判断后续是否有非吊装线的点
                    if (route == null || route.node.isExit || route.nextRoute == null)
                        canDel = false;
                    else 
                    {
                        canDel = false;
                        var tempRoute = route;
                        while (tempRoute.nextRoute != null) 
                        {
                            var tempS = tempRoute.node.nodePoint;
                            var tempE = tempRoute.nextRoute.node.nodePoint;
                            var bIsEnd = hostLines.Any(c => c != null && EmgPilotLampUtil.LineIsCollinear(c.StartPoint, c.EndPoint, tempS, tempE));
                            if (!bIsEnd)
                            {
                                canDel = true;
                                break;
                            }
                            tempRoute = tempRoute.nextRoute;
                        }
                    }
                }
                Line line = new Line(sNode.nodePoint, eNode.nodePoint);
                isExitLine = hostLines.Any(c => c != null && EmgPilotLampUtil.LineIsCollinear(c.StartPoint, c.EndPoint, line.StartPoint, line.EndPoint, 5, 1000, 15));
                if (unHostLineExit && !isExitLine && !isFirst)
                    break;
                var addLights = LineAddHostLight(line, pLine, pPoint, sNode, route, isExitLine, isFirst, ref notCreatePoints, canDel);
                if (null != addLights && addLights.Count > 0)
                    _ligthLayouts.AddRange(addLights);
                pLine = null;
                if (isExitLine && route.nextRoute != null)
                    pLine = new Line(sNode.nodePoint, eNode.nodePoint);
                if (isExitLine)
                    pPoint = sNode.nodePoint;
                isFirst = pLine ==null;
            }
        }
        /// <summary>
        /// 非主要路径的节点处的吊装指示灯判断，并获取相应的生成点
        /// </summary>
        /// <param name="nodeDirection">节点的入度，计算是否是双面的展示</param>
        /// <param name="sideLineDir">该节点所在线，壁装灯所在侧</param>
        /// <param name="isTwoSide">out bool 是否是双面指示</param>
        /// <param name="createPoint"></param>
        /// <returns></returns>
        private bool CheckAddHosting(NodeDirection nodeDirection,Vector3d sideLineDir,out bool isTwoSide,out Point3d createPoint) 
        {
            isTwoSide = false;
            bool isAdd = false;
            createPoint = new Point3d();
            if (nodeDirection == null || nodeDirection.inDirection.Count < 1)
                return false;
            var addDirs = new List<Vector3d>();
            foreach (var dir in nodeDirection.inDirection)
            {
                var dot = dir.DotProduct(sideLineDir);
                if (dot < 0)
                    isAdd = true;
                bool addToDir = true;
                foreach (var item in addDirs)
                {
                    if (!addToDir)
                        break;
                    dot = item.DotProduct(dir);
                    if (dot > 0)
                        addToDir = false;
                }
                if (addToDir)
                    addDirs.Add(dir);
            }
            isTwoSide = addDirs.Count > 1;
            if (isTwoSide)
                createPoint = nodeDirection.nodePointInLine - nodeDirection.outDirection.MultiplyBy(_lightOffset);
            else
                createPoint = nodeDirection.nodePointInLine - sideLineDir.MultiplyBy(_lightOffset);
            return isAdd;
        }
        /// <summary>
        /// 根据吊灯去移除相应范围，一定夹角内的壁灯
        /// 指示方向和吊灯指示方向不平行的移除
        /// </summary>
        /// <param name="inAngle"></param>
        /// <param name="distance"></param>
        /// <param name="isTwoNormal"></param>
        private void CheckAndRemove(double inAngle, double distance,bool isTwoNormal=true)
        {
            //根据吊装点位将多余的壁装点位删除
            List<LightLayout> hostingLights = new List<LightLayout>();
            List<LightLayout> wallLights = new List<LightLayout>();
            foreach (var light in _ligthLayouts) 
            {
                if (light == null)
                    continue;
                if (light.isHoisting)
                    hostingLights.Add(light);
                else
                    wallLights.Add(light);
            }
            var delLights = new List<LightLayout>();
            var angleCos = Math.Abs(Math.Cos(inAngle));
            foreach (var light in hostingLights) 
            {
                Vector3d dir = light.direction;
                if (null == light || !light.isCheckDelete)
                    continue;
                List<Line> hostLines = new List<Line>();
                foreach (var line in _mainLines) 
                {
                    var testDir = line.LineDirection();
                    if (EmgPilotLampUtil.PointInLine(light.linePoint, line, 10, 1000))
                        hostLines.Add(line);
                }
                if (hostLines == null || hostLines.Count < 1)
                    continue;
                foreach (var checkLight in wallLights) 
                {
                    var dis = light.pointInOutSide.DistanceTo(checkLight.pointInOutSide);
                    if (light.pointInOutSide.DistanceTo(checkLight.pointInOutSide) > distance)
                        continue;
                    if (delLights.Any(c => c.pointInOutSide.IsEqualTo(light.pointInOutSide, new Tolerance(1, 1))))
                        continue;
                    
                    //同方向的指示灯不需要删除
                    var checkDir = checkLight.direction;
                    var dot = checkDir.DotProduct(dir);
                    if (Math.Abs(dot) > Math.Abs(Math.Cos(Math.PI*(90.0-_delLightDirAngleToHostLight)/180)))
                        continue;
                    if (!EmgPilotLampUtil.PointInLines(checkLight.linePoint, hostLines, 10, 1000))
                        continue;
                    Vector3d hostToCheckDir = (checkLight.pointInOutSide - light.pointInOutSide).GetNormal();
                    dot = dir.DotProduct(hostToCheckDir);
                    double angle = dir.GetAngleTo(hostToCheckDir, _normal);
                    double lightAngle = dir.GetAngleTo(checkDir,_normal);
                    angle = angle % Math.PI;
                    if (angle < inAngle || angle > (Math.PI - inAngle))
                    {
                        if (dot > 0)
                        {
                            delLights.Add(checkLight);
                        }
                        else if (isTwoNormal)
                        {
                            delLights.Add(checkLight);
                        }
                    }
                }
            }
            _ligthLayouts.Clear();
            foreach (var item in wallLights) 
            {
                if (delLights.Any(c => c.pointInOutSide.IsEqualTo(item.pointInOutSide, new Tolerance(1, 1))))
                    continue;
                _ligthLayouts.Add(item);
            }
            _ligthLayouts.AddRange(hostingLights);
        }

        private void CheckAndRemoveNearExitWallLight(double distance) 
        {
            if (_isHostFirst)
                return;
            var delWallLights = new List<Point3d>();
            List<GraphNode> allExitNodes = _targetInfo.allNodes.Where(c => c.isExit).ToList();
            if (null == allExitNodes || allExitNodes.Count < 1)
                return;
            foreach (var item in _ligthLayouts) 
            {
                if (item.isHoisting)
                    continue;
                if (allExitNodes.Any(c => c.nodePoint.DistanceTo(item.pointInOutSide) <= distance))
                    delWallLights.Add(item.linePoint);
            }
            _ligthLayouts = _ligthLayouts.Where(c => c.isHoisting || (!c.isHoisting  && !delWallLights.Any(x => x.IsEqualTo(c.linePoint, new Tolerance(1, 1))))).ToList();
        }
        /// <summary>
        /// 主要疏散路径 -吊装
        /// </summary>
        private void GetLightLayoutPlanB(List<Curve> curves) 
        {
            
            var lineAllNodes = GetHostLineNodes(curves, out Dictionary<Line, List<NodeDirection>> lineTwoExits, out Dictionary<Line, List<NodeDirection>> lineNodes, out Dictionary<Line, List<NodeDirection>> lineDirNotEixtDir);
            List<Dictionary<GraphNode, GraphNode>> hisNodes = new List<Dictionary<GraphNode, GraphNode>>();
            lineAllNodes = lineAllNodes.OrderByDescending(c => c.distanceToExit).ToList();
            List<Point3d> notCreatePoints = new List<Point3d>();
            foreach (var node in lineAllNodes)
            {
                if (hisNodes.Any(c => c.FirstOrDefault().Key.nodePoint.IsEqualTo(node.graphNode.nodePoint, new Tolerance(1, 1))))
                    continue;
                var route = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes, node.graphNode, true).FirstOrDefault();
                if (null == route)
                    continue;
                bool isFirst = true;
                ///按照当前节点的路径一直走下去，如果遇到有非该线上的节点路径，直接结束本路径的计算
                while (route != null && route.nextRoute != null)
                {
                    if (hisNodes.Any(c => c.FirstOrDefault().Key.nodePoint.IsEqualTo(route.node.nodePoint, new Tolerance(1, 1))))
                        break;
                    var sNode = route.node;
                    var eNode = route.nextRoute.node;
                    hisNodes.Add(new Dictionary<GraphNode, GraphNode>() { { sNode, eNode } });
                    var dir = (eNode.nodePoint - sNode.nodePoint).GetNormal();
                    Line line = new Line(sNode.nodePoint, eNode.nodePoint);
                    var targetLine = curves.Where(c => c != null && ThGeometryTool.IsOverlapEx(c.StartPoint, c.EndPoint, line.StartPoint, line.EndPoint)).FirstOrDefault();
                    var isTargetLine = false;
                    if (targetLine != null) 
                        isTargetLine = eNode.nodePoint.IsPointOnLine(targetLine as Line, 10);
                    if (!isTargetLine)
                        break;
                    if (line.Length < 3000 || (isFirst && line.Length < 8000))
                    {
                        route = route.nextRoute;
                        isFirst = false;
                        continue;
                    }
                    if (isFirst && line.Length > 8000)
                    {
                        //在判断起点是否在其它线上，如果在其它线上，则该点不需要进行缩
                        List<Line> pointInLines = new List<Line>();
                        foreach (var item in curves) 
                        {
                            if (null == item)
                                continue;
                            var prjPoint = EmgPilotLampUtil.PointToLine(sNode.nodePoint, item as Line);
                            if (prjPoint.DistanceTo(sNode.nodePoint) < 2000  && prjPoint.IsPointOnLine(item as Line, 100))
                                pointInLines.Add(item as Line);
                            var test = (item as Line).IsOnLine(prjPoint);
                        }
                        if(pointInLines.Count <=1)
                            line = new Line(line.StartPoint + dir.MultiplyBy(5000), line.EndPoint);
                    }
                       
                    var addLights= LineAddHostLight(line, null, null, sNode, route, true, false, ref notCreatePoints);
                    if (null != addLights && addLights.Count > 0)
                        _ligthLayouts.AddRange(addLights);

                    route = route.nextRoute;
                    isFirst = false;
                }
            }
            //有些线段量的端点分别指向不同的出口，导致中间不进行排布,这里处理相应的数据
            foreach (var line in curves)
            {
                if (!_targetInfo.mainLines.Any(c => c != null && ThGeometryTool.IsOverlapEx(c.StartPoint, c.EndPoint, line.StartPoint, line.EndPoint)))
                    continue;
                var liNodes  = GetLineNodes(line as Line);
                if (liNodes.Count < 1)
                    continue;
                
                var thisLineNodes = new List<GraphNode>();
                foreach (var nodeDirs in liNodes) 
                {
                    if (null == nodeDirs.nodeDirections || nodeDirs.nodeDirections.Count < 1)
                        continue;
                    foreach (var node in nodeDirs.nodeDirections) 
                    {
                        if (node == null || thisLineNodes.Any(c=>c.nodePoint.IsEqualTo(node.graphNode.nodePoint,new Tolerance(1,1))))
                            continue;
                        thisLineNodes.Add(node.graphNode);
                    }
                }
                thisLineNodes = thisLineNodes.OrderBy(c => c.nodePoint.DistanceTo(line.StartPoint)).ToList();
                for (int i = 0; i < thisLineNodes.Count() - 1; i++) 
                {
                    var sNode = thisLineNodes[i];
                    var eNode = thisLineNodes[i + 1];
                    var dir = (eNode.nodePoint - sNode.nodePoint).GetNormal();
                    var leftDir = dir.RotateBy(Math.PI / 2, _normal).GetNormal();
                    var isInHis = false;
                    foreach (var hisItem in hisNodes) 
                    {
                        if (isInHis)
                            break;
                        if (hisItem == null || hisItem.Count < 1)
                            continue;
                        var keyValue = hisItem.FirstOrDefault();
                        isInHis = (keyValue.Key.nodePoint.IsEqualTo(sNode.nodePoint, new Tolerance(1, 1)) && keyValue.Value.nodePoint.IsEqualTo(eNode.nodePoint, new Tolerance(1, 1)))
                            || (keyValue.Key.nodePoint.IsEqualTo(eNode.nodePoint, new Tolerance(1, 1)) && keyValue.Value.nodePoint.IsEqualTo(sNode.nodePoint, new Tolerance(1, 1)));

                    }
                    if (isInHis )
                        continue;
                    var dis = sNode.nodePoint.DistanceTo(eNode.nodePoint);
                    hisNodes.Add(new Dictionary<GraphNode, GraphNode>() { { sNode, eNode } });
                    if (dis < _lightSpace)
                        continue;


                    var sRoute = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes, sNode, true).FirstOrDefault();
                    var eRoute = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes, eNode, true).FirstOrDefault();
                    if (null == sRoute || null == eRoute)
                        continue;
                    double sDisToExit = GraphUtils.GetRouteDisToEnd(sRoute);
                    double eDisToExit = GraphUtils.GetRouteDisToEnd(eRoute);
                    Line creatLi = new Line(sNode.nodePoint, eNode.nodePoint);
                    int count = (int)Math.Ceiling(dis / _lightSpace);
                    double step = dis / count;
                    var moveVect = GetHostMoveVector(sNode);
                    var createPoint = sNode.nodePoint + dir.MultiplyBy(step);
                    while (createPoint.DistanceTo(sNode.nodePoint) <= dis - 1000)
                    {
                        var exitDir = dir;
                        if (createPoint.DistanceTo(sNode.nodePoint) + sDisToExit < (createPoint.DistanceTo(eNode.nodePoint) + eDisToExit)) 
                            exitDir = dir.Negate();

                        if (!_ligthLayouts.Any(c => c.isHoisting && c.linePoint.DistanceTo(createPoint) < 1500))
                        {
                            var light1 = new LightLayout(createPoint, createPoint+ moveVect, creatLi, leftDir, exitDir, leftDir, sNode, true);
                            light1.isCheckDelete = false;
                            light1.isTwoSide = false;
                            _ligthLayouts.Add(light1);
                        }
                        createPoint = createPoint + dir.MultiplyBy(step);
                        
                    }
                }
            }
        }
       
        /// <summary>
        /// 获取线上的节点，并获取没有节点到出口处的距离
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        List<LineGraphNode> GetLineNodes(Line line,double maxDis=2300)
        {
            var LineGrapheNode = new List<LineGraphNode>();
            //step1 获取每一根线上的节点
            var liNodes = new List<NodeDirection>();
            Point3d sp = line.StartPoint;
            var dir = (line.EndPoint - line.StartPoint).GetNormal();
            var leftDir = dir.RotateBy(Math.PI / 2, _normal).GetNormal();
            //step2 优先获取线上的点
            foreach (var node in _targetInfo.allNodes)
            {
                if (null == node || node.nodePoint == null || node.isExit)
                    continue;
                var temp = (node.nodePoint - sp);
                double dis = temp.DotProduct(leftDir);
                if (Math.Abs(dis) > maxDis)
                    continue;
                Point3d pointInLine = node.nodePoint - leftDir.MultiplyBy(dis);
                if (pointInLine.DistanceTo(sp) + pointInLine.DistanceTo(line.EndPoint) > line.Length + 10)
                    continue;
                var tempNode = _targetInfo.allNodes.Where(c => c.nodePoint.DistanceTo(node.nodePoint) < 10).FirstOrDefault();
                List<GraphRoute> routes = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes, tempNode, true);
                if (null == routes || routes.Count < 1)
                    continue;
                GraphRoute nearRoute = routes.FirstOrDefault();
                var exitDir = (nearRoute.nextRoute.node.nodePoint - nearRoute.node.nodePoint).GetNormal();
                NodeDirection nodeDir = new NodeDirection(pointInLine, nearRoute, GraphUtils.GetRouteDisToEnd(nearRoute), exitDir, node);
                liNodes.Add(nodeDir);
            }
            var lineGraphe = new LineGraphNode(line);
            //lineGraphe.layoutLineSide = leftDir;
            lineGraphe.nodeDirections.AddRange(liNodes);
            LineGrapheNode.Add(lineGraphe);
            return LineGrapheNode;
        }

        /// <summary>
        /// 在一根线上判断需要生成吊装指示灯的位置，
        /// 根据线的长度，上一根线，后续路径信息判断需要在什么位置生成相应的东西
        /// </summary>
        /// <param name="line">要生成吊装灯的线</param>
        /// <param name="pLine">上一根线，可以为null,起点是没有上一根线</param>
        /// <param name="pPoint">上一根线的起点，有些线需要合并后，可能会用到</param>
        /// <param name="sNode">线的开始节点</param>
        /// <param name="endRoute">后节点的相应路径</param>
        /// <param name="isExtLine">是否是主要吊装疏散路径线</param>
        /// <param name="isLightFirst">是否该疏散路径上的第一盏灯</param>
        /// <param name="notCreatePoints">ref 不创建的点记录</param>
        List<LightLayout> LineAddHostLight(Line line, Line pLine, Point3d? pPoint,GraphNode sNode,GraphRoute endRoute,bool isExtLine,bool isLightFirst,ref List<Point3d> notCreatePoints,bool canDel=true,double minSpace=1500) 
        {
            var addLights = new List<LightLayout>();
            Point3d nodePoint = line.StartPoint;
            Vector3d exitDir = line.LineDirection();

            bool nextIsExit = endRoute !=null? endRoute.node.isExit:false;
            var leftDir = exitDir.RotateBy(Math.PI / 2, _normal).GetNormal();
            bool isTwoSide = true;
            var createPoint = nodePoint + exitDir.MultiplyBy(_lightOffset);
            var nextRoute = endRoute == null? null: endRoute.nextRoute;
            //长度太小，不进行排布
            if (line.Length < 500)
                return addLights;
            bool isAdd = false;
            Vector3d pDir = new Vector3d();
            if (pPoint.HasValue) 
                pDir = (nodePoint - pPoint.Value).GetNormal();
                
            if (line.Length < 2500)
            {
                if (pLine == null && isExtLine)
                    isAdd = isLightFirst;
                else if(pLine !=null)
                {
                    createPoint = nodePoint;
                    if (nextRoute != null)
                    {
                        var dirVector = nextRoute.node.nodePoint - endRoute.node.nodePoint;
                        exitDir = dirVector.GetNormal();

                        var dot = pDir.DotProduct(exitDir);
                        if (dirVector.Length < 2500)
                            exitDir = (line.EndPoint - line.StartPoint).GetNormal();
                        if (dot < 0)
                        {
                            exitDir = (line.EndPoint - line.StartPoint).GetNormal();
                            if (null != pLine)
                                createPoint = createPoint + pDir.MultiplyBy(_lightOffset);
                        }
                        else if (!isExtLine)
                            notCreatePoints.Add(nodePoint);
                        else
                            notCreatePoints.Add(endRoute.node.nodePoint);
                        
                        leftDir = exitDir.RotateBy(Math.PI / 2, _normal).GetNormal();
                        if(pLine.Length>2500)
                            isAdd = true;
                    }
                    else
                    {
                        if (line.Length>1500 && endRoute != null && endRoute.node.isExit && endRoute.node.tag !=null) 
                        {
                            var block = (BlockReference)endRoute.node.tag;
                            var angle = block.Rotation;
                            var longDir = Vector3d.XAxis.RotateBy(angle, Vector3d.ZAxis);
                            var lineAngle = longDir.GetAngleTo(exitDir, Vector3d.ZAxis);
                            lineAngle = lineAngle % Math.PI;
                            var angle30 = Math.PI * 30 / 180;
                            if (Math.Abs(lineAngle) > angle30 && Math.Abs(lineAngle) < Math.PI - angle30)
                                isAdd = true;
                        }
                        if (nextRoute!=null && line.Length > 1000 && pLine.Length > 5000)
                            isAdd = true;
                    }
                }
            }
            else if (line.Length < _lightSpace+ _lightOffset)
            {
                if (nextIsExit)
                {
                    if (pLine != null)
                    {
                        var dot = pDir.DotProduct(exitDir);
                        if (pLine.EndPoint.DistanceTo(nodePoint) > 10)
                            return addLights;
                    }
                }
                if (pLine != null)
                {
                    createPoint = nodePoint;
                    var dot = pDir.DotProduct(exitDir);
                    if (dot > 0.5)
                        createPoint = nodePoint + exitDir.MultiplyBy(_lightOffset);
                    if(!isExtLine)
                        createPoint = createPoint + pDir.MultiplyBy(_lightOffset);

                }
                isAdd = true;
            }
            else
            {
                //线长度比较长这里使用循环去添加布置点位
                int count = (int)Math.Ceiling(line.Length / _lightSpace);
                double step = line.Length / count;
                bool isFirst = true;
                Point3d pointOnLine = nodePoint;
                while (pointOnLine.DistanceTo(nodePoint) <= line.Length-1000)
                {
                    var moveVect = GetHostMoveVector(sNode);
                    createPoint = pointOnLine;
                    createPoint = pointOnLine + moveVect;
                    if (isFirst) 
                    {
                        if (null != moveVect && !moveVect.IsZeroLength())
                        {
                            var dot = moveVect.DotProduct(exitDir);
                            if (dot < 0)
                                createPoint = nodePoint - exitDir.MultiplyBy(_lightOffset);
                            //else
                            //    createPoint = nodePoint + exitDir.MultiplyBy(_lightOffset);
                        }
                    }
                    if (isFirst && !isExtLine)
                        break;
                    if (!_ligthLayouts.Any(c => c.isHoisting && c.linePoint.DistanceTo(pointOnLine) < minSpace))
                    {
                        if (isExtLine && !PointInLaneLine(pointOnLine,_pointInLineDistance))
                            createPoint = pointOnLine;
                        var light1 = new LightLayout(pointOnLine, createPoint, null, leftDir, exitDir, leftDir, sNode, true);
                        light1.isCheckDelete = isFirst && isLightFirst;
                        light1.isTwoSide = isTwoSide;
                        light1.canDelete = canDel;
                        //_ligthLayouts.Add(light1);
                        addLights.Add(light1);
                    }
                    pointOnLine = pointOnLine + exitDir.MultiplyBy(step);
                    isFirst = false;
                }
            }
            //线非疏散路径，判断是否需要指示灯
            if (isAdd && !isExtLine && pLine!= null && !_isHostFirst)
            {
                isAdd = EmgPilotLampCheck.LineNodeNeedHostLight(_wallGraphNodes,_targetInfo.allNodeRoutes,sNode, pDir);
            }
            foreach (var point in notCreatePoints)
            {
                if (!isAdd)
                    break;
                var checkPoint = createPoint.DistanceTo(nodePoint) < minSpace ? nodePoint : createPoint;
                isAdd = checkPoint.DistanceTo(point) > _lightOffset + 100; 
            }
            if (!isAdd)
                return addLights;

            var moveVect1 = GetHostMoveVector(sNode);
            if (null != moveVect1 && !moveVect1.IsZeroLength())
            {
                //if (isExtLine)
                //{
                //    var moveDir = exitDir;
                //    if (null != pLine)
                //    {
                //        moveDir = pDir;
                //    }
                //    var dot = moveVect1.DotProduct(moveDir);
                //    if (dot < 0)
                //        createPoint = nodePoint - moveDir.MultiplyBy(_lightOffset);
                //    else
                //        createPoint = nodePoint + moveDir.MultiplyBy(_lightOffset);
                //}
                //else
                //{
                //    createPoint = nodePoint + moveVect1;
                //}
                createPoint = nodePoint + moveVect1;
            }
            if (_ligthLayouts.Any(c => c.isHoisting && c.linePoint.DistanceTo(createPoint) < minSpace))
                return addLights;
            var prjPoint = EmgPilotLampUtil.PointToLine(createPoint, nodePoint, exitDir);
            if (isExtLine && prjPoint.DistanceTo(line.EndPoint) > _lightSpace) 
            {
                //移动后的排布点，导致到线的终点距离>间距，在中间在添加一个灯
                var createPoint1 = nodePoint  + exitDir.MultiplyBy(prjPoint.DistanceTo(line.EndPoint) / 2);
                var light1 = new LightLayout(createPoint1, createPoint1, null, leftDir, exitDir, leftDir, sNode, true);
                light1.isCheckDelete = false;
                light1.isTwoSide = isTwoSide;
                light1.canDelete = false;
                addLights.Add(light1);
            }
            var light = new LightLayout(nodePoint, createPoint, null, leftDir, exitDir, leftDir, sNode, true);
            light.isCheckDelete = isLightFirst;
            light.isTwoSide = isTwoSide;
            light.canDelete = canDel;
            addLights.Add(light);
            //_ligthLayouts.Add(light);
            return addLights;
        }
        /// <summary>
        /// 根据线的排布在那一侧，计算一个节点需要的偏移量
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        Vector3d GetHostMoveVector(GraphNode node) 
        {
            //获取所在的壁装线信息
            var moveDirs = new List<Vector3d>();
            foreach (var item in _wallGraphNodes) 
            {
                if (item == null || item.nodeDirections == null || item.nodeDirections.Count<1)
                    continue;
                bool nodeInLine = item.nodeDirections.Any(c => c.graphNode.nodePoint.IsEqualTo(node.nodePoint, new Tolerance(1, 1)));
                if (!nodeInLine)
                    continue;
                bool isAdd = true;
                foreach (var dir in moveDirs)
                {
                    if (null == dir || dir.IsZeroLength())
                        continue;
                    double angle = dir.GetAngleTo(item.layoutLineSide);
                    angle %= Math.PI;
                    if (angle < Math.PI / 4)
                        isAdd = false;
                    
                }
                if(isAdd)
                    moveDirs.Add(item.layoutLineSide);
            }
            Vector3d moveVect = new Vector3d(0,0,0);
            foreach (var dir in moveDirs) 
            {
                moveVect += dir.MultiplyBy(_lightOffset);
            }
            return moveVect;
        }

        void ChcekWallLightAddHostingLight() 
        {
            //布置灯具时是根据原始线合并距离比较小，这里将线合并和再进行判断间距，减少连续湾处的不符合条件的情况
            if (_isHostFirst || _lineWallLights ==null || _lineWallLights.Count<1)
                return;
            var objs = new DBObjectCollection();
            _targetInfo.mainLines.ForEach(x => objs.Add(x));
            _targetInfo.assistLines.ForEach(x => objs.Add(x));
            var lines = ThFEILineExtension.LineSimplifier(objs, 50, 50, 50, Math.PI / 180).Cast<Line>().ToList();
            objs.Clear();
            foreach (var line in lines)
                objs.Add(line);
            lines = ThFEILineExtension.LineSimplifier(objs, 50, 2500, 50.0, Math.PI * 15 / 180).Cast<Line>().ToList();
            lines = lines.Where(c => c.Length > 100).OrderByDescending(c=>c.Length).ToList();
            foreach (var mLine in lines)
            {
                if (mLine.Length <= _lightSpace)
                    continue;
                var lineLights = new List<LightLayout>();
                var mLineDir = mLine.LineDirection();
                LineGraphNode lineGraphNode = new LineGraphNode(mLine);
                foreach (var item in _lineWallLights)
                {
                    Line line = item.Key.line;
                    var lineDir = item.Key.lineDir;
                    var sideDir = item.Key.layoutLineSide;
                    if (!EmgPilotLampUtil.LineIsCollinear(line.StartPoint, line.EndPoint, mLine.StartPoint, mLine.EndPoint, 1, 2500, 15))
                        continue;
                    if (lineGraphNode.layoutLineSide.Length < 1) 
                    {
                        var normal = lineDir.CrossProduct(sideDir);
                        if (lineDir.DotProduct(mLineDir) < 0)
                        {
                            lineGraphNode.layoutLineSide = normal.CrossProduct(lineDir);
                        }
                        else 
                        {
                            lineGraphNode.layoutLineSide = lineDir.CrossProduct(normal);
                        }
                    }
                    lineGraphNode.nodeDirections.AddRange(item.Key.nodeDirections);
                    
                    if (null == item.Value || item.Value.Count<1)
                        continue;
                    
                    //获取中根线上的吊装灯，
                    foreach (var light in _ligthLayouts)
                    {
                        if (light == null)
                            continue;
                        if (lineLights.Any(c => c.linePoint.DistanceTo(light.linePoint) < 1000))
                            continue;
                        if (light.isHoisting)
                        {
                            if (light.linePoint.DistanceTo(light.pointInOutSide) > _lightOffset * 2)
                                continue;
                            if (EmgPilotLampUtil.PointInLine(light.linePoint, line, _lightOffset, _lightOffset * 2))
                                lineLights.Add(light);
                        }
                        else
                        {
                            if (item.Value.Any(c => c.linePoint.DistanceTo(light.linePoint) < 100))
                                lineLights.Add(light);
                        }
                    }
                    //没有找到相应的相应的灯，可能是应为端点处的
                    if (lineLights.Count < 1)
                    {
                        foreach (var node in item.Key.nodeDirections)
                        {
                            foreach (var light in _ligthLayouts)
                            {
                                if (light.nearNode.nodePoint.DistanceTo(node.graphNode.nodePoint) > 1000)
                                    continue;
                                if (light.isHoisting)
                                    continue;
                                if (Math.Abs(light.direction.DotProduct(lineDir)) < 0.5)
                                    continue;
                                if (lineLights.Any(c => c.nearNode.nodePoint.DistanceTo(light.nearNode.nodePoint) < 1000))
                                    continue;
                                lineLights.Add(light);
                            }
                        }
                    }
                }
                if (lineLights.Count < 1)
                {
                    //线上没有一个灯，该线段使用吊装灯逻辑
                    List<Curve> curves = new List<Curve>() { mLine };
                    GetLightLayoutPlanB(curves);
                }
                else
                {
                    lineLights = lineLights.OrderBy(c => c.linePoint.DistanceTo(mLine.StartPoint)).ToList();
                    var testDic = lineLights.ToDictionary(c => c, x => x.linePoint.DistanceTo(mLine.StartPoint));
                    var lineAddLights = new List<LightLayout>();
                    for (int i = 0; i < lineLights.Count; i++)
                    {
                        var light = lineLights[i];
                        if (i == 0)
                        {
                            if (light.linePoint.DistanceTo(mLine.StartPoint) > _lightSpace)
                            {
                                //需要添加
                                var addLights = AddHostLightInWallLine(lineGraphNode, mLine.StartPoint, light.linePoint);
                                if (null != addLights && addLights.Count > 0)
                                    lineAddLights.AddRange(addLights);
                            }
                        }
                        if (i == lineLights.Count - 1)
                        {
                            if (light.linePoint.DistanceTo(mLine.EndPoint) > _lightSpace + 500)
                            {
                                //需要添加
                                var addLights = AddHostLightInWallLine(lineGraphNode, light.linePoint, mLine.EndPoint);
                            }
                        }
                        if (lineLights.Count == 1 || i == 0)
                            continue;
                        var pLight = lineLights[i - 1];
                        var space = pLight.linePoint.DistanceTo(light.linePoint);
                        if (space < _lightSpace + 50)
                            continue;
                        if (pLight.isHoisting || light.isHoisting)
                        {
                            var pExitDir = pLight.direction;
                            var exitDir = light.direction;
                            var pDot = pExitDir.DotProduct(mLineDir);
                            var dot = exitDir.DotProduct(mLineDir);
                            if (pLight.isHoisting && light.isHoisting)
                            {
                                //两个灯都是吊装
                                if (Math.Abs(pDot) > 0.3 && Math.Abs(dot) > 0.3)
                                {
                                    //两个吊装灯和线方向一致，中间加灯
                                    var addLights = AddHostLightInWallLine(lineGraphNode, pLight.linePoint, light.linePoint);
                                    if (null != addLights && addLights.Count > 0)
                                        lineAddLights.AddRange(addLights);
                                }
                                else if (Math.Abs(pDot) > 0.3 || Math.Abs(dot) > 0.3)
                                {
                                    //两个有一个吊装和线垂直，进一步判断是否需要加吊装灯
                                }
                                else
                                {
                                    //两个吊装灯都和线垂直，这中情况不考虑中间加灯的情况
                                }
                            }
                            else
                            {
                                //两个灯有一个壁装，一个吊装，进一步判断方向和指向是否一致，进一步判断是否添加吊装，吊装的位置
                                if (pLight.isHoisting)
                                {
                                    //pLight是吊灯
                                    if (Math.Abs(pDot) > 0.3)
                                    {
                                        //前面一个吊灯和线平行
                                        var addLights = AddHostLightInWallLine(lineGraphNode, pLight.linePoint, light.linePoint);
                                        if (null != addLights && addLights.Count > 0)
                                            lineAddLights.AddRange(addLights);
                                    }
                                }
                                else
                                {
                                    if (Math.Abs(dot) > 0.3)
                                    {
                                        //前面一个吊灯和线平行
                                        var addLights = AddHostLightInWallLine(lineGraphNode, pLight.linePoint, light.linePoint);
                                        if (null != addLights && addLights.Count > 0)
                                            lineAddLights.AddRange(addLights);
                                    }
                                }
                            }
                        }
                        else
                        {
                            //两个都是壁装灯，中间等分进行添加灯
                            var addLights = AddHostLightInWallLine(lineGraphNode, pLight.linePoint, light.linePoint);
                            if (null != addLights && addLights.Count > 0)
                                lineAddLights.AddRange(addLights);
                        }
                    }
                    //还需要考虑拐点处是否需要添加吊灯，避免有些极端情况添加吊装后有视野盲区
                    CheckHostAddConner(lineGraphNode, lineLights, lineAddLights);
                }
            }
        }
        List<LightLayout> AddHostLightInWallLine(LineGraphNode lineInfo,Point3d startPoint,Point3d endPoint) 
        {
            var lights = new List<LightLayout>();
            var space = startPoint.DistanceTo(endPoint);
            if (space < _lightSpace + 50)
                return lights;
            int count = (int)Math.Ceiling(space / _lightSpace);
            double step = space / count;
            var addDir = (endPoint - startPoint).GetNormal();
            //两个都是壁装灯，中间等分进行添加灯
            var point = startPoint + addDir.MultiplyBy(step);
            while (true)
            {
                if (point.DistanceTo(endPoint) < 100 || point.DistanceTo(startPoint)> space)
                    break;
                var light1 = _emgWallLight.PointToExitDirection(lineInfo, point, lineInfo.layoutLineSide.Negate(), true);
                var moveVect = GetHostMoveVector(light1.nearNode);
                var createPoint = point + moveVect;
                light1 = new LightLayout(point, createPoint, null, light1.sideLineDir, light1.direction, light1.directionSide, light1.nearNode, true);
                light1.isCheckDelete = false;
                light1.isTwoSide = true;
                light1.canDelete = false;
                point += addDir.MultiplyBy(step);
                if (_ligthLayouts.Any(c => c.isHoisting && c.linePoint.DistanceTo(light1.linePoint) < 2000))
                    continue;
                lights.Add(light1);
                _ligthLayouts.Add(light1);
            }
            return lights;
        }

        void CheckHostAddConner(LineGraphNode lineInfo,List<LightLayout> lineLights,List<LightLayout> addLights) 
        {
            if (addLights == null || addLights.Count < 1)
                return;
            foreach (var item in lineInfo.nodeDirections) 
            {
                var dot = item.outDirection.DotProduct(lineInfo.lineDir);
                if (Math.Abs(dot) > 0.3)
                    //疏散方向和线方向平行，不需要判断
                    continue;
                if (lineLights.Any(c => c.isHoisting && c.linePoint.DistanceTo(item.graphNode.nodePoint) < 1000))
                    //该疏节点有吊灯，不需要后续判断
                    continue;
                var nodeAddLight = addLights.Where(c => c.isHoisting && c.nearNode.nodePoint.DistanceTo(item.graphNode.nodePoint) < 100).ToList();
                if (nodeAddLight == null || nodeAddLight.Count<1)
                    //添加的灯没有指向该节点的
                    continue;
                var nodeLights = lineLights.Where(c => !c.isHoisting && c.nearNode.nodePoint.DistanceTo(item.graphNode.nodePoint) < 100).ToList();
                if (null != nodeLights && nodeLights.Count > 0)
                    nodeAddLight.AddRange(nodeLights);
                else
                    continue;
                var dir1 = nodeLights.FirstOrDefault().direction;
                bool isAdd = false;
                foreach (var tempLight in nodeAddLight) 
                {
                    if (isAdd)
                        break;
                    dot = tempLight.direction.DotProduct(dir1);
                    isAdd = dot < -0.1;
                }
                if (!isAdd)
                    continue;
                //添加的有指向该节点的灯，进一步判断是否需要添加，有些极限情况出现可能行太低，这里不进行考虑，如T口处加的口处的吊灯添加判断
                var moveVect = GetHostMoveVector(item.graphNode);
                var createPoint = item.graphNode.nodePoint + moveVect;
                var light = new LightLayout(item.graphNode.nodePoint, createPoint, null, lineInfo.layoutLineSide, item.outDirection, lineInfo.layoutLineSide, item.graphNode, true);
                if (_ligthLayouts.Any(c => c.isHoisting && c.linePoint.DistanceTo(light.linePoint) < 2000))
                    continue;
                _ligthLayouts.Add(light);
            }
        }
        
        bool CheckMergeHostLine(GraphRoute route,List<Line> unHostLines, ref List<Point3d> notCreatePoints,ref List<GraphNode> hisNodes) 
        {
            if (EmgPilotLampUtil.IsAllHostLine(unHostLines, route))
                return true;
            if (hisNodes.Any(c => c.nodePoint.IsEqualTo(route.node.nodePoint, new Tolerance(1, 1))))
                return false;
            //该路径后续为全吊装，且后续点没有在车道中心线上
            List<Line> lines = EmgPilotLampUtil.RouteToLines(route,out List<GraphNode> routeNodes);
            if (null == lines || lines.Count < 1)
                return false;
            //对线进行合并
            var tempNotCreate =new List<Point3d>(); 
            var objs = new DBObjectCollection();
            lines.ForEach(x => objs.Add(x));
            List<Line> curves = ThFEILineExtension.LineSimplifier(objs, 500, 1500.0, 2500.0, Math.PI * 30 / 180.0).Cast<Line>().ToList();
            objs.Clear();
            curves.ForEach(x => objs.Add(x));
            curves = ThFEILineExtension.LineSimplifier(objs, 500, 1500.0, 2500.0, Math.PI * 30 / 180.0).Cast<Line>().ToList();
            if(curves.Count>1)
                curves = curves.Where(c => c.Length > 1500).ToList();

            //根据线重新构造节点，路径,起点固定，这里不在使用寻路算法
            List<GraphNode> newRouteNodes = new List<GraphNode>();
            var endNode = routeNodes.Last();
            var currentNode = routeNodes.First();
            while (curves.Count>0) 
            {
                Line nearPointLine = null;
                var dis = double.MaxValue;

                foreach (var line in curves) 
                {
                    var disSp = line.StartPoint.DistanceTo(currentNode.nodePoint);
                    var disEp = line.EndPoint.DistanceTo(currentNode.nodePoint);
                    var minDis = Math.Min(disSp,disEp);
                    if (minDis < dis) 
                    {
                        dis = minDis;
                        nearPointLine = line;
                    }
                }
                Line li = curves.Where(c => c.StartPoint.DistanceTo(currentNode.nodePoint) < 20 || c.EndPoint.DistanceTo(currentNode.nodePoint) < 20).FirstOrDefault();
                li = nearPointLine;
                if (li == null)
                    break;
                curves.Remove(li);
                var sp = li.StartPoint;
                var ep = li.EndPoint;
                GraphNode sNode = null,
                    eNode = null;
                if (sp.DistanceTo(currentNode.nodePoint) < ep.DistanceTo(currentNode.nodePoint))
                {
                    sNode = new GraphNode();
                    sNode.nodePoint = sp;

                    eNode = new GraphNode();
                    eNode.nodePoint = ep;
                }
                else 
                {
                    sNode = new GraphNode();
                    sNode.nodePoint = ep;

                    eNode = new GraphNode();
                    eNode.nodePoint = sp;
                }
                newRouteNodes.Add(sNode);
                currentNode = eNode;
                if (curves.Count < 1) 
                {
                    eNode.nodePoint = endNode.nodePoint;
                    eNode.isExit = true;
                    eNode.tag = endNode.tag;
                    eNode.nodeType = endNode.nodeType;
                    newRouteNodes.Add(eNode);
                }
            }
            var newRoute = EmgPilotLampUtil.InitRouteByNodes(newRouteNodes);
            Line pLine = null;
            Point3d? pPoint = null;
            bool isFirst = true;
            while (newRoute != null && newRoute.nextRoute != null)
            {
                if (hisNodes.Any(c => c.nodePoint.IsEqualTo(newRoute.node.nodePoint, new Tolerance(1, 1))))
                    break;
                var sNode = newRoute.node;
                var eNode = newRoute.nextRoute.node;
                var dir = (eNode.nodePoint - sNode.nodePoint).GetNormal();
                Line line = new Line(sNode.nodePoint, eNode.nodePoint);
                if (isFirst && !FirstIsCreate(sNode.nodePoint, line.Length, dir, 5, 100))
                {
                    newRoute = newRoute.nextRoute;
                    pLine = new Line(sNode.nodePoint, eNode.nodePoint);
                    pPoint = sNode.nodePoint;
                    isFirst = false;
                    continue;
                } 
                //hisNodes.Add(newRoute.node);
                //获取线,出口方向
                newRoute = newRoute.nextRoute;
                var addLights = LineAddHostLight(line, pLine, pPoint, sNode, newRoute, true, isFirst, ref tempNotCreate, false, isFirst?2500:1200);
                MoveLightToRealNode(addLights, lines, routeNodes);
                pLine = new Line(sNode.nodePoint, eNode.nodePoint);
                pPoint = sNode.nodePoint;
                isFirst = pLine == null;
                //newRoute = newRoute.nextRoute;
            }
            hisNodes.AddRange(routeNodes);
            return false;
        }
        void MoveLightToRealNode(List<LightLayout> moveLights,List<Line> oldLines, List<GraphNode> routeNodes) 
        {
            if (moveLights == null || moveLights.Count < 1)
                return;
            foreach (var light in moveLights) 
            {
                if (null == light)
                    continue;
                //获取夹角最小的线，且投影点在线上，将点投影到这根线上，并根据修改灯的指向为原始线的方向
                Line nearLine = null;
                double angle = double.MaxValue;
                double dis = double.MaxValue;
                ///这里的线认为是按照疏散方向的，这里不在考虑线的方向问题
                foreach (var line in oldLines) 
                {
                    var prjPoint = EmgPilotLampUtil.PointToLine(light.linePoint, line);
                    if (!EmgPilotLampUtil.PointInLine(prjPoint, line, 10))
                        continue;
                    var lineDir = line.LineDirection();
                    var lineAngle = lineDir.GetAngleTo(light.direction);
                    lineAngle %= Math.PI;
                    if (lineAngle > Math.PI / 2)
                        lineAngle = Math.PI - lineAngle;
                    var tempDis = prjPoint.DistanceTo(light.linePoint);
                    if (angle > lineAngle || (Math.Abs(angle-lineAngle)<Math.PI*5/180 && dis>tempDis) )
                    {
                        nearLine = line;
                        angle = lineAngle;
                        dis = tempDis;
                    }
                }
                if (null == nearLine)
                    continue;
                var newPoint = EmgPilotLampUtil.PointToLine(light.linePoint, nearLine);
                var node = routeNodes.OrderBy(c => c.nodePoint.DistanceTo(nearLine.StartPoint)).FirstOrDefault();
                var sideDir = nearLine.LineDirection().CrossProduct(Vector3d.ZAxis);
                //计算偏移量
                var oldMove = light.pointInOutSide - light.linePoint;
                var xMove = oldMove.DotProduct(light.direction);
                var yMove = oldMove.DotProduct(light.sideLineDir);
                var newMove = sideDir.MultiplyBy(yMove) + nearLine.LineDirection().MultiplyBy(xMove);
                newMove = oldMove;
                var createPoint = light.isCheckDelete ? newPoint + newMove: newPoint;
                var newLight = new LightLayout(newPoint, createPoint, nearLine, light.sideLineDir, nearLine.LineDirection(), sideDir, node, true);
                newLight.canDelete = light.canDelete;
                newLight.endType = light.endType;
                newLight.isCheckDelete = light.isCheckDelete;
                newLight.isTwoExitDir = light.isTwoExitDir;
                newLight.isTwoSide = light.isTwoSide;
                _ligthLayouts.Add(newLight);
            }
        }

        bool PointInLaneLine(Point3d point,double outDis, double extDis=5)
        {
            if (null == _mainLines || _mainLines.Count < 1)
                return false;
            foreach (var item in _mainLines)
            {
                if (EmgPilotLampUtil.PointInLine(point, item,extDis,outDis))
                    return true;
            }
            return false;
        }
        bool FirstIsCreate(Point3d startPoint,double length,Vector3d nodeDir, double outDis, double extDis = 5) 
        {
            return true;
            //这里是为了判断如果吊装疏散路径和壁装路线平行不进行放置灯具
            if (length > _lightSpace)
                return true;
            foreach (var line in _mainLines) 
            {
                if (!EmgPilotLampUtil.PointInLine(startPoint, line, extDis, outDis))
                    continue;
                var lineDir = line.LineDirection();
                if (Math.Abs(lineDir.DotProduct(nodeDir)) > 0.99)
                    return false;
            }
            return true;
        }
    }
}
