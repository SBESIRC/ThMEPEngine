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
        private Polygon InputBasement;//地库
        private List<LineSegment> Lanes;//车道
        private List<InfoCar> Cars;//车位
        #endregion

        #region 处理后输入
        public Polygon WallLine;//处理后边界
        public List<Polygon> BuildingBounds;//建筑聚合框
        public List<Polygon> Obstacles;//处理后障碍物
        public Polygon Basement;//地库
        private STRtree<int> ObstacleEngine = new STRtree<int>();// 障碍物空间索引
        private STRtree<LineSegment> WallLineEngine = new STRtree<LineSegment>();//边界线空间索引
        private STRtree<LineSegment> BasementEngine = new STRtree<LineSegment>();//地库所有线索引
        private STRtree<LineSegment> EdgeLineEngine = new STRtree<LineSegment>();//边界以及障碍物,以及车道(polygon)所有线的索引
        #endregion

        #region 运算结果
        public List<LineSegment> FireWalls = new List<LineSegment>();//防火墙
        public List<LineSegment> Shutters = new List<LineSegment>();//卷帘门
        
        #endregion
        #region 剪力墙连线相关
        private Dictionary<int, List<LineSegment>> ObstacleLines = new Dictionary<int, List<LineSegment>>();//障碍物索引对应的线
        private Dictionary<SWConnection, LineSegment> SWLines = new Dictionary<SWConnection, LineSegment>();//连接关系对应的线
        public List<LineSegment> BuildingFireLines = new List<LineSegment>();
        #endregion
        #region 车位线相关
        //private STRtree<Polygon> LaneEngine = new STRtree<Polygon>();//车道矩形框索引
        public List<LineSegment> CarFireLines = new List<LineSegment> ();//车位防火线
        #endregion

        #region 发射线相关
        public List<FireLineStartPoint> StartPoints = new List<FireLineStartPoint>();
        #endregion

        #region 辅助变量
        private Stopwatch _stopwatch = new Stopwatch();
        private double t_pre = 0;
        public Serilog.Core.Logger Logger = null;
        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);
        double LaneWidth = 2750;
        #endregion
        public FireLineGenerator(Polygon basement, List<LineSegment> lanes, List<InfoCar> cars)
        {
            InputBasement = basement;
            Lanes = lanes;
            Cars = cars;
            Preprocess();
        }
        #region 输入处理
        private void Preprocess(double tol = 75)
        {
            //輸入简化
            Basement = InputBasement.Buffer(-tol, MitreParam).Union().
                Buffer(2 * tol, MitreParam).Buffer(-tol, MitreParam).
                Get<Polygon>(false).OrderBy(p => p.Area).Last();
            WallLine = new Polygon(Basement.Shell);
            Obstacles = Basement.Holes.Select(r => new Polygon(r)).ToList();
            #region 剪力墻連綫部分
            for (int i = 0; i < Obstacles.Count; i++)
            {
                var obstacle = Obstacles[i];
                var lsegs = obstacle.Shell.ToLineSegments();
                ObstacleLines.Add(i, lsegs);
                ObstacleEngine.Insert(obstacle.EnvelopeInternal, i);
                foreach (var seg in lsegs)
                {
                    var envelop = new Envelope(seg.P0, seg.P1);
                    EdgeLineEngine.Insert(envelop, seg);
                    BasementEngine.Insert(envelop, seg);
                }
                    
            }
            var wallLines = WallLine.Shell.ToLineSegments();
            foreach(var line in wallLines)
            {
                var envelop = new Envelope(line.P0, line.P1);
                EdgeLineEngine.Insert(envelop, line);
                WallLineEngine.Insert(envelop, line);
                BasementEngine.Insert(envelop, line);
            }
            var buildingtol = 3000;
            BuildingBounds = new MultiPolygon(Obstacles.ToArray()).Buffer(buildingtol, MitreParam).Union().Get<Polygon>(true);//每一个polygong内部为一个建筑物
            BuildingBounds = new MultiPolygon(BuildingBounds.ToArray()).Buffer(-buildingtol, MitreParam).Get<Polygon>(true);
            #endregion
            #region 车位线部分
            foreach(var lane in Lanes)
            {
                var poly = lane.OGetRect(LaneWidth);
                var lsegs = poly.Shell.ToLineSegments();
                foreach (var seg in lsegs)
                    EdgeLineEngine.Insert(new Envelope(seg.P0, seg.P1), seg);
                //LaneEngine.Insert(poly.EnvelopeInternal, poly);
            }
            #endregion
        }
        #endregion

        #region 防火线以及卷帘生成
        public void Generate()
        {
            
            CarFireLines = GenerateLinesOfCars();
            BuildingFireLines = GenerateLinesInsideBuildings();
            StartPointsCreateFireLines();
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

            var InvalidIdxs = new HashSet<int>();
            //去除与车位线距离较近且平行的线，或者相交的线
            var carLineEngine = new STRtree<LineSegment>();
            foreach(var cLine in CarFireLines) carLineEngine.Insert(new Envelope(cLine.P0,cLine.P1), cLine);
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var envelop = new Envelope(line.P0, line.P1);
                envelop.ExpandBy(1000);
                var invaild = carLineEngine.Query(envelop).
                    Any(l =>l.Intersection(line)!= null || (l.ParallelTo(line) && l.Distance(line) < 1000));
                if(invaild) InvalidIdxs.Add(i);
            }
            lines = lines.SliceExcept(InvalidIdxs);
            //去除相交线
            InvalidIdxs = new HashSet<int>();
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
            return RemoveCloseLines(fireLines, 4000);
        }
        //找到与可布置区域的交集
        private LineSegment FindVaildPart(LineSegment fireLine ,Coordinate center)
        {
            var direction = fireLine.DirVector();
            var queried = EdgeLineEngine.Query(new Envelope(fireLine.P0, fireLine.P1));
            var intsections = queried.Select(l => l.Intersection(fireLine)).Where(l => l != null);
            var coors = new List<Coordinate> { fireLine.P0,fireLine.P1 };
            coors.AddRange(intsections);
            var posCoors = new List<Coordinate>();
            var negCoors = new List<Coordinate>();
            foreach(var coor in coors)
            {
                var vec = new Vector2D(center, coor);
                if(vec.Dot(direction) > 0) posCoors.Add(coor);
                else negCoors.Add(coor);
            }
            var posOrdered = posCoors.OrderBy(c =>c.Distance(center));
            var negOrdered = negCoors.OrderBy(c =>c.Distance(center));
            if(posOrdered.Count() == 0 || negOrdered.Count() == 0) return null;
            else return new LineSegment(posOrdered.First(),negOrdered.First());
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
                var center = fireLines[i].MidPoint;
                var extendedLine = extended[i];
                var envelop = new Envelope(extendedLine.P0, extendedLine.P1);
                var queried = engine.Query(envelop);
                queried.Remove(i);
                var intsetcions = extended.Slice(queried).Where(l =>!l.ParallelTo(extendedLine))
                    .Select(l => l.Intersection(extendedLine)).Where(c => c != null);
                coors.AddRange(intsetcions);
                coors = coors.PositiveOrder();
                var newLine = new LineSegment(coors.First(), coors.Last());
                newLine = FindVaildPart(newLine, center);
                if(newLine != null) results.Add(newLine);
            }
            return results;
        }
        private List<LineSegment> RemoveCloseLines(List<LineSegment> fireLines,double distance)
        {
            var bounds = WallLine.Shell.ToLineSegments();
            var engine = new STRtree<LineSegment>();
            foreach(var line in bounds)
                engine.Insert(new Envelope(line.P0,line.P1),line);
            var invaildIdxs = new HashSet<int>();
            for(int i = 0;i < fireLines.Count;i++)
            {
                var line = fireLines[i];
                var envelop = new Envelope(line.P0, line.P1);
                envelop.ExpandBy(distance);
                var queried = engine.Query(envelop);
                if(queried.Count == 0) continue;
                var dist0 = queried.Min(l => l.Distance(line.P0));
                var dist1 = queried.Min(l => l.Distance(line.P1));
                if (dist0 < distance && dist1 < distance && Math.Abs(dist0 - dist1) < 1000 )//距离边界过近且几乎平行于边界
                {
                    invaildIdxs.Add(i);
                }
            }
            return fireLines.SliceExcept(invaildIdxs);
        }
        #endregion

        #region 端点发射线
        public void StartPointsCreateFireLines()
        {
            CreateStartPoints();
        }
        private void CreateStartPoints()//生成端点
        {
            var coordinates = new HashSet<Coordinate>();
            var carFireLineEngine = new STRtree<int>();
            for(int i = 0;i<CarFireLines.Count;i++)
            {
                var line = CarFireLines[i];
                coordinates.Add(line.P0);
                coordinates.Add(line.P1);
                carFireLineEngine.Insert(new Envelope(line.P0, line.P1), i);
            }
            foreach(var coor in coordinates)
            {
                var envelop = new Envelope(coor);
                envelop.ExpandBy(5);
                if (BasementEngine.Query(envelop).Count > 0) continue;
                var startPt = new FireLineStartPoint(coor, CarFireLines.Slice(carFireLineEngine.Query(envelop)));
                if(startPt.Directions.Count > 0)StartPoints.Add(startPt);
            }
        }
        private LineSegment BestLineToPoints(
            Coordinate startPt,Vector2D direction,double depth,double width, STRtree<Point> engine,double tol = 5)
        {
            var bottomPt = direction.Multiply(depth).Translate(startPt);
            var p0 = direction.Multiply(width).RotateByQuarterCircle(1).Translate(bottomPt);
            var p1 = direction.Multiply(width).RotateByQuarterCircle(-1).Translate(bottomPt);
            var triAngle =new Polygon(new LinearRing(new Coordinate[] {p0,p1,startPt,p0}));
            var queried = engine.Query(triAngle.EnvelopeInternal).
                Where(pt => pt.Coordinate.Distance(startPt) > tol&& triAngle.Contains(pt)).
                OrderBy(p =>p.Coordinate.Distance(startPt));
            if (queried.Count() == 0) return null;
            else return new LineSegment(startPt, queried.First().Coordinate);
        }
        private LineSegment BestLineToLines(
    Coordinate startPt, Vector2D direction, double depth, STRtree<LineSegment> engine, double tol = 5)
        {
            var tempLine = new LineSegment(startPt,direction.Multiply(depth).Translate(startPt));
            var envelop = new Envelope(tempLine.P0, tempLine.P1);
            var queried = engine.Query(envelop).
                Where(l => l.Distance(startPt) > tol).Select(l => l.Intersection(tempLine)).
                Where(pt => pt != null).OrderBy(pt => pt.Distance(startPt));
            if (queried.Count() == 0) return null;
            else return new LineSegment(startPt, queried.First());
        }

        #endregion
    }
    public class SWConnection//两个障碍物(或边界 -1)之间连接关系
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

    public class FireLineStartPoint//防火线发射端点
    {
        public Coordinate StartPoint;//起始点
        public HashSet<Vector2D> Directions = new HashSet<Vector2D>();
        static double AngleTol = Math.PI / 3;
        public FireLineStartPoint(Coordinate startpoint)
        {
            StartPoint = startpoint;
        }
        public FireLineStartPoint(Coordinate startpoint,IEnumerable<LineSegment> connectedLines)
        {
            StartPoint=startpoint;
            if (connectedLines.Count() == 0) return;
            var baseDir = connectedLines.OrderBy(l =>l.Length).Last().DirVector();
            var temDirections = new List<Vector2D>(4);
            for(int i = 0; i < 4; i++)
            {
                temDirections.Add(baseDir.RotateByQuarterCircle(i));
            }
            var vectors = new List<Vector2D>();
            foreach (var line in connectedLines)
            {
                var vec0 = new Vector2D(StartPoint, line.P0);
                var vec1 = new Vector2D(StartPoint, line.P1);
                if(vec0.Length() > 1)vectors.Add(vec0);
                if(vec1.Length() > 1)vectors.Add(vec1);
            }
            foreach(var dir in temDirections)
            {
                if (!vectors.Any(v => v.Angle(dir) < AngleTol)) Add(dir); 
            }
        }
        public void Add(Vector2D vector)
        {
            Directions.Add(vector);
        }
        public void Remove(Vector2D vector)
        {
            Directions.Remove(vector);
        }
    }
}
