using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm.DijkstraAlgorithm;
using ThMEPLighting.Common;

namespace ThMEPLighting.DSFEI.ThEmgPilotLamp
{
    public class EmgPilotLampLineNode
    {
        public List<GraphNode> allNodes = null;
        public List<GraphRoute> cacheNodeRoutes = null;
        private List<GraphNode> _exitNodes = null;
        private List<Curve> _laneLines;
        private List<Curve> _exitLines;
        private List<BlockReference> _exitBlocks;
        private DijkstraAlgorithm _dijkstra;
        public List<Line> dijkstraLines;
        public EmgPilotLampLineNode(List<Curve> laneLines, List<Curve> exitLines,List<BlockReference> exitBlocks) 
        {
            allNodes = new List<GraphNode>();
            cacheNodeRoutes = new List<GraphRoute>();
            _exitNodes = new List<GraphNode>();
            dijkstraLines = new List<Line>();

            var objs = new DBObjectCollection();
            laneLines.ForEach(x => objs.Add(x));
            _laneLines = ThFEILineExtension.LineSimplifier(objs, 1500, 200.0, 1500, Math.PI / 180.0).Cast<Curve>().ToList();

            objs = new DBObjectCollection();
            exitLines.ForEach(x => objs.Add(x));
            _exitLines = ThFEILineExtension.LineSimplifier(objs, 1500, 200.0, 1500, Math.PI / 180.0).Cast<Curve>().ToList();

            _exitBlocks = exitBlocks;

            //step1 根据出口线，车道线，打断处理精度问题
            InitGraphLines();

            //step2 根据出口线，出口块，构造当前的node
            InitGraphNode();

            //step3 根据上述数据，计算出每个拐点到出口点的距离及路线
            InitGraphRoutCache();
        }
        /// <summary>
        /// 对线线进行预处理将多余，可以合并的线处理
        /// </summary>
        void InitGraphLines()
        {
            //线进行预处理，线可能存在多余或未连接
            if (null == _laneLines || _exitLines == null)
                return;
            List<Curve> tempCurves = new List<Curve>();
            tempCurves.AddRange(_laneLines);
            tempCurves.AddRange(_exitLines);
            var objs = new DBObjectCollection();
            tempCurves.ForEach(x => objs.Add(x));
            var allLines = ThFEILineExtension.LineSimplifier(objs, 50, 20.0, 2.0, Math.PI / 180.0);
            //allLines = allLines.Where(c => c.Length > 500).ToList();
            allLines = allLines.Select(y =>
            {
                var dir = (y.EndPoint - y.StartPoint).GetNormal();
                return new Line(y.StartPoint - dir * 1, y.EndPoint + dir * 1);
            })
                .ToList();
            var objs1 = new DBObjectCollection();
            allLines.ForEach(x => objs1.Add(x));
            var nodeGeo = objs1.ToNTSNodedLineStrings();
            if (nodeGeo != null)
            {
                dijkstraLines = nodeGeo.ToDbObjects()
                .SelectMany(x =>
                {
                    DBObjectCollection entitySet = new DBObjectCollection();
                    (x as Polyline).Explode(entitySet);
                    return entitySet.Cast<Line>().ToList();
                })
                .Where(x => x.Length > 5)
                .ToList();
            }
        }
        /// <summary>
        /// 初始化线上的节点信息，
        /// </summary>
        void InitGraphNode() 
        {
            if (_exitLines == null || _exitLines.Count < 1)
                return;
            if (_exitBlocks == null || _exitBlocks.Count < 1)
                return;
            foreach (var curve in dijkstraLines) 
            {
                bool isExitLine = _exitLines.Any(c => curve.IsCollinear(c as Line));
                Point3d sp = curve.StartPoint;
                Point3d ep = curve.EndPoint;
                bool spInNode = false,
                     epInNode = false;
                foreach (var tempNode in allNodes)
                {
                    if (null == tempNode)
                        continue;
                    if (!spInNode)
                        spInNode = tempNode.nodePoint.DistanceTo(sp) < 5;
                    if (!epInNode)
                        epInNode = tempNode.nodePoint.DistanceTo(ep) < 5;
                }
                
                if (!spInNode)
                {
                    var node = new GraphNode();
                    node.nodePoint = sp;
                    if (PointIsExit(sp, out int exitType,out BlockReference exitBlock, 450))
                    {
                        node.isExit = true;
                        node.nodeType = exitType;
                        node.tag = exitBlock;
                    }
                    else 
                    {
                        node.isExit = false;
                    }
                    allNodes.Add(node);
                }
                if(!epInNode) 
                {
                    var node = new GraphNode();
                    node.nodePoint = ep;
                    if (PointIsExit(ep, out int exitType,out BlockReference exitBlock, 450))
                    {
                        node.isExit = true;
                        node.nodeType = exitType;
                        node.tag = exitBlock;
                    }
                    else
                    {
                        node.isExit = false;
                    }
                    allNodes.Add(node);
                }
            }
        }
        
