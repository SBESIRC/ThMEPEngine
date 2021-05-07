using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
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
        private double _lightSpace = 10000;//灯具最大间距
        private double _lightOffset = 800;//单方向灯具偏移距离
        private double _lightDeleteMaxSpace = 10000;
        private double _lightDeleteMaxAngle = 30;
        public List<LineGraphNode> _wallGraphNodes;//壁装的在线的那一侧
        private List<GraphNode> _hostLightNodes;
        public EmgLampIndicatorLight(Polyline outPolyline,List<Polyline> columns, List<Polyline> walls, IndicatorLight indicator)
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
            _targetInfo = indicator;
        }
        public List<LightLayout> CalcLayout(bool isHostFirst)
        {
            _ligthLayouts.Clear();
            _wallGraphNodes.Clear();
            _hostLightNodes.Clear();

            //主要，辅助疏散路径（壁装）上的点的信息获取判断
            CalcMainLayout(isHostFirst);

            //主要线到出口处使用吊装
            CalcExitLayout();

            //拐角处的吊装判断
            CalcMainCornerHost();

            RemoveCornerHost();

            //辅助疏散路径（吊装计算）
            CalcAssitHostLayout();

            //对结果进行检查移除多余的节点
            CheckAndRemove(Math.PI* _lightDeleteMaxAngle / 180, _lightDeleteMaxSpace, true);
            return _ligthLayouts;
        }


        /// <summary>
        /// 主要疏散路径 - 壁装(或吊装)  辅助疏散路径 - 壁装(或吊装) 指示灯布置计算
        /// </summary>
        private void CalcMainLayout(bool isHostFirst)
        {
            //对线进行合并，这里要考虑连续拐弯的情况，整个一个线上的排布一线的同一侧
            var objs = new DBObjectCollection();
            _targetInfo.mainLines.ForEach(x => objs.Add(x));
            _targetInfo.assistLines.ForEach(x => objs.Add(x));
            List<Curve> curves = ThMEPLineExtension.LineSimplifier(objs, 500, 20.0, 2.0, Math.PI * 15 / 180.0).Cast<Curve>().ToList();
            _wallGraphNodes = InitAllLineNode(curves.Cast<Line>().ToList());
            _wallGraphNodes = _wallGraphNodes.OrderByDescending(c => c.line.Length).ToList();
            if (!isHostFirst)
            {
                foreach (var lineInfo in _wallGraphNodes)
                {
                    if (lineInfo.line.Length < 3000)
                        continue;
                    //根据线获取获取排布侧的墙柱
                    GetSideWallColumns(lineInfo.line, lineInfo.layoutLineSide, 6000, _targetColums, _targetWalls, out List<Polyline> inWalls, out List<Polyline> inColumns);

                    GetLightLayoutPlanA(lineInfo, inWalls, inColumns);
                }
            }
            else 
            {
                GetLightLayoutPlanB();
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
            List<Curve> exitLines = ThMEPLineExtension.ExplodeCurves(objs);

            var lineAllNodes = GetHostLineNodes(exitLines, out Dictionary<Line, List<NodeDirection>> lineTwoExits, out Dictionary<Line, List<NodeDirection>> noNodeLines);
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
            LineNodeToHostLight(exitLines, lineAllNodes, ref hisNodes, false);
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

        /// <summary>
        /// 计算主要疏散路径，壁装拐角处的吊装判断和生成
        /// </summary>
        private void CalcMainCornerHost()
        {
            if (null == _wallGraphNodes || _wallGraphNodes.Count < 1)
                return;
            ///获取需要布置的点，并记录线排布在那一侧
            var nodeSideDirs = new Dictionary<NodeDirection,Vector3d>();
            foreach (var lineNodes in _wallGraphNodes) 
            {
                if (null == lineNodes.nodeDirections || lineNodes.nodeDirections.Count < 1)
                    continue;
                if (lineNodes.line.Length < 5000)
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
                if (_ligthLayouts.Any(c => c.linePoint.DistanceTo(item.Key.nodePointInLine) < 1000))
                    continue;
                Point3d createPoint;
                bool isAdd = CheckAddHosting(item.Key, item.Value, out bool isTwoSide, out createPoint);
                if (!isAdd)
                    continue;
                var moveVect = GetHostMoveVector(item.Key.graphNode);
                createPoint = item.Key.graphNode.nodePoint + moveVect;
                if (_ligthLayouts.Any(c => c.linePoint.DistanceTo(createPoint) < 1500))
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
            List<Curve> mainLines = ThMEPLineExtension.ExplodeCurves(objs);
            if (null == mainLines || mainLines.Count < 1)
                return;
            var allLineNodes = GetHostLineNodes(mainLines, out Dictionary<Line, List<NodeDirection>> lineTwoExits, out Dictionary<Line, List<NodeDirection>> noNodeLines);
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
        }
        /// <summary>
        /// 辅助疏散路径 - 吊装指示灯布置计算
        /// </summary>
        private void CalcAssitHostLayout()
        {
            var objs = new DBObjectCollection();
            _targetInfo.assistHostLines.ForEach(x => objs.Add(x));
            List<Curve> assistHost = ThMEPLineExtension.ExplodeCurves(objs);
            if (null == assistHost || assistHost.Count < 1)
                return;
            //获取这些线上的节点，优先排布距离出口处距离远的节点
            var lineAllNodes = GetHostLineNodes(assistHost, out Dictionary<Line, List<NodeDirection>> lineTwoExits, out Dictionary<Line, List<NodeDirection>> noNodeLines);
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
        private List<NodeDirection> GetHostLineNodes(List<Curve> hostCurves,out Dictionary<Line, List<NodeDirection>> lineTwoExits,out Dictionary<Line, List<NodeDirection>> lineGraphNodes) 
        {
            //获取这些线上的节点，优先排布距离出口处距离远的节点
            var lineAllNodes = new List<NodeDirection>();
            lineTwoExits = new Dictionary<Line, List<NodeDirection>>();
            lineGraphNodes = new Dictionary<Line, List<NodeDirection>>();
            foreach (var line in hostCurves)
            {
                var liNodes = new List<NodeDirection>();
                Point3d sp = line.StartPoint;
                var dir = (line.EndPoint - line.StartPoint).GetNormal();
                var leftDir = dir.RotateBy(Math.PI / 2, _normal).GetNormal();
                var lineNodes = GetLineNodes(line as Line);
                
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
                        angle %= Math.PI;
                        if (angle > Math.PI / 18 && angle < (Math.PI - Math.PI / 18))
                            continue;
                        var nodeInfo = new NodeDirection(node.nodePointInLine, null, GraphUtils.GetRouteDisToEnd(route), node.outDirection, node.graphNode);
                        nodeInfo.inDirection.AddRange(node.inDirection);
                        liNodes.Add(nodeInfo);
                        lineAllNodes.Add(nodeInfo);
                    }
                }
                lineGraphNodes.Add(line as Line, liNodes);
                if (exitCount > 1 && liNodes.Count > 0)
                    lineTwoExits.Add(line as Line, liNodes);
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
        private void LineNodeToHostLight(List<Curve> hostLines,List<NodeDirection> lineAllNodes,ref List<GraphNode> hisNodes,bool unHostLineExit) 
        {
            lineAllNodes = lineAllNodes.OrderByDescending(c => c.distanceToExit).ToList();
            List<Point3d> notCreatePoints = new List<Point3d>();
            foreach (var node in lineAllNodes)
            {
                if (hisNodes.Any(c => c.nodePoint.IsEqualTo(node.graphNode.nodePoint, new Tolerance(1, 1))))
                    continue;
                var route = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes, node.graphNode, true).FirstOrDefault();
                bool isFirst = true;
                if (null == route)
                    continue;
                Line pLine = null;
                Point3d? pPoint = null;
                while (route != null && route.nextRoute != null)
                {
                    if (hisNodes.Any(c => c.nodePoint.IsEqualTo(route.node.nodePoint, new Tolerance(1, 1))))
                        break;
                    hisNodes.Add(route.node);
                    //获取线,出口方向
                    var sNode = route.node;
                    var eNode = route.nextRoute.node;
                    var dir = (eNode.nodePoint - sNode.nodePoint).GetNormal();
                    bool isExitLine = hostLines.Any(c => c != null && ThGeometryTool.IsOverlapEx(c.StartPoint, c.EndPoint, sNode.nodePoint, eNode.nodePoint));
                    if (!isExitLine)
                    {
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
                    Line line = new Line(sNode.nodePoint, eNode.nodePoint);
                    isExitLine = hostLines.Any(c => c != null && ThGeometryTool.IsOverlapEx(c.StartPoint, c.EndPoint, line.StartPoint, line.EndPoint));
                    if (unHostLineExit && !isExitLine)
                        break;
                    LineAddHostLight(line, pLine, pPoint, sNode, route, isExitLine, isFirst, ref notCreatePoints);
                    pLine = null;
                    if (isExitLine && route.nextRoute != null)
                        pLine = new Line(sNode.nodePoint, eNode.nodePoint);
                    if (isExitLine)
                        pPoint = sNode.nodePoint;
                    isFirst = false;
                }
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
                foreach (var checkLight in wallLights) 
                {
                    if (light.pointInOutSide.DistanceTo(checkLight.pointInOutSide) > distance)
                        continue;
                    if (delLights.Any(c => c.pointInOutSide.IsEqualTo(light.pointInOutSide, new Tolerance(1, 1))))
                        continue;
                    //同方向的指示灯不需要删除
                    var checkDir = checkLight.direction;
                    var dot = checkDir.DotProduct(dir);
                    if (Math.Abs(dot) > angleCos)
                        continue;
                    Vector3d hostToCheckDir = (checkLight.pointInOutSide - light.pointInOutSide).GetNormal();
                    dot = dir.DotProduct(hostToCheckDir);
                    double angle = dir.GetAngleTo(hostToCheckDir, _normal);
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

        /// <summary>
        /// 主疏散路径-壁装
        /// </summary>
        /// <param name="lineInfo"></param>
        /// <param name="inWalls"></param>
        /// <param name="inColumns"></param>
        private void GetLightLayoutPlanA(LineGraphNode lineInfo, List<Polyline> inWalls, List<Polyline> inColumns) 
        {
            Point3d sp = lineInfo.line.StartPoint;
            var sideDir = lineInfo.layoutLineSide;
            var lineLength = lineInfo.line.Length;
            LayoutToStructure toStructure = new LayoutToStructure();
            var startPt = sp;
            var endPoint = startPt + lineInfo.lineDir.MultiplyBy(_lightSpace);
            var pts = new List<Point3d>();
            var pointDirs = new Dictionary<Point3d, Vector3d>();
            //以线的起点开始，最大间隔10米找可以布置的墙或柱，
            //首先获取起点的对应的排布点，后续递归去找相应的点
            while (true)
            {
                pts.Clear();
                if (startPt.DistanceTo(lineInfo.line.EndPoint) < 500 || startPt.DistanceTo(sp)>lineLength+10)
                    break;
                pts.Add(startPt);
                pts.Add(endPoint);
                Line tempLine = new Line(startPt, endPoint);
                //获取该段线可以相交到的墙或柱
                GetSideWallColumns(tempLine, sideDir, 6000,inColumns,inWalls, out List<Polyline> newInWalls, out List<Polyline> newInColumns);
                var temp = toStructure.GetLayoutStructPt(pts, newInColumns, newInWalls, lineInfo.lineDir);
                if (null != temp && temp.Count > 0)
                {
                    temp =temp.OrderBy(c => c.Key.DistanceTo(sp)).ToDictionary(x=>x.Key,x=>x.Value);
                    double maxDis = double.MinValue;
                    //获取相应的下一个开始排布的点位
                    bool isAdd = true;
                    foreach (var item in temp)
                    {
                        double dis = (item.Key - sp).DotProduct(sideDir);
                        Point3d pointInLine = item.Key - sideDir.MultiplyBy(dis);
                        dis = pointInLine.DistanceTo(startPt);
                        if (dis > 10 && dis < _lightSpace+10 && dis > maxDis)
                            maxDis = dis;
                        if (pointDirs.Any(c => c.Key.DistanceTo(item.Key) < 1000))
                            continue;
                        if ((pointInLine.DistanceTo(sp) + pointInLine.DistanceTo(lineInfo.line.EndPoint)) > lineLength + 10)
                            continue;
                        if (isAdd) 
                        {
                            pointDirs.Add(item.Key, item.Value);
                            isAdd = false;
                        }
                    }
                    endPoint = startPt + lineInfo.lineDir.MultiplyBy(maxDis < 100?1000:maxDis);
                }
                startPt = endPoint;
                endPoint = startPt + lineInfo.lineDir.MultiplyBy(_lightSpace);
                if (endPoint.DistanceTo(sp) >= lineLength -100)
                    endPoint = lineInfo.line.EndPoint;
            }
            if (null == pointDirs || pointDirs.Count < 1)
                return;
            foreach (var item in pointDirs)
            {
                if (item.Key == null || item.Value == null)
                    continue;
                var light = PointToExitDirection(lineInfo, item.Key, item.Value);
                if (null == light)
                    continue;
                _ligthLayouts.Add(light);
            }
        }
        /// <summary>
        /// 主要疏散路径 -吊装
        /// </summary>
        private void GetLightLayoutPlanB() 
        {
            var objs = new DBObjectCollection();
            _targetInfo.mainLines.ForEach(x => objs.Add(x));
            _targetInfo.assistLines.ForEach(x => objs.Add(x));
            List<Curve> curves = ThMEPLineExtension.LineSimplifier(objs, 500, 20.0, 2.0, Math.PI * 15 / 180.0).Cast<Curve>().ToList();
            var lineAllNodes = GetHostLineNodes(curves, out Dictionary<Line, List<NodeDirection>> lineTwoExits, out Dictionary<Line, List<NodeDirection>> lineNodes);
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
                            var prjPoint = PointToLine(sNode.nodePoint, item as Line);
                            if (prjPoint.DistanceTo(sNode.nodePoint) < 2000  && prjPoint.IsPointOnLine(item as Line, 100))
                                pointInLines.Add(item as Line);
                            var test = (item as Line).IsOnLine(prjPoint);
                        }
                        if(pointInLines.Count <=1)
                            line = new Line(line.StartPoint + dir.MultiplyBy(5000), line.EndPoint);
                    }
                       
                    LineAddHostLight(line, null, null, sNode, route, false, false, ref notCreatePoints);
                    
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
        /// 初始化线的节点信息
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        List<LineGraphNode> InitAllLineNode(List<Line> lines) 
        {
            var lineGrapheNode = new List<LineGraphNode>();
            //step1 获取每一根线上的节点
            foreach (var line in lines)
            {
                if (null == line)
                    continue;
                var lineNodes = GetLineNodes(line);
                lineGrapheNode.AddRange(lineNodes);
            }
            //step2 根据每个节点的入口方向
            foreach (var lineNode in lineGrapheNode) 
            {
                if (null == lineNode || lineNode.nodeDirections == null || lineNode.nodeDirections.Count < 1)
                    continue;
                foreach (var node in lineNode.nodeDirections) 
                {
                    if (null == node)
                        continue;
                    //获取其他包含该点的其它线，判断该点的入度
                    var otherNodes = GetOtherNode(lineNode.line, node.graphNode, lineGrapheNode);
                    if (otherNodes == null || otherNodes.Count < 1)
                        continue;
                    double angle = node.outDirection.GetAngleTo(lineNode.lineDir, _normal);
                    angle = angle % Math.PI;
                    bool exitDirIsLineDir = angle < Math.PI / 6 || angle > Math.PI * 5 / 6;
                    if (!exitDirIsLineDir)
                    {
                        continue;
                    }
                    foreach (var other in otherNodes) 
                    {
                        var addDir = (node.graphNode.nodePoint - other.nodePoint).GetNormal();
                        node.inDirection.Add(addDir);
                    }
                }
                //step3 根据线上的点的出入度 更新该排布的方向
                var dir = lineNode.lineDir;
                var leftDir = dir.RotateBy(Math.PI / 2, _normal).GetNormal();
                int lefeInCount = 0,rightInCount=0;
                foreach (var node in lineNode.nodeDirections) 
                {
                    if (node == null || null == node.inDirection || node.inDirection.Count<1)
                        continue;
                    foreach (var inDir in node.inDirection) 
                    {
                        double dot = inDir.DotProduct(leftDir);
                        if (dot>0)
                            lefeInCount += 1;
                        else
                            rightInCount += 1;
                    }
                }
                if (lefeInCount >= rightInCount)
                {
                    lineNode.layoutLineSide = leftDir;
                }
                else 
                {
                    lineNode.layoutLineSide = leftDir.Negate();
                }
            }

            return lineGrapheNode;
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
                List<GraphRoute> routes = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes,node, true);
                if (null == routes || routes.Count < 1)
                    continue;
                GraphRoute nearRoute = routes.FirstOrDefault();
                var exitDir = (nearRoute.nextRoute.node.nodePoint - nearRoute.node.nodePoint).GetNormal();
                NodeDirection nodeDir = new NodeDirection(pointInLine, nearRoute, GraphUtils.GetRouteDisToEnd(nearRoute), exitDir, node);
                liNodes.Add(nodeDir);
            }
            var lineGraphe = new LineGraphNode(line);
            lineGraphe.nodeDirections.AddRange(liNodes);
            LineGrapheNode.Add(lineGraphe);
            return LineGrapheNode;
        }
        /// <summary>
        /// 获取某个线上的经过节点的其它节点
        /// </summary>
        /// <param name="line"></param>
        /// <param name="node"></param>
        /// <param name="otherNodeDirs"></param>
        /// <returns></returns>
        List<GraphNode> GetOtherNode(Line line,GraphNode node,List<LineGraphNode> otherNodeDirs) 
        {
            List<GraphNode> otherNodes = new List<GraphNode>();
            var normal = new Vector3d(0, 0, 1);
            var dir = (line.EndPoint - line.StartPoint).GetNormal();
            foreach (var nodeDir in otherNodeDirs) 
            {
                var liDir = nodeDir.lineDir;
                double angel = liDir.GetAngleTo(dir, normal);
                angel = angel % Math.PI;
                if (angel < Math.PI / 6 || angel > Math.PI * 5 / 6)
                    continue;
                bool isIn = false;
                foreach (var item in nodeDir.nodeDirections) 
                {
                    if (isIn)
                        break;
                    if (item == null || item.graphNode == null)
                        continue;
                    isIn = item.graphNode.nodePoint.IsEqualTo(node.nodePoint, new Tolerance(1, 1));
                }
                if (!isIn)
                    continue;
                foreach (var item in nodeDir.nodeDirections)
                {
                    if (item == null || item.graphNode == null)
                        continue;
                    if (!item.graphNode.nodePoint.IsEqualTo(node.nodePoint, new Tolerance(1, 1)))
                    {
                        otherNodes.Add(item.graphNode);
                    }
                }
            }
            return otherNodes;
        }

        /// <summary>
        /// 根据节点信息，创建在线的那一侧，进行生成相应灯的信息
        /// </summary>
        /// <param name="lineInfo"></param>
        /// <param name="point"></param>
        /// <param name="createSideDir"></param>
        /// <returns></returns>
        LightLayout PointToExitDirection(LineGraphNode lineInfo,Point3d point,Vector3d createSideDir) 
        {
            var sp = lineInfo.line.StartPoint;
            var sideDir = lineInfo.layoutLineSide;
            var temp = (point - sp);
            var tempDis = temp.DotProduct(sideDir);
            var prjPt = point - sideDir.MultiplyBy(tempDis);
            double nearDis = double.MaxValue;
            Vector3d exitDir = new Vector3d();
            GraphRoute nearRoute = null;
            GraphNode nearNode = null;
            foreach (var route in lineInfo.nodeDirections)
            {
                List<GraphRoute> routes = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes,route.graphNode, true);
                if (null == routes || routes.Count < 1)
                    continue;
                var tempRoute = routes.FirstOrDefault();
                var tempDir = (tempRoute.nextRoute.node.nodePoint - tempRoute.node.nodePoint).GetNormal();
                double dis = GraphUtils.GetRouteDisToEnd(tempRoute) + route.nodePointInLine.DistanceTo(prjPt);
                if (dis < nearDis)
                {
                    nearNode = route.graphNode;
                    if (prjPt.DistanceTo(route.graphNode.nodePoint) > 5)
                        exitDir = (route.graphNode.nodePoint - prjPt).GetNormal();
                    else
                        exitDir = tempDir;
                    nearDis = dis;
                    nearRoute = tempRoute;
                }
            }
            var endNode = GraphUtils.GraphRouteEndNode(nearRoute);
            LightLayout lightLayout = new LightLayout(prjPt, point, lineInfo.line, sideDir, exitDir, createSideDir, nearNode);
            lightLayout.isTwoSide = false;
            if (null != endNode)
                lightLayout.endType = endNode.nodeType;
            return lightLayout;
        }
        
        
        /// <summary>
        /// 获取线一侧的墙柱
        /// </summary>
        /// <param name="line"></param>
        /// <param name="sideDir"></param>
        /// <param name="sideDis"></param>
        /// <param name="walls"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        Polyline GetSideWallColumns(Line line, Vector3d sideDir, double sideDis,List<Polyline> targetColumns,List<Polyline> targetWalls, out List<Polyline> walls,out List<Polyline> columns) 
        {
            walls = new List<Polyline>();
            columns = new List<Polyline>();
            Polyline polyLine = LineToPolyline(line, sideDir, sideDis);
            if (null == polyLine)
                return null;
            //获取柱
            if (null != targetColumns && targetColumns.Count > 0) 
            {
                var objs = new DBObjectCollection();
                targetColumns.ForEach(x => objs.Add(x));
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyLine).Cast<Polyline>().ToList();
            }
            //获取墙
            if (null != targetWalls && targetWalls.Count > 0) 
            {
                var objs = new DBObjectCollection();
                targetWalls.ForEach(x => objs.Add(x));
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyLine).Cast<Polyline>().ToList();
            }
            return polyLine;
        }
        /// <summary>
        /// 将线按照一侧方向进行外扩为Polyline
        /// </summary>
        /// <param name="line"></param>
        /// <param name="sideDir"></param>
        /// <param name="sideDis"></param>
        /// <returns></returns>
        Polyline LineToPolyline(Line line, Vector3d sideDir, double sideDis) 
        {
            if (null == line)
                return null;
            Point3d sp = line.StartPoint;
            Point3d ep = line.EndPoint;
            Vector3d lineDir = (ep - sp).GetNormal();
            double dot = lineDir.DotProduct(sideDir);
            if (Math.Abs(dot) >0.9)//扩展方向和线的夹角太小
                return null;
            Point3d spNext = sp + sideDir.MultiplyBy(sideDis);
            Point3d epNext = ep + sideDir.MultiplyBy(sideDis);

            Point2d sp2d = new Point2d(sp.X, sp.Y);
            Point2d ep2d = new Point2d(ep.X, ep.Y);
            Point2d sp2dNext = new Point2d(spNext.X, spNext.Y);
            Point2d ep2dNext = new Point2d(epNext.X, epNext.Y);
            Polyline polyline = new Polyline ();
            polyline.AddVertexAt(0, sp2d, 0, 0, 0);
            polyline.AddVertexAt(1, ep2d, 0, 0, 0);
            polyline.AddVertexAt(2, ep2dNext, 0, 0, 0);
            polyline.AddVertexAt(3, sp2dNext, 0, 0, 0);
            polyline.Closed = true;
            return polyline;
        }

        /// <summary>
        /// 判断生生成的灯是否符合要求，如果被边框穿过，不符合要求
        /// （有些框线穿过边框，但需要保留）
        /// </summary>
        /// <param name="createPoint"></param>
        /// <param name="arrowDir"></param>
        /// <param name="outDir"></param>
        /// <returns></returns>
        bool LightIsCorrect(Point3d createPoint,Vector3d arrowDir,Vector3d outDir) 
        {
            List<Line> lines = new List<Line>();
            var sp = createPoint + arrowDir.MultiplyBy(250);
            var ep = createPoint - arrowDir.MultiplyBy(250);
            var spOut = sp + outDir.MultiplyBy(250);
            var epOut = ep + outDir.MultiplyBy(250);
            lines.Add(new Line(sp, ep));
            lines.Add(new Line(ep, epOut));
            lines.Add(new Line(epOut, spOut));
            lines.Add(new Line(spOut, sp));
            bool isIntersection = false;
            var maxLines = _maxPolyline.ExplodeLines();
            foreach (var line in lines) 
            {
                if (isIntersection)
                    break;
                var liDir = (line.EndPoint - line.StartPoint).GetNormal();
                foreach (var target in maxLines) 
                {
                    if (isIntersection)
                        break;
                    var targetDir = (target.EndPoint - target.StartPoint).GetNormal();
                    double angle = liDir.GetAngleTo(targetDir);
                    angle = angle % Math.PI;
                    if (angle < Math.PI / 18 || angle > (Math.PI - Math.PI / 18))
                        continue;
                    var res = ThCADCoreNTSLineExtension.Intersection(line, target, Intersect.OnBothOperands);
                    if (null != res)
                    {
                        Point3d pt = new Point3d(res.X, res.Y, createPoint.Z);
                        if (pt.DistanceTo(line.StartPoint) < 5 || pt.DistanceTo(line.EndPoint) < 5)
                            continue;
                        isIntersection = true;
                    }
                }
            }
            return isIntersection;
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
        void LineAddHostLight(Line line,Line pLine,Point3d? pPoint,GraphNode sNode,GraphRoute endRoute, bool isExtLine,bool isLightFirst,ref List<Point3d> notCreatePoints) 
        {
            Point3d nodePoint = line.StartPoint;
            Vector3d exitDir = line.LineDirection();

            bool nextIsExit = endRoute !=null? endRoute.node.isExit:false;
            var leftDir = exitDir.RotateBy(Math.PI / 2, _normal).GetNormal();
            bool isTwoSide = true;
            var createPoint = nodePoint + exitDir.MultiplyBy(_lightOffset);
            var nextRoute = endRoute == null? null: endRoute.nextRoute;
            //长度太小，不进行排布
            if (line.Length < 500)
                return;
            bool isAdd = false;
            Vector3d pDir = new Vector3d();
            if (pPoint.HasValue) 
                pDir = (nodePoint - pPoint.Value).GetNormal();
                
            if (line.Length < 1500)
            {
                if (pLine == null)
                    isAdd = true;
                else
                {
                    createPoint = nodePoint;
                    if (nextRoute != null)
                    {
                        exitDir = (nextRoute.node.nodePoint - endRoute.node.nodePoint).GetNormal();
                        var dot = pDir.DotProduct(exitDir);
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
                        isAdd = true;
                    }
                    else
                    {
                        if (nextRoute!=null && line.Length > 1000 && pLine.Length > 5000)
                            isAdd = true;
                    }
                }
            }
            else if (line.Length < _lightSpace)
            {
                if (nextIsExit)
                {
                    if (pLine != null)
                    {
                        var dot = pDir.DotProduct(exitDir);
                        if (dot < 0 || pLine.EndPoint.DistanceTo(nodePoint) > 10)
                            return;
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
                    if (isFirst) 
                    {
                        if (null != moveVect && !moveVect.IsZeroLength())
                        {
                            var dot = moveVect.DotProduct(exitDir);
                            if (dot < 0)
                                createPoint = nodePoint - exitDir.MultiplyBy(_lightOffset);
                            else
                                createPoint = nodePoint + exitDir.MultiplyBy(_lightOffset);
                        }
                    }
                    if (!isExtLine) 
                        createPoint = pointOnLine +moveVect;

                    if (!_ligthLayouts.Any(c => c.isHoisting && c.linePoint.DistanceTo(createPoint) < 1500))
                    {
                        var light1 = new LightLayout(pointOnLine, createPoint, null, leftDir, exitDir, leftDir, sNode, true);
                        light1.isCheckDelete = isFirst && isLightFirst;
                        light1.isTwoSide = isTwoSide;
                        _ligthLayouts.Add(light1);
                    }
                    pointOnLine = pointOnLine + exitDir.MultiplyBy(step);
                    isFirst = false;
                }
            }
            if (!isAdd)
                return;
            if (notCreatePoints.Any(c => c.DistanceTo(createPoint) < 900))
                return;

            var moveVect1 = GetHostMoveVector(sNode);
            if (null != moveVect1 && !moveVect1.IsZeroLength())
            {
                if (isExtLine)
                {
                    var moveDir = exitDir;
                    if (null != pLine)
                    {
                        moveDir = pDir;
                    }
                    var dot = moveVect1.DotProduct(moveDir);
                    if (dot < 0)
                        createPoint = nodePoint - moveDir.MultiplyBy(_lightOffset);
                    else
                        createPoint = nodePoint + moveDir.MultiplyBy(_lightOffset);
                }
                else 
                {
                    createPoint = nodePoint + moveVect1;
                }
                
            }
            if (_ligthLayouts.Any(c => c.isHoisting && c.linePoint.DistanceTo(createPoint) < 1500))
                return;
            var light = new LightLayout(nodePoint, createPoint, null, leftDir, exitDir, leftDir, sNode, true);
            light.isCheckDelete = isLightFirst;
            light.isTwoSide = isTwoSide;
            _ligthLayouts.Add(light);
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

        Point3d PointToLine(Point3d point,Line line)
        {
            Point3d lineSp = line.StartPoint;
            Vector3d lineDirection = (line.EndPoint - line.StartPoint).GetNormal();
            var vect = point - lineSp;
            var dot = vect.DotProduct(lineDirection);
            return lineSp + lineDirection.MultiplyBy(dot);
        }
    }
   
}
