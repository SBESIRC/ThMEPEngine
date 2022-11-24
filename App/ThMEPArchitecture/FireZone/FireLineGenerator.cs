using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Simplify;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.LineCleaner;
using ThParkingStall.Core.MPartitionLayout;
using ThParkingStall.Core.OTools;
using ThParkingStall.Core.Tools;

namespace ThMEPArchitecture.FireZone
{
    public class FireLineGenerator
    {
        #region 输入
        private Polygon Basement;//地库
        private List<LineSegment> Lanes;//车道
        private List<InfoCar> Cars;//车位
        #endregion

        #region 处理后输入
        public Polygon WallLine;//处理后边界
        public List<Polygon> BuildingBounds;//建筑聚合框
        public List<Polygon> Obstacles;//处理后障碍物
        private STRtree<int> ObstacleEngine = new STRtree<int>();// 障碍物空间索引
        private STRtree<LineSegment> WallLineEngine = new STRtree<LineSegment>();//边界线空间索引
        #endregion

        #region 运算结果
        public List<LineSegment> FireWalls;//防火墙
        public List<LineSegment> Shutters;//卷帘门

        #endregion
        #region 剪力墙连线相关
        private Dictionary<int, List<LineSegment>> ObstacleLines = new Dictionary<int, List<LineSegment>>();//障碍物索引对应的线
        private Dictionary<SWConnection, LineSegment> SWLines = new Dictionary<SWConnection, LineSegment>();//连接关系对应的线
        #endregion

        #region 辅助变量
        private Stopwatch _stopwatch = new Stopwatch();
        private double t_pre = 0;
        public Serilog.Core.Logger Logger = null;
        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);
        #endregion
        public FireLineGenerator(Polygon basement, List<LineSegment> lanes, List<InfoCar> cars)
        {
            Basement = basement;
            Lanes = lanes;
            Cars = cars;
            Preprocess();
        }
        #region 输入处理
        private void Preprocess(double tol = 75)
        {
            //輸入简化
            var buffered = Basement.Buffer(-tol, MitreParam).Union().
                Buffer(2 * tol, MitreParam).Buffer(-tol, MitreParam).
                Get<Polygon>(false).OrderBy(p => p.Area).Last();
            WallLine = new Polygon(buffered.Shell);
            Obstacles = buffered.Holes.Select(r => new Polygon(r)).ToList();
            #region 剪力墻連綫部分
            for (int i = 0; i < Obstacles.Count; i++)
            {
                var obstacle = Obstacles[i];
                ObstacleLines.Add(i, obstacle.Shell.ToLineSegments());
                ObstacleEngine.Insert(obstacle.EnvelopeInternal, i);
            }
            var wallLines = WallLine.Shell.ToLineSegments();
            foreach(var line in wallLines) 
                WallLineEngine.Insert(new Envelope(line.P0,line.P1), line);
            var buildingtol = 3000;
            BuildingBounds = new MultiPolygon(Obstacles.ToArray()).Buffer(buildingtol, MitreParam).Union().Get<Polygon>(true);//每一个polygong内部为一个建筑物
            BuildingBounds = new MultiPolygon(BuildingBounds.ToArray()).Buffer(-buildingtol, MitreParam).Get<Polygon>(true);
            #endregion
        }
        #endregion