        /// <summary>
        /// 计算所有点到最近出口的的路径
        /// </summary>
        void InitGraphRoutCache() 
        {
            if (null == dijkstraLines || dijkstraLines.Count < 1)
                return;
            _exitNodes = allNodes.Where(c => c.isExit).ToList();
            foreach (var item in allNodes) 
            {
                if (item == null || item.isExit)
                    continue;
                GraphRoute route = null;
                double dis = double.MaxValue;
                //获取到每个出口的距离，找到最近的一个
                foreach (var exit in _exitNodes) 
                {
                    _dijkstra = new DijkstraAlgorithm(dijkstraLines.Cast<Curve>().ToList());
                    var routePts= _dijkstra.FindingMinPath(item.nodePoint,exit.nodePoint);
                    if (null == routePts || routePts.Count <2)
                        continue;
                    if (routePts.LastOrDefault().DistanceTo(item.nodePoint) > 10)
                        continue;
                    routePts.Reverse();
                    double thisDis = 0;
                    for (int i = 0; i < routePts.Count - 1; i++)
                        thisDis += routePts[i].DistanceTo(routePts[i + 1]);
                    if (thisDis > dis)
                        continue;
                    dis = thisDis;
                    //构造route
                    route = InitRouteByPoints(routePts, null);
                    route.weightToNextNode = dis;
                }
                if (null == route)
                    continue;
                cacheNodeRoutes.Add(route);
            }
        }
        /// <summary>
        /// 判断点是否是出口点
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="maxDis"></param>
        /// <returns></returns>
        bool PointIsExit(Point3d pt, out int blockType,out BlockReference exitBlock, double maxDis = 100/304.8) 
        {
            blockType = -1;
            bool isExit = false;
            exitBlock = null;
            if (null == pt || null == _exitBlocks || _exitBlocks.Count < 1)
                return isExit;
            foreach (var item in _exitBlocks) 
            {
                if (isExit)
                    break;
                //遍历，获取一定距离内的出口块
                var point3dZ0 = new Point3d(item.Position.X, item.Position.Y, 0);
                var dis = point3dZ0.DistanceTo(pt);
                if (dis > maxDis)
                    continue;
                exitBlock = item;
                switch (item.Name)
                {
                    case ThMEPLightingCommon.FEI_EXIT_NAME100:
                        //壁装 E
                        blockType = 100;
                        break;
                    case ThMEPLightingCommon.FEI_EXIT_NAME101:
                        //吊装 E
                        blockType = 101;
                        break;
                    case ThMEPLightingCommon.FEI_EXIT_NAME102:
                        //壁装 S
                        blockType = 102;
                        break;
                    case ThMEPLightingCommon.FEI_EXIT_NAME103:
                        //吊装 S
                        blockType = 102;
                        break;
                    case ThMEPLightingCommon.FEI_EXIT_NAME140:
                        //壁装 E/N
                        blockType = 140;
                        break;
                    case ThMEPLightingCommon.FEI_EXIT_NAME141:
                        //吊装 E/N
                        blockType = 141;
                        break;
                }
                isExit = true;
            }
            return isExit;
        }
        /// <summary>
        /// 获取路径中以某个节点为起点的到终点的路径
        /// </summary>
        /// <param name="routePts"></param>
        /// <param name="pNode"></param>
        /// <returns></returns>
        GraphRoute InitRouteByPoints(List<Point3d> routePts, GraphRoute pNode)
        {
            if (routePts == null || routePts.Count < 1)
                return null;
            Point3d point = routePts.FirstOrDefault();
            var node = allNodes.Where(c => c.nodePoint.DistanceTo(point) < 2).FirstOrDefault();
            var currentRoute = new GraphRoute();
            currentRoute.node = node;
            List<Point3d> nextPts = new List<Point3d>();
            for (int i = 1; i < routePts.Count; i++)
                nextPts.Add(routePts[i]);
            currentRoute.nextRoute = InitRouteByPoints(nextPts, currentRoute);
            return currentRoute;
        }
    }
}
