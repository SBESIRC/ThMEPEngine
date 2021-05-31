using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;

namespace ThMEPLighting.FEI.ThEmgPilotLamp
{
    public class EmgWallLight
    {
        private Vector3d _normal = Vector3d.ZAxis;
        private double _wallLightMergeAngle = 45;
        private IndicatorLight _targetInfo;
        private Polyline _maxPolyline;
        private double _lineSideSpaceExt = 6000;
        private List<Polyline> _targetColums;
        private double _minDisToLight = 2500;
        private List<Polyline> _targetWalls;
        private double _lightSpace = 10000;//灯具最大间距
        //壁装的在线的那一侧
        public EmgWallLight(Polyline outPolyline,IndicatorLight targetInfo, List<Polyline> columns, List<Polyline> walls, double lineMergAngle,double maxSpace) 
        {
            _targetInfo = targetInfo;
            _wallLightMergeAngle = lineMergAngle;
            _maxPolyline = outPolyline;
            _targetColums = new List<Polyline>();
            _targetWalls = new List<Polyline>();
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
        }
        public List<LineGraphNode> GetLineWallGraphNodes() 
        {
            //对线进行合并，这里要考虑连续拐弯的情况，整个一个线上的排布一线的同一侧
            var objs = new DBObjectCollection();
            _targetInfo.mainLines.ForEach(x => objs.Add(x));
            _targetInfo.assistLines.ForEach(x => objs.Add(x));
            var lines = ThMEPLineExtension.LineSimplifier(objs, 50, 50, 50, Math.PI / 180).Cast<Line>().ToList();
            objs.Clear();
            foreach (var line in lines)
                objs.Add(line);
            lines = ThMEPLineExtension.LineSimplifier(objs, 50, 2500, 20.0, Math.PI * _wallLightMergeAngle / 180).Cast<Line>().ToList();
            lines = lines.Where(c => c.Length > 100).ToList();
            //合并后可能会导致线角度有变化，这里的线为后面的实际的线提供排布侧方向，
            //这里合并时给的间距过大有些情况会报错，这里不给太大间距
            var tempNodes = InitAllLineNode(lines);
            lines = ThMEPLineExtension.LineSimplifier(objs, 50, 500.0, 20.0, Math.PI * 15 / 180).Cast<Line>().ToList();
            var _wallGraphNodes = CalcLineLayoutSide(lines, tempNodes);
            return _wallGraphNodes;
        }
        /// <summary>
        /// 根据合并和的线计算实际线要要排布在那一侧
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="_wallGraphNodes"></param>
        /// <returns></returns>
        private List<LineGraphNode> CalcLineLayoutSide(List<Line> lines,List<LineGraphNode> _wallGraphNodes)
        {
            List<LineGraphNode> lineLayoutSides = InitAllLineNode(lines,true);
            foreach (var lineInfo in _wallGraphNodes)
            {
                var zero = new Vector3d(0, 0, 0);
                if (lineInfo.line.Length < 4500)
                    continue;
                //根据线获取获取排布侧的墙柱
                var dir = lineInfo.lineDir;
                var leftDir = dir.RotateBy(Math.PI / 2, _normal).GetNormal();
                int minCount = (int)(lineInfo.line.Length / _lightSpace);

                var leftLights = new List<LightLayout>();
                var rightLights = new List<LightLayout>();
                foreach (var item in lineLayoutSides) 
                {
                    var isColl = EmgPilotLampUtil.LineIsCollinear(lineInfo.line.StartPoint, lineInfo.line.EndPoint, item.line.StartPoint, item.line.EndPoint, 1, 1000, _wallLightMergeAngle);
                    if (!isColl)
                        continue;
                    if (item.leftWallLayouLight != null && item.leftWallLayouLight.Count > 0)
                    {
                        foreach (var light in item.leftWallLayouLight) 
                        {
                            //实际排布的线可能和这里计算的线有偏差，这里将灯进行修正
                            var prjPoint = EmgPilotLampUtil.PointToLine(light.pointInOutSide, lineInfo.line);
                            var newLight = new LightLayout(prjPoint, light.pointInOutSide, lineInfo.line, leftDir, light.direction, light.directionSide,light.nearNode);
                            leftLights.Add(newLight);
                        }
                    }
                    if (item.rightWallLayoutLight != null && item.rightWallLayoutLight.Count > 0)
                    {
                        foreach (var light in item.rightWallLayoutLight)
                        {
                            //实际排布的线可能和这里计算的线有偏差，这里将灯进行修正
                            var prjPoint = EmgPilotLampUtil.PointToLine(light.pointInOutSide, lineInfo.line);
                            var newLight = new LightLayout(prjPoint, light.pointInOutSide, lineInfo.line, leftDir, light.direction, light.directionSide, light.nearNode);
                            rightLights.Add(newLight);
                        }
                    }
                }
                var leftUnConform = leftLights == null || leftLights.Count < 1 || LineWallLightOverSpaceCount(lineInfo.line, leftLights) > 0;
                var rightUnConform = rightLights == null || rightLights.Count < 1 || LineWallLightOverSpaceCount(lineInfo.line, rightLights) > 0;

                var realSideDir = leftDir;
                if (lineInfo.layoutLineSide == null || lineInfo.layoutLineSide.IsEqualTo(zero))
                {
                    //起点线，排布在两侧都可以，计算左右的两侧获取到灯判断是否符合
                    if (leftUnConform && rightUnConform)
                    {
                        //两侧都不符合
                        realSideDir = GetLineSideToLayoutLight(lineInfo, leftLights, rightLights);
                    }
                    else if (!leftUnConform && !rightUnConform)
                    {
                        //两侧都符合要求
                        realSideDir = GetLineSideToLayoutLight(lineInfo, leftLights, rightLights);
                    }
                    else
                    {
                        //只有一侧符合要求
                        realSideDir = leftUnConform ? leftDir.Negate() : leftDir;
                    }
                }
                else
                {
                    //该线有需要排布在那一侧
                    if (leftUnConform && rightUnConform)
                    {
                        //两侧都不符合
                        realSideDir = GetLineSideToLayoutLight(lineInfo, leftLights, rightLights);
                    }
                    else if (!leftUnConform && !rightUnConform)
                    {
                        //两侧都符合要求,优先布置在非视野盲区
                        realSideDir = lineInfo.layoutLineSide;
                    }
                    else
                    {
                        //只有一侧符合要求
                        realSideDir = leftUnConform ? leftDir.Negate() : leftDir;
                    }
                }
                foreach (var item in lineLayoutSides)
                {
                    var isColl = EmgPilotLampUtil.LineIsCollinear(lineInfo.line.StartPoint, lineInfo.line.EndPoint, item.line.StartPoint, item.line.EndPoint, 1, 1000, _wallLightMergeAngle);
                    if (!isColl)
                        continue;
                    
                    var sideDir = _normal.CrossProduct(item.lineDir);
                    var dot = realSideDir.DotProduct(sideDir);
                    if (dot < -0.01)
                        sideDir = sideDir.Negate();
                    item.layoutLineSide = sideDir;
                }
            }
            return lineLayoutSides;
        }
        /// <summary>
        /// 主疏散路径-壁装
        /// </summary>
        /// <param name="lineInfo"></param>
        /// <param name="inWalls"></param>
        /// <param name="inColumns"></param>
        public List<LightLayout> GetLightLayoutPlan(LineGraphNode lineInfo, List<Polyline> inWalls, List<Polyline> inColumns)
        {
            List<LightLayout> lightLayouts = new List<LightLayout>();
            if ((null == inColumns || inColumns.Count < 1) && (null == inWalls || inWalls.Count < 1))
                return lightLayouts;
            Point3d sp = lineInfo.line.StartPoint;
            var sideDir = lineInfo.layoutLineSide;
            var lineLength = lineInfo.line.Length;
            LayoutToStructure toStructure = new LayoutToStructure(_maxPolyline, _lineSideSpaceExt);
            int count = (int)Math.Ceiling(lineLength / _lightSpace);
            double step = lineLength / count;
            step = _lightSpace;
            var startPt = WallLineStartPoint(lineInfo, inWalls, inColumns);
            var endPoint = startPt + lineInfo.lineDir.MultiplyBy(step);

            var pointDirs = new Dictionary<Point3d, Vector3d>();
            var pts = new List<Point3d>();
            //以线的起点开始，最大间隔10米找可以布置的墙或柱，
            //首先获取起点的对应的排布点，后续递归去找相应的点
            bool isFirst = true;
            while (true)
            {
                pts.Clear();
                bool beBreak = false;
                if (startPt.DistanceTo(sp) >= lineLength - 100)
                {
                    startPt = lineInfo.line.EndPoint - lineInfo.lineDir.MultiplyBy(100);
                    endPoint = lineInfo.line.EndPoint;
                    beBreak = true;
                }
                pts.Add(startPt);
                pts.Add(endPoint);
                Line tempLine = new Line(startPt, endPoint);
                //获取该段线可以相交到的墙或柱
                GetSideWallColumns(tempLine, sideDir, _lineSideSpaceExt, inColumns, inWalls, out List<Polyline> newInWalls, out List<Polyline> newInColumns);
                var temp = toStructure.GetLayoutStructPt(startPt, endPoint, newInColumns, newInWalls, lineInfo.layoutLineSide, isFirst);
                bool isAdd = true;
                if (null != temp && temp.Count > 0)
                {
                    temp = temp.OrderBy(c => c.Key.DistanceTo(sp)).ToDictionary(x => x.Key, x => x.Value);
                    double maxDis = double.MinValue;
                    //获取相应的下一个开始排布的点位
                    foreach (var item in temp)
                    {
                        double dis = (item.Key - sp).DotProduct(sideDir);
                        Point3d pointInLine = EmgPilotLampUtil.PointToLine(item.Key, lineInfo.line);
                        dis = pointInLine.DistanceTo(startPt);
                        if (dis > 10 && dis < step + 100 && dis > maxDis)
                            maxDis = dis;
                        //壁装2500范围不能有其它壁装灯
                        if (pointDirs.Any(c => c.Key.DistanceTo(item.Key) < _minDisToLight))
                            continue;
                        //终点优化判断，防止终点处过近
                        if (beBreak && pointDirs.Any(c => c.Key.DistanceTo(item.Key) < 3500))
                            continue;
                        if ((pointInLine.DistanceTo(sp) + pointInLine.DistanceTo(lineInfo.line.EndPoint)) > lineLength + 500 * 2 / 3)
                            continue;
                        if (isAdd)
                        {
                            pointDirs.Add(item.Key, item.Value);
                            isAdd = false;
                            endPoint = pointInLine;
                            break;
                        }
                    }
                }
                isFirst = isAdd;
                startPt = endPoint;
                endPoint = startPt + lineInfo.lineDir.MultiplyBy(step);
                if (startPt.DistanceTo(sp) >= lineLength)
                    startPt = endPoint;
                if (endPoint.DistanceTo(sp) >= lineLength)
                    endPoint = lineInfo.line.EndPoint + lineInfo.lineDir.MultiplyBy(100);
                if (beBreak)
                    break;
            }
            if (null == pointDirs || pointDirs.Count < 1)
                return lightLayouts;
            foreach (var item in pointDirs)
            {
                if (item.Key == null || item.Value == null)
                    continue;
                var light = PointToExitDirection(lineInfo, item.Key, item.Value);
                if (null == light)
                    continue;
                lightLayouts.Add(light);
            }
            return lightLayouts;
        }
        private Point3d WallLineStartPoint(LineGraphNode lineInfo, List<Polyline> inWalls, List<Polyline> inColumns)
        {
            Point3d sp = lineInfo.line.StartPoint;
            var sideDir = lineInfo.layoutLineSide;
            var lineLength = lineInfo.line.Length;
            LayoutToStructure toStructure = new LayoutToStructure(_maxPolyline, _lineSideSpaceExt);
            var startPt = sp - lineInfo.lineDir.MultiplyBy(500);
            var endPoint = startPt + lineInfo.lineDir.MultiplyBy(_lightSpace / 2);
            //起点优化，中间按间距计算，起点根据剩余距离计算
            double startSpace = (lineInfo.line.Length % _lightSpace) / 2;
            var pts = new List<Point3d>();
            Line tempLine = new Line(startPt, endPoint);
            GetSideWallColumns(tempLine, sideDir, _lineSideSpaceExt, inColumns, inWalls, out List<Polyline> newInWalls, out List<Polyline> newInColumns);
            //起点有柱子时，不进行处理
            if (newInColumns == null || newInColumns.Count < 1)
            {
                //起点没有柱子，起点平移
                startPt = startPt + lineInfo.lineDir.MultiplyBy(startSpace);
                return startPt;
            }

            //第一个范围内有柱子，起点偏移到柱子
            pts.Add(startPt);
            pts.Add(endPoint);
            var temp = toStructure.GetLayoutStructPtColumnFirst(pts, newInColumns, new List<Polyline>(), lineInfo.lineDir);
            if (temp == null || temp.Count < 1)
            {
                startPt = startPt + lineInfo.lineDir.MultiplyBy(startSpace);
                return startPt;
            }
            temp = temp.OrderBy(c => c.Key.DistanceTo(sp)).ToDictionary(x => x.Key, x => x.Value);
            var firstColunm = temp.FirstOrDefault();
            if (null == firstColunm.Key || null == firstColunm.Value)
                return startPt;
            double dis = (firstColunm.Key - sp).DotProduct(sideDir);
            Point3d pointInLine = firstColunm.Key - sideDir.MultiplyBy(dis);
            startPt = pointInLine;
            return startPt;
        }
        /// <summary>
        /// 根据节点信息，创建在线的那一侧，进行生成相应灯的信息
        /// </summary>
        /// <param name="lineInfo"></param>
        /// <param name="point"></param>
        /// <param name="createSideDir"></param>
        /// <returns></returns>
        public LightLayout PointToExitDirection(LineGraphNode lineInfo, Point3d point, Vector3d createSideDir,bool isHosting=false)
        {
            var sp = lineInfo.line.StartPoint;
            var ep = lineInfo.line.EndPoint;
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
                List<GraphRoute> routes = GraphUtils.GetGraphNodeRoutes(_targetInfo.allNodeRoutes, route.graphNode, true);
                if (null == routes || routes.Count < 1)
                    continue;
                var tempRoute = routes.FirstOrDefault();
                var tempDir = (tempRoute.nextRoute.node.nodePoint - tempRoute.node.nodePoint).GetNormal();
                double dis = GraphUtils.GetRouteDisToEnd(tempRoute) + route.nodePointInLine.DistanceTo(prjPt);
                if (dis < nearDis)
                {
                    nearNode = route.graphNode;
                    if (prjPt.DistanceTo(route.graphNode.nodePoint) > 10)
                        exitDir = (route.graphNode.nodePoint - prjPt).GetNormal();
                    else if (Math.Abs(tempDir.DotProduct(lineInfo.lineDir)) > 0.5)
                        exitDir = tempDir;
                    else
                        exitDir = lineInfo.lineDir;

                    nearDis = dis;
                    nearRoute = tempRoute;
                }
            }
            var endNode = GraphUtils.GraphRouteEndNode(nearRoute);
            LightLayout lightLayout = new LightLayout(prjPt, point, lineInfo.line, sideDir, exitDir, createSideDir, nearNode, isHosting);
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
        public Polyline GetSideWallColumns(Line line, Vector3d sideDir, double sideDis, List<Polyline> targetColumns, List<Polyline> targetWalls, out List<Polyline> walls, out List<Polyline> columns)
        {
            walls = new List<Polyline>();
            columns = new List<Polyline>();
            Polyline polyLine = EmgPilotLampUtil.LineToPolyline(line, sideDir, sideDis);
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
        public List<Polyline> ReomveColumns(Line line, Vector3d sideDir, List<Polyline> targetColumns, List<Polyline> targetWalls)
        {
            List<Polyline> columns = new List<Polyline>();
            if (null == targetColumns || targetColumns.Count < 1)
                return columns;
            var lineDir = line.LineDirection();
            var lineSp = line.StartPoint;
            var normal = lineDir.CrossProduct(sideDir).GetNormal();
            foreach (var column in targetColumns)
            {
                //获取布置边
                var lineOutDir = EmgPilotLampUtil.PolylineOutDir(column);
                Line targetLine = null;
                var disToLine = 0.0;
                Vector3d outDir = new Vector3d();
                foreach (var item in lineOutDir)
                {
                    var dot = lineDir.DotProduct(item.Key.LineDirection());
                    if (Math.Abs(dot) < 0.3)
                        continue;
                    Line itemLine = item.Key;
                    var midPoint = itemLine.StartPoint + itemLine.LineDirection().MultiplyBy(itemLine.Length / 2);
                    var prjPoint = EmgPilotLampUtil.PointToLine(midPoint, line);
                    var tempDis = prjPoint.DistanceTo(midPoint);
                    if (targetLine == null)
                    {
                        targetLine = item.Key;
                        outDir = item.Value;
                        disToLine = tempDis;
                        continue;
                    }
                    if (tempDis < disToLine)
                    {
                        targetLine = item.Key;
                        disToLine = tempDis;
                        outDir = item.Value;
                    }
                }
                if (targetLine == null)
                    continue;
                //判断布置边是否符合要求
                //将线外平移一定距离，判断是否可有其它相交
                var newSp = targetLine.StartPoint + outDir.MultiplyBy(20);
                var newEp = targetLine.EndPoint + outDir.MultiplyBy(20);
                var newLine = new Line(newSp, newEp);
                var targetPoly = EmgPilotLampUtil.LineToPolyline(newLine, outDir, 20, -20);
                if (null == targetPoly)
                    continue;
                //如果穿外框线，不满足要去
                //如果移动后还和墙相交也需要
                var objs = new DBObjectCollection();
                if (null != targetWalls && targetWalls.Count > 0)
                    targetWalls.ForEach(x => objs.Add(x));
                objs.Add(_maxPolyline);
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var cross = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(targetPoly).Cast<Polyline>().ToList();
                if (null != cross && cross.Count > 0)
                    continue;
                var layoutPoint = targetLine.StartPoint + targetLine.LineDirection().MultiplyBy(targetLine.Length / 2);
                var checkPoint = layoutPoint + outDir.MultiplyBy(30);
                if (!_maxPolyline.Contains(checkPoint))
                    continue;
                bool isCont = false;
                if (null != targetWalls && targetWalls.Count > 0) 
                {
                    foreach (var item in targetWalls) 
                    {
                        if (isCont)
                            break;
                        isCont = item.Contains(checkPoint);
                    }
                }
                if (isCont)
                    continue;
                columns.Add(column);

            }
            return columns;
        }
       
        List<LineGraphNode> InitAllLineNode(List<Line> lines,bool initWallLight=false)
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
                //获取每根线上的节点
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
                var dir = lineNode.lineDir;
                var leftDir = dir.RotateBy(Math.PI / 2, _normal).GetNormal();
                int lefeInCount = 0, rightInCount = 0;
                foreach (var node in lineNode.nodeDirections)
                {
                    if (node == null || null == node.inDirection || node.inDirection.Count < 1)
                        continue;
                    foreach (var inDir in node.inDirection)
                    {
                        double dot = inDir.DotProduct(leftDir);
                        if (dot > 0)
                            lefeInCount += 1;
                        else
                            rightInCount += 1;
                    }
                }
                if (lefeInCount == rightInCount && lefeInCount == 0)
                {
                    //两侧都可以进行布置
                    lineNode.layoutLineSide = new Vector3d();
                }
                else if (lefeInCount < rightInCount)
                    lineNode.layoutLineSide = leftDir.Negate();
                else
                    lineNode.layoutLineSide = leftDir;

                if (!initWallLight)
                    continue;
                //获取左右两侧的布置灯
                List<Polyline> inWalls = new List<Polyline>();
                List<Polyline> inColumns = new List<Polyline>();
                var inputLineInfo = new LineGraphNode(lineNode.line);
                inputLineInfo.layoutLineSide = leftDir;
                inputLineInfo.nodeDirections.AddRange(lineNode.nodeDirections);
                GetSideWallColumns(inputLineInfo.line, leftDir, _lineSideSpaceExt, _targetColums, _targetWalls, out inWalls, out inColumns);
                inColumns = ReomveColumns(inputLineInfo.line, leftDir, inColumns, inWalls);
                var leftLights = GetLightLayoutPlan(inputLineInfo, inWalls, inColumns);
                if (null != leftLights && leftLights.Count > 0)
                    lineNode.leftWallLayouLight.AddRange(leftLights);

                inputLineInfo.layoutLineSide = leftDir.Negate();
                GetSideWallColumns(inputLineInfo.line, leftDir.Negate(), _lineSideSpaceExt, _targetColums, _targetWalls, out inWalls, out inColumns);
                inColumns = ReomveColumns(inputLineInfo.line, leftDir.Negate(), inColumns, inWalls);
                var rightLights = GetLightLayoutPlan(inputLineInfo, inWalls, inColumns);
                if (null != rightLights && rightLights.Count > 0)
                    lineNode.rightWallLayoutLight.AddRange(rightLights);
            }
            return lineGrapheNode;
        }
        List<LineGraphNode> GetLineNodes(Line line, double maxDis = 2300)
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
        /// 获取某个线上的经过节点的其它节点
        /// </summary>
        /// <param name="line"></param>
        /// <param name="node"></param>
        /// <param name="otherNodeDirs"></param>
        /// <returns></returns>
        List<GraphNode> GetOtherNode(Line line, GraphNode node, List<LineGraphNode> otherNodeDirs)
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

