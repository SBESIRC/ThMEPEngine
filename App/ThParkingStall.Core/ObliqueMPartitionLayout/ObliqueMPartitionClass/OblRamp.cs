using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.InterProcess;
using ThParkingStall.Core.MPartitionLayout;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.ObliqueMPartitionLayout
{
    public class OblRamp
    {
        public OblRamp(Polygon polygon, List<Coordinate> coordinates, List<Vector2D> vecs)
        {
            if (coordinates.Count == vecs.Count)
            {
                Region = polygon;
                Points = coordinates;
                Vecs = vecs;
            }
        }
        public Polygon Region { get; set; }
        public List<Coordinate> Points { get; set; }
        public List<Vector2D> Vecs { get; set; }
    }

    public partial class ObliqueMPartition
    {
        void PreProcessRamp()
        {
            //var points = new List<Coordinate>() { new Coordinate(472925.0891, 64366.2936), 
            //    new Coordinate(480625.0891,64366.2936), new Coordinate(480625.0891,40816.2936), 
            //    new Coordinate(472925.0891,40816.2936) };
            //var p_a = new Coordinate(477548.7131, 64366.2936);
            //var p_b = new Coordinate(472925.0891, 60055.1535);
            //var pl = PolyFromPoints(points);
            //if (Boundary.Contains(pl.Centroid.Coordinate))
            //{
            //    OblRamp oblRamp = new OblRamp(pl, new List<Coordinate>() { p_b }, new List<Vector2D>() { new Vector2D(-1, 0) });
            //    RampList.Add(oblRamp);
            //}
            foreach (var ramp in RampList)
            {
                for (int i = 0; i < ramp.Points.Count; i++)
                {
                    var point = ramp.Points[i];
                    var vec = ramp.Vecs[i];
                    var testLineFromPoint = LineSegmentSDL(point, vec, MaxLength);
                    var testPointIntersectedBounds = Boundary.IntersectPoint(testLineFromPoint.ToLineString());
                    if (testPointIntersectedBounds.Count() == 0) continue;
                    //从坡道发出的点伸出最大长度线段与边界相交点，如果边界是凹的有可能有多个
                    var testPointIntersectedBound = testPointIntersectedBounds.OrderBy(p => p.Distance(point)).First();
                    var near_lanes = IniLanes.Where(e => e.Line.ClosestPoint(testPointIntersectedBound).Distance(testPointIntersectedBound) < 1);
                    var case_turned = true;//坡道伸出的线段未直连车道线，转折一次寻找
                    var turn_point = testPointIntersectedBound;//需要转折一次的转折点
                    //坡道伸出的线段直连车道线
                    if (near_lanes.Any())
                    {
                        var ramp_line = new LineSegment(point, testPointIntersectedBound);
                        var ramp_line_buffer = BufferReservedConnection(ramp_line, DisLaneWidth / 2);
                        ramp_line_buffer = ramp_line_buffer.Scale(ScareFactorForCollisionCheck);
                        var crossed_obs = ObstaclesSpatialIndex.SelectCrossingGeometry(ramp_line_buffer).Cast<Polygon>().ToList();
                        if (crossed_obs.Any())
                        {
                            var crossed_obs_points = new List<Coordinate>();
                            foreach (var obs in crossed_obs)
                            {
                                crossed_obs_points.AddRange(obs.Coordinates);
                                crossed_obs_points.AddRange(obs.IntersectPoint(ramp_line_buffer));
                            }
                            crossed_obs_points= crossed_obs_points.Where(p => ramp_line_buffer.Contains(p)).Select(p => ramp_line.ClosestPoint(p))
                                .OrderBy(p => p.Distance(point)).ToList();
                            turn_point= crossed_obs_points.First();
                        }
                        else
                        {
                            Lane ramp_lane_a = new Lane(ramp_line, Vector(ramp_line).Normalize().GetPerpendicularVector());
                            Lane ramp_lane_b = new Lane(ramp_line, -Vector(ramp_line).Normalize().GetPerpendicularVector());
                            ramp_lane_a.NOTJUDGELAYOUTBYPARENT = true;
                            ramp_lane_b.NOTJUDGELAYOUTBYPARENT = true;
                            IniLanes.Add(ramp_lane_a);
                            IniLanes.Add(ramp_lane_b);
                            CarBoxesSpatialIndex.Update(new List<Polygon>() { PolyFromLine(ramp_line) }, new List<Polygon>());
                            case_turned = false;
                        }
                    }
                    if (case_turned)
                    {
                        #region 计算拐点
                        var ramp_line = new LineSegment(point, testPointIntersectedBound);
                        var ramp_line_buffer = BufferReservedConnection(ramp_line, DisLaneWidth / 2);
                        ramp_line_buffer = ramp_line_buffer.Scale(ScareFactorForCollisionCheck);
                        var crossed_obs = ObstaclesSpatialIndex.SelectCrossingGeometry(ramp_line_buffer).Cast<Polygon>().ToList();
                        if (crossed_obs.Any())
                        {
                            var crossed_obs_points = new List<Coordinate>();
                            foreach (var obs in crossed_obs)
                            {
                                crossed_obs_points.AddRange(obs.Coordinates);
                                crossed_obs_points.AddRange(obs.IntersectPoint(ramp_line_buffer));
                            }
                            crossed_obs_points = crossed_obs_points.Where(p => ramp_line_buffer.Contains(p)).Select(p => ramp_line.ClosestPoint(p))
                                .OrderBy(p => p.Distance(point)).ToList();
                            turn_point = crossed_obs_points.First();
                        }
                        #endregion

                        turn_point = turn_point.Translation(-new Vector2D(point, turn_point).Normalize() * 8050);
                        var first_line = new LineSegment(point, turn_point);

                        var gline_a = new LineSegment();
                        var gline_b = new LineSegment();

                        var test_line_a = LineSegmentSDL(turn_point, Vector(first_line).GetPerpendicularVector(), MaxLength);
                        var testPointIntersectedBounds_a = Boundary.IntersectPoint(test_line_a.ToLineString());
                        if (testPointIntersectedBounds_a.Count() > 0)
                        {
                            //从坡道发出的点伸出最大长度线段与边界相交点，如果边界是凹的有可能有多个
                            var testPointIntersectedBound_a = testPointIntersectedBounds_a.OrderBy(p => p.Distance(turn_point)).First();
                            var near_lanes_a = IniLanes.Where(e => e.Line.ClosestPoint(testPointIntersectedBound_a).Distance(testPointIntersectedBound_a) < 1);
                            if (near_lanes_a.Any())
                            {
                                var ramp_line_a = new LineSegment(turn_point, testPointIntersectedBound_a);
                                var ramp_line_buffer_a = BufferReservedConnection(ramp_line_a, DisLaneWidth / 2);
                                ramp_line_buffer_a = ramp_line_buffer_a.Scale(ScareFactorForCollisionCheck);
                                var crossed_obs_a = ObstaclesSpatialIndex.SelectCrossingGeometry(ramp_line_buffer_a).Cast<Polygon>().ToList();
                                if (crossed_obs.Count() == 0)
                                {
                                    gline_a = ramp_line_a;
                                }
                            }
                        }

                        var test_line_b = LineSegmentSDL(turn_point, -Vector(first_line).GetPerpendicularVector(), MaxLength);
                        var testPointIntersectedBounds_b = Boundary.IntersectPoint(test_line_b.ToLineString());
                        if (testPointIntersectedBounds_b.Count() > 0)
                        {
                            //从坡道发出的点伸出最大长度线段与边界相交点，如果边界是凹的有可能有多个
                            var testPointIntersectedBound_b = testPointIntersectedBounds_b.OrderBy(p => p.Distance(turn_point)).First();
                            var near_lanes_b = IniLanes.Where(e => e.Line.ClosestPoint(testPointIntersectedBound_b).Distance(testPointIntersectedBound_b) < 1);
                            if (near_lanes_b.Any())
                            {
                                var ramp_line_b = new LineSegment(turn_point, testPointIntersectedBound_b);
                                var ramp_line_buffer_b = BufferReservedConnection(ramp_line_b, DisLaneWidth / 2);
                                ramp_line_buffer_b = ramp_line_buffer_b.Scale(ScareFactorForCollisionCheck);
                                var crossed_obs_b = ObstaclesSpatialIndex.SelectCrossingGeometry(ramp_line_buffer_b).Cast<Polygon>().ToList();
                                if (crossed_obs.Count() == 0)
                                {
                                    gline_b = ramp_line_b;
                                }
                            }
                        }

                        if (Math.Max(gline_a.Length, gline_b.Length) > 0)
                        {
                            var gline = gline_a.Length < gline_b.Length ? gline_a : gline_b;
                            ramp_line = new LineSegment(point, turn_point);
                            Lane ramp_lane_a = new Lane(ramp_line, Vector(ramp_line).Normalize().GetPerpendicularVector());
                            Lane ramp_lane_b = new Lane(ramp_line, -Vector(ramp_line).Normalize().GetPerpendicularVector());
                            ramp_lane_a.NOTJUDGELAYOUTBYPARENT = true;
                            ramp_lane_b.NOTJUDGELAYOUTBYPARENT = true;
                            IniLanes.Add(ramp_lane_a);
                            IniLanes.Add(ramp_lane_b);

                            Lane glane_a = new Lane(gline, Vector(gline).Normalize().GetPerpendicularVector());
                            Lane glane_b = new Lane(gline, -Vector(gline).Normalize().GetPerpendicularVector());
                            glane_a.NOTJUDGELAYOUTBYPARENT = true;
                            glane_b.NOTJUDGELAYOUTBYPARENT = true;
                            IniLanes.Add(glane_a);
                            IniLanes.Add(glane_b);
                            CarBoxesSpatialIndex.Update(new List<Polygon>() { PolyFromLine(ramp_line), PolyFromLine(gline) }, new List<Polygon>());

                        }

                    }

                }
            }
        }
    }
}
