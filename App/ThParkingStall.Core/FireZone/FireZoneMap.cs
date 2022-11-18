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
        private Dictionary<int, FireZoneEdge> IdToEdge = new Dictionary<int, FireZoneEdge>();

        private List<FireZonePath> PathsToExplore = new List<FireZonePath> ();//当前需要探索的路径
        private List<FireZonePath> PathsToSocre = new List<FireZonePath>();//需要打分的路径
        private Dictionary<FireZonePath, (Polygon,Polygon, double)> ScoredPaths = 
            new Dictionary<FireZonePath, (Polygon,Polygon, double)>();//打过分的的路径
        private double BestScore = double.MaxValue;//当前最优得分
        private FireZonePath BestPath = null;//当前最优路径


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
            if (node.Type == 0 && !NodeDic.ContainsKey(node.Coordinates.First())) 
                NodeDic.Add(node.Coordinates.First(), node);
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
            //if (!NodeDic.ContainsKey(edge.P0) || !NodeDic.ContainsKey(edge.P1)) return;//有bug

            var node0 = NodeDic[edge.P0];
            var node1 = NodeDic[edge.P1];
            if (node0.ObjId == node1.ObjId) return;//接到相同节点，跳过
            IdToEdge.Add(ObjId, edge);
            edge.ObjId = ObjId;
            ObjId += 1;
            node0.AddBranch(edge, node1);
            node1.AddBranch(edge, node0);
            

        }

        public (Polygon,Polygon,double) FindBestFireZone(double minArea ,double maxArea ,int StepSize = 14)
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
                Branch();//探索
                Score();//打分
            }
            Logger?.Information($"当前防火分区用时:{_stopwatch.Elapsed.TotalSeconds - t_start}s\n");
            if(BestPath != null) return ScoredPaths [BestPath];
            else return (null,null,-1);
        }
        private void Branch()//获取下一层路径
        {
            t_pre = _stopwatch.Elapsed.TotalSeconds;
            var nextLevelPaths = new List<FireZonePath>();
            foreach(var path in PathsToExplore)
            {
                var searchedPaths = Search(path,BestScore);
                foreach(var stepedPath in searchedPaths)
                {
                    if(stepedPath.Ended)
                    {
                        PathsToSocre.Add(stepedPath);
                    }
                    else nextLevelPaths.Add(stepedPath);
                }
            }
            PathsToExplore = nextLevelPaths;
            Logger?.Information($"遍历用时:{_stopwatch.Elapsed.TotalSeconds - t_pre}s");
        }
        private List<FireZonePath> Search(FireZonePath initPath, double minCost)
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
        private void Score()//打分,后面工作的重点
        {
            var polyCnt = 0;
            var planCnt = 0;
            t_pre = _stopwatch.Elapsed.TotalSeconds;
            foreach (var path in PathsToSocre)
            {
                if (ScoredPaths.ContainsKey(path)) continue;
                planCnt += 1;
                var cost = path.Cost;
                var splitted = Split(path);
                ScoredPaths.Add(path,(splitted.Item1, splitted.Item2, cost));
                Polygon poly = null;
                if (AreaVaild(splitted.Item1)) poly = splitted.Item1;
                else if(AreaVaild(splitted.Item2)) poly = splitted.Item2;
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
        private (Polygon,Polygon) Split(FireZonePath path)//基于path对图形切割
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
            var polys = polygonizer.GetPolygons().OfType<Polygon>().Except(nodePolys).OrderBy(p =>p.Area);
            if (polys.Count() != 2) throw new Exception($"Splitted {polys.Count()} Areas!");
            return (polys.First(), polys.Last());
        }
        private bool AreaVaild(Polygon polygon)
        {
            return polygon.Area<=MaxArea && polygon.Area>=MinArea;
        }

        #region 蚁群算法求解
        public (Polygon, Polygon, double) FindBestAC(double minArea, double maxArea,
            int StepSize = 25,int antCnt = 300,int loops = 300) 
        {
            MinArea = minArea * multiplier * multiplier;
            MaxArea = maxArea * multiplier * multiplier;
            var t_start = _stopwatch.Elapsed.TotalSeconds;
            var minCost = double.MaxValue;
            FireZonePath optSolution = null;
            for(int iter = 0; iter < loops; iter++)
            {
                var solutions = new List<FireZonePath>();
                for(int ant = 0; ant < antCnt; ant++)
                {
                    var solution = SearchOne(StepSize, minCost);
                    if (solution != null)
                    {
                        solutions.Add(solution);
                        if (solution.Cost < minCost)
                        {
                            minCost = solution.Cost;
                            optSolution = solution;
                        }
                    }
                }
                foreach(var solution in solutions)
                {
                    var Path = solution.Path;
                    for (int i = 0; i < Path.Count; i++)
                    {
                        var objId = Path[i];
                        if (i % 2 == 0)
                        {
                            IdToEdge[objId].Pheromone +=1* StepSize *1000/ solution.Cost;
                        }
                    }
                }
                foreach(var edge in IdToEdge.Values)
                {
                    edge.Pheromone *= 0.8;
                }
            }

            if (optSolution != null) return ScoredPaths[optSolution];
            else return (null, null, -1);
        }
        private FireZonePath SearchOne(int maxDepth,double minCost)
        {
            var path = new FireZonePath();
            for (int i = 0; i < maxDepth; i++)
            {
                var next = SearchAC(path, minCost);
                if (next == null) break;
                path = next;
            }
            if (!path.Ended) return null;

            if (!ScoredPaths.ContainsKey(path))
            {
                var splitted = Split(path);
                var areaDiff = Math.Min(AreaDiff(splitted.Item1), AreaDiff(splitted.Item2));
                path.Cost += 20 * Math.Sqrt(areaDiff);
                ScoredPaths.Add(path, (splitted.Item1, splitted.Item2, path.Cost));
            }
            else
            {
                path.Cost = ScoredPaths[path].Item3;
            }
            return path;
        }
        private FireZonePath SearchAC(FireZonePath initPath,double minCost)
        {
            var nextLevelPath = new List<FireZonePath>();
            if (initPath.Ended) return null;
            FireZoneNode initNode;
            if (initPath.Path.Count == 0)//初始节点，未添加路径
            {
                initNode = Root;
            }
            else
            {
                initNode = IdToNode[initPath.Path.Last()];
            }
            var prob = new List<double>();
            foreach (var branch in initNode.Branches)
            {
                var next_edge = branch.Item1;
                if (minCost < next_edge.Cost + initPath.Cost) continue;
                if (initPath.Contains(next_edge)) continue;
                var next_node = branch.Item2;
                if (initPath.Contains(next_node)) continue;
                var newPath = initPath.Step(next_edge, next_node);
                nextLevelPath.Add(newPath);
                prob.Add(next_edge.Pheromone);
            }
            if(prob.Count == 0) return null;
            var sum = prob.Sum();
            prob = prob.Select(p =>p/sum).ToList();
            var rand = ThParkingStallCoreTools.RandDouble();
            var p_sum = 0.0;
            for (int i = 0; i < prob.Count; i++)
            {
                p_sum += prob[i];
                if (p_sum > rand) return nextLevelPath[i]; 
            }
            return nextLevelPath.Last();
        }

        private double AreaDiff(Polygon poly)
        {
            if(poly.Area < MinArea) return MinArea - poly.Area;
            else if(poly.Area > MaxArea) return poly.Area - MaxArea;
            return 0;
        }

        #endregion
    }
}
