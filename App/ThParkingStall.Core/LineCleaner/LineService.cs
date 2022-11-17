using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Operation.Polygonize;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.OTools;
using ThParkingStall.Core.Tools;

namespace ThParkingStall.Core.LineCleaner
{
    public class LineService
    {
        private List<LineSegment> InitLines;
        public double Tol;
        public double Delta;

        public LineService(List<LineSegment> initLines , double tol = 5,double delta = 0.01)
        {
            InitLines = initLines;
            Tol = tol;
            Delta = delta;
        }
        #region 线清理，碎线合并，节点化，相邻线连接
        public List<LineSegment> Clean(bool clust = true)
        {
            if(!clust) return Clean(InitLines);
            var clusters = ClusterByIndex(InitLines);
            var result = new List<LineSegment>();
            foreach (var cluster in clusters)
            {
                result.AddRange(Clean(InitLines.Slice(cluster)));
            }
            return result;
        }
        public List<LineSegment> Clean(List<LineSegment> lines)//三板斧
        {
            //var paralled = MergeParalle(lines);//合并距离较近的平行线
            var noded = Noding(lines);//节点化
            return MergePoints(noded);//合并节点
        }
        public List<LineSegment> MergeParalle(List<LineSegment> inputLines)//把平行（角度小于delta）且相交(距离小于tol）的线合并
        {
            var groups = GroupByParalle(inputLines);
            return groups.Select(g => inputLines.Slice(g).Merge()).ToList();
        }
        private List<List<int>> GroupByParalle(List<LineSegment> lines)//把所有平行且距离小于tol的线基于idx分组
        {
            var LineIdxEngine = new STRtree<int>();
            var envelops = new List<Envelope>();
            double expandSize = Tol / 2;
            var IdxSet = new HashSet<int>();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var envelop = new Envelope(line.P0, line.P1);
                envelop.ExpandBy(expandSize);
                LineIdxEngine.Insert(envelop, i);
                envelops.Add(envelop);
                IdxSet.Add(i);
            }
            var groups = new List<List<int>>();
            while(IdxSet.Count > 0)
            {
                var idx = IdxSet.First();
                var envelop = envelops[idx].Copy();
                var line = lines[idx];
                var group = new HashSet<int> { idx};//平行于当前线的idx（包含当前线）
                var neighbors = new HashSet<int> { idx};
                while (true)
                {
                    neighbors = LineIdxEngine.Query(envelop).ToHashSet();
                    var invalidOnes = new List<int>();
                    foreach(var nidx in neighbors)//找到距离小于tol且平行的线
                    {
                        var otherLine = lines[nidx];
                        if(!otherLine.ParallelTo(line,Delta) || !group.Any(id =>lines[id].Distance(otherLine)<Tol))
                            invalidOnes.Add(nidx);
                    }
                    neighbors.ExceptWith(invalidOnes);
                    neighbors.ExceptWith(group);
                    if (neighbors.Count == 0) break;
                    foreach(var nidx in neighbors) group.Add(nidx);
                    envelops.Slice(neighbors).ForEach(e => envelop.ExpandToInclude(e));
                }
                groups.Add(group.ToList());
                IdxSet.ExceptWith(group);
            }
            return groups;
        }
        public List<LineSegment> Noding(List<LineSegment> lines)//当一根线与另一根线中间距离小于tol，打断该线为两部分
        {
            var LineIdxEngine = new STRtree<int>();
            var envelops = new List<Envelope>();
            double expandSize = Tol / 2;
            var IdxSet = new HashSet<int>();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var envelop = new Envelope(line.P0, line.P1);
                envelop.ExpandBy(expandSize);
                LineIdxEngine.Insert(envelop, i);
                envelops.Add(envelop);
                IdxSet.Add(i);
            }
            var nodedLine = new List<LineSegment>();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var neighbors = LineIdxEngine.Query(envelops[i]);
                var nodes = new List<Coordinate> { line.P0, line.P1 };//当前线所有的点
                foreach (var nidx in neighbors)
                {
                    var otherLine = lines[nidx];
                    if (otherLine.Distance(line) >= Tol) continue;
                    var intSecPt = line.Intersection(otherLine);
                    if (intSecPt != null && nodes.All(n => n.Distance(intSecPt) > Tol))//有交点，且现有记录里没有距离小于tol的点
                    {
                        nodes.Add(intSecPt);
                        continue;
                    }
                    if (line.Distance(otherLine.P0) < Tol)
                    {
                        var cloest2P0 = line.ClosestPoint(otherLine.P0);
                        if (nodes.All(n => n.Distance(cloest2P0) > Tol))
                        {
                            nodes.Add(cloest2P0);
                        }
                        continue;
                    }
                    if (line.Distance(otherLine.P1) < Tol)
                    {
                        var cloest2P1 = line.ClosestPoint(otherLine.P1);
                        if (nodes.All(n => n.Distance(cloest2P1) > Tol))
                        {
                            nodes.Add(cloest2P1);
                        }
                        continue;
                    }
                }
                nodes = nodes.OrderBy(n => n.Distance(line.P0)).ToList();
                for (int j = 1; j < nodes.Count; j++)
                {
                    nodedLine.Add(new LineSegment(nodes[j - 1], nodes[j]));
                }
            }
            return nodedLine;
        }
        public List<LineSegment> MergePoints(List<LineSegment> inputLines)//距离小于tol的点合为一个，返回合并后的线
        {
            var lines = inputLines.Select(l =>l.Clone()).ToList();
            var MPs = new List<MagneticPoint>();
            double expandSize = Tol / 2;
            for(int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                MPs.Add(new MagneticPoint(line.P0,expandSize,i,true));
                MPs.Add(new MagneticPoint(line.P1, expandSize, i, false));
            }
            var PtIdxEngine = new STRtree<int>();
            HashSet<int> IdxSet = new HashSet<int>();
            for(int j = 0; j < MPs.Count; j++)
            {
                var mp = MPs[j];
                IdxSet.Add(j);
                PtIdxEngine.Insert(mp.Envelope, j);
            }
            //var IsolatedIdx = new HashSet<int>();//孤立点（不与其他点合并的）
            var newMPs = new List<MagneticPoint>();
            while(IdxSet.Count > 0)
            {
                var idx = IdxSet.First();
                var envelop = MPs[idx].Envelope.Copy();
                var idxToMerge = new HashSet<int> { idx};
                var neighbors = new HashSet<int> { idx };
                while(true)
                {
                    neighbors = PtIdxEngine.Query(envelop).ToHashSet();
                    neighbors.ExceptWith(idxToMerge);//去除已经找到的
                    if (neighbors.Count == 0) break;
                    foreach(var nidx in neighbors) idxToMerge.Add(nidx);
                    MPs.Slice(neighbors).ForEach(x => envelop.ExpandToInclude( x.Envelope));
                }
                newMPs.Add(Merge(MPs.Slice(idxToMerge)));//合并距离过近的点
                IdxSet.ExceptWith(idxToMerge);
            }
            
            foreach(var mp in newMPs)
            {
                if(mp.LineIdxs.Count > 1)
                {
                    foreach(var idx in mp.LineIdxs)
                    {
                        if (idx.Item2) lines[idx.Item1].P0 = mp.Pt;
                        else lines[idx.Item1].P1 = mp.Pt;
                    }
                }
            }
            return lines.Where(l =>l.Length > 0).ToLineStrings().ToHashSet().ToLineSegments();
        }
        private MagneticPoint Merge(IEnumerable<MagneticPoint> mPs)
        {
            if (mPs.Count() == 0) throw new ArgumentException();
            if(mPs.Count() == 1) return mPs.First();
            var MPs = mPs.ToList();
            var center = new MultiPoint(MPs.Select(mp => mp.Pt.ToPoint()).ToArray()).Centroid;
            double expandSize = Tol / 2;
            var result = new MagneticPoint(center.Coordinate, expandSize);
            MPs.ForEach(mp => result.LineIdxs.AddRange(mp.LineIdxs));
            return result;
        }
        #endregion
        #region 线聚类,用于问题简化
        public List<List<int>> ClusterByIndex(List<LineSegment> lines)
        {
            var LineIdxEngine = new STRtree<int>();
            var envelops = new List<Envelope>();
            double expandSize = Tol / 2;
            var IdxSet = new HashSet<int>();
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var envelop = new Envelope(line.P0, line.P1);
                envelop.ExpandBy(expandSize);
                LineIdxEngine.Insert(envelop, i);
                envelops.Add(envelop);
                IdxSet.Add(i);
            }
            var groups = new List<List<int>>();
            while (IdxSet.Count > 0)
            {
                var idx = IdxSet.First();
                var line = lines[idx];
                var group = new HashSet<int> { idx };//平行于当前线的idx（包含当前线）
                var neighbors = new HashSet<int> { idx };
                while (true)
                {
                    var orgLines = lines.Slice(neighbors);
                    var outerEnvelops = envelops.Slice(neighbors);
                    neighbors = new HashSet<int>();
                    outerEnvelops.ForEach(e => LineIdxEngine.Query(e).ToList().ForEach(id => neighbors.Add(id)));//query出neighbors的邻居
                    var invalidOnes = new List<int>();
                    foreach (var nidx in neighbors)//过滤掉距离大于tol的线
                    {
                        var otherLine = lines[nidx];
                        if (!orgLines.Any(l => l.Distance(otherLine) < Tol))
                            invalidOnes.Add(nidx);
                    }
                    neighbors.ExceptWith(invalidOnes);
                    neighbors.ExceptWith(group);
                    if (neighbors.Count == 0) break;
                    foreach (var nidx in neighbors) group.Add(nidx);
                }
                groups.Add(group.ToList());
                IdxSet.ExceptWith(group);
            }
            return groups;
        }

        #endregion
        #region 求面域
        public List<Polygon> GetPolygons(bool union = false,bool keepHoles = true,bool multithread = true)
        {
            var clusters = ClusterByIndex(InitLines);
            if (multithread)
            {
                var bag = new ConcurrentBag<Polygon>();
                Parallel.ForEach(clusters, cluster =>
                {
                    var polygons = GetPolygons(cluster, union,keepHoles);
                    polygons.ForEach(p => bag.Add(p));
                });
                return bag.ToList();
            }
            else
            {
                var result = new List<Polygon>();
                foreach (var cluster in clusters)
                {
                    var polygons = GetPolygons(cluster, union, keepHoles);
                    result.AddRange(polygons);
                }
                return result;
            }
        }
        private List<Polygon> GetPolygons(List<int> cluster,bool union = false,bool keepHoles = true)
        {
            var cleaned = Clean(InitLines.Slice(cluster));
            var polygons = GetPolygons(cleaned, keepHoles);
            if (union)
            {
                polygons = new MultiPolygon(polygons.ToArray()).Union().Get<Polygon>(!keepHoles);
            }
            return polygons;
        }
        private List<Polygon> GetPolygons(List<LineSegment> lines ,bool keepHoles = true)//输入必须为nodedlines
        {
            var polygonizer = new Polygonizer();
            lines.ForEach(l => polygonizer.Add(l.ToGeometry(GeometryFactory.Default)));
            var polygons = polygonizer.GetPolygons().Cast<Polygon>().ToList();
            if(!keepHoles) polygons = polygons.Select(p =>new Polygon(p.Shell)).ToList();
            return polygons;
        }

        #endregion
    }
    public class MagneticPoint//磁力点，与相近磁力点合并
    {
        public Coordinate Pt;
        public List<(int,bool)> LineIdxs = new List<(int,bool)> ();//int 代表线序号，bool代表是否为P0
        public Envelope Envelope;
        public MagneticPoint(Coordinate pt,double expandSize)
        {
            Pt = pt;
            Envelope = new Envelope(pt);
            Envelope.ExpandBy(expandSize);
        }
        public MagneticPoint(Coordinate pt,double expandSize,int idx,bool isP0):
            this(pt,expandSize)
        {
            AddIdx(idx,isP0);
        }
        public void AddIdx((int,bool) idx)
        {
            LineIdxs.Add(idx);
        }
        public void AddIdx(int idx, bool isP0)
        {
            LineIdxs.Add((idx,isP0));
        }
    }
}
