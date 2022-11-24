using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.OTools;
using ThParkingStall.Core.Tools;

namespace ThMEPArchitecture.FireZone
{
    public class ShearWallLineCreator
    {
        private List<Polygon> Obstacles;
        private Dictionary<int, List<LineSegment>> ObstacleLines = new Dictionary<int, List<LineSegment>>();
        private Dictionary<SWConnection, LineSegment> SWLines = new Dictionary<SWConnection, LineSegment>();
        private STRtree<int> ObstacleEngine = new STRtree<int>();
        private double MaxLength;
        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);
        private List<Polygon> BuildingBounds = new List<Polygon>();
        public ShearWallLineCreator(List<Polygon> obstacles,double maxLength)
        {
            var filterSize = 200 * 200;
            var buffered = new MultiPolygon(obstacles.Where(p =>p.Area> filterSize).ToArray()).
                Buffer(300,MitreParam).Union().Get<Polygon>(true);
            var unbuffered = new MultiPolygon(buffered.ToArray()).Buffer(-290,MitreParam).Get<Polygon>(true);

            Obstacles = unbuffered;
            MaxLength = maxLength;
            for(int i = 0; i < Obstacles.Count; i++)
            {
                var obstacle = Obstacles[i];
                ObstacleLines.Add(i, obstacle.Shell.ToLineSegments());
                ObstacleEngine.Insert(obstacle.EnvelopeInternal, i);
            }
            var buildingtol = 3000;
            buffered = new MultiPolygon(Obstacles.ToArray()).Buffer(buildingtol, MitreParam).Union().Get<Polygon>(true);//每一个polygong内部为一个建筑物
            BuildingBounds = new MultiPolygon(buffered.ToArray()).Buffer(-buildingtol-0.1, MitreParam).Get<Polygon>(true);

        }
        //-基于obb画网格
        //切除聚合框之外的
        //切除容差范围内的障碍物内的线
        public List<LineSegment> GenerateBaseOnObb(double stepLength = 6 * 1000, double filterSize =50* 1000*1000)
        {
            var lines = new List<LineSegment>();
            foreach(var bound in BuildingBounds)
            {
                var idxs = ObstacleEngine.Query(bound.EnvelopeInternal);
                var filtered = Obstacles.Slice(idxs).Where(p => (p.Area > filterSize && bound.Contains(p)));
                var geo = bound as Geometry;
                foreach(var p in filtered) geo = geo.Difference(p);
                var obb = geo.GetObb();

                //lines.AddRange(GenerateGrid(obb, stepLength));
                var grid = new MultiLineString(GenerateGrid(obb, stepLength).ToLineStrings().ToArray());
                lines.AddRange(grid.Intersection(geo).Get<LineString>().ToLineSegments());
            }
            return lines;
        }
        private List<LineSegment> GenerateGrid(Polygon polygon,double stepLength)
        {
            var p0 = polygon.Coordinates[0];
            var p1 = polygon.Coordinates[1];
            var p2 = polygon.Coordinates[2];
            var line0 = new LineSegment(p0, p1);
            var line1 = new LineSegment(p1, p2);
            var n1 = (int) (line1 .Length/ stepLength);
            var n2 = (int) (line0.Length/stepLength);
            var gridLines = new List<LineSegment>();
            var vec0 = new Vector2D(p1, p2).Normalize();
            for (int i = 0; i < n1-1; i++)
            {
                var distance = line1.Length * (i + 1) / n1;
                var vec = vec0.Multiply(distance);
                gridLines.Add(new LineSegment(vec.Translate(p0), vec.Translate(p1)));
            }
            var vec1 = new Vector2D(p1, p0).Normalize();
            for (int i = 0; i < n2-1; i++)
            {
                var distance = line0.Length * (i + 1) / n2;
                var vec = vec1.Multiply(distance);
                gridLines.Add(new LineSegment(vec.Translate(p1), vec.Translate(p2)));
            }
            return gridLines;

        }
        //障碍物上下左右发射4条线
        //去重（保留两两障碍物之前最短的)
        public List<LineSegment> GenerateLines()
        {
            //var lines = new List<LineSegment>();
            for(int i = 0; i < ObstacleLines.Count; i++)
            {
                var tempLines = GenerateLines(i);
                foreach(var tempLine in tempLines)
                {
                    var envelop = new Envelope(tempLine.P0,tempLine.P1);
                    var selectedIdxs = ObstacleEngine.Query(envelop);
                    selectedIdxs.Remove(i);
                    var sortest = Sortest(tempLine,selectedIdxs);
                    if (sortest.Item2 == -1) continue;
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
            for(int i = 0; i < lines.Count-1; i++)
            {
                var line = lines[i];
                for(int j = i+1; j < lines.Count; j++)
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
        public List<LineSegment> _GenerateLines()
        {
            var lines = new List<LineSegment>();
            for(int i = 0; i < ObstacleLines.Count; i++)
            {
                lines.AddRange(GenerateLines(i));
            }
            return lines;
        }
        private List<LineSegment> GenerateLines(int idx)
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
                results.Add(new LineSegment(furthestPoint, vec.Multiply(MaxLength).Translate(furthestPoint)));
            }
            return results;
        }

        private LineSegment SortestLine(LineSegment inLine,int idx)
        {
            var otherLines = ObstacleLines[idx];
            var coors = new List<Coordinate>();
            foreach(var line in otherLines)
            {
                var intSection = inLine.Intersection(line);
                if(intSection != null) coors.Add(intSection);
            }
            if(coors.Count == 0) return null;
            var nearestCoor = coors.OrderBy(c => c.Distance(inLine.P0)).First();
            return new LineSegment(inLine.P0, nearestCoor);
        }
        private (LineSegment,int) Sortest(LineSegment inLine,IEnumerable<int> idxs)
        {
            var minLength = double.MaxValue;
            int sortestIdx = -1;
            LineSegment sortestLine = null;
            foreach(var idx in idxs)
            {
                var line = SortestLine(inLine, idx);
                if(line != null && line.Length < minLength)
                {
                    minLength = line.Length;
                    sortestIdx = idx;
                    sortestLine = line;
                }
            }
            return(sortestLine,sortestIdx); 
        }
    }

    //public class SWConnection
    //{
    //    public int Idx1;
    //    public int Idx2;
    //    public SWConnection(int idx1,int idx2)
    //    {
    //        Idx1 = idx1; Idx2 = idx2;
    //    }
    //    public override bool Equals(object obj)
    //    {
    //        if (obj == null) return false;
    //        if(obj is SWConnection other)
    //        {
    //            return (this.Idx1 == other.Idx1 && this.Idx2 == other.Idx2)||
    //                (this.Idx2 == other.Idx1 && this.Idx1 == other.Idx2);
    //        }
    //        return false;
    //    }
    //    public override int GetHashCode()
    //    {
    //        return Idx1 ^ Idx2;
    //    }
    //}
}
