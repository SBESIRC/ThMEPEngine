using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
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

        public ShearWallLineCreator(List<Polygon> obstacles,double maxLength)
        {
            Obstacles = obstacles;
            MaxLength = maxLength;
            for(int i = 0; i < obstacles.Count; i++)
            {
                var obstacle = obstacles[i];
                ObstacleLines.Add(i, obstacle.Shell.ToLineSegments());
                ObstacleEngine.Insert(obstacle.EnvelopeInternal, i);
            }
        }
        public List<LineSegment> _GenerateLines()
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
            return SWLines.Values.ToList();
        }
        public List<LineSegment> GenerateLines()
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

    public class SWConnection
    {
        public int Idx1;
        public int Idx2;
        public SWConnection(int idx1,int idx2)
        {
            Idx1 = idx1; Idx2 = idx2;
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if(obj is SWConnection other)
            {
                return (this.Idx1 == other.Idx1 && this.Idx2 == other.Idx2)||
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