        int LineWallLightOverSpaceCount(Line line,List<LightLayout> lightLayouts) 
        {
            var linePoints = LigthPoint(line, lightLayouts) ;
            int overSpaceCount = 0;
            for (int i = 0; i < linePoints.Count - 1; i++)
            {
                var sp = linePoints[i];
                var ep = linePoints[i + 1];
                if (sp.DistanceTo(ep) < _lightSpace + 50)
                    continue;
                overSpaceCount += 1;
            }
            return overSpaceCount;
        }


        Vector3d GetLineSideToLayoutLight(LineGraphNode lineInfo, List<LightLayout> leftLights,List<LightLayout> rightLights) 
        {
            var dir = lineInfo.lineDir;
            var leftDir = dir.RotateBy(Math.PI / 2, _normal).GetNormal();
            var realDir = leftDir;
            if (leftLights.Count > 0 && rightLights.Count > 0)
            {
                var noDir = lineInfo.layoutLineSide.IsEqualTo(new Vector3d());
                var leftPoints = LigthPoint(lineInfo.line, leftLights);
                var rightPoints = LigthPoint(lineInfo.line, rightLights);
                //两侧都有灯，根据距离，间距个数，非视野盲区
                //非视野盲区权重 +5
                int leftWeight = 0;
                int rightWeight = 0;
                if (noDir)
                {
                    leftWeight = 5;
                    rightWeight = 5;
                }
                else if (leftDir.IsEqualTo(lineInfo.layoutLineSide))
                {
                    leftWeight = 5;
                }
                else 
                {
                    rightWeight = 5;
                }
                //间距超出一个 -2
                leftWeight -= LineWallLightOverSpaceCount(lineInfo.line, leftLights) * 2;
                rightWeight -= LineWallLightOverSpaceCount(lineInfo.line, rightLights) * 2;

                //计算距离中心线的偏差
                var leftVD = VarianceDesityDistance(lineInfo.line, leftLights);
                var rightVD = VarianceDesityDistance(lineInfo.line, rightLights);
                if (leftVD < rightVD)
                {
                    rightWeight -= 2;
                    leftWeight += 1;
                }
                else 
                {
                    leftWeight -= 2;
                    rightWeight += 1;
                }

                //计算疏密
                var leftD = VarianceDensitySpace(leftPoints);
                var rightD = VarianceDensitySpace(rightPoints);
                if (leftD > rightD)
                {
                    leftWeight += 2;
                }
                else 
                {
                    rightWeight -= 2;
                }
                //计算左右的进入点数
                int lefeInCount = 0, rightInCount = 0;
                foreach (var node in lineInfo.nodeDirections)
                {
                    if (node == null || null == node.inDirection || node.inDirection.Count < 1)
                        continue;
                    foreach (var inDir in node.inDirection)
                    {
                        double dot = inDir.DotProduct(leftDir);
                        if (dot > 0)
                            lefeInCount += 1;
                        else
                            rightInCount += 1;
                    }
                }
                leftWeight -= lefeInCount;
                rightWeight -= rightInCount;
                if (leftWeight < rightWeight)
                    realDir = leftDir.Negate();

            }
            else if (leftLights.Count > 0 || rightLights.Count > 0)
            {
                //有一侧一个灯都没有，使用有灯的一侧
                realDir = leftLights.Count > rightLights.Count ? leftDir : leftDir.Negate();
            }
            else 
            {
                //两侧都没有灯，该路径要使用吊装,方向使用那一侧都一样。
            }
            return realDir;
        }
        List<Point3d> LigthPoint(Line line, List<LightLayout> lightLayouts) 
        {
            List<Point3d> linePoints = lightLayouts.Select(c => c.linePoint).ToList();
            Point3d lineSp = line.StartPoint;
            Point3d lineEp = line.EndPoint;
            bool spAdd = true, epAdd = true;
            foreach (var point in linePoints)
            {
                if (spAdd && point.DistanceTo(lineSp) < 100)
                    spAdd = false;
                if (epAdd && point.DistanceTo(lineEp) < 100)
                    epAdd = false;
            }
            if (spAdd)
                linePoints.Add(lineSp);
            if (epAdd)
                linePoints.Add(lineEp);
            linePoints = linePoints.OrderBy(c => c.DistanceTo(lineSp)).ToList();
            return linePoints;
        }

