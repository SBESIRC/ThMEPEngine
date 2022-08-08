using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.MPartitionLayout
{
    public partial class MParkingPartitionPro
    {
        private void GenerateEstimateLaneBox()
        {
            IniLanes = IniLanes.OrderBy(e => e.Line.Length).ToList();
            for (int i = 0; i < IniLanes.Count; i++)
            {
                var lane = IniLanes[i];
                var segs = new List<LineSegment>();
                DivideCurveByLength(lane.Line, DisCarAndHalfLaneBackBack, ref segs);
                foreach (var seg in segs)
                {
                    if (Math.Abs(seg.Length - DisCarAndHalfLaneBackBack) > 1) continue;
                    var edge = seg.Translation(lane.Vec.Normalize() * MaxLength);
                    var rec=PolyFromLines(seg,edge);
                    rec = rec.Scale(ScareFactorForCollisionCheck);
                    var points = new List<Coordinate>();
                    var obs_crossed=ObstaclesSpatialIndex.SelectCrossingGeometry(rec).ToList();
                    foreach (var obs in obs_crossed)
                    {
                        points.AddRange(obs.Coordinates);
                        points.AddRange(obs.IntersectPoint(rec));
                    }
                    points.AddRange(Boundary.IntersectPoint(rec));
                    foreach (var box in EstimateLaneBoxes.Select(e => e.Box.Scale(ScareFactorForCollisionCheck)))
                    {
                        points.AddRange(box.IntersectPoint(rec));
                    }
                    rec = PolyFromLines(seg, edge);
                    points = points.Where(p => rec.Contains(p))
                        .Where(p => seg.ClosestPoint(p).Distance(p) > 1)
                        .OrderBy(p => seg.ClosestPoint(p).Distance(p)).ToList();
                    if (points.Any())
                    {
                        var p = points[0];
                        var length = seg.ClosestPoint(p).Distance(p);
                        rec = PolyFromLines(seg, seg.Translation(lane.Vec.Normalize() * length));
                        EstimateLaneBox estimate = new EstimateLaneBox();
                        estimate.Box = rec;
                        estimate.EffectiveLength = length;
                        EstimateLaneBoxes.Add(estimate);
                    }
                }
            }
            EstimateLaneBoxes= EstimateLaneBoxes.OrderByDescending(e => e.Box.Area).ToList();
            if (EstimateLaneBoxes.Count > 1)
            {
                for (int i = 1; i < EstimateLaneBoxes.Count; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (EstimateLaneBoxes[j].Box.Contains(EstimateLaneBoxes[i].Box.Centroid.Coordinate))
                        {
                            EstimateLaneBoxes.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
        }
        public class EstimateLaneBox
        {
            public EstimateLaneBox()
            {

            }
            public EstimateLaneBox(Polygon box)
            {
                Box = box;
            }
            public Polygon Box;
            public double EffectiveLength = 0;
        }
    }
}