        #region 防火线以及卷帘生成
        public void Generate()
        {
            FireWalls = GenerateLinesInsideBuildings();
            Shutters = GenerateLinesOfCars();
        }
        #endregion
        #region 障碍物连线
        //障碍物上下左右发射4条线
        //去重（保留两两障碍物之间最短的)
        public List<LineSegment> GenerateLinesInsideBuildings(double maxLength = 9000)
        {
            for (int i = 0; i < ObstacleLines.Count; i++)
            {
                var tempLines = GenerateLines(i, maxLength);
                foreach (var tempLine in tempLines)
                {
                    var envelop = new Envelope(tempLine.P0, tempLine.P1);
                    var selectedIdxs = ObstacleEngine.Query(envelop);
                    selectedIdxs.Remove(i);
                    var sortest = Sortest(tempLine, selectedIdxs);
                    if (sortest.Item2 == -2) continue;
                    var pair = new SWConnection(i, sortest.Item2);
                    if (SWLines.ContainsKey(pair))
                    {
                        if (SWLines[pair].Length > sortest.Item1.Length) SWLines[pair] = sortest.Item1;
                    }
                    else SWLines.Add(pair, sortest.Item1);
                }
            }
            var lines = SWLines.Values.ToList();
            //去除相交线
            var InvalidIdxs = new HashSet<int>();
            for (int i = 0; i < lines.Count - 1; i++)
            {
                var line = lines[i];
                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (line.Intersection(lines[j]) != null)
                    {
                        InvalidIdxs.Add(i);
                        InvalidIdxs.Add(j);
                        break;
                    }
                }
            }
            return lines.SliceExcept(InvalidIdxs);
        }
        private List<LineSegment> GenerateLines(int idx,double maxLength)
        {
            var centroid = Obstacles[idx].Centroid.Coordinate;
            var lines = ObstacleLines[idx];
            var dir = lines.OrderBy(l => l.Length).Last().DirVector();
            var results = new List<LineSegment>();
            var coors = lines.Select(l => l.MidPoint);
            for (int i = 0; i < 4; i++)
            {
                var vec = dir.RotateByQuarterCircle(i);
                var furthestPoint = coors.OrderBy(c => vec.Dot(new Vector2D(centroid, c))).Last();
                results.Add(new LineSegment(furthestPoint, vec.Multiply(maxLength).Translate(furthestPoint)));
            }
            return results;
        }

        private LineSegment SortestLine(LineSegment inLine, int idx)//线与序号中障碍物的最近的部分
        {
            return SortestLine(inLine, ObstacleLines[idx]);
        }
        private LineSegment SortestLine(LineSegment inLine)//线与边界的最近的部分
        {
            return SortestLine(inLine, WallLineEngine.Query(new Envelope(inLine.P0, inLine.P1)));
        }
        private LineSegment SortestLine(LineSegment inLine,IEnumerable<LineSegment> others)
        {
            var coors = new List<Coordinate>();
            foreach (var line in others)
            {
                var intSection = inLine.Intersection(line);
                if (intSection != null) coors.Add(intSection);
            }
            if (coors.Count == 0) return null;
            var nearestCoor = coors.OrderBy(c => c.Distance(inLine.P0)).First();
            return new LineSegment(inLine.P0, nearestCoor);
        }
        private (LineSegment, int) Sortest(LineSegment inLine, IEnumerable<int> idxs)
        {
            var minLength = double.MaxValue;
            int sortestIdx = -2;
            LineSegment sortestLine = SortestLine(inLine);
            if(sortestLine!= null)
            {
                minLength = sortestLine.Length;
                sortestIdx = -1;
            }
            foreach (var idx in idxs)
            {
                var line = SortestLine(inLine, idx);
                if (line != null && line.Length < minLength)
                {
                    minLength = line.Length;
                    sortestIdx = idx;
                    sortestLine = line;
                }
            }
            return (sortestLine, sortestIdx);
        }
        #endregion

        #region 车位防火线
        public List<LineSegment> GenerateLinesOfCars(double tol = 1000)
        {
            var sideLines = new List<LineSegment>();
            var tailLines = new List<LineSegment>();
            var fireLines = new List<LineSegment>();
            foreach (var car in Cars)
            {
                var linesegs = car.Polyline.Shell.ToLineSegments();
                sideLines.Add(linesegs[1]);
                tailLines.Add(linesegs[2]);
                sideLines.Add(linesegs[3]);
            }
            var cleaner = new LineService(sideLines,50);
            var idxs_group = cleaner.GroupByParalle(sideLines);
            //对于一个group个数》=2：如果存在两根线互相投影长度较短则丢弃(>1m),否则保留
            foreach (var group in idxs_group)
            {
                bool keepgroup = true;
                if (group.Count() >= 2)
                {
                    for (int i = 0; i < group.Count() - 1; i++)
                    {
                        var l1 = sideLines[group[i]];
                        for (int j = i + 1; j < group.Count(); j++)
                        {
                            var l2 = sideLines[group[j]];
                            var projection = l1.Project(l2);
                            if (projection != null && projection.Length > tol)
                            {
                                keepgroup = false;
                                break;
                            }
                        }
                        if (!keepgroup) break;
                    }
                }
                if (keepgroup) fireLines.AddRange(sideLines.Slice(group));
            }
            fireLines.AddRange(tailLines);
            cleaner = new LineService(fireLines, tol);
            fireLines = cleaner.MergeParalle(fireLines);
            fireLines = ExtendToOthers(fireLines, tol);
            return RemoveCloseLines(fireLines,6000);
        }

        private List<LineSegment> ExtendToOthers(List<LineSegment> fireLines, double distance)
        {
            var extended = fireLines.Select(l => l.OExtend(distance)).ToList();
            var engine = new STRtree<int>();
            for (int i = 0; i < extended.Count; i++)
            {
                engine.Insert(new Envelope(extended[i].P0, extended[i].P1), i);
            }
            var results = new List<LineSegment>();
            for (int i = 0; i < fireLines.Count; i++)
            {
                var coors = new List<Coordinate> { fireLines[i].P0, fireLines[i].P1 };
                var extendedLine = extended[i];
                var envelop = new Envelope(extendedLine.P0, extendedLine.P1);
                var queried = engine.Query(envelop);
                queried.Remove(i);
                var intsetcions = extended.Slice(queried).Where(l =>!l.ParallelTo(extendedLine))
                    .Select(l => l.Intersection(extendedLine)).Where(c => c != null);
                coors.AddRange(intsetcions);
                coors = coors.PositiveOrder();
                results.Add(new LineSegment(coors.First(), coors.Last()));
            }
            return results;
        }
        private List<LineSegment> RemoveCloseLines(List<LineSegment> fireLines,double distance)
        {
            var results = new List<LineSegment>();
            var shell = WallLine.Shell;
            foreach (var line in fireLines)
            {
                if(line.P0.ToPoint().Distance(shell) > distance || line.P1.ToPoint().Distance(shell) > distance)
                {
                    results.Add(line);
                }
            }
            return results;
        }
        #endregion
    }
    public class SWConnection//两个障碍物之间连接关系
    {
        public int Idx1;
        public int Idx2;
        public SWConnection(int idx1, int idx2)
        {
            Idx1 = idx1; Idx2 = idx2;
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is SWConnection other)
            {
                return (this.Idx1 == other.Idx1 && this.Idx2 == other.Idx2) ||
                    (this.Idx2 == other.Idx1 && this.Idx1 == other.Idx2);
            }
            return false;
        }
        public override int GetHashCode()
        {
            return Idx1 ^ Idx2;
        }
    }
}
