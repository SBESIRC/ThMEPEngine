using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Algorithm.DijkstraAlgorithm;
using ThMEPEngineCore.Algorithm.GraphDomain;

namespace ThMEPLighting.ParkingStall.Worker.LightConnect
{
    class LaneLineRoute
    {
        Polyline _outPolyline;
        List<Line> _allLaneLines;
        List<Line> _dijkstraLines;
        List<GraphRoute> _allGraphRoutes;
        List<PointGraphNode> _allGraphNodes;
        Point3d _alPoint;
        public LaneLineRoute(Polyline outPolyline,List<Line> laneLines, Point3d alPoint) 
        {
            _outPolyline = outPolyline;
            _allLaneLines = new List<Line>();
            _dijkstraLines = new List<Line>();
            _allGraphNodes = new List<PointGraphNode>();
            _allGraphRoutes = new List<GraphRoute>();
            if (null != laneLines && laneLines.Count > 0)
            {
                laneLines.ForEach(c => _allLaneLines.Add(c));
            }
            _alPoint = alPoint;
            var objs1 = new DBObjectCollection();
            _allLaneLines.ForEach(x => 
            {
                var dir = (x.EndPoint - x.StartPoint).GetNormal();
                var line = new Line(x.StartPoint - dir * 1, x.EndPoint + dir * 1);
                objs1.Add(line);
            });
            var nodeGeo = objs1.ToNTSNodedLineStrings();
            if (nodeGeo != null)
            {
                _dijkstraLines = nodeGeo.ToDbObjects()
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
        public Point3d CalcALPoint() 
        {
            var point = GetPointByOutPolyline(_allLaneLines);
            return point;
        }
        Point3d GetPointByOutPolyline(List<Line> targetLines) 
        {
            var listPly = new List<Polyline> { _outPolyline };
            var outLines = ThMEPLineExtension.ExplodeCurves(listPly.ToCollection()).Where(c => c is Line).Cast<Line>().ToList();
            var allALPoints = new List<Point3d>();
            foreach (var line in targetLines)
            {
                var sp = line.StartPoint;
                var ep = line.EndPoint;
                if (!allALPoints.Any(c => c.DistanceTo(sp) < 1))
                    allALPoints.Add(sp);
                if (!allALPoints.Any(c => c.DistanceTo(ep) < 1))
                    allALPoints.Add(ep);
            }
            var alPoint = GetALPointInPolylinePoint();
            var tempList = allALPoints.OrderBy(c => c.DistanceTo(alPoint)).ToList();
            return tempList.FirstOrDefault();
        }

        Point3d GetALPointInPolylinePoint() 
        {
            if (_outPolyline.Contains(_alPoint))
                return _alPoint;
            return _outPolyline.GetClosePoint(_alPoint);
        }
        public List<GraphRoute> GetAllGraphRoute(bool addEnd) 
        {
            _allGraphRoutes.Clear();
            _allGraphNodes.Clear();
            var endPoint = CalcALPoint();
            InitGraphNode(endPoint);
            CalcRoute();
            List<GraphRoute> graphRoutes = new List<GraphRoute>();
            _allGraphRoutes.ForEach(c => graphRoutes.Add(c));
            if (addEnd) 
            {
                foreach (var node in _allGraphNodes) 
                {
                    if (null == node || !node.IsEnd)
                        continue;
                    graphRoutes.Add(new GraphRoute(node, 0));
                }
            }
            return graphRoutes;
        }
        void InitGraphNode(Point3d endPoint) 
        {
            foreach (var item in _dijkstraLines) 
            {
                PointGraphNode startNode = new PointGraphNode(item.StartPoint);
                if (item.StartPoint.DistanceTo(endPoint) < 5)
                    startNode.IsEnd = true;
                PointGraphNode endNode = new PointGraphNode(item.EndPoint);
                if (item.EndPoint.DistanceTo(endPoint) < 5)
                    endNode.IsEnd = true;
                if (!_allGraphNodes.Any(c => c.NodeIsEqual(startNode, 1.0, null)))
                {
                    _allGraphNodes.Add(startNode);
                }
                if (!_allGraphNodes.Any(c => c.NodeIsEqual(endNode, 1.0, null)))
                {
                    _allGraphNodes.Add(endNode);
                }
            }
        }
        void CalcRoute() 
        {
            var endNodes = _allGraphNodes.Where(c => c.IsEnd).ToList();
            var cacheGraphRoutes = new List<GraphRoute>();
            foreach (var item in _allGraphNodes)
            {
                if (item == null || item.IsEnd)
                    continue;
                GraphRoute route = null;
                double dis = double.MaxValue;
                //获取到每个出口的距离，找到最近的一个
                foreach (var exit in endNodes)
                {
                    var dijkstra = new DijkstraAlgorithm(_dijkstraLines.Cast<Curve>().ToList());
                    var startPoint = (Point3d)item.GraphNode;
                    var endPoint = (Point3d)exit.GraphNode;
                    var routePts = dijkstra.FindingMinPath(startPoint, endPoint);
                    if (null == routePts || routePts.Count < 2)
                        continue;
                    if (routePts.LastOrDefault().DistanceTo((Point3d)item.GraphNode) > 10)
                        continue;
                    routePts.Reverse();
                    double thisDis = 0;
                    for (int i = 0; i < routePts.Count - 1; i++)
                        thisDis += routePts[i].DistanceTo(routePts[i + 1]);
                    if (thisDis > dis)
                        continue;
                    dis = thisDis;
                    //构造route
                    var graphNodes = new List<IGraphNode>();
                    foreach (var point in routePts)
                    {
                        var node = _allGraphNodes.Where(c => ((Point3d)c.GraphNode).DistanceTo(point) < 10).FirstOrDefault();
                        graphNodes.Add(node);
                    }
                    route = GraphUtils.GraphNodeToRoute(graphNodes);
                    route.weightToStart = dis;
                }
                if (null == route)
                    continue;
                _allGraphRoutes.Add(route);
            }
        }
    }
}
