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

        private Dictionary<int, FireZoneNode> IdToNode = new Dictionary<int, FireZoneNode>();
        //public List<FireZoneNode> NodeList = new List<FireZoneNode>();
        //public List<FireZoneEdge> Edges = new List<FireZoneEdge>();
        private Dictionary<int, FireZoneEdge> IdToEdge = new Dictionary<int, FireZoneEdge>();

        private List<FireZonePath> PathsToExplore = new List<FireZonePath> ();//当前需要探索的路径
        private List<FireZonePath> PathsToSocre = new List<FireZonePath>();//需要打分的路径
        private Dictionary<FireZonePath, (Polygon, double)> ScoredPaths = new Dictionary<FireZonePath, (Polygon, double)>();//打过分的的路径
        private double BestScore = double.MaxValue;//当前最优得分
        private FireZonePath BestPath;//当前最优路径


        private int ObjId = 0;
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
            //NodeList.Add(node);
            IdToNode.Add(ObjId, node);
            node.ObjId = ObjId;
            ObjId += 1;
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
            //Edges.Add(edge);
            IdToEdge.Add(ObjId, edge);
            edge.ObjId = ObjId;
            ObjId += 1;
            var node0 = NodeDic[edge.P0];
            var node1 = NodeDic[edge.P1];
            node0.AddBranch(edge, node1);
            node1.AddBranch(edge, node0);
        }

        public Polygon FindBestFireZone(double minArea ,double maxArea ,int StepSize = 15)
        {
            MinArea = minArea * multiplier* multiplier;
            MaxArea = maxArea * multiplier* multiplier;
            var t_start = _stopwatch.Elapsed.TotalSeconds;
            PathsToExplore.Add(new FireZonePath());
            for (int i = 0; i < StepSize; i++)
            {
                if (PathsToExplore.Count == 0) break;
                Logger?.Information($"第{i}步:");
                Logger?.Information($"节点个数:{PathsToExplore.Count}");
                Step();
                Score();
            }
            Logger?.Information($"当前防火分区用时:{_stopwatch.Elapsed.TotalSeconds - t_start}s\n");
            return ScoredPaths [BestPath].Item1;
        }
        private void Step()//获取下一层路径
        {
            t_pre = _stopwatch.Elapsed.TotalSeconds;
            var nextLevelPaths = new List<FireZonePath>();
            foreach(var path in PathsToExplore)
            {
                var stepedPaths = Step(path,BestScore);
                foreach(var stepedPath in stepedPaths)
                {
                    if(stepedPath.Ended)
                    {
                        if(!ScoredPaths.ContainsKey(stepedPath)) PathsToSocre.Add(stepedPath);
                    }
                    else nextLevelPaths.Add(stepedPath);
                }
            }
            PathsToExplore = nextLevelPaths;
            Logger?.Information($"遍历用时:{_stopwatch.Elapsed.TotalSeconds - t_pre}s");
        }
        private List<FireZonePath> Step(FireZonePath initPath, double minCost)
        {
            var nextLevelPath = new List<FireZonePath>();
            if (initPath.Ended) return nextLevelPath;
            FireZoneNode initNode;
            if (initPath.Path.Count == 0)//初始节点，未添加路径
            {
                initNode = Root;
            }
            else
            {
                initNode =IdToNode[initPath.Path.Last()];
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
        private void Score()//打分
        {
            var polyCnt = 0;
            var planCnt = 0;
            foreach (var path in PathsToSocre)
            {
                if (ScoredPaths.ContainsKey(path)) continue;
                planCnt += 1;
                var cost = path.Cost;
                var poly = Split(path);
                ScoredPaths.Add(path,(poly,cost));
                if (poly == null) continue;
                polyCnt += 1;
                FireZones.Add(poly);
                if (cost < BestScore)
                {
                    BestScore = cost;
                    BestPath = path;
                    Logger?.Information($"最短距离：{cost * 0.001}m");
                }
            }
            Logger?.Information($"方案个数：{planCnt}");
            Logger?.Information($"有效个数：{polyCnt}");
            Logger?.Information($"计算用时:{_stopwatch.Elapsed.TotalSeconds - t_pre}s\n");
            PathsToSocre.Clear();
        }
        private Polygon Split(FireZonePath path)//基于path对图形切割
        {
            var polygonizer = new Polygonizer();
            polygonizer.Add(Root.Segments);
            var Path = path.Path;
            var nodePolys = new HashSet<Polygon>();
            for (int i = 0; i < Path.Count; i++)
            {
                var objId = Path[i];
                if (i % 2 == 0)
                {
                    polygonizer.Add(IdToEdge[objId].Path);
                }
                else
                {
                    var node = IdToNode[objId];
                    if (node.Type != 1) continue;
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
