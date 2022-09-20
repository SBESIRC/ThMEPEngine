using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.MPartitionLayout;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;
using static ThParkingStall.Core.SParkingPartition.Sparam;

namespace ThParkingStall.Core.SParkingPartition
{
    public class SLane
    {
        public SLane(LineSegment line, Vector2D vec)
        {
            Line = line;
            Vec = vec;
        }
        public LineSegment Line { get; set; }
        public Vector2D Vec { get; set; }

        public static List<SLane> ConstructFromLine(List<LineSegment> lines, Polygon bound)
        {
            var lanes = new List<SLane>();
            double overlayTol = 1;
            foreach (var line in lines)
            {
                var ps = line.P0;
                var pe = line.P1;
                //如果起点终点均在边界上，认为该线段在边界上
                if (bound.ClosestPoint(ps).Distance(ps) < overlayTol && bound.ClosestPoint(ps).Distance(ps) < overlayTol)
                {
                    var vec = Vector(line).GetPerpendicularVector().Normalize();
                    var ptest = line.MidPoint.Translation(vec);
                    if (!bound.Contains(ptest))
                        vec = -vec;
                    lanes.Add(new SLane(line.New(), vec));
                }
                //如果与边界相交，与相交剪切后边界内最长车道线双向成车道
                else if (bound.IntersectPoint(line.ToLineString()).Count() > 0)
                {
                    var split = SplitLine(line, bound).Where(e => bound.Contains(e.MidPoint)).OrderByDescending(e => e.Length).First();
                    var vec = Vector(split).GetPerpendicularVector().Normalize();
                    lanes.Add(new SLane(split, vec));
                    lanes.Add(new SLane(split, -vec));
                }
                //如果在边界内，生成双向车道
                else if (bound.Contains(line.MidPoint))
                {
                    var vec = Vector(line).GetPerpendicularVector().Normalize();
                    lanes.Add(new SLane(line.New(), vec));
                    lanes.Add(new SLane(line.New(), -vec));
                }
                else { }
            }
            return lanes;
        }

        public static void SortLaneByDirection(List<SLane> lanes, int mode, Vector2D parentDir)
        {
            var comparer = new SLaneComparer(mode, DisCarAndHalfLaneBackBack, parentDir);
            lanes.Sort(comparer);
        }
        class SLaneComparer : IComparer<SLane>
        {
            public SLaneComparer(int mode, double filterLength, Vector2D parentDir)
            {
                Mode = mode;
                FilterLength = filterLength;
                ParentDir = parentDir;
            }
            private int Mode;
            private double FilterLength;
            private Vector2D ParentDir;
            public int Compare(SLane a, SLane b)
            {
                if (Mode == ((int)LayoutDirection.LENGTH))
                {
                    return CompareLength(a.Line, b.Line);
                }
                else if (Mode == ((int)LayoutDirection.FOLLOWPREVIOUS))
                {
                    if (ParentDir == Vector2D.Zero)
                        return CompareLength(a.Line, b.Line);
                    else
                    {
                        var vec_a = Vector(a.Line);
                        var vec_b = Vector(b.Line);
                        if (IsPerpVector(vec_a, ParentDir) && !IsPerpVector(vec_b, ParentDir))
                            return -1;
                        else if (!IsPerpVector(vec_a, ParentDir) && IsPerpVector(vec_b, ParentDir))
                            return 1;
                        else
                            return CompareLength(a.Line, b.Line);
                    }
                }
                else return 0;
            }
            int CompareLength(LineSegment a, LineSegment b)
            {
                if (a.Length > b.Length) return -1;
                else if (a.Length < b.Length) return 1;
                return 0;
            }
        }


    }
}