        double VarianceDensitySpace(List<Point3d> lightPoints) 
        {
            //计算间距，间距的平均值
            int count = 0;
            double sumSpace = 0.0;
            for (int i = 0; i < lightPoints.Count - 1; i++) 
            {
                var sp = lightPoints[i];
                var ep = lightPoints[i + 1];
                sumSpace += sp.DistanceTo(ep);
                count += 1;
            }
            double avgValue = sumSpace / count;
            double varianceSum = 0.0;
            for (int i = 0; i < lightPoints.Count - 1; i++) 
            {
                var sp = lightPoints[i];
                var ep = lightPoints[i + 1];
                var dis = sp.DistanceTo(ep);
                varianceSum+= Math.Abs(avgValue - dis);
            }
            return varianceSum;
        }
        double VarianceDesityDistance(Line line, List<LightLayout> lightLayouts) 
        {
            var disSum = 0.0;
            foreach (var item in lightLayouts) 
            {
                disSum += item.linePoint.DistanceTo(item.pointInOutSide);
            }
            var avgValue = disSum / lightLayouts.Count;
            double varianceSum = 0.0;
            foreach (var item in lightLayouts) 
            {
                varianceSum += item.linePoint.DistanceTo(item.pointInOutSide);
            }
            return varianceSum;
        }
    }
}
