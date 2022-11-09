using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Polygonize;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.Tools;

namespace ThParkingStall.Core.FireZone
{
    public class FireZoneMap
    {
        public FireZoneNode Root;//代表边界的节点

        private Dictionary<Coordinate, FireZoneNode> NodeDic = new Dictionary<Coordinate, FireZoneNode>();
        public List<FireZoneNode> NodeList = new List<FireZoneNode>();
        public List<FireZoneEdge> Edges = new List<FireZoneEdge>();

        public Stopwatch _stopwatch = new Stopwatch();
        public Serilog.Core.Logger Logger = null;
        private double t_pre;
        private double MinArea;
        private double MaxArea;
        private double multiplier = 1000;

        public List<Polygon> FireZones = new List<Polygon>();
        public FireZoneMap(FireZoneNode root,Serilog.Core.Logger logger = null)//输入根节点
        {
            _stopwatch.Start();
            t_pre = _stopwatch.Elapsed.TotalSeconds;
            Root = root;
            Add(root);
            Logger = logger;    
        }
        public void Add(FireZoneNode node)
        {
            NodeList.Add(node);
            if (node.Type == 0) NodeDic.Add(node.Coordinates.First(), node);
            else
            {
                for (int i = 0; i < node.Coordinates.Length - 1; i++)
                {
                    var coor = node.Coordinates[i];
                    NodeDic.Add(coor, node);
                }
            }
        }
        public void Add(FireZoneEdge edge)
        {
            Edges.Add(edge);
            var node0 = NodeDic[edge.P0];
            var node1 = NodeDic[edge.P1];
            node0.AddBranch(edge, node1);
            node1.AddBranch(edge, node0);
        }

        public Polygon FindBestFireZone(double minArea ,double maxArea ,int StepSize = 15)
        {
            MinArea = minArea * multiplier* multiplier;
            MaxArea = maxArea * multiplier* multiplier;
            var rootPath = new FireZonePath();
            var pathsToExplore = new HashSet<FireZonePath> { rootPath };
            var EndPaths = new HashSet<FireZonePath>();
            var minCost = double.MaxValue;
            Polygon optPoly = null;
            var t_start = _stopwatch.Elapsed.TotalSeconds;
            for (int i = 0; i < StepSize; i++)
            {
                if (pathsToExplore.Count == 0) break;
                Logger?.Information($"第{i}步:");
                Logger?.Information($"节点个数:{pathsToExplore.Count}");
                t_pre = _stopwatch.Elapsed.TotalSeconds;
                var nextLevelPaths = Step(pathsToExplore, minCost);
                pathsToExplore.Clear();
                EndPaths.Clear();
                foreach (var path in nextLevelPaths)
                {
                    if (path.Ended) EndPaths.Add(path);
                    else pathsToExplore.Add(path);
                }
                Logger?.Information($"遍历用时:{_stopwatch.Elapsed.TotalSeconds - t_pre}s");
                t_pre = _stopwatch.Elapsed.TotalSeconds;

                var polyCnt = 0;
                foreach (var path in EndPaths)
                {
                    var cost = path.Cost;
                    var poly = Split(path);
                    if (poly == null) continue;
                    polyCnt += 1;
                    FireZones.Add(poly);
                    if (cost < minCost)
                    {
                        minCost = cost;
                        optPoly = poly;
                        Logger?.Information($"最短距离：{cost * 0.001}m");
                    }
                }
                Logger?.Information($"方案个数：{EndPaths.Count}");
                Logger?.Information($"有效个数：{polyCnt}");
                Logger?.Information($"计算用时:{_stopwatch.Elapsed.TotalSeconds - t_pre}s\n");
            }
            return optPoly;
        }
        private HashSet<FireZonePath> Step(HashSet<FireZonePath> initPath, double minCost)//获取下一层路径
        {
            var nextLevelPath = new HashSet<FireZonePath>();

            foreach (var path in initPath)
            {
                Step(path, minCost).ForEach(p => nextLevelPath.Add(p));
            }
            return nextLevelPath;
        }
        private List<FireZonePath> Step(FireZonePath initPath, double minCost)
        {
            var nextLevelPath = new List<FireZonePath>();
            if (initPath.Ended) return nextLevelPath;
            FireZoneNode initNode;
            if (initPath.Edges.Count == 0)//初始节点，未添加路径
            {
                initNode = Root;
            }
            else
            {
                initNode = initPath.Nodes.Last();
            }
            foreach (var branch in initNode.Branches)
            {
                var next_edge = branch.Item1;
                if (minCost < next_edge.Cost + initPath.Cost) continue;
                if (initPath.Contains(next_edge)) continue;
                var next_node = branch.Item2;
                if (initPath.Contains(next_node)) continue;
                var newPath = initPath.Step(next_edge, next_node);
                nextLevelPath.Add(newPath);
            }
            return nextLevelPath;
        }

        private Polygon Split(FireZonePath path)//基于path对图形切割
        {
            var polygonizer = new Polygonizer();
            polygonizer.Add(Root.Segments);
            path.Edges.ForEach(e => polygonizer.Add(e.Path));
            var nodePolys = new HashSet<Polygon>();
            foreach(var node in path.Nodes)
            {
                if(node.Type == 1)
                {
                    nodePolys.Add(node.polygon);
                    polygonizer.Add(node.Segments);
                }
            }
            var polys = polygonizer.GetPolygons().OfType<Polygon>().Except(nodePolys);
            foreach(var poly in polys)
            {
                if (poly.Area > MinArea && poly.Area <= MaxArea) return poly;
            }
            return null;
        }
    }
}
