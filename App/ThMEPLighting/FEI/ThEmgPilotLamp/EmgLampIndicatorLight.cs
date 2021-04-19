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
        public List<Point3d> testPoints;
        public List<LineGraphNode> _lineGraphNodes;
        private double _lightSpace = 10000;
        private double _lightOffset = 800;
        public EmgLampIndicatorLight(Polyline outPolyline,List<Polyline> columns, List<Polyline> walls, IndicatorLight indicator)
        {
            _maxPolyline = outPolyline;
            _targetInfo = new IndicatorLight();
            _targetColums = new List<Polyline>();
            _targetWalls = new List<Polyline>();
            _ligthLayouts = new List<LightLayout>();
            testPoints = new List<Point3d>();
            _lineGraphNodes = new List<LineGraphNode>();
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
        public List<LightLayout> CalcLayout()
        {
            _ligthLayouts.Clear();
            testPoints.Clear();
            //主要线到出口处使用吊装
            CalcExitLayout();

            //主要，辅助疏散路径上的点的信息获取判断
            CalaMainLayout();

            //对结果进行检查移除多余的节点
            CheckAndRemove(Math.PI*30/180, _lightSpace, false);
            return _ligthLayouts;
        }
        /// <summary>
        /// 出口连接主要疏散路径的的连接线的灯连接
        /// </summary>
        private void CalcExitLayout()
        {
            //主疏散路径，一般是车道中线到出口的最后一段路线，这里用吊装指示.这里吊灯使用双面
            var objs = new DBObjectCollection();
            _targetInfo.exitLines.ForEach(x => objs.Add(x));
            List<Curve> exitLines =ThMEPLineExtension.ExplodeCurves(objs);

            //获取这些线上的节点，优先排布距离出口处距离远的节点
            var lineAllNodes = new List<NodeDirection>();
            var lineTwoExits = new Dictionary<Line,List<NodeDirection>>();
            foreach (var line in exitLines)
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
                        List<GraphRoute> routes = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes,node.graphNode, true);
                        if (null == routes || routes.Count < 1)
                            continue;
                        var route = routes.First();
                        //应为考虑精度，会将线附近的其它点获取到，这里需要将不符合要求的点过滤掉
                        var exitDir = (route.nextRoute.node.nodePoint - route.node.nodePoint).GetNormal();
                        double angle = exitDir.GetAngleTo(dir);
                        angle %= Math.PI;
                        if (angle > Math.PI / 18 && angle < (Math.PI - Math.PI / 18))
                            continue;

                        var newRoute = new NodeDirection(node.nodePointInLine, null, GraphUtils.GetRouteDisToEnd(route), node.outDirection, node.graphNode);
                        newRoute.inDirection.AddRange(node.inDirection);
                        liNodes.Add(newRoute);
                        lineAllNodes.Add(newRoute);
                    }
                }
                if (exitCount > 1 && liNodes.Count>0)
                    lineTwoExits.Add(line as Line, liNodes);
            }
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
                        if (_ligthLayouts.Any(c => c.linePoint.DistanceTo(node.graphNode.nodePoint)<1000))
                            continue;
                        var light = new LightLayout(node.graphNode.nodePoint, node.graphNode.nodePoint, null, leftDir, exitDir, leftDir, node.graphNode, true);
                        light.isTwoExitDir = true;
                        _ligthLayouts.Add(light);
                        hisNodes.Add(node.graphNode);
                    }
                }
            }
            //优先从距离出口最远的点进行开始排布
            lineAllNodes = lineAllNodes.OrderByDescending(c => c.distanceToExit).ToList();
            List<Point3d> notCreatePoints = new List<Point3d>();
            foreach (var node in lineAllNodes) 
            {
                if (hisNodes.Any(c => c.nodePoint.IsEqualTo(node.graphNode.nodePoint, new Tolerance(1, 1))))
                    continue;
                var route = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes,node.graphNode, true).FirstOrDefault();
                if (null == route)
                    continue;
                Line pLine = null;
                while (route.nextRoute != null) 
                {
                    if (hisNodes.Any(c => c.nodePoint.IsEqualTo(route.node.nodePoint, new Tolerance(1, 1))))
                        break;
                    hisNodes.Add(route.node);
                    
                    //获取线,出口方向
                    Line line = new Line(route.node.nodePoint, route.nextRoute.node.nodePoint);
                    bool isExitLine =false;
                    Point3d nodePoint = route.node.nodePoint;
                    Vector3d exitDir = line.LineDirection();
                    Line testLine = new Line(nodePoint, nodePoint + exitDir.MultiplyBy(100));
                    foreach (var item in exitLines) 
                    {
                        if (isExitLine)
                            break;
                        isExitLine = ThGeometryTool.IsCollinearEx(item.StartPoint, item.EndPoint,line.StartPoint, line.EndPoint);
                    }
                    LineAddHostLight(line, pLine, route, isExitLine,ref notCreatePoints);

                    pLine = null;
                    if (isExitLine && route.nextRoute != null)
                        pLine = new Line(route.node.nodePoint, route.nextRoute.node.nodePoint);

                    route = route.nextRoute;
                }
            }
        }
        #region
        private void CalaMainLayout()
        {
            //对线进行合并，这里要考虑连续拐弯的情况，整个一个线上的排布一线的同一侧
            var objs = new DBObjectCollection();
            _targetInfo.mainLines.ForEach(x => objs.Add(x));
            _targetInfo.assistLines.ForEach(x => objs.Add(x));
            List<Curve> curves = ThMEPLineExtension.LineSimplifier(objs, 500, 20.0, 2.0, Math.PI*15 / 180.0).Cast<Curve>().ToList();
            var lineGrapheNodes = InitAllLineNode(curves.Cast<Line>().ToList());
            lineGrapheNodes = lineGrapheNodes.OrderByDescending(c => c.line.Length).ToList();
            foreach (var lineInfo in lineGrapheNodes)
            {
                Point3d sp = lineInfo.line.StartPoint;
                var sideDir = lineInfo.layoutLineSide;
                var lineLength = lineInfo.line.Length;
                if (lineInfo.line.Length < 3000)
                    continue;
                //根据线获取获取排布侧的墙柱
                Polyline pLine = GetSideWallColumns(lineInfo.line, sideDir, 6000,_targetColums,_targetWalls, out List<Polyline> inWalls, out List<Polyline> inColumns);

                GetLightLayoutPlanA(lineInfo,inWalls,inColumns) ;
                //continue;
                if (null == lineInfo.nodeDirections || lineInfo.nodeDirections.Count < 1)
                    continue;
                if (lineInfo.line.Length < 5000)
                    continue;
                foreach (var node in lineInfo.nodeDirections.OrderByDescending(c=>c.inDirection.Count)) 
                {
                    if (_ligthLayouts.Any(c => c.linePoint.DistanceTo(node.nodePointInLine) < 1000))
                        continue;
                    var lineAngle = node.outDirection.GetAngleTo(lineInfo.lineDir);
                    lineAngle = lineAngle % Math.PI;
                    if (lineAngle > Math.PI * 15 / 180 && lineAngle < Math.PI * 165 / 180)
                        continue;
                    Point3d createPoint;
                    bool isAdd = CheckAddHosting(node, lineInfo.layoutLineSide, out bool isTwoSide,out createPoint);
                    if (!isAdd)
                        continue;
                    var light = new LightLayout(node.nodePointInLine, createPoint, null,lineInfo.layoutLineSide,node.outDirection,lineInfo.layoutLineSide , node.graphNode,true);
                    if (_ligthLayouts.Any(c => c.linePoint.DistanceTo(createPoint) < 1500))
                        continue;
                    light.isTwoSide = isTwoSide;
                    _ligthLayouts.Add(light);
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
                createPoint = nodeDirection.nodePointInLine - nodeDirection.outDirection.MultiplyBy(800);
            else
                createPoint = nodeDirection.nodePointInLine - sideLineDir.MultiplyBy(800);
            return isAdd;
        }
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
            foreach (var light in hostingLights) 
            {
                Vector3d dir = light.direction;
                foreach (var checkLight in wallLights) 
                {
                    if (light.pointInOutSide.DistanceTo(checkLight.pointInOutSide) > distance)
                        continue;
                    if (delLights.Any(c => c.pointInOutSide.IsEqualTo(light.pointInOutSide, new Tolerance(1, 1))))
                        continue;
                    Vector3d hostToCheckDir = (checkLight.pointInOutSide - light.pointInOutSide).GetNormal();
                    var dot = dir.DotProduct(hostToCheckDir);
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

        private void GetLightLayoutPlanA(LineGraphNode lineInfo, List<Polyline> inWalls, List<Polyline> inColumns) 
        {
            Point3d sp = lineInfo.line.StartPoint;
            var sideDir = lineInfo.layoutLineSide;
            var lineLength = lineInfo.line.Length;
            testPoints.AddRange(LineSplitToLayoutPoint(lineInfo.line));
            LayoutToStructure toStructure = new LayoutToStructure();
            var startPt = sp;
            var endPoint = startPt + lineInfo.lineDir.MultiplyBy(_lightSpace);
            var pts = new List<Point3d>();
            var pointDirs = new Dictionary<Point3d, Vector3d>();
            List<Point3d> hisPoint = new List<Point3d>();
            while (true)
            {
                pts.Clear();
                if (startPt.DistanceTo(lineInfo.line.EndPoint) < 500 || startPt.DistanceTo(sp)>lineLength+10)
                    break;
                pts.Add(startPt);
                pts.Add(endPoint);
                Line tempLine = new Line(startPt, endPoint);
                GetSideWallColumns(tempLine, sideDir, 6000,inColumns,inWalls, out List<Polyline> newInWalls, out List<Polyline> newInColumns);
                var temp = toStructure.GetLayoutStructPt(pts, newInColumns, newInWalls, lineInfo.lineDir);
                if (null != temp && temp.Count > 0)
                {
                    temp =temp.OrderBy(c => c.Key.DistanceTo(sp)).ToDictionary(x=>x.Key,x=>x.Value);
                    double maxDis = double.MinValue;
                    bool isAdd = true;
                    foreach (var item in temp)
                    {
                        double dis = (item.Key - sp).DotProduct(sideDir);
                        var light = PointToExitDirection(lineInfo, item.Key, item.Value);
                        var iscorrect= LightIsCorrect(item.Key, light.direction, light.directionSide);
                        if (iscorrect) 
                        {
                            maxDis = 1000;
                            break;
                        }
                        Point3d pointInLine = item.Key - sideDir.MultiplyBy(dis);
                        dis = pointInLine.DistanceTo(startPt);
                        if (dis > 10 && dis < _lightSpace+20 && dis > maxDis)
                            maxDis = dis;
                        if (pointDirs.Any(c => c.Key.DistanceTo(item.Key) < 1000))
                            continue;
                        
                        hisPoint.Add(pointInLine);
                        endPoint = pointInLine + lineInfo.lineDir.MultiplyBy(100);
                        if ((pointInLine.DistanceTo(sp) + pointInLine.DistanceTo(lineInfo.line.EndPoint)) > lineLength + 10)
                            continue;
                        if (isAdd)
                        {
                            pointDirs.Add(item.Key, item.Value);
                            isAdd = false;
                        }
                        isAdd = false;
                    }
                    if (maxDis < 100)
                        endPoint = startPt + lineInfo.lineDir.MultiplyBy(1000);
                    else
                        endPoint = startPt + lineInfo.lineDir.MultiplyBy(maxDis);
                }
                startPt = endPoint;
                endPoint = startPt + lineInfo.lineDir.MultiplyBy(_lightSpace);
                if (endPoint.DistanceTo(sp) >= lineLength -100)
                    endPoint = lineInfo.line.EndPoint-lineInfo.lineDir.MultiplyBy(100);
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
        private void GetLightLayoutPlanB(LineGraphNode lineInfo, List<Polyline> inWalls, List<Polyline> inColumns) 
        {
            Point3d sp = lineInfo.line.StartPoint;
            var sideDir = lineInfo.layoutLineSide;
            var lineLength = lineInfo.line.Length;
            if ((null == inWalls || inWalls.Count < 1) && (null == inColumns || inColumns.Count < 1))
            {
                //相交处无梁，无柱使用吊装
            }
            else
            {
                //1.疏散指示灯具间距≤10m（可通过面板配置10/15m）
                //2.转弯/丁字路口/十字路口处，应优先保障人员在转弯/路口处可以看到下一段路的疏散指示灯（下一段路的疏散指示灯应装在车道线外侧），
                //  如不能满足时，需要在转弯处加装吊装的疏散指示灯
                var columnLights = GetLineColumnLayoutPoints(lineInfo, inColumns);
                if (null == columnLights)
                    columnLights = new List<LightLayout>();
                //这跟线上没有柱子
                columnLights = columnLights.OrderBy(c => c.disToHostLineSp).ToList();
                columnLights = columnLights.Where(c => c.disToHostLineSp > 100 && c.disToHostLineSp < (lineLength - 10)).ToList();
                if (columnLights.Count < 1)
                {
                    //没有柱子

                }
                else
                {
                    //判断最近的柱子是否满足
                    Point3d origin = sp;
                    LightLayout lightLayout = null;
                    while (columnLights.Count > 0)
                    {
                        var light = columnLights.FirstOrDefault();
                        double nearDis = light.linePoint.DistanceTo(origin);
                        if (nearDis >= _lightSpace)
                        {
                            if (null == lightLayout)
                            {
                                //点不符合要求,进一步找墙判断是否有墙可以放置
                                origin = light.linePoint;
                                lightLayout = null;
                            }
                            else
                            {
                                _ligthLayouts.Add(lightLayout);
                                origin = lightLayout.linePoint;
                                lightLayout = light;
                            }
                        }
                        else
                        {
                            //该点符合要求
                            lightLayout = light;
                        }
                        columnLights.Remove(light);
                    }
                    if (null != lightLayout)
                        _ligthLayouts.Add(lightLayout);
                }
            }
        }
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
        List<LineGraphNode> GetLineNodes(Line line)
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
                if (Math.Abs(dis) > 2300)
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

        List<LightLayout> GetLineColumnLayoutPoints(LineGraphNode lineInfo, List<Polyline> columns)
        {
            var resLayout = new List<LightLayout>();
            if (null == lineInfo || columns == null || columns.Count < 1)
                return resLayout;
            Point3d sp = lineInfo.line.StartPoint;
            var sideDir = lineInfo.layoutLineSide;
            LayoutToStructure toStructure = new LayoutToStructure();
            foreach (var column in columns)
            {
                //柱子为闭合回路，点会多一个
                var allPts = column.GetPoints().ToList();
                allPts.Remove(allPts.First());
                var sumX = allPts.Sum(c => c.X);
                var sumY = allPts.Sum(c => c.Y);
                Point3d centerPt = new Point3d(sumX / allPts.Count, sumY / allPts.Count, 0);
                //获取线上的投影点
                var temp = (centerPt - sp);
                var tempDis = temp.DotProduct(sideDir);
                var prjPt = centerPt - sideDir.MultiplyBy(tempDis);
                var pointDir = toStructure.GetColumnLayoutPoint(column, prjPt, lineInfo.lineDir);
                var light = PointToExitDirection(lineInfo, pointDir.Value.Item1, pointDir.Value.Item2);
                if(null != light)
                    resLayout.Add(light);
            }
            return resLayout;
        }
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
        /// 获取不经过某些路径的路径
        /// </summary>
        /// <param name="routes"></param>
        /// <param name="targetLines"></param>
        /// <returns></returns>
        List<GraphRoute> GetGrapNotThroughLines(List<GraphRoute> routes, List<Line> targetLines)
        {
            List<GraphRoute> resRoutes = new List<GraphRoute>();
            if (null == routes || null == targetLines)
                return resRoutes;
            foreach (var route in routes)
            {
                if (null == route)
                    continue;
                if (GraphUtils.RouteThroughLines(route, targetLines))
                    continue;
                resRoutes.Add(route);
            }
            return resRoutes;
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
        
        //这个是针对墙柱线上的
        List<Point3d> LineSplitToLayoutPoint(Line line)
        {
            Vector3d dir = (line.EndPoint - line.StartPoint).GetNormal();
            int count = (int)Math.Ceiling(line.Length / _lightSpace);
            double step = line.Length / count;
            List<Point3d> breakPts = new List<Point3d>();
            Point3d sp = line.StartPoint;
            Point3d point = sp ;
            while (point.DistanceTo(sp)<= line.Length)
            {
                breakPts.Add(point);
                point = point + dir.MultiplyBy(step);
            }
            return breakPts;
        }
        List<Point3d> LineSplitToLayoutPoint(Line line,int count)
        {
            Vector3d dir = (line.EndPoint - line.StartPoint).GetNormal();
            double step = line.Length / count;
            List<Point3d> breakPts = new List<Point3d>();
            Point3d sp = line.StartPoint;
            Point3d point = sp;
            while (point.DistanceTo(sp) <= line.Length)
            {
                breakPts.Add(point);
                point = point + dir.MultiplyBy(step);
            }
            return breakPts;
        }
        #endregion
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
        void LineAddHostLight(Line line,Line pLine,GraphRoute route, bool isExtLine,ref List<Point3d> notCreatePoints) 
        {
            Point3d nodePoint = route.node.nodePoint;
            Vector3d exitDir = line.LineDirection();

            bool nextIsExit = route.nextRoute.node.isExit;
            var leftDir = exitDir.RotateBy(Math.PI / 2, _normal).GetNormal();
            bool isTwoSide = true;
            var createPoint = nodePoint + exitDir.MultiplyBy(_lightOffset);
            var nextRoute = route.nextRoute;
            Vector3d pDir = new Vector3d();
            if (null != pLine)
                pDir = (pLine.EndPoint - pLine.StartPoint).GetNormal();
            //长度太小，不进行排布
            if (line.Length < 500)
                return;
            bool isAdd = false;
            if (line.Length < 3000)
            {
                if (pLine == null)
                    isAdd = true;
                else
                {
                    createPoint = nodePoint;
                    if (nextRoute.nextRoute != null)
                    {
                        if (line.Length >= 1000)
                        {
                            exitDir = (nextRoute.nextRoute.node.nodePoint - nextRoute.node.nodePoint).GetNormal();
                            var dot = pDir.DotProduct(exitDir);
                            if (dot < 0)
                            {
                                exitDir = (line.EndPoint - line.StartPoint).GetNormal();
                                if(null != pLine)
                                    createPoint = createPoint + pDir.MultiplyBy(800);
                            }
                            else if (!isExtLine)
                                notCreatePoints.Add(nodePoint);
                            else
                                notCreatePoints.Add(nextRoute.node.nodePoint);
                            leftDir = exitDir.RotateBy(Math.PI / 2, _normal).GetNormal();
                            isAdd = true;
                        }
                        else
                        {
                            if (!isExtLine)
                                notCreatePoints.Add(nodePoint);
                        }
                    }
                    else
                    {
                        if (line.Length > 1500 && pLine.Length > 5000)
                            isAdd = true;
                    }
                }
            }
            else if (line.Length < 10000)
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
                        createPoint = createPoint + pDir.MultiplyBy(800);

                }
                isAdd = true;
            }
            else
            {
                //线长度比较长这里使用循环去添加布置点位
                int count = (int)Math.Ceiling(line.Length / _lightSpace);
                double step = line.Length / count;
                while (createPoint.DistanceTo(nodePoint) <= line.Length)
                {
                    var light1 = new LightLayout(createPoint, createPoint, null, leftDir, exitDir, leftDir, route.node, true);
                    light1.isTwoSide = isTwoSide;
                    _ligthLayouts.Add(light1);
                    createPoint = createPoint + exitDir.MultiplyBy(step);
                }
            }
            if (!isAdd)
                return;
            if (notCreatePoints.Any(c => c.DistanceTo(createPoint) < 900))
                return;
            if (_ligthLayouts.Any(c => c.linePoint.DistanceTo(createPoint) < 1000))
                return;
            var light = new LightLayout(createPoint, createPoint, null, leftDir, exitDir, leftDir, route.node, true);
            light.isTwoSide = isTwoSide;
            _ligthLayouts.Add(light);
        }
    }
   
}
